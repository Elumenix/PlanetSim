using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public GravitationalBody target; // the object to follow
    public GravityManager planetManager;
    public GameObject circlePrefab;
    private List<ValueTuple<GameObject, float>> circles; // Value 1: Planet, Value 2: Progress in Lerp function
    private List<Renderer> circleRenderers;
    public GameObject orientationModel;

    // Variables to track camera following a spline
    private GravitationalBody startCurvePlanet;
    private GravitationalBody endCurvePlanet;
    private bool followingCurve;
    protected internal float lerpAmount;
    
    // Controls how responsive mouse rotation is
    public float distance = 10.0f; // distance from target
    public float xSpeed = 120.0f; // rotation speed around target horizontally
    public float ySpeed = 120.0f; // rotation speed around target vertically
    private float scrollSpeed; // How far scrolling moves you, this changes for each planet
    private bool mouseDown; // Tracked so that first clicking on screen doesn't teleport camera
    private bool dragging; // Tracked so that releasing mouse after drag doesn't suddenly teleport to a planet
    private int clickTimer = 15; // How many frames the mouse can be held down before a click will no longer register
    public bool followPlanetRotation; // Will the camera rotate with a planet

    // Variables that track circle Ui changes
    private int lastIndexHovered;
    private Vector3 minScale;
    private Vector3 maxScale;
    private Camera _camera;

    // Initialization
    void Start()
    {
        // Instantiations done first
        // Renderers could be gotten at runtime, but that requires multiple getComponent calls, which is expensive
        circleRenderers = new List<Renderer>();
        
        // Unity won't let me instantiate transforms on their own so this is my workaround for that
        // It doesn't use much more memory and I won't ever create new game objects for these variables, so it's alright
        startCurvePlanet = new GameObject().AddComponent<GravitationalBody>();
        startCurvePlanet.name = "SplineBeginning";
        endCurvePlanet = new GameObject().AddComponent<GravitationalBody>();
        endCurvePlanet.name = "SplineEnding";

        _camera = Camera.main;
        
        // Circles are scaled down a lot to work well in scene, they only change this much
        minScale = new Vector3(0.2f, 0.2f, 0.2f);
        maxScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        circles = new List<ValueTuple<GameObject, float>>();
        
        // Camera control variables for the starting planet are set
        distance = target.transform.localScale.x * 10f;
        scrollSpeed = distance / 4;
        lerpAmount = 1;
        
        // Match rotation vector of target planet and position accordingly
        Vector3 position = target.transform.rotation * new Vector3(0.0f, 0.0f, -distance) + target.transform.position;
        //transform.rotation = target.transform.rotation;
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
                "Neptune" => new Color(80f / 255, 100f / 255, 255f / 255),
                _ => Color.white
            };

            circlePointer.color = newColor;
            
            // I don't care about the moon or sun
            if (planet.name is "Moon" or "Sun")
            {
                circle.SetActive(false);
            }
            
            // Track values needed to alter circles
            circles.Add(new ValueTuple<GameObject, float>(circle, 0));
            circleRenderers.Add(circles.Last().Item1.GetComponent<Renderer>());
        }
    }

    // Happens after planets move
    private void LateUpdate()
    {
        // Save variables that will be used for Scaling calculations
        Vector2 mousePosition = Input.mousePosition;
        int indexHovered = -1;
        float currentLeastDistance = float.MaxValue;
        bool overUI = EventSystem.current.IsPointerOverGameObject(); // If over UI, input will be ignored
        
        for (int i = 0; i < circles.Count; i++)
        {
            // Following code linearly interpolates ui circle scale of all planets based on which was hovered the last update
            #region PreviousScalingChange
            
            // Mouse hover for last frame is applied. The hovered circle increases in size while all others decrease
            float timeValue =
                lastIndexHovered == i && !overUI
                    ? Mathf.Clamp(circles[i].Item2 + Time.unscaledDeltaTime * 5, 0, 1)   // Increase
                    : Mathf.Clamp(circles[i].Item2 - Time.unscaledDeltaTime * 5, 0, 1);  // Decrease

            // If the hover length of a circle has changed, replace its struct
            if (!Mathf.Approximately(timeValue, circles[i].Item2))
            {
                circles[i] = new ValueTuple<GameObject, float>(circles[i].Item1, timeValue);
            }

            // Shift towards new scaling
            circles[i].Item1.transform.localScale = Vector3.Lerp(minScale, maxScale, circles[i].Item2);

            #endregion

            // Figures out what planet the mouse is hovering this frame, before the camera/circles move
            #region NextScalingHover
            
            // Convert ui to screen space to compare to mouse position
            float mouseDistance =
                Vector2.Distance(mousePosition, _camera!.WorldToScreenPoint(planetManager.planets[i].transform.position));

            if (mouseDistance <= 35f) // Within Range : This is essentially the circles radius on screen
            {
                // Don't let the moon be an option unless on Earth
                if (i == 4 && planetManager.planets[3].transform != target.transform)
                {
                    continue;    
                }
                
                // Series of checks to find which circle the mouse is closest to
                // It is of note that the Sun is always an option, even though it doesn't have a circle
                if (indexHovered == -1) // If nothing is hovered yet
                {
                    indexHovered = i;
                    currentLeastDistance = mouseDistance;
                }
                else if (mouseDistance < currentLeastDistance &&
                         (circles[i].Item1.name == "Sun" || circles[i].Item1.activeSelf)) 
                { 
                    // This is the new closest viable planet to mouse
                    currentLeastDistance = mouseDistance;
                    indexHovered = i;
                }
            }
            
            #endregion
        }
        
        // Planet hover for next frame is saved
        lastIndexHovered = indexHovered;
        
        #region FollowingSpline

        if (followingCurve)
        {
            // 5 seconds should pass for transition to finish
            lerpAmount += Time.unscaledDeltaTime / 5; // Unscaled means Time.timescale won't affect speed
            
            // Position and rotation of end planet changes every update, so it keeps needing to be recalculated
            // Note that we aren't calculating the planets transform, but instead the camera's end goal
            endCurvePlanet.transform.position = endCurvePlanet.transform.rotation * new Vector3(0.0f, 0.0f, -distance) +
                                                target.transform.position;
            endCurvePlanet.Velocity = target.Velocity;
            
            FollowSpline(startCurvePlanet, endCurvePlanet, lerpAmount > 1 ? 1 : lerpAmount);
            
            // Transition has finished
            if (lerpAmount >= 1)
            {
                followingCurve = false;
            }
        }

        #endregion
        
        // User controls are taken away during camera transitions, and positioning is handled by the FollowingSpline region 
        // Input will not be accepted during this step so that things don't get messed up
        // This and FollowingSpline can both run in a frame on the very last frame of followingCurve
        if (!followingCurve)
        {
            // Camera direction and zoom will be modified based on user controls, as long as not in a transition
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
                    Vector3 displacement = transform.position - planetManager.planets[lastIndexHovered].transform.position; 
                    float dot = Vector3.Dot(displacement, transform.forward);

                    // Confirms the clicked on planet is in front of the camera
                    // There's a small unlikely chance the user manages to click on a UI circle behind the camera
                    if (dot < 0)
                    {
                        // TODO: add additional check to change or disable spline altogether when option is added
                        if (planetManager.planets[lastIndexHovered] != target)
                        {
                            // If the camera is too close in the Earth Moon system, It will look bad
                            // The camera essentially can't follow because the velocity of the planets
                            if (target.name is "Moon" or "Earth" &&
                                Vector3.Distance(planetManager.planets[lastIndexHovered].transform.position,
                                    _camera.transform.position) <= 150f)
                            {
                                followingCurve = false;
                            }
                            else
                            {
                                followingCurve = true; // Will start this process next frame
                                lerpAmount = 0;
                            }
                            
                            // This will be constant, so save this now. Need copies instead of references as the camera moves
                            startCurvePlanet.transform.position = transform.position;
                            startCurvePlanet.transform.rotation = transform.rotation;
                            startCurvePlanet.Velocity = target.Velocity;

                            // Rotation will be constant to prevent massive changes as rotation updates
                            // Get rotation value from view of current camera position
                            endCurvePlanet.transform.position = _camera.transform.position; // temp to get rotation
                            endCurvePlanet.Velocity = planetManager.planets[lastIndexHovered].Velocity;

                            // Matches rotation and obliquity to orbit
                            endCurvePlanet.transform.LookAt(planetManager.planets[lastIndexHovered].transform,
                                planetManager.planets[lastIndexHovered].transform.up);
                        }

                        // Switch tracked planet and scale movement values relative to it's size
                        target = planetManager.planets[lastIndexHovered];
                        distance = target.transform.localScale.x * 10f;
                        scrollSpeed = distance / 4.0f;


                        // If the camera isn't set up to follow the spline it will go to the next planet immediately
                        if (!followingCurve)
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

            if (Input.GetMouseButton(0)) // left mouse button down
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
            if (Input.GetMouseButton(1)) // right mouse button down
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

            // FINAL CAMERA POSITION UPDATE
            // Actual position update can be skipped on spline start frame so there isn't a single frame of the other planet
            if (!followingCurve)
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
            // This still needs to update no matter what
            orientationModel.transform.rotation = transform.rotation;
        }

        // Updates the Ui circles for each planet to be between the camera and planet at the appropriate size
        #region CirclePositioning&Visibility
        
        // Now handle UI for far away planets by drawing circles over them
        for (int i = 0; i < planetManager.planets.Count; i++)
        {
            GravitationalBody planet = planetManager.planets[i];

            // This will hide circles if hidden behind other planets, mainly the Sun but also handles the Earth and Moon
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
                    if (Physics.Linecast(_camera.transform.position, planet.transform.position, layerMask))
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
                // Don't want to see the Moon or Sun ui at all
                case "Moon" or "Sun":
                    circles[i].Item1.SetActive(false);
                    continue;

                // Don't show Earth circle if focused on the Moon
                case "Earth" when planetManager.planets[4].transform == target.transform:
                    circles[i].Item1.SetActive(false);
                    continue;
            }

            // Getting information on positioning of planet relative to the camera
            Vector3 dir = planet.transform.position - transform.position;
            float dist = dir.magnitude;
            float tarDist = (target.transform.position - _camera!.transform.position).magnitude;
            
            // Prevents 3-dimensional z-fighting. Unfortunately needs pretty expensive method calls
            // Draws things closer to the camera first, surprisingly giving a lot more depth
            circleRenderers[i].sortingOrder = (500 - (int)((Mathf.Sqrt(dist)) * 1.75f)); // Furthest sqrt: neptune to uranus at ~275
            
            // Prevents sharing value (Rare). Ternary handles the Moon being between Mars and Earth 
            if (circleRenderers[i].sortingOrder == circleRenderers[i - (i != 5 ? 1 : 2)].sortingOrder)
            {
                circleRenderers[i].sortingOrder += 1;
            }
            

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
                
                // Rotates circle to look at point on a line that bisects the camera and target planet/circle,
                // As well as past the circle itself. This will line its rotation to be facing the camera correctly
                circles[i].Item1.transform.LookAt(newPosition * 2 - transform.position, _camera.transform.up);
            }
            else
            {
                // Hide the circle if camera is too close to the planet, or this planet is the target
                circles[i].Item1.SetActive(false);
            }
        }
        
        #endregion
    }

    public void FixedUpdate()
    {
        // This set of instructions make it so that the camera rotates around the planet at the same rate the planet rotates
        // This is done by doing calculations based on rotationSpeed, which only applies to planets during physics updates
        // Because this needs to follow physics Updates, it is done in FixedUpdate instead of LateUpdate
        if (followPlanetRotation && Time.timeScale != 0 && !followingCurve)
        {
            // Reverse Direction if time is reversed
            int timeDir = !planetManager.reversed ? 1 : -1;
            
            // This is a series of vector/quaternion transformations to make the camera rotate around the planet
            // at the exact same speed that the planet is rotating, making it look still
            float deltaRotation = (float) (target.RotationSpeed * timeDir * Time.fixedDeltaTime);
            Vector3 rotationAxis = transform.InverseTransformDirection(target.transform.up); // Aligns camera to planet
            transform.Rotate(rotationAxis, deltaRotation, Space.Self); // transformation relative to camera
        }
    }

    private void FollowSpline(GravitationalBody start, GravitationalBody end, float t)
    {
        // Get the positions of the start and end transforms
        Vector3 p1 = start.transform.position;
        Vector3 p2 = end.transform.position;

        float dist = Vector3.Distance(p1, p2);
        
        // Defines the control points for a Catmull-Rom spline
        // This spline functions on velocity via position, so it makes sense to use actual velocity values
        Vector3 p0 = p1 + new Vector3((float)start.Velocity.x, (float)start.Velocity.y, (float)start.Velocity.z);
        Vector3 p3 = p2 - new Vector3((float)end.Velocity.x, (float)end.Velocity.y, (float)end.Velocity.z);

        // These are needed for math things
        float t2 = t * t;
        float t3 = t2 * t;

        // Calculate the position along the spline using Catmull-Rom spline formula
        transform.position = 0.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
                                     (-p0 + 3 * p1 - 3 * p2 + p3) * t3);

        switch (dist)
        {
            // Decision on how to handle rotation
            case > 300: // Will happen most of the time
                
                // Rotate camera over time to match final rotation
                transform.rotation = Quaternion.Slerp(start.transform.rotation, end.transform.rotation, t);
                
                // This is just quadratic equation to change to fov of the camera throughout the transition
                // The camera will go from 27 (default fov) to p, then back to 27
                // The point of this is to give a feeling of speed during a transition
                // This should relatively well for Catmull-Rom as it is based on velocity
                float p = 27 + Mathf.Pow(dist, 1f / 3f) * 1.5f; // Peak of quadratic
                _camera.fieldOfView = (4 * (27 - p) * t2) - (4 * (27 - p) * t) + 27;
                break;
            
            default:
                
                // Unless the user does weird things with the camera, this is only really for an earth/moon transition
                // This will give a much better view for nearer object. Essentially rotates much faster at the beginning
                transform.rotation = Quaternion.Slerp(start.transform.rotation, end.transform.rotation, Mathf.Pow(t, 0.25f));
            
                // The major problem with this whole system is that the planets still move while the camera is
                // so the end of the spline will shift a lot more from the beginning and the camera will always be
                // playing catch-up. Catmull-Rom is relatively straight so users won't notice with larger distances
                // for near planets it's not so clear-cut and the fact that the planets still move may be obvious depending
                // on the angle the user views the other planet at. Aside from pausing the simulation during the camera 
                // transition, which I don't want to do, having the camera more closely track what's going on is the best solution
                break;
        }
    }
}
