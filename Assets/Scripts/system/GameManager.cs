using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.SceneManagement; // シーン切り替えのために追加

public class GameManager : MonoBehaviour
{
    // --- 外部マネージャーの参照 ---
    [Header("External Managers")]
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private ScoreManager scoreManager; // ScoreManagerの参照

    // --- ゲーム設定 ---
    [Header("Game Goals & Settings")]
    [Tooltip("ゲームクリアに必要な時間 (秒)")]
    [SerializeField] private float gameClearTime = 60f;
    [Tooltip("敗北条件: 許容される敵の最大通過数")]
    [SerializeField] private int maxEnemyPasses = 10;
    [Tooltip("リトライ時にロードするシーンの名前")]
    [SerializeField] private string retrySceneName = "GameScene"; // 例: 現在のゲームシーン名
    [Tooltip("タイトルに戻る時にロードするシーンの名前")]
    [SerializeField] private string titleSceneName = "TitleScene";

    // --- UI参照 (パネルはリスト、ボタンは単一変数) ---
    [Header("UI Panels & Buttons")]
    [Tooltip("ゲームオーバー時に表示するUIパネル全体 (複数設定可能)")]
    [SerializeField] private List<GameObject> gameOverPanels = new List<GameObject>();
    [Tooltip("ゲームクリア時に表示するUIパネル全体 (複数設定可能)")]
    [SerializeField] private List<GameObject> gameClearPanels = new List<GameObject>();

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI playTimeDisplay;
    [SerializeField] private TextMeshProUGUI finalCurrencyDisplay;
    [SerializeField] private TextMeshProUGUI totalScoreDisplay;
    // ▼▼▼ 修正点 ▼▼▼ UIテキストの参照を追加
    [Tooltip("敵の通過数を表示するUIテキスト")]
    [SerializeField] private TextMeshProUGUI enemyPassesDisplay;


    [Header("Enemy Kill Stats UI")]
    [Tooltip("各パネル内の、敵の討伐数表示用UIをすべてリスト化")]
    [SerializeField] private List<TextMeshProUGUI> enemyKillDisplays = new List<TextMeshProUGUI>();


    // --- 統計データ ---
    private float gameTimeElapsed = 0f;
    private int enemiesPassed = 0;

    // ゲームの状態管理
    public static bool IsGameActive = true;
    public bool IsGameOver { get; private set; } = false;
    public bool IsGameClear { get; private set; } = false;

    // --- 他クラスへの公開（UIManager / CountdownTimer などが参照する）---
    public static GameManager Instance { get; private set; }
    public int EnemiesPassed => enemiesPassed;
    public int MaxEnemyPasses => maxEnemyPasses;
    public float RemainingTime => Mathf.Max(0f, gameClearTime - gameTimeElapsed);

    void Awake()
    {
        Instance = this;

        // 【重要】IsGameActive と timeScale はグローバルな状態なので、
        // 前のシーンの値を持ち越さないよう必ず Awake で戻す。
        // Start に置くと、敗北後に別ステージへ入ったとき Weapon.Update() が
        // 冒頭で return して武器が一発も撃たなくなる。
        IsGameActive = true;
        Time.timeScale = 1f;
    }

    void Start()
    {
        Debug.Log("DEBUG_CRASH_CHECK: GameManager.Start() 実行開始");

        enemiesPassed = 0;

        // ▼▼▼ 修正点 ▼▼▼ ゲーム開始時にUIを初期化
        UpdatePassesDisplay();

        SetPanelActive(gameOverPanels, false);
        SetPanelActive(gameClearPanels, false);


        if (currencyManager == null)
        {
            currencyManager = FindAnyObjectByType<CurrencyManager>();
        }
        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<ScoreManager>();
        }

