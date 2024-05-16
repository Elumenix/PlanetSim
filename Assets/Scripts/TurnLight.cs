using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnLight : MonoBehaviour
{
    public GravitationalBody chosenPlanet; 
        
        
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Make directional Light face planet
        Vector3 direction = chosenPlanet.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(direction, transform.up);
    }
}
