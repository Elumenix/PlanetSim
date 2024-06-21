using System.Collections.Generic;
using UnityEngine;

// This is the class used to save and read orbit paths from json files
// This doesn't affect the positions of planets, just the shape of the line2D in the scene 
[System.Serializable]
public class OrbitPositions
{
    public List<Vector3> positions;
}
