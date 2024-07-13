using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
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
            CreateNewPath("Line2DPaths/MercuryOrbit.json"),
            CreateNewPath("Line2DPaths/VenusOrbit.json"),
            CreateNewPath("Line2DPaths/EarthOrbit.json"),
            CreateNewPath("Line2DPaths/MarsOrbit.json"),
            CreateNewPath("Line2DPaths/JupiterOrbit.json"),
            CreateNewPath("Line2DPaths/SaturnOrbit.json"),
            CreateNewPath("Line2DPaths/UranusOrbit.json"),
            CreateNewPath("Line2DPaths/NeptuneOrbit.json")
        };

        moonRenderer = CreateNewPath("Line2DPaths/MoonOrbit.json");
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
        
        // Adjust scaling and position of each orbit so that they look good to the camera
        for (int i = 0; i < sunBound.Count; i++)
        {
            // Line orbits up with the sun first
            LineRenderer lineRenderer = sunBound[i];
            lineRenderer.gameObject.transform.position = orbitCenter;
            
            // Which planet is a bit complex as sun is 0 and moon is 4
            GravitationalBody curPlanet = simulation.planets[i < 3 ? i + 1 : i + 2];
            float distance = Vector3.Distance(curPlanet.transform.position, _camera.transform.position);

            // Special condition for previous orbit while transitioning between planets
            // Return to normal gradually after switching planets
            if (curPlanet == previousPlanet && control.lerpAmount < 1)
            {
                lineRenderer.widthMultiplier = Mathf.Lerp(.1f,
                    lineRenderer.widthMultiplier = (i < 4 ? 5 : 10) + distance / 2000.0f, control.lerpAmount);
                
                lineRenderer.widthMultiplier +=
                    (1.0f - control.lerpAmount) * ((distance / curPlanet.transform.localScale.x) / 80);
                
                continue;
            }
            
            // Condition most planets go through for determining line width
            if (distance > 2000) // Helps maintain visibility
            {
                lineRenderer.widthMultiplier = (i < 4 ? 5 : 10) + distance / 2000.0f;
            }
            else
            {
                lineRenderer.widthMultiplier = i < 4 ? 5 : 10;
            }
            
            // Special condition for the planet the camera is currently focusing on
            // Shrink width for the planet the camera is targeting
            if (control.target == curPlanet || curPlanet.name == "Earth" && control.target.name == "Moon")
            {
                lineRenderer.widthMultiplier = Mathf.Lerp(lineRenderer.widthMultiplier, .1f, control.lerpAmount);


                lineRenderer.widthMultiplier +=
                    control.lerpAmount * ((distance / curPlanet.transform.localScale.x) / 80);
            }
            
            // Save the last traveled to planet
            if (control.target == curPlanet && control.lerpAmount >= 1)
            {
                previousPlanet = curPlanet;
            }
        }
    }

    private LineRenderer CreateNewPath(string filePath)
    {
        // We'll start with things that don't require the json yet
        // Create a new object in the scene to hold a line renderer and initialize its values
        LineRenderer lineRenderer = (new GameObject("line")).AddComponent<LineRenderer>();
        lineRenderer.positionCount = 361;
        lineRenderer.widthMultiplier = 20;
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false; // Needs to be relative to orbiting body
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.material = orbitMaterial;
        lineRenderer.sortingOrder = -1; // makes render before circles, mostly, lineRenderer is unreliable 
        
        
        string planetName = filePath.Substring(12, filePath.Length - 22); // 12 from start, 10 from end
        Color lineColor = planetName switch
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
        lineRenderer.material.color = lineColor;
        
        // Actual File Loading Things Onward
        
        // WebGL has more restrictions, so I have to go through streamingAssets
        string path = Path.Combine(Application.streamingAssetsPath, filePath);
        string json;

        // Check if this is the Unity editor or a WebGL build
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // If it's a WebGL build, use a Coroutine to read the file and set positions
            StartCoroutine(ReadFile(path, result => {
                json = result;
                OrbitPositions orbitPositions = JsonUtility.FromJson<OrbitPositions>(json);
                // Get info about the json
                int numPoints = orbitPositions.positions.Count;
                lineRenderer.positionCount = numPoints;
        

                for (int i = 0; i < numPoints; i++)
                {
                    lineRenderer.SetPosition(i, orbitPositions.positions[i]);
                }
            }));
            
            // There won't be any data yet when this is returned, but it will be updated when the coroutine finishes
            return lineRenderer;
        }
        else
        {
            // If it's running in the editor, we can read the file directly
            json = File.ReadAllText(path);
            OrbitPositions orbitPositions = JsonUtility.FromJson<OrbitPositions>(json);
            
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
    
    // This is necessary as WebGL uses http requests to load files/json in script
    private IEnumerator ReadFile(string path, Action<string> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get(path);
        yield return www.SendWebRequest();
        callback(www.downloadHandler.text);
    }

}
