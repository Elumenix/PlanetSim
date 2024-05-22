using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeTracker : MonoBehaviour
{
    public GravityManager trackedScene;
    private int year = 2024;
    private int day = 1;
    private int hour = 0;
    private int minuet = 0;
    private int second = 0;
    private double lessThanSecond = 0;

    private TextMeshProUGUI textField;
    
    // Start is called before the first frame update
    void Start()
    {
        textField = gameObject.GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (trackedScene.TimeScale)
        {
            case TimeScale.second:
                lessThanSecond += Time.fixedDeltaTime;
                break;
            case TimeScale.minute:
                lessThanSecond += Time.fixedDeltaTime * 60.0;
                break;
            case TimeScale.hour:
                lessThanSecond += Time.fixedDeltaTime * 60.0 * 60.0;
                break;
            case TimeScale.day:
                lessThanSecond += Time.fixedDeltaTime * 60.0 * 60.0 * 24.0;
                break;
            case TimeScale.week:
                lessThanSecond += Time.fixedDeltaTime * 60.0 * 60.0 * 24.0 * 7.0;
                break;
            case TimeScale.month:
                lessThanSecond += Time.fixedDeltaTime * 60.0 * 60.0 * 24.0 * 30.437;
                break;
            case TimeScale.year:
                lessThanSecond += Time.fixedDeltaTime * 60.0 * 60.0 * 24.0 * 365.25;
                break;
        }


        if (lessThanSecond >= 1)
        {
            int wholeSeconds = (int)lessThanSecond; 
            lessThanSecond -= wholeSeconds; 
            second += wholeSeconds; 

            if (second >= 60)
            {
                minuet += second / 60;
                second %= 60;

                if (minuet >= 60)
                {
                    hour += minuet / 60;
                    minuet %= 60;

                    if (hour >= 24)
                    {
                        day += hour / 24;
                        hour %= 24;
                        
                        // Leap year shenanigans : Assumes time step will not be more than a year
                        if ((day > 365 && !IsLeapYear(year)) || (day > 366 && IsLeapYear(year)))
                        {
                            year++;
                            day = 1;
                        }
                    }
                }
            }
        }

        string output = "";
        
        // Figure out the month
        int[] daysInMonth = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
        if (IsLeapYear(year)) {
            daysInMonth[1] = 29;  // February has 29 days in a leap year
        }

        int month = 0;
        int dayTracker = day;
        
        while (dayTracker > daysInMonth[month]) {
            dayTracker -= daysInMonth[month];
            month++;
        }
        
        output = string.Format("{0:00}/{1:00}/{2}", month + 1, dayTracker, year);

        // Get Time zone hour
        string specifier;
        int specifiedHour;

        if (hour >= 12)
        {
            specifier = "PM";
        }
        else
        {
            specifier = "AM";
        }

        if (hour is 0 or 12)
        {
            // 12 to 1 am, an exception to modulus
            specifiedHour = 12;
        }
        else
        {
            // specific hour for am/pm format
            specifiedHour = hour % 12;
        }

        output = string.Format("<mspace=0.75em>{0}</mspace>\n<mspace=0.75em>{1:00}:{2:00} {3}</mspace>", output, specifiedHour, minuet,
            specifier);
        
        // Overwrite current textMesh value
        textField.text = output;
    }
    
    bool IsLeapYear(int year)
    {
        return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
    }
}
