using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void LoadGameScene(string sceneName)
    {
        // This will stop the current scene and start the new one
        SceneManager.LoadScene(sceneName);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
