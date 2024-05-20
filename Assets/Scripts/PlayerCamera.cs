using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    //public float degreesX;
    //public float degreesY;

    private Quaternion rotationX;
    private Quaternion rotationY;

    /*private bool isSnapping = false;
    private float initSnapTime;
    private float snapDuration;
    private bool smoothStepToDegrees;
    private float snapTargetX;
    private float snapTargetY;
    private float initSnapDegreesX;
    private float initSnapDegreesY;*/
    
    // Start is called before the first frame update
    void Start()
    {
        rotationX = Quaternion.AngleAxis(transform.rotation.eulerAngles.x, Vector3.up);
        rotationY = Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.right);
    }

    void Update()
    {
        UpdateCamera(Time.deltaTime);
    }

    void UpdateCamera(float deltaTime)
    {
        UpdateInput(deltaTime);
        
        /*if (isSnapping)
        {
            float num = Mathf.InverseLerp(initSnapTime, initSnapTime + snapDuration, Time.unscaledTime);

            if (smoothStepToDegrees)
            {
                num = Mathf.SmoothStep(0f, 1f, num);
            }

            if (num >= 1f)
            {
                isSnapping = false;
            }

            degreesX = Mathf.Lerp(initSnapDegreesX, snapTargetX, num);
            degreesY = Mathf.Lerp(initSnapDegreesY, snapTargetY, num);
        }*/
        
        UpdateRotation();
    }

    void UpdateInput(float deltaTime)
    {
        /*if (isSnapping)
        {
            return;
        }*/
        
        Vector2 sensitivityScale = Vector2.one;

        
        if (Time.timeScale > 1f)
        {
            sensitivityScale /= Time.timeScale;
        }
        
        //degreesX
        float mouseX = Input.GetAxis("Mouse X") * 120 * sensitivityScale.x * deltaTime;
        //degreesY +=
        float mouseY = Input.GetAxis("Mouse Y") * 120 * sensitivityScale.y * deltaTime;
        
        
        rotationX *= Quaternion.AngleAxis(mouseX, Vector3.up);
        rotationY *= Quaternion.AngleAxis(mouseY, Vector3.right);
    }

    void UpdateRotation()
    {
        
        //degreesX %= 360f;
        //degreesY %= 360f;
        

        /*if (!isSnapping)
        {
            degreesX = Mathf.Clamp(degreesX, -60f, 60f);
            degreesY = Mathf.Clamp(degreesY, -35f, 80f);
        }*/
        
        

        //rotationX = Quaternion.AngleAxis(degreesX, Vector3.up);
        //rotationY = Quaternion.AngleAxis(degreesY, -Vector3.right);

        Quaternion localRotation = rotationX * rotationY * Quaternion.identity;
        transform.localRotation = localRotation;
    }
    
    
    /*private void SnapToDegreesOverSeconds(float targetX, float targetY, float duration, bool smoothStep = false)
    {
        if (duration < Time.deltaTime)
        {
            degreesX = targetX;
            degreesY = targetY;
            return;
        }

        initSnapTime = Time.unscaledTime;
        snapDuration = duration;
        isSnapping = true;
        smoothStepToDegrees = smoothStep;
        snapTargetX = targetX;
        snapTargetY = targetY;
        initSnapDegreesX = degreesX;
        initSnapDegreesY = degreesY;
    }*/
}
