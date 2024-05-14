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
    public TimeScale TimeScale
    {
        get { return _timeScale; }
    }

    private TimeScale previousTimeScale;
    
    public void SetTimeScale(TimeScale newScale)
    {
        if (_timeScale != newScale)
        {
            _timeScale = newScale;
            G = GetGravitationalConstant(_timeScale);
            foreach (GravitationalBody body in planets)
            {
                body.Velocity = ConvertVelocityToTimeScale(body.Velocity, _timeScale, newScale);
            }
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        previousTimeScale = TimeScale;
        G = GetGravitationalConstant(TimeScale);

        // All of my default values assume AU/s
        if (TimeScale != TimeScale.second)
        {
            // Convert initial velocities to match time scale
            foreach (GravitationalBody body in planets)
            {
                body.Velocity = ConvertVelocityToTimeScale(body.Velocity, TimeScale.second, TimeScale);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // All velocities and gravitational constant need to be immediately updated
        if (previousTimeScale != TimeScale)
        {
            G = GetGravitationalConstant(_timeScale);
            foreach (GravitationalBody body in planets)
            {
                body.Velocity = ConvertVelocityToTimeScale(body.Velocity, previousTimeScale, TimeScale);
            }

            previousTimeScale = _timeScale;
        }
        
        // Updates acceleration of all planets due to gravity
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
