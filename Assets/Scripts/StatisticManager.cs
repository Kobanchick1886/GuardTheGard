using UnityEngine;
using System.IO;
using System.Diagnostics;

public class StatisticsManager : MonoBehaviour
{
    [Header("Настройки файла")]
    public string exeFileName = "analytics.exe"; // Впиши сюда точное имя своего файла

    void Start()
    {
        // Путь, где файл должен лежать в итоге (AppData/LocalLow/...)
        string targetPath = Path.Combine(Application.persistentDataPath, exeFileName);

        // Проверяем, есть ли уже файл в AppData (первый ли это запуск)
        if (!File.Exists(targetPath))
        {
            // Путь, откуда брать исходник (наша папка StreamingAssets)
            string sourcePath = Path.Combine(Application.streamingAssetsPath, exeFileName);

            // Если исходник существует, копируем его в AppData
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, targetPath);
                UnityEngine.Debug.Log("Экзешка успешно скопирована в AppData!");
            }
            else
            {
                UnityEngine.Debug.LogError("Файл не найден в StreamingAssets!");
            }
        }
    }

    // Этот метод нужно выбрать в OnClick() кнопки "Statistics"
    public void OpenStatisticsApp()
    {
        string targetPath = Path.Combine(Application.persistentDataPath, exeFileName);

        if (File.Exists(targetPath))
        {
            // Создаем специальные настройки для запуска процесса
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = targetPath;

            // ЖЕСТКО ЗАДАЕМ рабочую папку, чтобы экзешник искал csv-файлы именно там
            startInfo.WorkingDirectory = Application.persistentDataPath;

            // Запускаем с этими настройками
            Process.Start(startInfo);
        }
        else
        {
            UnityEngine.Debug.LogError("Невозможно запустить: файл отсутствует в AppData.");
        }
    }
}