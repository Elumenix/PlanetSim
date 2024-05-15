using System;
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
            Velocity =  Position - previousPosition;
            previousPosition = tempPosition;
        }

        // still runs in editor mode
        transform.position = new Vector3((float)Position.x, (float)Position.y, (float)Position.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3d temp = Position + Velocity * 10000000;
        Vector3d temp2 = Position + acceleration * 10000000;
        Gizmos.DrawLine(new Vector3((float)Position.x, (float)Position.y, (float)Position.z), new Vector3((float)temp.x, (float)temp.y, (float)temp.z));
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3((float)Position.x, (float)Position.y, (float)Position.z), new Vector3((float)temp2.x, (float)temp2.y, (float)temp2.z));
    }
}
