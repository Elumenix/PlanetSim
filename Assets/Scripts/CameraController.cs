using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public GravitationalBody target; // the object to follow
    public GravityManager planetManager;
    public GameObject circlePrefab;
    private List<ValueTuple<GameObject, float>> circles; // Value 1: Planet, Value 2: Progress in Lerp function
    public GameObject orientationModel;

    private Transform startBezierTransform;
    private Transform endBezierTransform;
    private bool followingBezier;
    private float lerpAmount;
    
    // Controls how responsive mouse rotation is
    public float distance = 10.0f; // distance from target
    public float xSpeed = 120.0f; // rotation speed around target
    public float ySpeed = 120.0f;
    public float scrollSpeed;
    private bool mouseDown; // Tracked so that first clicking on screen doesn't teleport camera
    private bool dragging; // Tracked so that releasing mouse after drag doesn't suddenly teleport to a planet
    private int clickTimer = 15;
    public bool followPlanetRotation;

    // Variables that track circle Ui changes
    private int lastIndexHovered;
    private Vector3 minScale;
    private Vector3 maxScale;
    private Camera _camera;

    // Use this for initialization
    void Start()
    {
        // Unity won't let me instantiate transforms on their own so this is my workaround for that
        // It doesn't use much more memory and I won't ever create new game objects for these variables, so it's alright
        startBezierTransform = new GameObject().transform;
        endBezierTransform = new GameObject().transform;

        _camera = Camera.main;
        
        // Circles are scaled down a lot to work well in scene, they only change this much
        minScale = new Vector3(0.2f, 0.2f, 0.2f);
        maxScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        circles = new List<ValueTuple<GameObject, float>>();
        
        distance = target.transform.localScale.x * 10f;
        scrollSpeed = distance / 4;
        
        // Match rotation vector of target planet and position accordingly
        Vector3 position = target.transform.rotation * new Vector3(0.0f, 0.0f, -distance) + target.transform.position;
        transform.rotation = target.transform.rotation;
        transform.position = position;
        orientationModel.transform.rotation = target.transform.rotation;
        

        // Create a UI circle for each planet
        foreach (GravitationalBody planet in planetManager.planets)
        {
            GameObject circle = Instantiate(circlePrefab);
            circle.GetComponentInChildren<TextMeshPro>().text = planet.name;
            
            SpriteRenderer circlePointer = circle.GetComponent<SpriteRenderer>();

            Color newColor = planet.name switch
            {
                "Mercury" => new Color(151f / 255, 151f / 255, 159f / 255),
                "Venus" => new Color(187f / 255, 183f / 255, 171f / 255),
                "Earth" => new Color(140f / 255, 177f / 255, 222f / 255),
                "Mars" => new Color(226f / 255, 123f / 255, 88f / 255),
                "Jupiter" => new Color(200f / 255, 139f / 255, 58f / 255),
                "Saturn" => new Color(195f / 255, 161f / 255, 113f / 255),
                "Uranus" => new Color(187f / 255, 225f / 255, 228f / 255),
                "Neptune" => new Color(33f / 255, 35f / 255, 84f / 255),
                _ => Color.white
            };

            circlePointer.color = newColor;
            
            // I don't care about the moon or sun
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

            // If timescale is 0, try to emulate a speed of deltaTime
            float necessaryDelta = Time.timeScale == 0 ? .01666f : Time.deltaTime;
            
            // Mouse hover for last frame is applied. The hovered circle increases in size while all others decrease
            float timeValue =
                lastIndexHovered == i && !overUI
                    ? Mathf.Clamp(circles[i].Item2 + necessaryDelta * 5, 0, 1)   // Increase
                    : Mathf.Clamp(circles[i].Item2 - necessaryDelta * 5, 0, 1);  // Decrease

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
                else if (mouseDistance < currentLeastDistance &&
                         (circles[i].Item1.name == "Sun" || circles[i].Item1.activeSelf)) 
                { // This is the new closest viable planet to mouse
                    currentLeastDistance = mouseDistance;
                    indexHovered = i;
                }
            }
            
            #endregion
        }

        // User controls are taken away during camera transition, and are instead handled by FollowBezierCurve
        // This is done in fixedUpdate instead of update because it isn't visually related and the math
        // To follow the Bézier curve needs to much more precise and related to the current physics for large time steps
        // Input will not be accepted during this step so that things don't get messed up
        if (!followingBezier)
        {
            // Camera direction and zoom will be modified based on user controls, as long as not in transition
            #region CameraOrientationUpdate

            // Variables are established here to be incremented throughout this section 
            float deltaX = 0; // Assign pitch (side-to-side) to x
            float deltaY = 0; // Assign yaw (up-and-down) to y
            float deltaZ = 0; // Assign roll (forward-and-backward) to z

            // Clicking while over Ui shouldn't affect scene
            if (!overUI)
            {
                // Go to different Planet : only if not dragging or click completes within 15 frames of press
                if (Input.GetMouseButtonUp(0) && lastIndexHovered != -1 && (!dragging || clickTimer > 0))
                {
                    Vector3 displacement = transform.position - planetManager.planets[lastIndexHovered].transform.position; // B and A are your game objects
                    float dot = Vector3.Dot(displacement, transform.forward);

                    // Confirms the clicked on planet is in front of the camera
                    // There's a small unlikely chance the user manages to click on a UI circle behind the camera
                    if (dot < 0)
                    {
                        // TODO: add additional check to disable bezier altogether when option is added
                        if (planetManager.planets[lastIndexHovered] != target)
                        {
                            // This will be constant so save this now. Need copies instead of references as the camera moves
                            startBezierTransform.position = transform.position;
                            startBezierTransform.rotation = transform.rotation;
                            followingBezier = true; // Will start this process next frame
                            lerpAmount = 0;
                        }

                        if (followingBezier)
                        {
                            // Rotation will be constant to prevent massive bezier changes as rotation updates

                            // Get rotation value from view of current camera position
                            endBezierTransform.position = transform.position;
                            endBezierTransform.LookAt(planetManager.planets[lastIndexHovered].transform, Vector3.up);
                        }

                        // Switch tracked planet and scale movement values relative to it's size
                        target = planetManager.planets[lastIndexHovered];
                        distance = target.transform.localScale.x * 10f;
                        scrollSpeed = distance / 4;


                        if (!followingBezier)
                        {
                            // Position camera relative to the new target planet
                            Vector3 pos = target.transform.rotation * new Vector3(0.0f, 0.0f, -distance) +
                                          target.transform.position;
                            transform.position = pos;
                            transform.rotation = target.transform.rotation;
                        }
                    }
                }
            }

            // Scroll Wheel affects distance to the planet
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
                clickTimer--; // Decrement

                // Prevents teleportation of camera on the first frame you click on the window
                if (mouseDown)
                {
                    deltaX += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                    deltaY -= (Input.GetAxis("Mouse Y") * ySpeed * 0.02f);

                    // Mouse is being used for navigation, not clicking on a planet
                    if (!Mathf.Approximately(0, deltaX) || !Mathf.Approximately(0, deltaX))
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
                clickTimer = 15;
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
                        deltaZ += Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                    }
                    else if (mousePosition.x > middleScreen)
                    {
                        deltaZ -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                    }

                    if (mousePosition.y > middleScreenY)
                    {
                        deltaZ += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                    }
                    else if (mousePosition.y < middleScreenY)
                    {
                        deltaZ -= Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                    }
                }
                else
                {
                    mouseDown = true;
                }
            }

            // Actual position update can be skipped on bezier start frame so there isn't a single from of the other planet
            if (!followingBezier)
            {
                // Update position of camera, required every frame since planets are moving
                // Z Quaternion is added separately so that it doesn't affect the x/y mouse direction
                Quaternion rotationXY = Quaternion.Euler(deltaY, deltaX, 0);
                Quaternion rotationZ = Quaternion.Euler(0, 0, deltaZ);
                Quaternion
                    rotation = transform.rotation * rotationZ *
                               rotationXY; // Correct multiplication order to make x/y consistent

                // Update position of camera accordingly
                Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.transform.position;

                // Make sure everything fits the new orientation
                transform.rotation = rotation;
                transform.position = position;
                orientationModel.transform.rotation = rotation;
            }

            #endregion
        }
        else
        {
            // This still needs to update
            orientationModel.transform.rotation = transform.rotation;
        }

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
                    if (Physics.Linecast(transform.position, planet.transform.position, layerMask))
                    {
                        // Will be invisible, continue
                        circles[i].Item1.SetActive(false);
                        continue;
                    }

                    circles[i].Item1.SetActive(true);
                }
            }

            switch (planet.name)
            {
                // Don't want to see the moon ui at all
                case "Moon" or "Sun":
                    circles[i].Item1.SetActive(false);
                    continue;

                // Don't show Earth circle if focused on the moon
                case "Earth" when planetManager.planets[4].transform == target.transform:
                    circles[i].Item1.SetActive(false);
                    continue;
            }

            Vector3 dir = planet.transform.position - transform.position;
            float dist = dir.magnitude;
            float tarDist = (target.transform.position - _camera!.transform.position).magnitude;
            
            // Prevents 3-dimensional z-fighting. Unfortunately needs pretty expensive method calls
            // Draws things closer to the camera first, surprisingly giving a lot more depth
            Renderer rend = circles[i].Item1.GetComponent<Renderer>();
            rend.sortingOrder = (500 - (int)((Mathf.Sqrt(dist)) * 1.75f)); // Furthest sqrt: neptune to uranus at ~275

            // Don't draw the target planet at all, or planets really close to the camera
            if (planet.transform != target.transform &&
                dist > planet.transform.localScale.x + Mathf.Pow(planet.transform.localScale.x, 2))
            {
                circles[i].Item1.SetActive(true);

                // Put circles the same distance from the camera as the planet, and scale them to maintain the same size
                // The goal of this is so that the planet covers the ui for other planets that would be hidden behind it
                Vector3 newPosition = transform.position + dir.normalized * tarDist;
                
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

    public void FixedUpdate()
    {
        if (followPlanetRotation && Time.timeScale != 0 && !followingBezier)
        {
            // Reverse Direction if time is reversed
            int timeDir = !planetManager.reversed ? 1 : -1;
            
            // This is a series of vector/quaternion transformations to make the camera rotate around the planet
            // at the exact same speed that the planet is rotating, making it look still
            float deltaRotation = (float) (target.RotationSpeed * timeDir * Time.fixedDeltaTime);
            Vector3 rotationAxis = transform.InverseTransformDirection(target.transform.up); // Aligns camera to planet
            transform.Rotate(rotationAxis, deltaRotation, Space.Self); // transformation relative to camera
        }

        if (followingBezier)
        {
            // 5 seconds should pass for transition to finish
            lerpAmount += (Time.fixedDeltaTime / 5) / Time.timeScale;
            
            // Position and rotation of end planet changes every update, so it keeps needing to be recalculated
            // Note that we aren't calculating the planets transform, but instead the camera's end goal
            
            endBezierTransform.position = endBezierTransform.rotation * new Vector3(0.0f, 0.0f, -distance) +
                                          target.transform.position;
            
            FollowBezierCurve(startBezierTransform, endBezierTransform, lerpAmount > 1 ? 1 : lerpAmount);
            
            // Transition has finished
            if (lerpAmount >= 1)
            {
                followingBezier = false;
            }
        }
    }

    // This function isn't meant to move the camera along a Bézier curve to transition between planets
    private void FollowBezierCurve(Transform start, Transform end, float t)
    {
        // Because I want to create a cubic Bézier curve, and this isn't an animation system, I need 4 control points
        // Because I only start with two, I need to mathematically create two more control points to use
        // this should be mathematically reasonable so that the camera doesn't move all over the place
        
        // Calculate direction away from planets that control points should be
        // Because all planets hover around z = 0, I want the direction to meaningfully deviate on that axis
        Vector3 direction = (end.position - start.position);

        if (direction.magnitude > 1000) // We'll use a cubic curve
        {
            direction = new Vector3(direction.x, direction.y, direction.magnitude / 3).normalized;

            // How far away I want the control point to be from others
            float controlPointDistance = Vector3.Distance(start.position, end.position) / 10f;

            Vector3 controlPoint1 = start.position + direction * controlPointDistance;
            Vector3 controlPoint2 = end.position - direction * controlPointDistance;

            // We follow De Casteljau's Algorithm for following the Bézier curve
            Vector3 A = Vector3.Lerp(start.position, controlPoint1, t);
            Vector3 B = Vector3.Lerp(controlPoint1, controlPoint2, t);
            Vector3 C = Vector3.Lerp(controlPoint2, endBezierTransform.position, t);
            Vector3 D = Vector3.Lerp(A, B, t);
            Vector3 E = Vector3.Lerp(B, C, t);

            transform.position = Vector3.Lerp(D, E, t);


            // Same process for position is used for rotation, though curve is purposely different to closely face end position
            Quaternion rotationDirection = Quaternion.Inverse(start.rotation) * end.rotation;
            Quaternion controlRotation1 =
                start.rotation * Quaternion.Lerp(Quaternion.identity, rotationDirection, 5 / 6f);
            Quaternion controlRotation2 =
                start.rotation * Quaternion.Lerp(Quaternion.identity, rotationDirection, 1 / 6f);

            // Rotation Bezier curve
            Quaternion RA = Quaternion.Lerp(start.rotation, controlRotation1, t);
            Quaternion RB = Quaternion.Lerp(controlRotation1, controlRotation2, t);
            Quaternion RC = Quaternion.Lerp(controlRotation2, end.rotation, t);
            Quaternion RD = Quaternion.Lerp(RA, RB, t);
            Quaternion RE = Quaternion.Lerp(RB, RC, t);
            transform.rotation = Quaternion.Lerp(RD, RE, t);
        }
        else 
        { 
            // Regular interpolation is used so short distances don't look weird
            Vector3 P = Vector3.Lerp(start.position, end.position, t);

            transform.position = P;
            
            
            // Quadratic Bézier curve used for rotation
            Quaternion rotationDirection = Quaternion.Inverse(start.rotation) * end.rotation;
            Quaternion controlRotation1 =
                start.rotation * Quaternion.Lerp(Quaternion.identity, rotationDirection, 5 / 6f);
            
            Quaternion RA = Quaternion.Lerp(start.rotation, controlRotation1, t);
            Quaternion RB = Quaternion.Lerp(controlRotation1, end.rotation, t);
            
            transform.rotation = Quaternion.Lerp(RA, RB, t);
        }
    }
}
