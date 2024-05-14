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
    
    
    // Start is called before the first frame update
    void Start()
    {
        acceleration = Vector3d.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying)
        {
            Velocity += acceleration;
            Position += Velocity * Time.deltaTime;
        }

        // still runs in editor mode
        transform.position = new Vector3((float)Position.x, (float)Position.y, (float)Position.z);


        acceleration = Vector3d.zero;
    }
}
