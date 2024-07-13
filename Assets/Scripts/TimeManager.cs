using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public GravityManager trackedScene;
    public TextMeshProUGUI textOutput;

    // This function is used to update the text shown to the user
    void Update()
    {
        // Get timeScale name
        string placeHolder = trackedScene.TimeScale.ToString();
        
        // Capitalize first letter
        placeHolder = char.ToUpper(placeHolder[0]) + placeHolder.Substring(1);

        // Pluralize if necessary
        if (!Mathf.Approximately(Time.timeScale, 1))
        {
            placeHolder += "s";
        }

        placeHolder = Time.timeScale + " " + placeHolder;
        
        // Alter to add negative sign
        if (trackedScene.reversed && Time.timeScale != 0)
        {
            placeHolder = "-" + placeHolder;
        }

        textOutput.text = placeHolder;
    }

    public void smallTimeIncrease()
    {
        if (Time.timeScale < 100 && !trackedScene.reversed)
        {
            Time.timeScale++;
        }
        else if (trackedScene.reversed)
        {
            // Transition to not reversed
            if (Time.timeScale == 0)
            {
                Time.timeScale++;
                trackedScene.reversed = false;
            }
            else
            {
                Time.timeScale--;
            }
        }
    }

    public void smallTimeDecrease()
    {
        if (Time.timeScale > 0)
        {
            if (!trackedScene.reversed)
            {
                Time.timeScale--;
            }
            else if (Time.timeScale < 100)
            {
                Time.timeScale++;
            }
        }
        else if (Time.timeScale == 0)
        {
            Time.timeScale++;
            trackedScene.reversed = true;   
        }
    }

    public void largeTimeIncrease()
    {
        trackedScene.IncreaseTimescale();
    }

    public void largeTimeDecrease()
    {
        trackedScene.DecreaseTimescale();
    }
}
