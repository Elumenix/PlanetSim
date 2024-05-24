using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // the object to follow
    public GravityManager planetManager;
    public GameObject circlePrefab;
    private List<ValueTuple<GameObject, float>> circles;
    
    public float distance = 10.0f; // distance from target
    public float xSpeed = 120.0f; // rotation speed around target
    public float ySpeed = 120.0f;
    public float scrollSpeed = 5;

    private float x = 0.0f;
    private float y = 0.0f;

    private int lastIndexHovered;

    private Vector3 minScale;
    private Vector3 maxScale;

    // Use this for initialization
    void Start()
    {
        minScale = new Vector3(0.2f, 0.2f, 0.2f);
        maxScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        circles = new List<ValueTuple<GameObject, float>>();
        
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        
        distance = target.transform.localScale.x * 10f;
        scrollSpeed = distance / 10;
        
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;

        transform.rotation = rotation;
        transform.position = position;

        // Create a UI circle for each planet
        foreach (GravitationalBody planet in planetManager.planets)
        {
            GameObject circle = Instantiate(circlePrefab);
            circle.GetComponentInChildren<TextMeshPro>().text = planet.name;
            // Todo: maybe change colors here
            
            // I don't care about the moon
            if (planet.name == "Moon")
            {
                circle.SetActive(false);
            }
            
            circles.Add(new ValueTuple<GameObject, float>(circle, 0));
        }
    }

    // Happens after planets move
    private void LateUpdate()
    {
        // Save variables that will be used for Scaling calculations
        Vector2 mousePosition = Input.mousePosition;
        Camera camera = Camera.main;
        int indexHovered = -1;
        float currentLeastDistance = float.MaxValue;

        for (int i = 0; i < circles.Count; i++)
        {
            // Following code linearly interpolates scale of all planets based on which was hovered the last update
            #region PreviousScalingChange

            float timeValue;
            // Mouse hover for last frame is applied
            if (lastIndexHovered == i)
            {
                timeValue = Mathf.Clamp(circles[i].Item2 + Time.fixedDeltaTime * 2, 0, 1);
            }
            else
            {
                timeValue = Mathf.Clamp(circles[i].Item2 - Time.fixedDeltaTime * 2, 0, 1);
            }

            if (!Mathf.Approximately(timeValue, circles[i].Item2))
            {
                circles[i] = new ValueTuple<GameObject, float>(circles[i].Item1, timeValue);
            }

            // Shift towards new scaling
            circles[i].Item1.transform.localScale = Vector3.Lerp(minScale, maxScale, circles[i].Item2);

            #endregion

            // Figures out what planet the mouse is hovering this frame
            #region NextScalingHover

            // Mouse hover for next frame is figured out
            if (i == 1)
            {
                Debug.Log(Vector2.Distance(mousePosition,
                    camera!.WorldToScreenPoint(circles[i].Item1.transform.position)));
            }

            // convert ui to screen space to compare to mouse position
            float mouseDistance =
                Vector2.Distance(mousePosition, camera!.WorldToScreenPoint(circles[i].Item1.transform.position));

            if (mouseDistance <= 35f) // Within Range
            {
                if (indexHovered == -1) // If nothing is hovered yet
                {
                    indexHovered = i;
                    currentLeastDistance = mouseDistance;
                }
                else if (mouseDistance < currentLeastDistance) // This is the new closest planet to mouse
                {
                    currentLeastDistance = mouseDistance;
                    indexHovered = i;
                }
            }
            
            #endregion
        }

        // Camera direction and zoom will be modified based on user controls
        #region CameraOrientationUpdate
        
        // Go to different Planet
        if (Input.GetMouseButtonUp(0) && lastIndexHovered != -1)
        {
            target = planetManager.planets[lastIndexHovered].transform;
            
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;
        
            distance = target.transform.localScale.x * 10f;
            scrollSpeed = distance / 10;
        
            Quaternion rot = Quaternion.Euler(y, x, 0);
            Vector3 pos = rot * new Vector3(0.0f, 0.0f, -distance) + target.position;

            transform.rotation = rot;
            transform.position = pos;
        }
        
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

        #endregion
        
        // Updates the Ui circles for each planet to be between the camera and planet at the appropriate size
        #region CirclePositioning&Visibility
        
        // Now handle UI for far away planets by drawing circles over them
        for (int i = 0; i < planetManager.planets.Count; i++)
        {
            GravitationalBody planet = planetManager.planets[i];

            // Don't want to see the moon ui at all
            if (planet.name is "Moon")
            {
                continue;
            }
            
            Vector3 dir = planet.transform.position - position;
            float dist = dir.magnitude;

            if (planet.transform != target && dist > Mathf.Pow(planet.transform.localScale.x, 2))
            {
                circles[i].Item1.SetActive(true);
                
                // Put circle 20 units from camera in the direction of the planet 
                circles[i].Item1.transform.position = position + dir.normalized * 20;
                
                // Rotates circle to look at the camera
                circles[i].Item1.transform.LookAt(planet.transform.position * 2 - transform.position);
            }
            else
            {
                // Hide the circle if camera is too close to the planet
                circles[i].Item1.SetActive(false);
            }
        }
        
        #endregion

        // planet hover for next frame is saved
        lastIndexHovered = indexHovered;
    }
}
