using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // --- ステージ設定 ---
    [Header("Stage")]
    [Tooltip("選択画面を経由せず直接このシーンを再生したときに使う StageData")]
    [SerializeField] private StageData fallbackStage;
    [Tooltip("全ステージ一覧。「次の時限へ」の遷移先を引くのに使う")]
    [SerializeField] private StageCatalog catalog;

    /// <summary>このステージの設定。Awake で解決される</summary>
    public StageData Stage { get; private set; }

    // --- 開始前カウントダウン ---
    [Header("Start Countdown")]
    [Tooltip("開始までの秒数。3 なら 3 → 2 → 1 → GO")]
    [SerializeField] private float countdownSeconds = 3f;
    [Tooltip("「GO」を表示している時間（秒）")]
    [SerializeField] private float goDuration = 0.6f;
    [Tooltip("カウント 0 のときに出す文字")]
    [SerializeField] private string goText = "GO!";
    [Tooltip("カウントダウンを表示する TextMeshPro（画面中央）")]
    [SerializeField] private TextMeshProUGUI countdownText;

    /// <summary>開始前カウントダウン中は true</summary>
    public bool IsCountingDown { get; private set; }

    // --- 外部マネージャーの参照 ---
    [Header("External Managers")]
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private ScoreManager scoreManager;

    // --- ゲーム設定（StageData があれば Awake で上書きされる）---
    [Header("Game Goals & Settings")]
    [SerializeField] private float gameClearTime = 30f;
    [SerializeField] private int maxEnemyPasses = 50;
    [Tooltip("タイトルに戻る時にロードするシーン名")]
    [SerializeField] private string titleSceneName = "StartMenu";
    [Tooltip("時限選択に戻る時にロードするシーン名")]
    [SerializeField] private string stageSelectSceneName = "StageSelect";
    [Tooltip("敗北時にリザルトへ出す文字（処分に至らなかった、の意）")]
    [SerializeField] private string gameOverTitle = "処分なし";

    // --- UI ---
    [Header("UI Panels & Buttons")]
    [SerializeField] private List<GameObject> gameOverPanels = new List<GameObject>();
    [SerializeField] private List<GameObject> gameClearPanels = new List<GameObject>();
    [Tooltip("クリアパネル内の「次の時限へ」ボタン。最終ステージでは自動的に隠れる")]
    [SerializeField] private GameObject nextStageButton;

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI playTimeDisplay;
    [SerializeField] private TextMeshProUGUI finalCurrencyDisplay;
    [SerializeField] private TextMeshProUGUI totalScoreDisplay;
    [SerializeField] private TextMeshProUGUI enemyPassesDisplay;

    [Header("Enemy Kill Stats UI")]
    [SerializeField] private List<TextMeshProUGUI> enemyKillDisplays = new List<TextMeshProUGUI>();

    // --- 統計データ ---
    private float gameTimeElapsed = 0f;
    private int enemiesPassed = 0;
    private int lastTotalScore = 0;

    // --- ゲームの状態 ---
    public static bool IsGameActive = true;
    public bool IsGameOver { get; private set; } = false;
    public bool IsGameClear { get; private set; } = false;

    // --- 他クラスへの公開 ---
    public int EnemiesPassed => enemiesPassed;
    public int MaxEnemyPasses => maxEnemyPasses;
    public float RemainingTime => Mathf.Max(0f, gameClearTime - gameTimeElapsed);

    /// <summary>
    /// 処分の軽い順。StageData.rankThresholds と添字を対応させる。
    /// プレイヤーは出席を妨害する側なので、重い処分ほど良い結果。
    /// </summary>
    private static readonly string[] RANKS =
        { "訓告", "厳重注意", "1週間停学", "無期限停学", "退学処分" };

    void Awake()
    {
        Instance = this;

        // 【重要】IsGameActive と timeScale はグローバルな状態なので、
        // 前のシーンの値を持ち越さないよう必ず Awake で戻す。
        // ここでは false から始め、カウントダウンが明けたら true にする。
        IsGameActive = false;
        IsCountingDown = true;
        Time.timeScale = 1f;

        // 【重要】StageData を解決するのはこの 1 箇所だけ。
        // Awake はすべての Start より先に走るので、他のコンポーネントは
        // Start で GameManager.Instance.Stage を読めば必ず解決済みになる。
        Stage = StageContext.Resolve(fallbackStage);

        if (Stage != null)
        {
            gameClearTime = Stage.clearTime;
            maxEnemyPasses = Stage.maxEnemyPasses;
        }
        else
        {
            Debug.LogError("GameManager: StageData を解決できませんでした。"
                         + "Inspector の Fallback Stage を設定してください。", this);
        }
    }

    void Start()
    {
        enemiesPassed = 0;
        UpdatePassesDisplay();

        SetPanelActive(gameOverPanels, false);
        SetPanelActive(gameClearPanels, false);

        if (currencyManager == null) currencyManager = FindAnyObjectByType<CurrencyManager>();
        if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();

        StartCoroutine(BeginStage());
    }

    /// <summary>
    /// 3 → 2 → 1 → GO を表示してからゲームを開始する。
    /// Spawner はこのフラグが立つのを待ってから湧かせはじめる。
    /// </summary>
    private IEnumerator BeginStage()
    {
        IsCountingDown = true;
        IsGameActive = false;

        if (countdownText != null) countdownText.gameObject.SetActive(true);

        for (int n = Mathf.CeilToInt(countdownSeconds); n > 0; n--)
        {
            if (countdownText != null) countdownText.text = n.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null) countdownText.text = goText;
        yield return new WaitForSeconds(goDuration);

        if (countdownText != null) countdownText.gameObject.SetActive(false);

        IsCountingDown = false;
        IsGameActive = true;
    }

    private void SetPanelActive(List<GameObject> panels, bool active)
    {
        foreach (GameObject panel in panels)
        {
            if (panel != null) panel.SetActive(active);
        }
    }

    void Update()
    {
        if (!IsGameActive || IsGameOver || IsGameClear) return;

        gameTimeElapsed += Time.deltaTime;

        if (gameTimeElapsed >= gameClearTime)
        {
            GameClear();
        }
    }

    /// <summary>
    /// 敵がゴール地点を通過したときに Enemy から呼ばれる
    /// </summary>
    public void RecordEnemyPass()
    {
        if (!IsGameActive || IsGameOver || IsGameClear) return;

        enemiesPassed++;
        UpdatePassesDisplay();

        if (enemiesPassed >= maxEnemyPasses)
        {
            GameOver();
        }
    }

    private void UpdatePassesDisplay()
    {
        if (enemyPassesDisplay != null)
        {
            enemyPassesDisplay.text = enemiesPassed + " / " + maxEnemyPasses;
        }
    }

    public void GameClear()
    {
        if (IsGameClear || IsGameOver) return;

        IsGameClear = true;
        IsGameActive = false;
        Time.timeScale = 0;
        SetPanelActive(gameClearPanels, true);

        DisplayStats(true);

        if (Stage != null)
        {
            StageProgress.MarkCleared(Stage.stageNumber);
            StageProgress.SubmitScore(Stage.stageNumber, lastTotalScore);
        }

        // 最終ステージでは「次の時限へ」を出さない
        if (nextStageButton != null)
        {
            bool hasNext = catalog != null && catalog.Next(Stage) != null;
            nextStageButton.SetActive(hasNext);
        }
    }

    public void GameOver()
    {
        if (IsGameClear || IsGameOver) return;

        IsGameOver = true;
        IsGameActive = false;
        Time.timeScale = 0;
        SetPanelActive(gameOverPanels, true);
        DisplayStats(false);
    }

    /// <summary>
    /// スコアを確定させ、リザルト UI を埋める。
    /// 見出しはスコアから処分の重さを決めて出す。
    /// </summary>
    private void DisplayStats(bool cleared)
    {
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager が設定されていません。統計表示をスキップします。");
            return;
        }

        scoreManager.UpdateStatsUI();
        UpdateEnemyKillStats();

        TimeSpan time = TimeSpan.FromSeconds(gameTimeElapsed);
        if (playTimeDisplay != null) playTimeDisplay.text = $"{time.Minutes:D2}:{time.Seconds:D2}";

        int finalCurrency = (currencyManager != null) ? currencyManager.GetCurrentCurrency() : 0;
        if (finalCurrencyDisplay != null) finalCurrencyDisplay.text = finalCurrency.ToString();

        const int KILL_SCORE_WEIGHT = 50;
        const int CURRENCY_SCORE_WEIGHT = 1;
        const int TIME_SCORE_WEIGHT = 10;

        lastTotalScore = scoreManager.TotalKills * KILL_SCORE_WEIGHT
                       + finalCurrency * CURRENCY_SCORE_WEIGHT
                       + Mathf.FloorToInt(gameTimeElapsed) * TIME_SCORE_WEIGHT;

        if (totalScoreDisplay != null) totalScoreDisplay.text = lastTotalScore.ToString();

        // 見出しはスコア確定後に決める（順序を逆にすると常に最軽量の処分になる）
        if (titleText != null)
        {
            titleText.text = cleared ? RankOf(lastTotalScore) : gameOverTitle;
        }
    }

    /// <summary>
    /// スコアから処分の重さを決める。閾値はステージごとに StageData で調整する。
    /// </summary>
    private string RankOf(int score)
    {
        int[] th = (Stage != null) ? Stage.rankThresholds : null;
        if (th == null || th.Length == 0) return RANKS[RANKS.Length - 1];

        int n = Mathf.Min(th.Length, RANKS.Length);
        for (int i = n - 1; i >= 0; i--)
        {
            if (score >= th[i]) return RANKS[i];
        }
        return RANKS[0];
    }

    private void UpdateEnemyKillStats()
    {
        if (scoreManager == null) return;

        List<ScoreManager.EnemyStatsSetting> stats = scoreManager.GetStatsSettings();

        // 敵が 1 種も登録されていないと下の剰余算がゼロ除算になるため先に抜ける
        if (stats == null || stats.Count == 0) return;

        for (int i = 0; i < enemyKillDisplays.Count; i++)
        {
            if (enemyKillDisplays[i] == null) continue;
            enemyKillDisplays[i].text = stats[i % stats.Count].killCount.ToString();
        }
    }

    // ───── ボタンから呼ぶ遷移メソッド ─────
    // 【重要】どれも Time.timeScale を 1 に戻してから遷移すること。
    // GameOver / GameClear で 0 にしたまま遷移すると次のシーンが停止状態で始まる。

    public void OnRetryButtonClicked()
    {
        Time.timeScale = 1f;
        // シーン名を手で持たない。6 枚に増えたとき書き忘れる典型的な箇所だった。
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnNextStageButtonClicked()
    {
        Time.timeScale = 1f;

        StageData next = (catalog != null) ? catalog.Next(Stage) : null;
        if (next == null)
        {
            OnStageSelectButtonClicked();
            return;
        }

        StageContext.Select(next);
        SceneManager.LoadScene(next.sceneName);
    }

    public void OnStageSelectButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }

    public void OnTitleButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}
