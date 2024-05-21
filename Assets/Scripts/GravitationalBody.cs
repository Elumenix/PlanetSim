using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class GravitationalBody : MonoBehaviour
{
    public Vector3d Position;
    public Vector3d Velocity;
    public double RotationSpeed;
    public double Mass;

    [HideInInspector] public Vector3d acceleration;
    [HideInInspector] public Vector3d lastAcceleration;
    
    
    // Start is called before the first frame update
    void Start()
    {
        acceleration = Vector3d.zero;
    }

    // Update is called once per frame
    void Update()
    {
        // still runs in editor mode
        transform.localPosition = new Vector3((float)Position.x, (float)Position.y, (float)Position.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3d temp = /*new Vector3d(transform.up.x, transform.up.y, transform.up.z)*/ Position + Velocity * 10000000;
        Vector3d temp2 = Position + acceleration * 10000000;
        Gizmos.DrawLine(new Vector3((float)Position.x, (float)Position.y, (float)Position.z), new Vector3((float)temp.x, (float)temp.y, (float)temp.z));
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3((float)Position.x, (float)Position.y, (float)Position.z), new Vector3((float)temp2.x, (float)temp2.y, (float)temp2.z));
    }
}
