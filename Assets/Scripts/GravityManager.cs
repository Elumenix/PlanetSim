using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public enum TimeScale
{
    second, minute, hour, day, week, month, year
}

public class GravityManager : MonoBehaviour
{
    public List<GravitationalBody> planets;
    private double G;

    [SerializeField]
    private TimeScale _timeScale;

    public TimeScale TimeScale
    {
        get { return _timeScale; }
    }

    private TimeScale previousTimeScale;
    public float StartingSpeed = 1;
    public bool reversed;
    private OrbitPositions orbitPositions;
    private int Timer = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        orbitPositions = new OrbitPositions();
        orbitPositions.positions = new List<Vector3>();

        previousTimeScale = TimeScale;
        G = GetGravitationalConstant(TimeScale);
        Time.timeScale = StartingSpeed;

        if (_timeScale != TimeScale.second)
        {
            foreach (GravitationalBody planet in planets)
            {
                // Initial values that may need to be changed to match the simulation parameters
                planet.Velocity = ConvertVelocityToTimeScale(planet.Velocity, TimeScale.second, TimeScale);
                Vector3d temp = new Vector3d(0, 0, planet.RotationSpeed);
                temp = ConvertVelocityToTimeScale(temp, TimeScale.second, TimeScale);
                planet.RotationSpeed = temp.z;
            }
        }
    }

    void FixedUpdate()
    {
        // No point in running calculations that won't do anything
        if (Time.timeScale == 0)
        {
            return;
        }
        
        // Gravitational constant has a time parameter, so it will need to be updated
        if (previousTimeScale != TimeScale)
        {
            G = GetGravitationalConstant(_timeScale);
            
            foreach (GravitationalBody planet in planets)
            {
                planet.Velocity = ConvertVelocityToTimeScale(planet.Velocity, previousTimeScale, TimeScale);
                Vector3d temp = new Vector3d(0, 0, planet.RotationSpeed);
                temp = ConvertVelocityToTimeScale(temp, previousTimeScale, TimeScale);
                planet.RotationSpeed = temp.z;
            }
            
            previousTimeScale = TimeScale;
        }
        
        // The integration method I'm using to simulate gravity is the fourth-order Runge-Kutta
        // Integration is required for these calculations, especially on larger timescales, because acceleration
        // is not consistent over each time-step, so the approximation of total acceleration over a frame and how
        // it affects positioning is necessary for an accurate simulation of this sort of complexity

        // For reversing time, I just need to invert DeltaTime every frame, so this will be multiplied by DeltaTime
        // This works because RK4 is symmetrical, so the orbits are properly followed even if time is reversed
        int timeDir = !reversed ? 1 : -1;
        
        
        // Calculate initial accelerations (k1)
        CalculateAccelerations();
        List<Vector3d> initialPositions = new List<Vector3d>();
        List<Vector3d> initialVelocities = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            initialPositions.Add(new Vector3d(body.Position.x, body.Position.y, body.Position.z));
            initialVelocities.Add(new Vector3d(body.Velocity.x, body.Velocity.y, body.Velocity.z));
            
            // Doesn't accelerate, therefore doesn't need integration method
            body.transform.Rotate(transform.up, (float) body.RotationSpeed * timeDir * Time.fixedDeltaTime);
        }
        
        // Using Accelerations from last frame (k1)
        List<Vector3d> k1_velocities = new List<Vector3d>();
        List<Vector3d> k1_accelerations = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            k1_velocities.Add(body.Velocity);
            k1_accelerations.Add(body.Acceleration);
        }

        // Update variables and calculate mid-point accelerations (k2)
        for (int i = 0; i < planets.Count; i++)
        {
            GravitationalBody body = planets[i];
            body.Position = initialPositions[i] + 0.5 * timeDir * Time.fixedDeltaTime * k1_velocities[i];
            body.Velocity = initialVelocities[i] + 0.5 * timeDir * Time.fixedDeltaTime * k1_accelerations[i];
        }
        
        CalculateAccelerations();
        List<Vector3d> k2_velocities = new List<Vector3d>();
        List<Vector3d> k2_accelerations = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            k2_velocities.Add(body.Velocity);
            k2_accelerations.Add(body.Acceleration);
        }

        // Update variables and calculate second mid-point accelerations (k3)
        for (int i = 0; i < planets.Count; i++)
        {
            GravitationalBody body = planets[i];
            body.Position = initialPositions[i] + 0.5 * timeDir * Time.fixedDeltaTime * k2_velocities[i];
            body.Velocity = initialVelocities[i] + 0.5 * timeDir * Time.fixedDeltaTime * k2_accelerations[i];
        }
        
        CalculateAccelerations();
        List<Vector3d> k3_velocities = new List<Vector3d>();
        List<Vector3d> k3_accelerations = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            k3_velocities.Add(body.Velocity);
            k3_accelerations.Add(body.Acceleration);
        }

        // Update variables and calculate end-point accelerations (k4)
        for (int i = 0; i < planets.Count; i++)
        {
            GravitationalBody body = planets[i];
            body.Position = initialPositions[i] + timeDir * Time.fixedDeltaTime * k3_velocities[i];
            body.Velocity = initialVelocities[i] + timeDir * Time.fixedDeltaTime * k3_accelerations[i];
        }
        
        CalculateAccelerations();
        List<Vector3d> k4_velocities = new List<Vector3d>();
        List<Vector3d> k4_accelerations = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            k4_velocities.Add(body.Velocity);
            k4_accelerations.Add(body.Acceleration);
        }

        // Update positions and velocities using RK4 method
        for (int i = 0; i < planets.Count; i++)
        {
            GravitationalBody body = planets[i];
            body.Position = initialPositions[i] + (1.0 / 6.0) * timeDir * Time.fixedDeltaTime *
                (k1_velocities[i] + 2 * k2_velocities[i] + 2 * k3_velocities[i] + k4_velocities[i]);
            body.Velocity = initialVelocities[i] + (1.0 / 6.0) * timeDir * Time.fixedDeltaTime *
                (k1_accelerations[i] + 2 * k2_accelerations[i] + 2 * k3_accelerations[i] + k4_accelerations[i]);
        }

        // Record every xth fixed Update depending on what timescale I need to use for distance
        if (Timer == 0)
        {
            orbitPositions.positions.Add(planets[4].transform.position - planets[3].transform.position);
        }

        Timer++;

        if (Timer == 5)
        {
            Timer = 0;
        }
    }

    void CalculateAccelerations()
    {
        // Reset accelerations
        foreach (GravitationalBody body in planets)
        {
            body.Acceleration = Vector3d.zero;
        }

        // Calculate new accelerations
        for (int i = 0; i < planets.Count; i++)
        {
            // Compare to other planets
            for (int j = i + 1; j < planets.Count; j++)
            {
                GravitationalBody body1 = planets[i];
                GravitationalBody body2 = planets[j];

                // I'm technically using AU for distance but that's scaled up by 1500 so everything isn't crammed together
                // I need to scale it back down during calculations, so that gravitational constant works
                Vector3d direction = body2.Position - body1.Position;
                double distance = direction.magnitude / 1500.0;

                if (distance != 0) // Distance realistically shouldn't ever be 0 in this specific simulation
                {
                    // This is just the gravitational formula
                    double forceMagnitude = G * (body1.Mass * body2.Mass) / Math.Pow(distance, 2);
                    Vector3d force = direction.normalized * forceMagnitude;
                    
                    // Apply forces: F = MA
                    body1.Acceleration += (force / body1.Mass);
                    body2.Acceleration += (-force / body2.Mass); // Apply force in opposite direction
                }
            }
        }
    }
    
    double GetGravitationalConstant(TimeScale scale)
    {
        // Calculations for G need to be really precise since every time-step and
        // calculation uses it multiple times every frame for acceleration 

        double baseG = 6.674484e-11; // in (m^3)/((kg)(s^2))
        double au = 149597870.7e03; // in m
        double earthMass = 5.9722e24; // in kg

        // Convert G to (AU^3)/((Earth Mass)(TimeScale^2)) : Time scale part is done in the switch statement below
        G = baseG * (earthMass / Math.Pow(au, 3)) * 1500; // 1500 is to preserve the rule that 1 au equals 1500 units in unity

        switch (scale)
        {
            case TimeScale.second:
                // No conversion needed
                return G;
            case TimeScale.minute:
                // Convert to per minute squared
                return G * 60 * 60;
            case TimeScale.hour:
                // Convert to per hour squared
                return G * 3600 * 3600;
            case TimeScale.day:
                // Convert to per day squared
                return G * 86400 * 86400;
            case TimeScale.week:
                // Convert to per week squared
                return G * (7 * 86400) * (7 * 86400);
            case TimeScale.month:
                // Convert to per month squared
                return G * (30.437 * 86400) * (30.437 * 86400);
            case TimeScale.year:
                // Convert to per year squared
                return G * (365.25 * 86400) * (365.25 * 86400);
            default:
                // Default to base G if scale is not recognized
                return G;
        }
    }
    
    Vector3d ConvertVelocityToTimeScale(Vector3d velocity, TimeScale oldScale, TimeScale newScale)
    {
        // Year needs a much more precise internal time-step or the moon and mercury will fly off
        Time.fixedDeltaTime = newScale == TimeScale.year ? 
            0.001f :         // Will definitely cause lag if scaled up, but that's largely unavoidable
            0.016f;             // Regular time-step. A bit over 60 physics updates per second
        
        // Convert velocity from old timescale to base units (AU per second)
        switch (oldScale)
        {
            case TimeScale.minute:
                velocity /= 60;
                break;
            case TimeScale.hour:
                velocity /= 3600;
                break;
            case TimeScale.day:
                velocity /= (86400);
                break;
            case TimeScale.week:
                velocity /= (7 * 86400);
                break;
            case TimeScale.month:
                velocity /= (30.437 * 86400);
                break;
            case TimeScale.year:
                velocity /= (365.25 * 86400);
                break;
        }

        // Convert velocity from base units to new timescale
        switch (newScale)
        {
            case TimeScale.minute:
                velocity *= 60;
                break;
            case TimeScale.hour:
                velocity *= 3600;
                break;
            case TimeScale.day:
                velocity *= (86400);
                break;
            case TimeScale.week:
                velocity *= (7 * 86400);
                break;
            case TimeScale.month:
                velocity *= (30.437 * 86400);
                break;
            case TimeScale.year:
                velocity *= (365.25 * 86400);
                break;
        }

        return velocity;
    }

    public void IncreaseTimescale()
    {
        if (Time.timeScale != 0)
        {
            Time.timeScale = 1;
        }

        switch (_timeScale)
        {
            case TimeScale.second:
                _timeScale = TimeScale.minute;
                break;
            
            case TimeScale.minute:
                _timeScale = TimeScale.hour;
                break;
            
            case TimeScale.hour:
                _timeScale = TimeScale.day;
                break;
            
            case TimeScale.day:
                _timeScale = TimeScale.week;
                break;
            
            case TimeScale.week:
                _timeScale = TimeScale.month;
                break;
            
            case TimeScale.month:
                _timeScale = TimeScale.year;
                break;
            
            // Don't Change
            case TimeScale.year:
                break;
        }
    }
    
    public void DecreaseTimescale()
    {
        if (Time.timeScale != 0)
        {
            Time.timeScale = 1;
        }

        switch (_timeScale)
        {
            // Don't Change
            case TimeScale.second:
                break;
            
            case TimeScale.minute:
                _timeScale = TimeScale.second;
                break;
            
            case TimeScale.hour:
                _timeScale = TimeScale.minute;
                break;
            
            case TimeScale.day:
                _timeScale = TimeScale.hour;
                break;
            
            case TimeScale.week:
                _timeScale = TimeScale.day;
                break;
            
            case TimeScale.month:
                _timeScale = TimeScale.week;
                break;
            
            case TimeScale.year:
                _timeScale = TimeScale.month;
                break;
        }
    }

    private void OnApplicationQuit()
    {
        //string json = JsonUtility.ToJson(orbitPositions);
        //System.IO.File.WriteAllText("Assets/Line2DPaths/MoonOrbit.json", json);
    }
}
