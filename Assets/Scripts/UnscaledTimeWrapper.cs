using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnscaledTimeWrapper : MonoBehaviour
{
    private Material mat;
    private static readonly int UnscaledTime = Shader.PropertyToID("_UnscaledTime");

    // Start is called before the first frame update
    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        // Because increasing time means adjusting timescale, shader animations look weird unless I do this
        mat.SetFloat(UnscaledTime, Time.unscaledTime);
    }
}
