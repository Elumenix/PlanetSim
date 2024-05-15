using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum TimeScale
{
    second, minute, hour, day, year
}

public class GravityManager : MonoBehaviour
{
    public List<GravitationalBody> planets;
    private double G;

    [SerializeField]
    private TimeScale _timeScale;

    private TimeScale TimeScale
    {
        get { return _timeScale; }
    }

    private TimeScale previousTimeScale;
    public float speed = 1;
    
    // Start is called before the first frame update
    void Start()
    {
        previousTimeScale = TimeScale;
        G = GetGravitationalConstant(TimeScale);

        if (_timeScale != TimeScale.second)
        {
            foreach (GravitationalBody planet in planets)
            {
                planet.Velocity = ConvertVelocityToTimeScale(planet.Velocity, TimeScale.second, TimeScale);
            }
        }
    }

    void FixedUpdate()
    {
        // Gravitational constant has a time parameter, so it will need to be updated
        if (previousTimeScale != TimeScale)
        {
            G = GetGravitationalConstant(_timeScale);
            previousTimeScale = TimeScale;
        }

        // Should let me take numbers of a timescale that isn't one
        Time.timeScale = speed;
        
        
        // Save initial states
        List<Vector3d> initialPositions = new List<Vector3d>();
        List<Vector3d> initialVelocities = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            initialPositions.Add(new Vector3d(body.Position.x, body.Position.y, body.Position.z));
            initialVelocities.Add(new Vector3d(body.Velocity.x, body.Velocity.y, body.Velocity.z));
        }
        
        // Calculate initial accelerations (k1)
        CalculateAccelerations();
        List<Vector3d> k1_velocities = new List<Vector3d>();
        List<Vector3d> k1_positions = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            k1_velocities.Add(body.Velocity);
            k1_positions.Add(body.acceleration);
        }

        // Calculate mid-point accelerations (k2)
        for (int i = 0; i < planets.Count; i++)
        {
            GravitationalBody body = planets[i];
            body.Position = initialPositions[i] + 0.5 * Time.deltaTime * k1_velocities[i];
            body.Velocity = initialVelocities[i] + 0.5 * Time.deltaTime * k1_positions[i];
        }
        CalculateAccelerations();
        List<Vector3d> k2_velocities = new List<Vector3d>();
        List<Vector3d> k2_positions = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            k2_velocities.Add(body.Velocity);
            k2_positions.Add(body.acceleration);
        }

        // Calculate second mid-point accelerations (k3)
        for (int i = 0; i < planets.Count; i++)
        {
            GravitationalBody body = planets[i];
            body.Position = initialPositions[i] + 0.5 * Time.deltaTime * k2_velocities[i];
            body.Velocity = initialVelocities[i] + 0.5 * Time.deltaTime * k2_positions[i];
        }
        CalculateAccelerations();
        List<Vector3d> k3_velocities = new List<Vector3d>();
        List<Vector3d> k3_positions = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            k3_velocities.Add(body.Velocity);
            k3_positions.Add(body.acceleration);
        }

        // Calculate end-point accelerations (k4)
        for (int i = 0; i < planets.Count; i++)
        {
            GravitationalBody body = planets[i];
            body.Position = initialPositions[i] + Time.deltaTime * k3_velocities[i];
            body.Velocity = initialVelocities[i] + Time.deltaTime * k3_positions[i];
        }
        CalculateAccelerations();
        List<Vector3d> k4_velocities = new List<Vector3d>();
        List<Vector3d> k4_positions = new List<Vector3d>();
        foreach (GravitationalBody body in planets)
        {
            k4_velocities.Add(body.Velocity);
            k4_positions.Add(body.acceleration);
        }

        // Update positions and velocities using RK4 method
        for (int i = 0; i < planets.Count; i++)
        {
            GravitationalBody body = planets[i];
            body.Position = initialPositions[i] + (1.0 / 6.0) * Time.deltaTime * (k1_velocities[i] + 2 * k2_velocities[i] + 2 * k3_velocities[i] + k4_velocities[i]);
            body.Velocity = initialVelocities[i] + (1.0 / 6.0) * Time.deltaTime * (k1_positions[i] + 2 * k2_positions[i] + 2 * k3_positions[i] + k4_positions[i]);
        }
    }

    void CalculateAccelerations()
    {
        // Reset accelerations
        foreach (GravitationalBody body in planets)
        {
            body.acceleration = Vector3d.zero;
        }

        // Calculate new accelerations
        for (int i = 0; i < planets.Count; i++)
        {
            for (int j = i + 1; j < planets.Count; j++)
            {
                GravitationalBody body1 = planets[i];
                GravitationalBody body2 = planets[j];

                // I'm technically using AU for distance but that's scaled up by 1500 so everything isn't crammed together
                // I need to scale it back down during calculations, so that gravitational constant works
                Vector3d direction = body2.Position - body1.Position;
                double distance = direction.magnitude / 1500.0;

                if (distance != 0)
                {
                    double forceMagnitude = G * (body1.Mass * body2.Mass) / Math.Pow(distance, 2);
                    Vector3d force = direction.normalized * forceMagnitude;
                    
                    body1.acceleration += (force / body1.Mass);
                    body2.acceleration += (-force / body2.Mass); // Apply force in opposite direction
                }
            }
        }
    }
    
    double GetGravitationalConstant(TimeScale scale)
    {
        // The gravitational constant in N*(m^2)/(kg^2)
        double baseG = 6.67430e-11;

        // Convert G to N*(AU^2)/(Earth Mass^2)
        G = baseG * ((1.496e+11 * 1.496e+11) / 5.972e+24) / 1500;

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
            case TimeScale.year:
                velocity *= (365.25 * 86400);
                break;
        }

        return velocity;
    }
}
