using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    // Use a constant for the scene name to avoid magic strings.
    private const string StageSelectSceneName = "StageSelect";

    /// <summary>
    /// Called when the "Start Game" button is pressed.
    /// </summary>
    public void OnStartButtonClicked()
    {
        Debug.Log("Loading the Stage Select screen.");
        SceneManager.LoadScene(StageSelectSceneName);
    }

    /// <summary>
    /// Called when the "Options" button is pressed.
    /// </summary>
    public void OnOptionsButtonClicked()
    {
        Debug.Log("Options button was pressed.");
        // Add your options menu logic here.
    }

    /// <summary>
    /// Called when the "Quit" button is pressed.
    /// </summary>
    public void OnQuitButtonClicked()
    {
        Debug.Log("Quitting game.");
        // Handle quitting differently in the editor vs. a built application.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}