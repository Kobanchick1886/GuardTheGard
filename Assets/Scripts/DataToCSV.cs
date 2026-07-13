using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DataToCSV : MonoBehaviour
{
    // Добавили второй аргумент: int[] missedColors
    public void LogDefeatData(Dictionary<string, int> stats, int[] missedColors)
    {
        string path = Path.Combine(Application.persistentDataPath, "DefeatAnalytics.csv");

        // Читаем имя игрока
        string playerName = PlayerPrefs.GetString("CurrentPlayerName", "Unknown");

        int mowerKills = 0;
        MowerItself mower = Object.FindFirstObjectByType<MowerItself>(FindObjectsInactive.Include);
        if (mower != null) mowerKills = mower.smashedEnemies;

        int scissorsKills = 0;
        ScissorsCombo scissors = Object.FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
        if (scissors != null) scissorsKills = scissors.cuttedEnemies;

        // Если файла нет — создаем его с заголовками (добавили 4 колонки под цвета в конец)
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "Timestamp,PlayerName,Top,Right,Bottom,Left,TotalMissed,MowerKills,ScissorsKills,LightMissed,BlueMissed,RedMissed,YellowMissed\n");
        }

        // Записываем данные. Индексы массива строго совпадают с твоей задумкой:
        // missedColors[0] = Light, missedColors[1] = Blue, missedColors[2] = Red, missedColors[3] = Yellow
        string row = $"{System.DateTime.Now},{playerName},{stats["TOP"]},{stats["RIGHT"]},{stats["BOTTOM"]},{stats["LEFT"]},{stats["TOTAL"]},{mowerKills},{scissorsKills},{missedColors[0]},{missedColors[1]},{missedColors[2]},{missedColors[3]}\n";

        File.AppendAllText(path, row);

        Debug.Log($"<color=cyan>Telemetry Saved for {playerName}! Mower: {mowerKills} | Scissors: {scissorsKills}</color>");
        Debug.Log($"<color=yellow>FILE PATH: {path}</color>");
    }
}