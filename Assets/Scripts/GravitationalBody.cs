using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GravitationalBody : MonoBehaviour
{
    public Vector3d Position;
    public Vector3d Velocity;
    public double Mass;

    [HideInInspector] public Vector3d acceleration;
    [HideInInspector] public Vector3d previousPosition;
    
    
    // Start is called before the first frame update
    void Start()
    {
        acceleration = Vector3d.zero;
        previousPosition = Position - Velocity * Time.deltaTime;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Application.isPlaying)
        {
            // Verlet Integration is used for more stable/accurate orbits of bodies
            Vector3d tempPosition = Position;
            Position = 2 * Position - previousPosition + acceleration * Time.deltaTime * Time.deltaTime;
            Velocity = previousPosition - Position;
            previousPosition = tempPosition;
        }

        // still runs in editor mode
        transform.position = new Vector3((float)Position.x, (float)Position.y, (float)Position.z);


        acceleration = Vector3d.zero;
    }
}
