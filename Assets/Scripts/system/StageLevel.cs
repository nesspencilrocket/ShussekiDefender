using System;
using UnityEngine;

/// <summary>
/// ステージ内のレベル。学生を倒した数でレベルが上がり、
/// レベルに応じて使える設置スポット（Node）の数が増える。
///
/// ステージを始めるたびに Lv1 から数え直す（時限をまたいで持ち越さない）。
///
/// 数はステージごとに StageData で決める。
///   levelUpKills  … レベルが上がる撃破数の累計
///   initialNodes  … Lv1 で使える数
///   nodesPerLevel … 1 レベルごとに増える数
///   maxNodes      … このステージで使える最大数（0 ならすべて）
/// どのスポットから開くかは、シーンに置いた各 Node の unlockOrder で決める。
/// 6 つの時限は同じシーンを使うので、順番はシーン、数はステージ、と分けてある。
///
/// シーンに置かなくても、Node が Ensure() で作る。
/// </summary>
public class StageLevel : MonoBehaviour
{
    public static StageLevel Instance { get; private set; }

    /// <summary>レベルが上がったとき。引数は新しいレベル</summary>
    public static Action<int> OnLevelChanged;

    /// <summary>今のレベル。1 から始まる</summary>
    public int Level { get; private set; } = 1;

    /// <summary>このステージで倒した学生の数</summary>
    public int Kills { get; private set; }

    // StageData が無いときに使う既定値（StageData の既定と同じ）
    private int[] thresholds = { 5, 15, 30 };
    private int initialNodes = 4;
    private int nodesPerLevel = 2;
    private int maxNodes = 0;

    /// <summary>これ以上上がらないレベル</summary>
    public int MaxLevel => thresholds.Length + 1;

    public bool IsMaxLevel => Level >= MaxLevel;

    /// <summary>このステージで使える設置スポットの最大数。0 なら上限なし</summary>
    public int MaxNodes => maxNodes;

    /// <summary>次のレベルまであと何人倒せばよいか。最大レベルなら 0</summary>
    public int KillsToNext => IsMaxLevel ? 0 : Mathf.Max(0, thresholds[Level - 1] - Kills);

    /// <summary>今のレベルの中での進み具合（0〜1）。最大レベルなら 1</summary>
    public float Progress
    {
        get
        {
            if (IsMaxLevel) return 1f;
            int from = (Level >= 2) ? thresholds[Level - 2] : 0;
            int to = thresholds[Level - 1];
            return (to <= from) ? 1f : Mathf.Clamp01((Kills - from) / (float)(to - from));
        }
    }

    /// <summary>
    /// そのレベルで使える設置スポットの数（unlockOrder がこの数以下のスポットが使える）
    /// </summary>
    public int UnlockedCountAt(int level)
    {
        int count = initialNodes + nodesPerLevel * Mathf.Max(0, level - 1);
        return (maxNodes > 0) ? Mathf.Min(count, maxNodes) : count;
    }

    /// <summary>今のレベルで使える設置スポットの数</summary>
    public int UnlockedCount => UnlockedCountAt(Level);

    /// <summary>
    /// シーンに居なければ作って返す。
    /// </summary>
    public static StageLevel Ensure()
    {
        if (Instance != null) return Instance;

        StageLevel found = FindFirstObjectByType<StageLevel>();
        if (found != null) return found;

        return new GameObject("StageLevel").AddComponent<StageLevel>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        EnemyHP.OnEnemyDead += OnEnemyDead;
    }

    private void OnDisable()
    {
        EnemyHP.OnEnemyDead -= OnEnemyDead;
    }

    private void Start()
    {
        // GameManager.Awake で StageData が決まる前に Awake が走った場合に備えて読み直す
        LoadSettings();
    }

    /// <summary>StageData から数を読む。無ければ既定値のまま</summary>
    private void LoadSettings()
    {
        StageData stage = (GameManager.Instance != null) ? GameManager.Instance.Stage : null;
        if (stage == null) return;

        if (stage.levelUpKills != null && stage.levelUpKills.Length > 0) thresholds = stage.levelUpKills;
        initialNodes = Mathf.Max(0, stage.initialNodes);
        nodesPerLevel = Mathf.Max(0, stage.nodesPerLevel);
        maxNodes = Mathf.Max(0, stage.maxNodes);
    }

    private void OnEnemyDead(EnemyHP _)
    {
        Kills++;

        // 一度に複数のしきい値を越えたときも 1 段ずつ上げ、そのたびに知らせる
        while (!IsMaxLevel && Kills >= thresholds[Level - 1])
        {
            Level++;
            OnLevelChanged?.Invoke(Level);
        }
    }
}
