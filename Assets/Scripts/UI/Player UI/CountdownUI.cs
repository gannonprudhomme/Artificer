using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#nullable enable

public class CountdownUI : MonoBehaviour {
    public TextMeshProUGUI? mainTimeText; // Minutes & seconds
    public TextMeshProUGUI? millisecondsText; // The small time to the top right of the main time

    // Update is called once per frame
    void Update(){
        // Get the time in minutes & seconds into a string
        // the minutes & seconds should always be to two digits
        string minutes = ((int)Time.time / 60).ToString("D2");
        string seconds = ((int)Time.time % 60).ToString("D2");
            
        // Get the milliseconds into a string, but only to two digits
        string milliseconds = ((int) (Time.time * 100) % 100).ToString("D2");

        // Set it on the text
        mainTimeText!.text = $"{minutes}:{seconds}";
        millisecondsText!.text = milliseconds;
    }
}
