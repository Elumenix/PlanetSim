using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TurnLight : MonoBehaviour
{
    public GravitationalBody chosenPlanet; 

    // Update is called once per frame
    void Update()
    {
        // Make directional Light face planet
        Vector3 direction = chosenPlanet.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(direction, transform.up);
    }
}
