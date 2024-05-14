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
    }

    // Update is called once per frame
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
        
        // Updates acceleration of all planets due to gravity : Euler part of Verlet Implementation
        // Positions and Velocities get updated in the GravitationalBody Update Method
        for (int i = 0; i < planets.Count; i++)
        {
            for (int j = i + 1; j < planets.Count; j++)
            {
                GravitationalBody body1 = planets[i];
                GravitationalBody body2 = planets[j];

                // I'm technically using AU for distance but that's scaled up by 1500 so everything isn't crammed together
                // I need to scale it back down during calculations, so that gravitational constant works
                Vector3d direction = body2.Position / 1500.0 - body1.Position / 1500.0;
                double distance = direction.magnitude;

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
        G = baseG * ((1.496e+11 * 1.496e+11) / 5.972e+24);

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
}
