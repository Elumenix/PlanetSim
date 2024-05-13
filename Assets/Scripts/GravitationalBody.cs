using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class GravitationalBody : MonoBehaviour
{
    public Vector3d Position;
    public Vector3d Velocity;
    public double Mass;

    [HideInInspector] public Vector3d acceleration;
    
    
    // Start is called before the first frame update
    void Start()
    {
        acceleration = Vector3d.zero;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.Translate(DirVelocity, Space.World);
        //velocityView = rb.velocity;
        Velocity += acceleration;
        Position += Velocity;
        transform.position = new Vector3((float)Position.x, (float)Position.y, (float)Position.z);
        acceleration = Vector3d.zero;
    }
}
#endif
