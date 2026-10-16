using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class DataToCSV : MonoBehaviour
{
    public void LogDefeatData(Dictionary<string, int> stats, int[] missedColors)
    {
        bool isMultiplayer = PlayerPrefs.GetInt("IsMultiplayer", 0) == 1 ||
                             SceneManager.GetActiveScene().name == "SampleSceneMultiplayer";

        int mowerKills = 0;
        MowerItself mower = Object.FindFirstObjectByType<MowerItself>(FindObjectsInactive.Include);
        if (mower != null) mowerKills = mower.smashedEnemies;

        int scissorsKills = 0;
        ScissorsCombo scissors = Object.FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
        if (scissors != null) scissorsKills = scissors.cuttedEnemies;

        if (!isMultiplayer)
        {
            // --- ЗАПИСЬ ДЛЯ ОДИНОЧНОЙ ИГРЫ ---
            string path = Path.Combine(Application.persistentDataPath, "DefeatAnalytics.csv");
            string playerName = PlayerPrefs.GetString("CurrentPlayerName", "Unknown");

            string sessionKey = "SessionCount_" + playerName;
            int sessionNumber = PlayerPrefs.GetInt(sessionKey, 0) + 1;
            PlayerPrefs.SetInt(sessionKey, sessionNumber);
            PlayerPrefs.Save();

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "Timestamp,SessionNumber,PlayerName,Top,Right,Bottom,Left,TotalMissed,MowerKills,ScissorsKills,LightMissed,BlueMissed,RedMissed,YellowMissed\n");
            }

            string row = $"{System.DateTime.Now},{sessionNumber},{playerName},{stats["TOP"]},{stats["RIGHT"]},{stats["BOTTOM"]},{stats["LEFT"]},{stats["TOTAL"]},{mowerKills},{scissorsKills},{missedColors[0]},{missedColors[1]},{missedColors[2]},{missedColors[3]}\n";
            File.AppendAllText(path, row);

            Debug.Log($"<color=cyan>[Singleplayer] Telemetry Saved! Session: {sessionNumber} | Player: {playerName} | Mower: {mowerKills} | Scissors: {scissorsKills}</color>");
            Debug.Log($"<color=yellow>FILE PATH: {path}</color>");
        }
        else
        {
            // --- ЗАПИСЬ ДЛЯ СПЛИТ-СКРИНА (ОТДЕЛЬНЫЙ CSV) ---
            string multiPath = Path.Combine(Application.persistentDataPath, "DefeatAnalyticsMultiplayer.csv");

            string player1 = PlayerPrefs.GetString("Player1Name", "Player1");
            string player2 = PlayerPrefs.GetString("Player2Name", "Player2");

            // Счетчик совместных сессий для этой пары игроков
            string pairSessionKey = $"MultiSessionCount_{player1}_{player2}";
            int sessionNumber = PlayerPrefs.GetInt(pairSessionKey, 0) + 1;
            PlayerPrefs.SetInt(pairSessionKey, sessionNumber);
            PlayerPrefs.Save();

            if (!File.Exists(multiPath))
            {
                File.WriteAllText(multiPath, "Timestamp,SessionNumber,Player1Name,Player2Name,Top,Right,Bottom,Left,TotalMissed,MowerKills,ScissorsKills,LightMissed,BlueMissed,RedMissed,YellowMissed\n");
            }

            string row = $"{System.DateTime.Now},{sessionNumber},{player1},{player2},{stats["TOP"]},{stats["RIGHT"]},{stats["BOTTOM"]},{stats["LEFT"]},{stats["TOTAL"]},{mowerKills},{scissorsKills},{missedColors[0]},{missedColors[1]},{missedColors[2]},{missedColors[3]}\n";
            File.AppendAllText(multiPath, row);

            Debug.Log($"<color=green>[Multiplayer] Telemetry Saved! Session: {sessionNumber} | P1: {player1} & P2: {player2} | Mower: {mowerKills} | Scissors: {scissorsKills}</color>");
            Debug.Log($"<color=yellow>FILE PATH: {multiPath}</color>");
        }
    }
}