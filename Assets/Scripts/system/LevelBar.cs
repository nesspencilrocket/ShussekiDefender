using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面上端の中央に、ステージのレベルと設置スポットの解放状況を出す。
///
///   3限目
///   Lv.2        設置場所 6 / 10
///   ■■■■□□□□  次の解放まで あと 4 人
///
/// 左上の「3限目」は、いま遊んでいる時限（StageData.displayName）。
///
/// レベルが上がった瞬間は、下に「設置場所が 2 つ増えた！」を少しの間出す。
/// UI は実行時に MenuUI で組み立てるので、シーンにはこのコンポーネントと
/// フォントの参照だけを置けばよい。本編中だけ表示し、カウントダウンとリザルトの間は隠す。
/// </summary>
public class LevelBar : MonoBehaviour
{
    [Header("見た目")]
    [Tooltip("日本語が出るフォント")]
    [SerializeField] private TMP_FontAsset font;

    [Tooltip("画面上端の中央からの位置")]
    [SerializeField] private Vector2 position = new Vector2(0f, -24f);

    [Tooltip("レベルが上がったときのお知らせを出しておく秒数")]
    [SerializeField] private float noticeSeconds = 2f;

    private static readonly Vector2 Size = new Vector2(560f, 104f);
    private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.14f, 0.78f);
    private static readonly Color TextMain = new Color(0.93f, 0.95f, 0.98f, 1f);
    private static readonly Color TextMuted = new Color(0.65f, 0.70f, 0.78f, 1f);
    private static readonly Color Accent = new Color(1f, 0.82f, 0.35f, 1f);

    private StageLevel level;
    private Node[] nodes = new Node[0];

    private GameObject panel;
    private TextMeshProUGUI stageText;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI slotsText;
    private TextMeshProUGUI nextText;
    private TextMeshProUGUI noticeText;
    private Image fill;

    private float noticeUntil;
    // 出すお知らせの待ち行列。レベルアップと敵の強化が重なっても、1 つずつ順に出す
    private readonly System.Collections.Generic.Queue<string> pendingNotices =
        new System.Collections.Generic.Queue<string>();
    private int shownLevel = -1;
    private int shownKills = -1;

    private void Start()
    {
        level = StageLevel.Ensure();
        nodes = FindObjectsByType<Node>(FindObjectsSortMode.None);
        Build();

        // 時限の名前は GameManager.Awake で解決済み（Start より前）
        StageData stage = (GameManager.Instance != null) ? GameManager.Instance.Stage : null;
        stageText.text = (stage != null) ? stage.displayName : "";

        Refresh();
    }

    private void OnEnable()
    {
        StageLevel.OnLevelChanged += OnLevelUp;
        Spawner.OnEnemyHPRaised += OnEnemyHPRaised;
        NodeFinalPhase.OnNotice += OnNodeNotice;
    }

    private void OnDisable()
    {
        StageLevel.OnLevelChanged -= OnLevelUp;
        Spawner.OnEnemyHPRaised -= OnEnemyHPRaised;
        NodeFinalPhase.OnNotice -= OnNodeNotice;
    }

    private void Build()
    {
        Canvas canvas = MenuUI.CreateCanvas(gameObject);
        // リザルトや開始カウントダウンより下に置く。本編中だけの UI
        canvas.sortingOrder = -1;

        RectTransform rt = MenuUI.Rect(transform, "LevelPanel", Vector2.zero, Size);
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = position;
        panel = rt.gameObject;

        Image bg = panel.AddComponent<Image>();
        bg.color = PanelColor;
        bg.raycastTarget = false;

        float half = Size.x / 2f;

        // 左のブロックは 2 段。上に時限、下にレベル
        stageText = MenuUI.Text(rt, "Stage", "", new Vector2(-half + 90f, 30f), new Vector2(160f, 34f),
                                26f, TextMain, font, TextAlignmentOptions.Center);

        levelText = MenuUI.Text(rt, "Level", "", new Vector2(-half + 90f, -8f), new Vector2(160f, 54f),
                                40f, Accent, font, TextAlignmentOptions.Center);

        slotsText = MenuUI.Text(rt, "Slots", "", new Vector2(80f, 22f), new Vector2(360f, 40f),
                                28f, TextMain, font, TextAlignmentOptions.Right);

        nextText = MenuUI.Text(rt, "Next", "", new Vector2(80f, -16f), new Vector2(360f, 32f),
                               22f, TextMuted, font, TextAlignmentOptions.Right);

        // 進み具合のバー（下端いっぱい）
        MenuUI.Panel(rt, "BarTrack", new Vector2(0f, -Size.y / 2f + 10f), new Vector2(Size.x - 32f, 8f),
                     new Color(1f, 1f, 1f, 0.15f));
        fill = MenuUI.Panel(rt, "BarFill", new Vector2(0f, -Size.y / 2f + 10f), new Vector2(Size.x - 32f, 8f),
                            Accent);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        // Image.Type.Filled はスプライトが無いと塗られないため、白い四角を当てる
        fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

        noticeText = MenuUI.Text(transform, "Notice", "", Vector2.zero, new Vector2(700f, 50f),
                                 32f, Accent, font, TextAlignmentOptions.Center);
        RectTransform nrt = noticeText.rectTransform;
        nrt.anchorMin = new Vector2(0.5f, 1f);
        nrt.anchorMax = new Vector2(0.5f, 1f);
        nrt.pivot = new Vector2(0.5f, 1f);
        nrt.anchoredPosition = position + new Vector2(0f, -Size.y - 10f);
        noticeText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (level == null || panel == null) return;

        // 本編中だけ出す。カウントダウン中とリザルト中は隠す
        bool show = GameManager.IsGameActive;
        if (panel.activeSelf != show) panel.SetActive(show);

        // 今のお知らせが終わっていたら、待っている次のお知らせを出す
        if (show && Time.time >= noticeUntil && pendingNotices.Count > 0)
        {
            noticeText.text = pendingNotices.Dequeue();
            noticeUntil = Time.time + noticeSeconds;
        }

        bool notice = show && Time.time < noticeUntil;
        if (noticeText.gameObject.activeSelf != notice) noticeText.gameObject.SetActive(notice);

        if (!show) return;

        // 倒した数かレベルが変わったときだけ文字を作り直す
        if (level.Kills != shownKills || level.Level != shownLevel) Refresh();
    }

    private void Refresh()
    {
        if (level == null || levelText == null) return;

        shownKills = level.Kills;
        shownLevel = level.Level;

        // 分母は「このステージのうちに使えるようになる数」。maxNodes を超える順番のスポットは含めない
        int unlocked = 0;
        int available = 0;
        foreach (Node n in nodes)
        {
            if (n == null) continue;
            if (n.IsUnlocked) unlocked++;
            if (n.IsAvailableInStage) available++;
        }

        levelText.text = $"Lv.{level.Level}";
        slotsText.text = $"設置場所 {unlocked} / {available}";

        if (unlocked >= available)
        {
            nextText.text = "すべて解放";
        }
        else if (level.IsMaxLevel)
        {
            nextText.text = "最大レベル";
        }
        else
        {
            int opening = CountUnlockingAt(level.Level + 1);
            nextText.text = opening > 0
                ? $"次の解放まで あと {level.KillsToNext} 人"
                : $"次のレベルまで あと {level.KillsToNext} 人";
        }

        fill.fillAmount = level.Progress;
    }

    /// <summary>
    /// そのレベルに上がったときに新しく使えるようになるスポットの数。
    /// 順番が「1 つ前のレベルで開いている数」より大きく「そのレベルで開いている数」以下のもの
    /// </summary>
    private int CountUnlockingAt(int lv)
    {
        int before = level.UnlockedCountAt(lv - 1);
        int after = level.UnlockedCountAt(lv);
        int count = 0;
        foreach (Node n in nodes)
        {
            if (n != null && n.UnlockOrder > before && n.UnlockOrder <= after) count++;
        }
        return count;
    }

    private void OnLevelUp(int newLevel)
    {
        int opened = CountUnlockingAt(newLevel);
        pendingNotices.Enqueue(opened > 0
            ? $"Lv.{newLevel}！ 設置場所が {opened} つ増えた"
            : $"Lv.{newLevel}！");
        Refresh();
    }

    /// <summary>終盤に設置スポットが使えなくなるとき（予告と当日）</summary>
    private void OnNodeNotice(string message)
    {
        pendingNotices.Enqueue(message);
    }

    /// <summary>波が変わって、敵の HP が上がったとき。レベルアップと同じ場所に知らせる</summary>
    private void OnEnemyHPRaised(float oldHP, float newHP)
    {
        pendingNotices.Enqueue($"敵の体力が強化！ {oldHP:0.#} → {newHP:0.#}");
    }
}
