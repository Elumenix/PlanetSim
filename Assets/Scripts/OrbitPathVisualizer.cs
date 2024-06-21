using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OrbitPathVisualizer : MonoBehaviour
{
    public GravityManager simulation;
    private LineRenderer moonRenderer;
    private List<LineRenderer> sunBound;
    
    // Start is called before the first frame update
    void Start()
    {
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
        moonRenderer.widthMultiplier = 0.125f;
    }

    private void LateUpdate()
    {
        // Because everything, including the sun, moves, and orbits are drawn relative to what is being orbited:
        // All lines need to be moved to line up with their major body
        
        // Line up with Earth
        moonRenderer.gameObject.transform.position = simulation.planets[3].transform.position;

        // Line up with Sun
        foreach (LineRenderer lineRenderer in sunBound)
        {
            lineRenderer.gameObject.transform.position = simulation.planets[0].transform.position;
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
        //lineRenderer.sortingOrder = 0; // Not quite sure how this one works yet
        
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
