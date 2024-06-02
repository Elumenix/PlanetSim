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
    private bool previouslyReversed;
    
    // Start is called before the first frame update
    void Start()
    {
        textField = gameObject.GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Reversed time is tracked with negative numbers while normal time is with positive numbers
        // A change needs to happen on reversal or the first 48 hours won't be calculated correctly
        if (previouslyReversed != trackedScene.reversed)
        {
            if (trackedScene.reversed) // Normal to Reverse time
            {
                ChangeToReverse();
            }
            else // Reverse to Normal Time
            {
                ChangeToNormal();
            }

            // Update to share value
            previouslyReversed = trackedScene.reversed;
        }
        
        // this deltaTime also handles whether time is reversed or not
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
                    if ((day > 365 && !IsLeapYear()) || (day > 366 && IsLeapYear()))
                    {
                        year++;
                        day = 1;
                    }
                    else if (trackedScene.reversed && (day < -365 && !IsLeapYear()) || (day < -366 && IsLeapYear())) // Time is reversed 
                    {
                        year--;
                        day = -1; // Last day of the year
                    }
                }
            }
        }

        // Figure out the month
        int[] daysInMonth = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
        if (IsLeapYear()) {
            daysInMonth[1] = 29;  // February has 29 days in a leap year
        }

        int month = 0;
        int dayTracker = day;

        if (trackedScene.reversed) // Use relative positive day for calculations
        {
            dayTracker = (IsLeapYear() ? 366 : 365) + day;
        }
        
        while (dayTracker > daysInMonth[month]) {
            dayTracker -= daysInMonth[month];
            month++;
        }
        
        string output = string.Format("{0:00}/{1:00}/{2}", month + 1, dayTracker, year);

        // Get Time zone hour, and handle potential of negative hour
        int displayHour = hour < 0 ? 24 + hour : hour;

        var specifier = displayHour >= 12 ? "PM" : "AM";
        
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
    
    bool IsLeapYear()
    {
        return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
    }

    private void ChangeToReverse()
    {
        // Changes all positive values to associated negative values
        lessThanSecond -= 1;
        second -= 60;
        minute -= 60;
        hour -= 24;
        day -= IsLeapYear() ? 366 : 365;
    }

    private void ChangeToNormal()
    {
        // All calculations change from negative time values to associated positive values
        lessThanSecond += 1;
        second += 60;
        minute += 60;
        hour += 24;
        day += IsLeapYear() ? 366 : 365;
        // Year is never inverted, unless making it to BC, in which case it could be considered correct
    }
}
