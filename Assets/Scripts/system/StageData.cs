using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ 1 枚分の「ルール」をまとめたアセット。
/// シーンには背景・Node・経路といった「配置」だけを置き、
/// 数値はすべてここに集約することで、6 枚に増えても設定が分岐しない。
/// </summary>
[CreateAssetMenu(menuName = "Shusseki/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("見せ方")]
    [Tooltip("1〜6。解放判定に使う")]
    public int stageNumber = 1;

    [Tooltip("画面に出す名前（例：1限目）")]
    public string displayName = "1限目";

    [TextArea(3, 5)]
    [Tooltip("ステージ選択画面に出す説明文")]
    public string description;

    [Tooltip("ステージ選択画面のボタン画像")]
    public Sprite selectButtonImage;

    [Tooltip("読み込むシーン名。Build Settings に登録されていること")]
    public string sceneName = "Stage_1";

    [Header("ルール")]
    [Tooltip("出現させる敵の波表。Spawner がここから読む")]
    public List<WaveData> waves = new List<WaveData>();

    [Tooltip("この秒数を耐え切ればクリア")]
    public float clearTime = 30f;

    [Tooltip("この数だけ敵に通過されると敗北")]
    public int maxEnemyPasses = 50;

    [Tooltip("ステージ開始時の所持コイン")]
    public int initialCoin = 100;

    [Header("結果の区分")]
    [Tooltip("スコアの境界。低い順に 訓告 / 厳重注意 / 1週間停学 / 無期限停学 / 退学処分。"
           + "妨害する側なので、重い処分ほど良い結果")]
    public int[] rankThresholds = new int[] { 0, 800, 1400, 2000, 2600 };

    [Header("演出")]
    public AudioClip bgm;

    [Tooltip("背景の色調。1限目=朝、6限目=夕、のように時限で変える（Phase 4 で使用）")]
    public Color backgroundTint = Color.white;

    // ───── 処分の判定 ─────
    //
    // リザルト画面と成績確認画面の両方が同じ判定を必要とするため、
    // 閾値を持っている StageData 側に置いてある。
    // GameManager に private で持たせていた頃は、成績画面から呼べなかった。

    /// <summary>
    /// 処分の軽い順。rankThresholds と添字を対応させる。
    /// プレイヤーは出席を妨害する側なので、重い処分ほど良い結果。
    /// </summary>
    public static readonly string[] Ranks =
        { "訓告", "厳重注意", "1週間停学", "無期限停学", "退学処分" };

    /// <summary>スコアから処分の重さを決める。</summary>
    public string RankOf(int score)
    {
        if (rankThresholds == null || rankThresholds.Length == 0)
        {
            return Ranks[Ranks.Length - 1];
        }

        int n = Mathf.Min(rankThresholds.Length, Ranks.Length);
        for (int i = n - 1; i >= 0; i--)
        {
            if (score >= rankThresholds[i]) return Ranks[i];
        }
        return Ranks[0];
    }

    /// <summary>
    /// 1 つ上の処分に届くのに必要なスコア。最上位に達していれば -1。
    /// 成績画面で「あと何点で次の処分か」を出すために使う。
    /// </summary>
    public int NextRankScore(int score)
    {
        if (rankThresholds == null) return -1;

        int n = Mathf.Min(rankThresholds.Length, Ranks.Length);
        for (int i = 0; i < n; i++)
        {
            if (score < rankThresholds[i]) return rankThresholds[i];
        }
        return -1;
    }
}
