using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GravityManager : MonoBehaviour
{
    public List<GravitationalBody> planets;
    //public float secondsPerRealSecond = 1;
    private const double G = 6.67408e-11f; // Updated gravitational constant for scaling
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < planets.Count; i++)
        {
            for (int j = i + 1; j < planets.Count; j++)
            {
                GravitationalBody body1 = planets[i];
                GravitationalBody body2 = planets[j];

                Vector3d direction = body2.Position - body1.Position;
                double distance = direction.magnitude;

                if (distance != 0)
                {
                    double forceMagnitude = G * (body1.Mass * body2.Mass) / Math.Pow(distance, 2);
                    Vector3d force = direction.normalized * forceMagnitude;

                    body1.acceleration += (force * Time.deltaTime);
                    body2.acceleration += (-force * Time.deltaTime); // Apply force in opposite direction
                }
            }
        }
    }
}
