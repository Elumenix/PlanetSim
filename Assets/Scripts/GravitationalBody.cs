using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GravitationalBody : MonoBehaviour
{
    public Vector3d Position;
    public Vector3d Velocity;
    public double RotationSpeed;
    public double Mass;
    public Vector3d Acceleration;
    
    
    void Start()
    {
        Acceleration = Vector3d.zero;
    }

    // Doesn't need to be fixed Update because transform doesn't need to be updated every calculation, only when drawn
    void Update()
    {
        // Mass check just confirms this isn't being used as a placeholder in CameraController
        if (Mass != 0)
        {
            transform.position = new Vector3((float) Position.x, (float) Position.y, (float) Position.z);
        }
    }

    private void OnDrawGizmos()
    {
        /*Gizmos.color = Color.white;
        Vector3d temp = /*new Vector3d(transform.up.x, transform.up.y, transform.up.z)*/ /*Position + Velocity * 10000000;
        Vector3d temp2 = Position + acceleration * 10000000;
        Gizmos.DrawLine(new Vector3((float)Position.x, (float)Position.y, (float)Position.z), new Vector3((float)temp.x, (float)temp.y, (float)temp.z));
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3((float)Position.x, (float)Position.y, (float)Position.z), new Vector3((float)temp2.x, (float)temp2.y, (float)temp2.z));*/
    }
}
