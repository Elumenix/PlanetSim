using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitationalBody : MonoBehaviour
{
    public Rigidbody rb;
    public Vector3 initialVelocity;

    [SerializeField]
    private Vector3 velocityView;
    
    public Vector3 Velocity
    {
        get { return velocityView; }
        //set { velocity += value; }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        rb.velocity = initialVelocity;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.Translate(DirVelocity, Space.World);
        velocityView = rb.velocity;
    }
}
