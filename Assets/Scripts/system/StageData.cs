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
    public string sceneName = "_Stage_1";

    [Header("ルール")]
    [Tooltip("出現させる敵の波表。Spawner がここから読む")]
    public List<WaveData> waves = new List<WaveData>();

    [Tooltip("この秒数を耐え切ればクリア")]
    public float clearTime = 30f;

    [Tooltip("この数だけ敵に通過されると敗北")]
    public int maxEnemyPasses = 50;

    [Tooltip("ステージ開始時の所持コイン")]
    public int initialCoin = 100;

    [Header("演出")]
    public AudioClip bgm;

    [Tooltip("背景の色調。1限目=朝、6限目=夕、のように時限で変える（Phase 4 で使用）")]
    public Color backgroundTint = Color.white;
}
