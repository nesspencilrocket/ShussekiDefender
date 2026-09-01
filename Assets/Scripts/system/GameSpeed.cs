using UnityEngine;

/// <summary>
/// ゲーム進行速度の一元管理。
///
/// これまで Time.timeScale を 3 ファイル 9 箇所で直接書いていたため、
/// 倍速を入れると「敗北 → タイトル → 再挑戦」で設定が 1 倍に戻ってしまう。
/// 速度の保持と適用をここに集約し、他は Pause / Resume だけを呼ぶ。
///
/// 【使い分け】
///   Pause()   … 勝敗表示など、進行を止めたいとき
///   Resume()  … 止めた状態から戻すとき、シーン開始時、遷移の直前
///   Select()  … プレイヤーが速度を切り替えたとき
/// </summary>
public static class GameSpeed
{
    public const float NORMAL = 1f;
    public const float FAST = 1.5f;

    private const string SAVE_KEY = "GAME_SPEED";

    // 未読み込みを表す番兵。PlayerPrefs は初回アクセス時にだけ読む
    private static float selected = -1f;

    /// <summary>プレイヤーが選んだ速度。ステージをまたいで保持される</summary>
    public static float Selected
    {
        get
        {
            if (selected < 0f)
            {
                selected = PlayerPrefs.GetFloat(SAVE_KEY, NORMAL);
                // 想定外の値が保存されていても壊れないようにする
                if (!Mathf.Approximately(selected, FAST)) selected = NORMAL;
            }
            return selected;
        }
    }

    /// <summary>いま倍速が選ばれているか。ボタンの見た目切り替えに使う</summary>
    public static bool IsFast => Mathf.Approximately(Selected, FAST);

    /// <summary>進行が止まっているか</summary>
    public static bool IsPaused => Time.timeScale == 0f;

    /// <summary>
    /// 速度を選ぶ。保存もここで行う。
    /// 止まっている最中に呼ばれても、勝手に再開はしない。
    /// </summary>
    public static void Select(float speed)
    {
        selected = Mathf.Approximately(speed, FAST) ? FAST : NORMAL;

        PlayerPrefs.SetFloat(SAVE_KEY, selected);
        PlayerPrefs.Save();

        if (!IsPaused) Time.timeScale = selected;
    }

    /// <summary>等速と倍速を入れ替える。ボタンから呼ぶ</summary>
    public static void Toggle()
    {
        Select(IsFast ? NORMAL : FAST);
    }

    /// <summary>進行を止める（勝敗表示など）</summary>
    public static void Pause()
    {
        Time.timeScale = 0f;
    }

    /// <summary>選ばれている速度で再開する</summary>
    public static void Resume()
    {
        Time.timeScale = Selected;
    }

#if UNITY_EDITOR
    /// <summary>テスト用。保存された速度設定を消して等速へ戻す</summary>
    [UnityEditor.MenuItem("Shusseki/ゲーム速度をリセット")]
    private static void ResetSpeed()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        selected = -1f;
        Debug.Log("ゲーム速度を等速に戻しました");
    }
#endif
}