        Debug.Log("DEBUG_CRASH_CHECK: GameManager.Start() 実行完了");
    }

    private void SetPanelActive(List<GameObject> panels, bool active)
    {
        foreach (GameObject panel in panels)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }


    void Update()
    {
        if (!IsGameActive || IsGameOver || IsGameClear) return;

        gameTimeElapsed += Time.deltaTime;

        if (gameTimeElapsed >= gameClearTime)
        {
            Debug.Log("ゲームクリア");
            GameClear();
        }
    }

    /// <summary>
    /// 敵がゴール地点を通過したときに呼ばれるメソッド
    /// </summary>
    public void RecordEnemyPass()
    {
        if (!IsGameActive || IsGameOver || IsGameClear) return;

        enemiesPassed++;

        // ▼▼▼ 修正点 ▼▼▼ 敵が通過するたびにUIを更新
        UpdatePassesDisplay();

        if (enemiesPassed >= maxEnemyPasses)
        {
            GameOver();
        }
    }

    // ▼▼▼ 修正点 ▼▼▼ UIを更新するための新しいメソッド
    /// <summary>
    /// 敵の通過数UIを更新する
    /// </summary>
    private void UpdatePassesDisplay()
    {
        if (enemyPassesDisplay != null)
        {
            // 例：「5 / 10」のように表示
            enemyPassesDisplay.text = enemiesPassed.ToString() + " / " + maxEnemyPasses.ToString();
        }
    }


    // --- (これ以降のコードは変更ありません) ---

    public void GameClear()
    {
        Debug.Log("ゲームクリア: 時間達成");
        if (IsGameClear || IsGameOver) return;
        IsGameClear = true;
        IsGameActive = false;
        Time.timeScale = 0;
        SetPanelActive(gameClearPanels, true);
        DisplayStats("退学処分");
    }

    public void GameOver()
    {
        Debug.Log("ゲームオーバー: 敵通過数が上限に達しました");
        if (IsGameClear || IsGameOver) return;
        IsGameOver = true;
        IsGameActive = false;
        Time.timeScale = 0;
        SetPanelActive(gameOverPanels, true);
        DisplayStats(" ");
    }

    private void DisplayStats(string resultTitle)
    {
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManagerが設定されていません。統計表示をスキップします。");
            return;
        }
        scoreManager.UpdateStatsUI();
        UpdateEnemyKillStats();
        TimeSpan time = TimeSpan.FromSeconds(gameTimeElapsed);
        if (titleText != null) titleText.text = resultTitle;
        if (playTimeDisplay != null) playTimeDisplay.text = $"{time.Minutes:D2}:{time.Seconds:D2}";
        int finalCurrency = 0;
        if (currencyManager != null)
        {
            finalCurrency = currencyManager.GetCurrentCurrency();
        }
        if (finalCurrencyDisplay != null) finalCurrencyDisplay.text = finalCurrency.ToString();
        int totalKills = scoreManager.TotalKills;
        const int KILL_SCORE_WEIGHT = 50;
        const int CURRENCY_SCORE_WEIGHT = 1;
        const int TIME_SCORE_WEIGHT = 10;
        int totalScore = totalKills * KILL_SCORE_WEIGHT
                           + finalCurrency * CURRENCY_SCORE_WEIGHT
                           + Mathf.FloorToInt(gameTimeElapsed) * TIME_SCORE_WEIGHT;
        if (totalScoreDisplay != null) totalScoreDisplay.text = totalScore.ToString();
    }

    private void UpdateEnemyKillStats()
    {
        if (scoreManager == null) return;
        List<ScoreManager.EnemyStatsSetting> stats = scoreManager.GetStatsSettings();
        int enemyTypeCount = stats.Count;
        for (int i = 0; i < enemyKillDisplays.Count; i++)
        {
            if (enemyKillDisplays[i] != null)
            {
                int statIndex = i % enemyTypeCount;
                if (statIndex < stats.Count)
                {
                    int kills = stats[statIndex].killCount;
                    enemyKillDisplays[i].text = kills.ToString();
                }
                else
                {
                    enemyKillDisplays[i].text = "0";
                }
            }
        }
    }

    public void OnRetryButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(retrySceneName);
    }

    public void OnTitleButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}