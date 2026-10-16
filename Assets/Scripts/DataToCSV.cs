using System.Collections.Generic;
using System.Linq;
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

        if (!isMultiplayer)
        {
            // --- ЗАПИСЬ ДЛЯ ОДИНОЧНОЙ ИГРЫ (БЕЗ ИЗМЕНЕНИЙ) ---
            int scissorsKills = 0;
            ScissorsCombo scissors = Object.FindFirstObjectByType<ScissorsCombo>(FindObjectsInactive.Include);
            if (scissors != null) scissorsKills = scissors.cuttedEnemies;

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
            // --- ЗАПИСЬ ДЛЯ МУЛЬТИПЛЕЕРА (РАЗДЕЛЬНАЯ СТАТИСТИКА ДЛЯ ОБОИХ ИГРОКОВ) ---
            string multiPath = Path.Combine(Application.persistentDataPath, "DefeatAnalyticsMultiplayer.csv");

            string player1 = PlayerPrefs.GetString("Player1Name", "Player1");
            string player2 = PlayerPrefs.GetString("Player2Name", "Player2");

            // Счетчик совместных сессий для этой пары игроков
            string pairSessionKey = $"MultiSessionCount_{player1}_{player2}";
            int sessionNumber = PlayerPrefs.GetInt(pairSessionKey, 0) + 1;
            PlayerPrefs.SetInt(pairSessionKey, sessionNumber);
            PlayerPrefs.Save();

            // Поиск обоих игроков на сцене
            Player p1 = null;
            Player p2 = null;
            Player[] allPlayers = Object.FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Player p in allPlayers)
            {
                if (p.playerIndex == 2 || p.name.Contains("2"))
                    p2 = p;
                else
                    p1 = p;
            }

            if (p1 == null && allPlayers.Length > 0) p1 = allPlayers[0];
            if (p2 == null && allPlayers.Length > 1) p2 = allPlayers[1];

            // Статистика Игрока 1 (WASD)
            int p1ExpCollected = p1 != null ? p1.ExpCollected : 0;
            int p1ExpGiven = p1 != null ? p1.ExpGiven : 0;
            ScissorsCombo p1Scissors = p1 != null ? p1.GetComponentInChildren<ScissorsCombo>(true) : null;
            int p1ScissorsKills = p1Scissors != null ? p1Scissors.cuttedEnemies : 0;
            Magnet p1Magnet = p1 != null ? p1.GetComponentInChildren<Magnet>(true) : null;
            int p1SprayShots = p1Magnet != null ? p1Magnet.ShotsFired : 0;

            // Статистика Игрока 2 (Стрелочки)
            int p2ExpCollected = p2 != null ? p2.ExpCollected : 0;
            int p2ExpGiven = p2 != null ? p2.ExpGiven : 0;
            ScissorsCombo p2Scissors = p2 != null ? p2.GetComponentInChildren<ScissorsCombo>(true) : null;
            int p2ScissorsKills = p2Scissors != null ? p2Scissors.cuttedEnemies : 0;
            Magnet p2Magnet = p2 != null ? p2.GetComponentInChildren<Magnet>(true) : null;
            int p2SprayShots = p2Magnet != null ? p2Magnet.ShotsFired : 0;

            int totalScissorsKills = p1ScissorsKills + p2ScissorsKills;

            string header = "Timestamp,SessionNumber,Player1Name,Player2Name,P1_ExpCollected,P1_ExpGiven,P1_ScissorsKills,P1_SprayShots,P2_ExpCollected,P2_ExpGiven,P2_ScissorsKills,P2_SprayShots,Top,Right,Bottom,Left,TotalMissed,MowerKills,ScissorsKills,LightMissed,BlueMissed,RedMissed,YellowMissed\n";

            if (!File.Exists(multiPath))
            {
                File.WriteAllText(multiPath, header);
            }
            else
            {
                // Миграция старого заголовка при необходимости
                string firstLine = File.ReadLines(multiPath).FirstOrDefault();
                if (firstLine != null && !firstLine.Contains("P1_ExpCollected"))
                {
                    string[] existingLines = File.ReadAllLines(multiPath);
                    List<string> upgraded = new List<string> { header.TrimEnd('\n') };
                    for (int i = 1; i < existingLines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(existingLines[i])) continue;
                        string[] parts = existingLines[i].Split(',');
                        if (parts.Length >= 15)
                        {
                            // Вставляем 0 для 8 новых колонок
                            string upRow = $"{parts[0]},{parts[1]},{parts[2]},{parts[3]},0,0,0,0,0,0,0,0,{parts[4]},{parts[5]},{parts[6]},{parts[7]},{parts[8]},{parts[9]},{parts[10]},{parts[11]},{parts[12]},{parts[13]},{parts[14]}";
                            upgraded.Add(upRow);
                        }
                        else
                        {
                            upgraded.Add(existingLines[i]);
                        }
                    }
                    File.WriteAllLines(multiPath, upgraded);
                }
            }

            string row = $"{System.DateTime.Now},{sessionNumber},{player1},{player2},{p1ExpCollected},{p1ExpGiven},{p1ScissorsKills},{p1SprayShots},{p2ExpCollected},{p2ExpGiven},{p2ScissorsKills},{p2SprayShots},{stats["TOP"]},{stats["RIGHT"]},{stats["BOTTOM"]},{stats["LEFT"]},{stats["TOTAL"]},{mowerKills},{totalScissorsKills},{missedColors[0]},{missedColors[1]},{missedColors[2]},{missedColors[3]}\n";
            File.AppendAllText(multiPath, row);

            Debug.Log($"<color=green>[Multiplayer] Session: {sessionNumber} Saved! P1 '{player1}': ExpCollected={p1ExpCollected}, ExpGiven={p1ExpGiven}, Scissors={p1ScissorsKills}, Shots={p1SprayShots} | P2 '{player2}': ExpCollected={p2ExpCollected}, ExpGiven={p2ExpGiven}, Scissors={p2ScissorsKills}, Shots={p2SprayShots}</color>");
            Debug.Log($"<color=yellow>FILE PATH: {multiPath}</color>");
        }
    }
}