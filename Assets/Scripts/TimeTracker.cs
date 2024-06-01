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
    private int minute = 0;
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
        // Also handles whether time is reversed or not
        double timeDelta = Time.fixedDeltaTime * (trackedScene.reversed ? -1 : 1);
        
        switch (trackedScene.TimeScale)
        {
            case TimeScale.second:
                lessThanSecond += timeDelta;
                break;
            case TimeScale.minute:
                lessThanSecond += timeDelta * 60.0;
                break;
            case TimeScale.hour:
                lessThanSecond += timeDelta * 60.0 * 60.0;
                break;
            case TimeScale.day:
                lessThanSecond += timeDelta * 60.0 * 60.0 * 24.0;
                break;
            case TimeScale.week:
                lessThanSecond += timeDelta * 60.0 * 60.0 * 24.0 * 7.0;
                break;
            case TimeScale.month:
                lessThanSecond += timeDelta * 60.0 * 60.0 * 24.0 * 30.437;
                break;
            case TimeScale.year:
                lessThanSecond += timeDelta * 60.0 * 60.0 * 24.0 * 365.25;
                break;
        }

        // Int casting essentially takes the place of the modulus and division operations here
        second += (int)lessThanSecond;
        lessThanSecond -= (int)lessThanSecond;

        while (Mathf.Abs(second) >= 60)
        {
            // From here on, the operation patterns work regardless of if time is reversed (positive or negative values)
            int minuteChange = second / 60;
            second -= minuteChange * 60;
            minute += minuteChange;

            while (Mathf.Abs(minute) >= 60)
            {
                int hourChange = minute / 60;
                minute -= hourChange * 60;
                hour += hourChange;

                // The reverse time condition for this almost drove me insane. I still don't understand why it works yet
                while (hour is >= 24 or <= -25)
                {
                    int dayChange = hour / 24;
                    hour -= dayChange * 24;
                    day += dayChange;

                    // Leap year shenanigans : Assumes time step will not be more than a year
                    // This will always be true because timescale is used for multiple years, which does them separately
                    if ((day > 365 && !IsLeapYear(year)) || (day > 366 && IsLeapYear(year)))
                    {
                        year++;
                        day = 1;
                    }
                    else if (day < 1) // Time is reversed 
                    {
                        year--;
                        day = IsLeapYear(year) ? 366 : 365;
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
        
        // handles positive day values : Forward Time
        while (dayTracker > daysInMonth[month]) {
            dayTracker -= daysInMonth[month];
            month++;
        }
        
        // handles negative day values : Reverse Time
        while (dayTracker <= 0) {
            month--;
            if (month < 0) {
                month = 11;
                year--;
                if (IsLeapYear(year)) {
                    daysInMonth[1] = 29;
                } else {
                    daysInMonth[1] = 28;
                }
            }
            dayTracker += daysInMonth[month];
        }
        
        output = string.Format("{0:00}/{1:00}/{2}", month + 1, dayTracker, year);

        // Get Time zone hour, and handle potential of negative hour
        string specifier;
        int displayHour = hour < 0 ? 24 + hour : hour;

        if (displayHour >= 12)
        {
            specifier = "PM";
        }
        else
        {
            specifier = "AM";
        }
        
        if (displayHour is 0 or 12) 
        {
            // 12 to 1 am, an exception to modulus
            displayHour = 12;
        }
        else
        {
            // specific hour for am/pm format
            displayHour %= 12;
        }
        
        // Handles negative minute values
        int displayMinute = minute < 0 ? 60 + minute : minute;


        output = string.Format("<mspace=0.75em>{0}</mspace>\n<mspace=0.75em>{1:00}:{2:00} {3}</mspace>", output,
            displayHour, displayMinute, specifier);
        
        // Overwrite current textMesh value
        textField.text = output;
    }
    
    bool IsLeapYear(int year)
    {
        return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
    }
}
