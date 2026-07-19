using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DataToCSV : MonoBehaviour
{
    public void LogDefeatData(Dictionary<string, int> stats, int[] missedColors)
    {
        string path = Path.Combine(Application.persistentDataPath, "DefeatAnalytics.csv");

        // Читаем имя игрока
        string playerName = PlayerPrefs.GetString("CurrentPlayerName", "Unknown");

        // Логика нумерации сессий
        string sessionKey = "SessionCount_" + playerName;
        int sessionNumber = PlayerPrefs.GetInt(sessionKey, 0) + 1;
        PlayerPrefs.SetInt(sessionKey, sessionNumber);
        PlayerPrefs.Save();

        int mowerKills = 0;
        MowerItself mower = Object.FindFirstObjectByType<MowerItself>(FindObjectsInactive.Include);
        if (mower != null) mowerKills = mower.smashedEnemies;

        int scissorsKills = 0;
        ScissorsCombo scissors = Object.FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
        if (scissors != null) scissorsKills = scissors.cuttedEnemies;

        // Изменили порядок в заголовках: Timestamp -> SessionNumber -> PlayerName
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "Timestamp,SessionNumber,PlayerName,Top,Right,Bottom,Left,TotalMissed,MowerKills,ScissorsKills,LightMissed,BlueMissed,RedMissed,YellowMissed\n");
        }

        // Изменили порядок переменных в строке записи
        string row = $"{System.DateTime.Now},{sessionNumber},{playerName},{stats["TOP"]},{stats["RIGHT"]},{stats["BOTTOM"]},{stats["LEFT"]},{stats["TOTAL"]},{mowerKills},{scissorsKills},{missedColors[0]},{missedColors[1]},{missedColors[2]},{missedColors[3]}\n";

        File.AppendAllText(path, row);

        Debug.Log($"<color=cyan>Telemetry Saved! Session: {sessionNumber} | Player: {playerName} | Mower: {mowerKills} | Scissors: {scissorsKills}</color>");
        Debug.Log($"<color=yellow>FILE PATH: {path}</color>");
    }
}