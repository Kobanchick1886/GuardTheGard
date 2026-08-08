using UnityEngine;
using System.IO;
using System.Diagnostics;

public class StatisticsManager : MonoBehaviour
{
    [Header("Настройки файла")]
    public string exeFileName = "analytics.exe";
    public string internalFolderName = "_internal"; // Название служебной папки PyInstaller

    void Start()
    {
        // 1. Копируем сам analytics.exe
        string targetExePath = Path.Combine(Application.persistentDataPath, exeFileName);
        string sourceExePath = Path.Combine(Application.streamingAssetsPath, exeFileName);

        if (!File.Exists(targetExePath))
        {
            if (File.Exists(sourceExePath))
            {
                File.Copy(sourceExePath, targetExePath);
                UnityEngine.Debug.Log("Экзешка успешно скопирована в AppData!");
            }
            else
            {
                UnityEngine.Debug.LogError("Файл analytics.exe не найден в StreamingAssets!");
            }
        }

        // 2. Копируем папку _internal (со всеми python314.dll и библиотеками)
        string sourceFolderPath = Path.Combine(Application.streamingAssetsPath, internalFolderName);
        string targetFolderPath = Path.Combine(Application.persistentDataPath, internalFolderName);

        if (Directory.Exists(sourceFolderPath) && !Directory.Exists(targetFolderPath))
        {
            UnityEngine.Debug.Log("Начинаем копирование папки _internal в AppData...");
            CopyDirectory(sourceFolderPath, targetFolderPath);
            UnityEngine.Debug.Log("<color=green>Папка _internal успешно скопирована в AppData!</color>");
        }
    }

    // Рекурсивный метод для полного копирования папки со всеми подпапками и файлами
    private void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        // Копируем все файлы в текущей директории
        foreach (string filePath in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(filePath);
            // Пропускаем .meta файлы Unity, если они попали туда
            if (fileName.EndsWith(".meta")) continue;

            string destFilePath = Path.Combine(targetDir, fileName);
            File.Copy(filePath, destFilePath, true);
        }

        // Рекурсивно копируем все подпапки
        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string subDirName = Path.GetFileName(subDir);
            string destSubDir = Path.Combine(targetDir, subDirName);
            CopyDirectory(subDir, destSubDir);
        }
    }

    public void OpenStatisticsApp()
    {
        string targetPath = Path.Combine(Application.persistentDataPath, exeFileName);

        if (File.Exists(targetPath))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = targetPath;
            startInfo.WorkingDirectory = Application.persistentDataPath;

            Process.Start(startInfo);
        }
        else
        {
            UnityEngine.Debug.LogError("Невозможно запустить: файл отсутствует в AppData.");
        }
    }
}