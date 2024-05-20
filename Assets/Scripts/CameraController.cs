using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // the object to follow
    public GravityManager planetManager;
    public GameObject circlePrefab;
    private List<GameObject> circles;
    
    public float distance = 10.0f; // distance from target
    public float xSpeed = 120.0f; // rotation speed around target
    public float ySpeed = 120.0f;
    public float scrollSpeed = 5;

    private float x = 0.0f;
    private float y = 0.0f;

    // Use this for initialization
    void Start()
    {
        circles = new List<GameObject>();
        
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        
        distance = target.transform.localScale.x * 10f;
        scrollSpeed = distance / 10;
        
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;

        transform.rotation = rotation;
        transform.position = position;


        foreach (GravitationalBody planet in planetManager.planets)
        {
            GameObject circle = Instantiate(circlePrefab);
            circle.GetComponentInChildren<TextMeshPro>().text = planet.name;
            // Todo: maybe change colors here


            if (planet.name == "Moon")
            {
                circle.SetActive(false);
            }
            
            circles.Add(circle);
        }
    }

    // Happens after planets move
    private void LateUpdate()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            distance -= Input.mouseScrollDelta.y * scrollSpeed;

            if (distance < target.transform.localScale.x * 2)
            {
                distance = target.transform.localScale.x * 2;
            }
        } 
        
        if (Input.GetMouseButton(0))
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
        }
        
        // Update position of camera, required every frame since planets are moving
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;

        transform.rotation = rotation;
        transform.position = position;
        
        
        // Now handle UI for far away planets by drawing circles over them
        for (int i = 0; i < planetManager.planets.Count; i++)
        {
            GravitationalBody planet = planetManager.planets[i];

            if (planet.name is "Moon")
            {
                continue;
            }
            
            Vector3 dir = planet.transform.position - position;
            float dist = dir.magnitude;

            if (dist > Mathf.Pow(planet.transform.localScale.x, 2))
            {
                circles[i].SetActive(true);
                // Put circle 20 units from camera in the direction of the planet 
                circles[i].transform.position = position + dir.normalized * 20;
                
                circles[i].transform.LookAt(planet.transform.position * 2 - transform.position);
            }
            else
            {
                circles[i].SetActive(false);
            }
        }
    }
}
