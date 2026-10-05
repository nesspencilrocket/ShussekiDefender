using UnityEngine;

/// <summary>
/// タイトル画面の進行役。
/// 【重要】クラス名はファイル名 StartMenuManager と一致させること。
/// Unity の規約であり、Library キャッシュ再構築時に解決できなくなるのを防ぐ。
///
/// 「妨害開始」「成績確認」「設定」の遷移は、各ボタン画像の SpriteButton が
/// 直接シーンを読み込む。ここでは画面に入ったときの後始末だけを行う。
/// </summary>
public class StartMenuManager : MonoBehaviour
{
    void Start()
    {
        // 敗北・クリア時に止めたまま戻ってくるとタイトルが固まる
        GameSpeed.Resume();
    }

    /// <summary>
    /// ゲームを終了する。いまは呼び出し元が無いが、ビルドはフルスクリーンで起動し
    /// 終了する手段が Alt+F4 しかないため、「終了」ボタンを置くときに使う。
    /// </summary>
    public void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
