using System.Globalization;
using UnityEngine;

/// <summary>
/// ゲーム進行速度の一元管理。
///
/// これまで Time.timeScale を 3 ファイル 9 箇所で直接書いていたため、
/// 倍速を入れると「敗北 → タイトル → 再挑戦」で設定が 1 倍に戻ってしまう。
/// 速度の保持と適用をここに集約し、他は Pause / Resume だけを呼ぶ。
///
/// 【表示上の速度と、実際の速度】
/// 画面に出す ×1.0 は、実際の Time.timeScale = 0.8（BASE_SCALE）。
/// プレイヤーが選ぶのは、その ×1.0 を起点にした倍率（×0.3 / 0.5 / 0.8 / 1.0 / 1.2）で、
/// 実際の速度は BASE_SCALE × 倍率になる。
///
/// 【使い分け】
///   Pause()   … 勝敗表示など、進行を止めたいとき
///   Resume()  … 止めた状態から戻すとき、シーン開始時、遷移の直前
///   Select()  … プレイヤーが速度を選んだとき
///   Next()    … ボタンで次の速度へ切り替えるとき
/// </summary>
public static class GameSpeed
{
    /// <summary>表示上の ×1.0 に当たる、実際の Time.timeScale</summary>
    public const float BASE_SCALE = 0.8f;

    /// <summary>選べる倍率（BASE_SCALE を 1 とした割合）。遅い順</summary>
    public static readonly float[] Options = { 0.3f, 0.5f, 0.8f, 1.0f, 1.2f };

    /// <summary>標準の倍率</summary>
    public const float NORMAL = 1f;

    // 以前の保存値（GAME_SPEED）は実際の Time.timeScale そのものだったので、意味が違う。
    // 取り違えないよう、別の名前で保存する
    private const string SAVE_KEY = "GAME_SPEED_MULTIPLIER";

    // 未読み込みを表す番兵。PlayerPrefs は初回アクセス時にだけ読む
    private static float selected = -1f;

    /// <summary>プレイヤーが選んだ倍率（表示上の速度）。ステージをまたいで保持される</summary>
    public static float Selected
    {
        get
        {
            if (selected < 0f)
            {
                selected = Normalize(PlayerPrefs.GetFloat(SAVE_KEY, NORMAL));
            }
            return selected;
        }
    }

    /// <summary>いま実際に掛ける Time.timeScale</summary>
    public static float TimeScale => BASE_SCALE * Selected;

    /// <summary>進行が止まっているか</summary>
    public static bool IsPaused => Time.timeScale == 0f;

    /// <summary>ボタンなどに出す文字（例：×0.5）</summary>
    public static string Label(float multiplier)
    {
        return "×" + multiplier.ToString("0.0", CultureInfo.InvariantCulture);
    }

    /// <summary>いま選ばれている速度の文字</summary>
    public static string SelectedLabel => Label(Selected);

    /// <summary>
    /// 速度（倍率）を選ぶ。保存もここで行う。
    /// 止まっている最中に呼ばれても、勝手に再開はしない。
    /// </summary>
    public static void Select(float multiplier)
    {
        selected = Normalize(multiplier);

        PlayerPrefs.SetFloat(SAVE_KEY, selected);
        PlayerPrefs.Save();

        if (!IsPaused) Time.timeScale = TimeScale;
    }

    /// <summary>選べる速度を遅い順に 1 つ進める。いちばん速い次は、いちばん遅いものに戻る</summary>
    public static void Next()
    {
        int index = IndexOf(Selected);
        Select(Options[(index + 1) % Options.Length]);
    }

    /// <summary>進行を止める（勝敗表示など）</summary>
    public static void Pause()
    {
        Time.timeScale = 0f;
    }

    /// <summary>選ばれている速度で再開する</summary>
    public static void Resume()
    {
        Time.timeScale = TimeScale;
    }

    // 選べる倍率のうち、いちばん近いものに丸める。想定外の値が保存されていても壊れない
    private static float Normalize(float multiplier)
    {
        return Options[IndexOf(multiplier)];
    }

    private static int IndexOf(float multiplier)
    {
        int best = 0;
        float bestGap = float.MaxValue;
        for (int i = 0; i < Options.Length; i++)
        {
            float gap = Mathf.Abs(Options[i] - multiplier);
            if (gap < bestGap)
            {
                bestGap = gap;
                best = i;
            }
        }
        return best;
    }

#if UNITY_EDITOR
    /// <summary>テスト用。保存された速度設定を消して標準（×1.0）へ戻す</summary>
    [UnityEditor.MenuItem("Shusseki/ゲーム速度をリセット")]
    private static void ResetSpeed()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        selected = -1f;
        Debug.Log("ゲーム速度を標準（×1.0）に戻しました");
    }
#endif
}
