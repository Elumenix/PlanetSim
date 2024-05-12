using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GravityManager : MonoBehaviour
{
    public List<GravitationalBody> planets;
    public float secondsPerRealSecond = 1;
    private const float G = 6.67408e-17f; // Updated gravitational constant for scaling
    
    // Start is called before the first frame update
    void Start()
    {
        // Scale doesn't seem to affect any calulations, so I'm making them easier to see
        foreach (GravitationalBody body in planets)
        {
            body.transform.localScale *= 100;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // First step is to calculate all forces
        Dictionary<GravitationalBody, Vector3> allForces = new Dictionary<GravitationalBody, Vector3>();

        foreach (GravitationalBody body in planets)
        {
            Vector3 acceleration = Vector3.zero;

            foreach (GravitationalBody body2 in planets)
            {
                // Do not be attracted to yourself
                if (body2 == body)
                {
                    continue;
                }
                
                Vector3 direction = body.transform.position - body2.transform.position;
                float forceGravity = G * ((body.mass * body2.mass) / Mathf.Pow(direction.magnitude, 2));

                // Accel vector due to gravity
                acceleration += direction.normalized * (forceGravity * (secondsPerRealSecond * Time.deltaTime));
            }

            // Already delta-Timed
            allForces[body] = acceleration;
        }

        // Update all velocities simultaneously
        foreach (GravitationalBody body in planets)
        {
            body.DirVelocity += allForces[body];
        }
    }
}
