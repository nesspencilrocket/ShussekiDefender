using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル画面のボタン処理。
/// 【重要】クラス名はファイル名 StartMenuManager と一致させること。
/// Unity の規約であり、Library キャッシュ再構築時に解決できなくなるのを防ぐ。
/// </summary>
public class StartMenuManager : MonoBehaviour
{
    private const string StageSelectSceneName = "StageSelect";

    void Start()
    {
        // 敗北・クリア時に 0 にした timeScale が残っているとタイトルが固まる
        Time.timeScale = 1f;
    }

    /// <summary>「ゲームスタート」ボタンから呼ぶ</summary>
    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene(StageSelectSceneName);
    }

    /// <summary>「オプション」ボタンから呼ぶ（未実装）</summary>
    public void OnOptionsButtonClicked()
    {
        Debug.Log("Options button was pressed.");
    }

    /// <summary>「終了」ボタンから呼ぶ</summary>
    public void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
