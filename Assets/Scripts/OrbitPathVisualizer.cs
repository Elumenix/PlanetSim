using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OrbitPathVisualizer : MonoBehaviour
{
    public GravityManager simulation;
    private LineRenderer moonRenderer;
    private List<LineRenderer> sunBound;
    public Material orbitMaterial;
    private Camera _camera;
    private CameraController control;
    private GravitationalBody previousPlanet;

    // Start is called before the first frame update
    void Start()
    {
        _camera = Camera.main;
        control = _camera!.GetComponent<CameraController>();
        sunBound = new List<LineRenderer>
        {
            // Because I don't want to do a second fast pace simulation in the background to figure out the shape of 
            // Each planets orbit, I instead recorded a same of positions of each planets' first orbit in a json
            // and will instead read it to create a line2D for each planet at the beginning of the program.
            // This helps prevent the need to do much more math every fixedUpdate, which would cause lag
            // The only big problem is that the lines don't update for Apsidal precession, but most users won't notice that
            CreateNewPath("Assets/Line2DPaths/MercuryOrbit.json"),
            CreateNewPath("Assets/Line2DPaths/VenusOrbit.json"),
            CreateNewPath("Assets/Line2DPaths/EarthOrbit.json"),
            CreateNewPath("Assets/Line2DPaths/MarsOrbit.json"),
            CreateNewPath("Assets/Line2DPaths/JupiterOrbit.json"),
            CreateNewPath("Assets/Line2DPaths/SaturnOrbit.json"),
            CreateNewPath("Assets/Line2DPaths/UranusOrbit.json"),
            CreateNewPath("Assets/Line2DPaths/NeptuneOrbit.json")
        };

        moonRenderer = CreateNewPath("Assets/Line2DPaths/MoonOrbit.json");
        moonRenderer.widthMultiplier = 0.1f;
    }

    private void LateUpdate()
    {
        // Because everything, including the sun, moves, and orbits are drawn relative to what is being orbited:
        // All lines need to be moved to line up with their major body
        
        // Line up with Earth
        moonRenderer.gameObject.transform.position = simulation.planets[3].transform.position;
        
        // Line up with the Sun
        Vector3 orbitCenter = simulation.planets[0].transform.position;
        
        for (int i = 0; i < sunBound.Count; i++)
        {
            // Line orbits up with the sun first
            LineRenderer lineRenderer = sunBound[i];
            lineRenderer.gameObject.transform.position = orbitCenter;
            
            // Which planet is a bit complex as sun is 0 and moon is 4
            GravitationalBody curPlanet = simulation.planets[i < 3 ? i + 1 : i + 2];
            
            float distance = Vector3.Distance(curPlanet.transform.position, _camera.transform.position);
            //lineRenderer.widthMultiplier = Mathf.Max(distance * 2 / 1000.0f, curPlanet.transform.localScale.x / 1.5f); // Prevent line getting too small


            // Return to normal gradually
            if (curPlanet == previousPlanet && control.lerpAmount < 1)
            {
                lineRenderer.widthMultiplier = Mathf.Lerp(.1f,
                    lineRenderer.widthMultiplier = (i < 4 ? 5 : 10) + distance / 2000.0f, control.lerpAmount);
                continue;
            }
            
            if (distance > 2000) // Helps maintain visibility
            {
                lineRenderer.widthMultiplier = (i < 4 ? 5 : 10) + distance / 2000.0f;
            }
            else
            {
                lineRenderer.widthMultiplier = i < 4 ? 5 : 10;
            }
            
            // Shrink width for the planet the camera is targeting
            if (control.target == curPlanet || curPlanet.name == "Earth" && control.target.name == "Moon")
            {
                lineRenderer.widthMultiplier = Mathf.Lerp(lineRenderer.widthMultiplier, .1f, control.lerpAmount);
            }
            
            // Save the last traveled to planet
            if (control.target == curPlanet && control.lerpAmount >= 1)
            {
                previousPlanet = curPlanet;
            }
            
            
            
            /*
            // Then scale the size of orbit lines by how close the camera is to them:
            // First, get the current planet : this requires a check because the moon is at index 4
            float averageOrbitRadius = i < 4
                ? simulation.planets[i + 1].AverageOrbitRadius
                : simulation.planets[i + 2].AverageOrbitRadius;


            // Get a point that Averages the orbit radius from the sun closest to the camera on the xy plane
            // This isn't exact as it won't follow the exact angle away from the xy plane the planet orbits (which changes)
            // I could try to actively calculate a more correct point, but that may lead to clear visual morphing
            // of how wide the line is over time, which is absolutely to be avoided. This is more consistent
            Vector3 relativeClosestOrbitPoint = (_camera.transform.position - orbitCenter).normalized;
            relativeClosestOrbitPoint.z = 0;
            relativeClosestOrbitPoint *= averageOrbitRadius;


            // Calculate the distance from the camera to the closest point on the orbit. Width will be based on this
            float distanceToCamera = Vector3.Distance(relativeClosestOrbitPoint, _camera.transform.position);

            float desiredWidth = distanceToCamera * .005f;
            lineRenderer.widthMultiplier = desiredWidth;*/
        }
    }

    private LineRenderer CreateNewPath(string filePath)
    {
        string json = System.IO.File.ReadAllText(filePath);
        OrbitPositions orbitPositions = JsonUtility.FromJson<OrbitPositions>(json);
        
        // Create a new object in the scene to hold a line renderer and initialize its values
        LineRenderer lineRenderer = (new GameObject("line")).AddComponent<LineRenderer>();
        lineRenderer.positionCount = 361;
        lineRenderer.widthMultiplier = 20;
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false; // Needs to be relative to orbiting body
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.material = orbitMaterial;
        lineRenderer.sortingOrder = -1; // makes render before circles, mostly, lineRenderer is unreliable 
        
        
        string planetName = filePath.Substring(19, filePath.Length - 29); // 19 from start, 10 from end
        Color lineColor = planetName switch
        {
            "Mercury" => new Color(151f / 255, 151f / 255, 159f / 255),
            "Venus" => new Color(187f / 255, 183f / 255, 171f / 255),
            "Earth" => new Color(140f / 255, 177f / 255, 222f / 255),
            "Mars" => new Color(226f / 255, 123f / 255, 88f / 255),
            "Jupiter" => new Color(200f / 255, 139f / 255, 58f / 255),
            "Saturn" => new Color(195f / 255, 161f / 255, 113f / 255),
            "Uranus" => new Color(187f / 255, 225f / 255, 228f / 255),
            "Neptune" => new Color(49f / 255, 51f / 255, 175f / 255),
            _ => Color.white
        };

        lineRenderer.material.color = lineColor;
        
        // Get info about the json
        int numPoints = orbitPositions.positions.Count;
        lineRenderer.positionCount = numPoints;
        

        for (int i = 0; i < numPoints; i++)
        {
            lineRenderer.SetPosition(i, orbitPositions.positions[i]);
        }

        return lineRenderer;
    }
}
