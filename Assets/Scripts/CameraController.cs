using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

public class CameraController : MonoBehaviour
{
    public Transform target; // the object to follow
    public GravityManager planetManager;
    public GameObject circlePrefab;
    private List<ValueTuple<GameObject, float>> circles; // Value 1: Planet, Value 2: Progress in Lerp function
    public GameObject orientationModel;
    
    // Controls how responsive mouse rotation is
    public float distance = 10.0f; // distance from target
    public float xSpeed = 120.0f; // rotation speed around target
    public float ySpeed = 120.0f;
    public float scrollSpeed = 5;
    private bool mouseDown; // tracked so that first clicking on screen doesn't teleport camera
    private bool dragging; // Tracked so that releasing mouse after drag doesn't suddenly teleport to a planet

    // Euler rotation values for x and y
    private float x = 0.0f;
    private float y = 0.0f;
    private float z = 0.0f;

    // Variables that track circle Ui changes
    private int lastIndexHovered;
    private Vector3 minScale;
    private Vector3 maxScale;
    private Camera _camera;

    // Use this for initialization
    void Start()
    {
        _camera = Camera.main;
        minScale = new Vector3(0.2f, 0.2f, 0.2f);
        maxScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        circles = new List<ValueTuple<GameObject, float>>();
        
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        
        distance = target.transform.localScale.x * 10f;
        scrollSpeed = distance / 10;
        
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        orientationModel.transform.rotation = rotation;
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;

        transform.rotation = rotation;
        transform.position = position;

        // Create a UI circle for each planet
        foreach (GravitationalBody planet in planetManager.planets)
        {
            GameObject circle = Instantiate(circlePrefab);
            circle.GetComponentInChildren<TextMeshPro>().text = planet.name;
            
            SpriteRenderer circlePointer = circle.GetComponent<SpriteRenderer>();
            Color newColor;
            
            switch(planet.name)
            {
                case "Mercury":
                    newColor = new Color(151f / 255, 151f / 255, 159f / 255);
                    break;
                
                case "Venus":
                    newColor = new Color(187f / 255, 183f / 255, 171f / 255);
                    break;
                
                case "Earth":
                    newColor = new Color(140f / 255, 177f / 255, 222f / 255);
                    break;
                
                case "Mars":
                    newColor = new Color(226f / 255, 123f / 255, 88f / 255);
                    break;
                
                case "Jupiter":
                    newColor = new Color(200f / 255, 139f / 255, 58f / 255);
                    break;
                
                case "Saturn":
                    newColor = new Color(195f / 255, 161f / 255, 113f / 255);
                    break;
                
                case "Uranus":
                    newColor = new Color(187f / 255, 225f / 255, 228f / 255);
                    break;
                
                case "Neptune":
                    newColor = new Color(33f / 255, 35f / 255, 84f / 255);
                    break;
                
                default:
                    newColor = Color.white;
                    break;
            }

            circlePointer.color = newColor;
            
            // I don't care about the moon
            if (planet.name is "Moon" or "Sun")
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
        int indexHovered = -1;
        float currentLeastDistance = float.MaxValue;
        bool overUI = EventSystem.current.IsPointerOverGameObject();
        
        for (int i = 0; i < circles.Count; i++)
        {
            // Following code linearly interpolates scale of all planets based on which was hovered the last update
            #region PreviousScalingChange
            
            // Mouse hover for last frame is applied. The hovered circle increases in size while all others decrease
            float timeValue =
                lastIndexHovered == i && !overUI
                    ? Mathf.Clamp(circles[i].Item2 + Time.deltaTime * 5, 0, 1)   // Increase
                    : Mathf.Clamp(circles[i].Item2 - Time.deltaTime * 5, 0, 1);  // Decrease

            if (!Mathf.Approximately(timeValue, circles[i].Item2))
            {
                circles[i] = new ValueTuple<GameObject, float>(circles[i].Item1, timeValue);
            }

            // Shift towards new scaling
            circles[i].Item1.transform.localScale = Vector3.Lerp(minScale, maxScale, circles[i].Item2);

            #endregion

            // Figures out what planet the mouse is hovering this frame
            #region NextScalingHover
            
            // convert ui to screen space to compare to mouse position
            float mouseDistance =
                Vector2.Distance(mousePosition, _camera!.WorldToScreenPoint(planetManager.planets[i].transform.position));

            if (mouseDistance <= 35f) // Within Range
            {
                // Don't let the moon be an option unless on Earth
                if (i == 4 && planetManager.planets[3].transform != target.transform)
                {
                    continue;    
                }
                
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

        // Clicking while over Ui shouldn't affect scene
        if (!overUI)
        {
            // Go to different Planet
            if (Input.GetMouseButtonUp(0) && lastIndexHovered != -1 && !dragging)
            {
                target = planetManager.planets[lastIndexHovered].transform;

                Vector3 angles = transform.eulerAngles;
                x = angles.y;
                y = angles.x;
                z = angles.z;

                distance = target.transform.localScale.x * 10f;
                scrollSpeed = distance / 4;

                Quaternion rot = Quaternion.Euler(y, x, z);
                Vector3 pos = rot * new Vector3(0.0f, 0.0f, -distance) + target.position;

                transform.rotation = rot;
                transform.position = pos;

                orientationModel.transform.rotation = rot;
            }
        }

        if (Input.mouseScrollDelta.y != 0 && mouseDown)
        {
            distance -= Input.mouseScrollDelta.y * scrollSpeed;

            if (distance < target.transform.localScale.x * 2)
            {
                distance = target.transform.localScale.x * 2;
            }
        } 
        
        if (Input.GetMouseButton(0))
        {
            // Prevents teleportation of camera on the first frame you click on the window
            if (mouseDown)
            {
                float saveX = x;
                float saveY = y;
                
                x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

                // Mouse is being used for navigation, not clicking on a planet
                if (!Mathf.Approximately(saveX, x) || !Mathf.Approximately(saveY, y))
                {
                    dragging = true;
                }
            }
            else
            {
                mouseDown = true;
            }
        }
        else
        {
            dragging = false;
        }
        
        // Using right click will allow movement along the third axis
        if (Input.GetMouseButton(1))
        {
            if (mouseDown)
            {
                float middleScreen = Screen.width / 2.0f;
                float middleScreenY = Screen.height / 2.0f;

                // Direction of axis change should be dependent on the side of the screen that the mouse is on
                if (mousePosition.x < middleScreen)
                {
                    z += Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                }
                else if (mousePosition.x > middleScreen)
                {
                    z -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                }
                
                if (mousePosition.y > middleScreenY)
                {
                    z += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                }
                else if (mousePosition.y < middleScreenY)
                {
                    z -= Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                }
            }
            else
            {
                mouseDown = true;
            }
        }
        
        
        // Update position of camera, required every frame since planets are moving
        Quaternion rotation = Quaternion.Euler(y, x, z);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;
        orientationModel.transform.rotation = rotation;

        transform.rotation = rotation;
        transform.position = position;

        #endregion
        
        // Updates the Ui circles for each planet to be between the camera and planet at the appropriate size
        #region CirclePositioning&Visibility
        
        // Now handle UI for far away planets by drawing circles over them
        for (int i = 0; i < planetManager.planets.Count; i++)
        {
            GravitationalBody planet = planetManager.planets[i];

            // This will hide circles if hidden behind other planets, mainly sun but also handles earth and moon
            if (planetManager.planets[0].transform != target.transform) // Ignore if tracking the sun
            {
                // Don't do this part for the planet you are currently looking at
                if (planet.transform != target.transform)
                {
                    // Block things behind earth if the player is focused on the moon
                    int layerMask =
                        planetManager.planets[4].transform == target.transform ? 
                        LayerMask.GetMask("Sun", "Earth") : // Sun, Earth, and Moon    
                        LayerMask.GetMask("Sun"); // Only Sun

                    // Essentially, hide circle if the planet is obscured by the sun, (and/or earth/moon in moons case)
                    if (Physics.Linecast(position, planet.transform.position, layerMask))
                    {
                        // Will be invisible, continue
                        circles[i].Item1.SetActive(false);
                        continue;
                    }
                    else
                    {
                        circles[i].Item1.SetActive(true);
                    }
                }
            }

            // Don't want to see the moon ui at all
            if (planet.name is "Moon" or "Sun")
            {
                circles[i].Item1.SetActive(false);
                continue;
            }
            if (planet.name is "Earth" && planetManager.planets[4].transform == target.transform)
            {
                // Don't show Earth circle if focused on the moon
                circles[i].Item1.SetActive(false);
                continue; 
            }
            
            Vector3 dir = planet.transform.position - position;
            float dist = dir.magnitude;
            float tarDist = (target.transform.position - _camera!.transform.position).magnitude;
            
            // Prevents 3-dimensional z-fighting. Unfortunately needs pretty expensive method calls
            // Draws things closer to the camera first, surprisingly giving a lot more depth
            Renderer rend = circles[i].Item1.GetComponent<Renderer>();
            rend.sortingOrder = (500 - (int)(Mathf.Sqrt(dist))); // Furthest: neptune to uranus at ~275

            // Don't draw the target planet at all, or planets really close to the camera
            if (planet.transform != target &&
                dist > planet.transform.localScale.x + Mathf.Pow(planet.transform.localScale.x, 2))
            {
                circles[i].Item1.SetActive(true);

                // Put circles the same distance from the camera as the planet, and scale them to maintain the same size
                // The goal of this is so that the planet covers the ui for other planets that would be hidden behind it
                Vector3 newPosition = position + dir.normalized * tarDist;
                
                circles[i].Item1.transform.position = newPosition;

                // Maintain scale relative to camera
                circles[i].Item1.transform.localScale *= tarDist / 20f;
                
                // Rotates circle to look at the camera
                circles[i].Item1.transform
                    .LookAt(planet.transform.position * 2 - transform.position, _camera.transform.up);
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
