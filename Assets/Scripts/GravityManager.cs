using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GravityManager : MonoBehaviour
{
    public List<GravitationalBody> planets;
    public float secondsPerRealSecond = 1;
    private const float G = 6.67408e-11f; // Updated gravitational constant for scaling
    
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

                Vector3 direction = body2.transform.position - body1.transform.position;
                float distance = direction.magnitude;
                float forceMagnitude = G * (body1.rb.mass * body2.rb.mass) / Mathf.Pow(distance, 2);

                Vector3 force = direction.normalized * forceMagnitude;

                body1.rb.AddForce(force * (Time.deltaTime * secondsPerRealSecond));
                body2.rb.AddForce(-force * (Time.deltaTime * secondsPerRealSecond)); // Apply force in opposite direction
            }
        }
    }
}
