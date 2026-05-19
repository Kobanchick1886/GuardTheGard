using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DataToCSV : MonoBehaviour
{
    public void LogDefeatData(Dictionary<string, int> stats)
    {
        // This targets %userprofile%\AppData\LocalLow\[CompanyName]\[ProjectName]\DefeatAnalytics.csv
        string path = Path.Combine(Application.persistentDataPath, "DefeatAnalytics.csv");

        int mowerKills = 0;
        MowerItself mower = Object.FindFirstObjectByType<MowerItself>(FindObjectsInactive.Include);
        if (mower != null) mowerKills = mower.smashedEnemies;

        int scissorsKills = 0;
        ScissorsCombo scissors = Object.FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
        if (scissors != null) scissorsKills = scissors.cuttedEnemies;

        if (!File.Exists(path))
        {
            File.WriteAllText(path, "Timestamp,Top,Right,Bottom,Left,TotalMissed,MowerKills,ScissorsKills\n");
        }

        string row = $"{System.DateTime.Now},{stats["TOP"]},{stats["RIGHT"]},{stats["BOTTOM"]},{stats["LEFT"]},{stats["TOTAL"]},{mowerKills},{scissorsKills}\n";

        File.AppendAllText(path, row);

        // Print the EXACT clickable path in the console so you can find it easily
        Debug.Log($"<color=cyan>Telemetry Saved! Mower Kills: {mowerKills} | Scissors Kills: {scissorsKills}</color>");
        Debug.Log($"<color=yellow>FILE PATH: {path}</color>");
    }
}