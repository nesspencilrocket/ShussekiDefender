using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// お金を払って雨を降らせる。画面右下、倍速ボタンの左にある「雨」ボタンから使う。
///
/// 雨の間は学生が傘をさして歩くので、全員の歩く速さが落ちる（Enemy.WeatherSpeedScale）。
/// 降っている間と、やんでからしばらくはボタンを押せない。
///
///   使える      … 雨 ／ 50円
///   降っている  … 雨 ／ あと 5 秒
///   やんだ直後  … 雨 ／ 次まで 8 秒
///
/// UI は実行時に MenuUI で組み立てるので、シーンにはこのコンポーネントと
/// フォントの参照だけを置けばよい。本編中だけ表示し、カウントダウンとリザルトの間は隠す。
/// 6 つの時限は同じシーンを使うので、値段や効き目はどの時限でも同じ。
/// </summary>
public class RainController : MonoBehaviour
{
    [Header("雨の仕様")]
    [Tooltip("1 回降らせるのにかかる金額（円）")]
    [Min(0)] [SerializeField] private int cost = 50;

    [Tooltip("降っている秒数")]
    [Min(0.1f)] [SerializeField] private float duration = 8f;

    [Tooltip("雨の間の歩く速さの倍率。0.5 なら半分")]
    [Range(0.1f, 1f)] [SerializeField] private float speedScale = 0.5f;

    [Tooltip("やんでから次に降らせられるまでの秒数")]
    [Min(0f)] [SerializeField] private float cooldown = 10f;

    [Header("見た目")]
    [Tooltip("日本語が出るフォント")]
    [SerializeField] private TMP_FontAsset font;

    [Tooltip("画面右下の角からの位置（ボタンの中心）。既定は倍速ボタンのすぐ左")]
    [SerializeField] private Vector2 buttonPosition = new Vector2(-390f, 80f);

    [Tooltip("雨粒の数")]
    [Min(0)] [SerializeField] private int dropCount = 90;

    private static readonly Vector2 ButtonSize = new Vector2(220f, 100f);
    private static readonly Color ButtonColor = new Color(0.20f, 0.36f, 0.58f, 1f);
    private static readonly Color TextMain = new Color(0.95f, 0.97f, 1f, 1f);
    private static readonly Color TextSub = new Color(0.80f, 0.88f, 1f, 1f);

    // 雨の見た目
    private static readonly Color ScrimColor = new Color(0.16f, 0.22f, 0.34f, 0.30f);
    private static readonly Color DropColor = new Color(0.80f, 0.88f, 1f, 0.55f);
    private static readonly Vector2 DropSize = new Vector2(3f, 46f);
    // 画面上の落ちる速さ（参照解像度 1920x1080 の単位 / 秒）。少し左へ流す
    private static readonly Vector2 Fall = new Vector2(-280f, -1500f);
    private const float FadeSeconds = 0.6f;

    private CurrencyManager currency;

    private GameObject buttonRoot;
    private Button button;
    private TextMeshProUGUI label;
    private TextMeshProUGUI subText;

    private GameObject overlay;
    private Canvas overlayCanvas;
    private CanvasGroup overlayGroup;
    private RectTransform[] drops = new RectTransform[0];
    private float[] dropSpeeds = new float[0];

    private float rainLeft;
    private float cooldownLeft;
    private float intensity;
    private int shownSubKey = -1;

    /// <summary>いま雨が降っているか</summary>
    public bool IsRaining => rainLeft > 0f;

    private void Awake()
    {
        // 前のステージの雨を持ち越さない（倍率はゲーム全体で共有している）
        Enemy.WeatherSpeedScale = 1f;
    }

    private void Start()
    {
        currency = CurrencyManager.instance;
        if (currency == null) currency = FindAnyObjectByType<CurrencyManager>();

        BuildButton();
        BuildOverlay();
        RefreshButton();
    }

    private void OnDisable()
    {
        Enemy.WeatherSpeedScale = 1f;
    }

    private void OnDestroy()
    {
        // 雨の幕はシーンの直下に作っているので、自分で片付ける
        if (overlay != null) Destroy(overlay);
    }

    // ───── 組み立て ─────

    private void BuildButton()
    {
        Canvas canvas = MenuUI.CreateCanvas(gameObject);
        // リザルトや開始カウントダウンより下に置く。本編中だけの UI
        canvas.sortingOrder = -1;
        // 倍速ボタンのある Canvas と同じく幅に合わせて縮める。
        // 縮み方が違うと、画面の縦横比によって 2 つのボタンが重なる
        GetComponent<CanvasScaler>().matchWidthOrHeight = 0f;

        button = MenuUI.TextButton(transform, "RainButton", "", Vector2.zero, ButtonSize,
                                   28f, ButtonColor, TextMain, font);
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.anchoredPosition = buttonPosition;
        buttonRoot = button.gameObject;

        // TextButton の文字は 1 行だけなので、「雨」と下の小さい行を作り直す
        label = button.GetComponentInChildren<TextMeshProUGUI>();
        label.text = "雨";
        label.fontSize = 44f;
        label.rectTransform.offsetMin = new Vector2(0f, ButtonSize.y * 0.36f);

        subText = MenuUI.Text(rt, "Sub", "", Vector2.zero, ButtonSize, 24f, TextSub, font,
                              TextAlignmentOptions.Center);
        subText.rectTransform.anchorMin = Vector2.zero;
        subText.rectTransform.anchorMax = new Vector2(1f, 0.4f);
        subText.rectTransform.offsetMin = Vector2.zero;
        subText.rectTransform.offsetMax = Vector2.zero;

        button.onClick.AddListener(TryStartRain);
        buttonRoot.SetActive(false);
    }

    /// <summary>
    /// 画面いっぱいの雨の幕を作る。HUD（Canvas の並び順 0）より下に描くので、
    /// 所持金や武器の購入パネルは雨に隠れない。
    /// クリックは一切受け取らない（設置スポットを押せなくならないように）。
    /// </summary>
    private void BuildOverlay()
    {
        overlay = new GameObject("RainOverlay", typeof(RectTransform));
        overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = -2;

        CanvasScaler scaler = overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        overlayGroup = overlay.AddComponent<CanvasGroup>();
        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable = false;
        overlayGroup.alpha = 0f;

        Image scrim = MenuUI.Stretch(overlay.transform, "Scrim").gameObject.AddComponent<Image>();
        scrim.color = ScrimColor;
        scrim.raycastTarget = false;

        // 雨粒は落ちる向きに傾けた細い棒
        float tilt = Mathf.Atan2(Fall.x, -Fall.y) * Mathf.Rad2Deg;
        drops = new RectTransform[dropCount];
        dropSpeeds = new float[dropCount];
        for (int i = 0; i < dropCount; i++)
        {
            Image drop = MenuUI.Panel(overlay.transform, "Drop", Vector2.zero, DropSize, DropColor);
            drops[i] = drop.rectTransform;
            drops[i].localEulerAngles = new Vector3(0f, 0f, tilt);
            dropSpeeds[i] = Random.Range(0.8f, 1.2f);
        }

        overlay.SetActive(false);
    }

    // ───── 毎フレーム ─────

    private void Update()
    {
        if (buttonRoot == null) return;

        bool active = GameManager.IsGameActive;
        if (buttonRoot.activeSelf != active) buttonRoot.SetActive(active);

        // 勝敗が決まったら、降っている途中でもすぐやませる
        if (!active)
        {
            if (IsRaining) StopRain();
            HideOverlayNow();
            return;
        }

        float dt = Time.deltaTime;
        if (IsRaining)
        {
            rainLeft -= dt;
            if (rainLeft <= 0f)
            {
                StopRain();
                cooldownLeft = cooldown;
            }
        }
        else if (cooldownLeft > 0f)
        {
            cooldownLeft -= dt;
        }

        UpdateOverlay(dt);
        RefreshButton();
    }

    private void TryStartRain()
    {
        if (!GameManager.IsGameActive) return;
        if (IsRaining || cooldownLeft > 0f) return;
        if (currency == null || currency.totalCoins < cost) return;

        currency.RemoveCoins(cost);

        rainLeft = duration;
        Enemy.WeatherSpeedScale = speedScale;

        if (overlay != null && !overlay.activeSelf)
        {
            overlay.SetActive(true);
            ScatterDrops();
        }
        RefreshButton();
    }

    private void StopRain()
    {
        rainLeft = 0f;
        Enemy.WeatherSpeedScale = 1f;
    }

    private void RefreshButton()
    {
        if (button == null) return;

        bool affordable = currency != null && currency.totalCoins >= cost;
        bool usable = !IsRaining && cooldownLeft <= 0f && affordable;
        button.interactable = usable;

        // 押せない間は文字も薄くする（ボタンの地の色が変わるだけでは分かりにくい）
        float alpha = usable ? 1f : 0.55f;
        if (!Mathf.Approximately(label.alpha, alpha))
        {
            label.alpha = alpha;
            subText.alpha = alpha;
        }

        // 表示が変わるときだけ文字を作り直す（毎フレーム文字列を作らない）
        int mode = IsRaining ? 1 : (cooldownLeft > 0f ? 2 : 0);
        int value = mode == 1 ? Mathf.CeilToInt(rainLeft)
                  : mode == 2 ? Mathf.CeilToInt(cooldownLeft)
                  : cost;
        int key = mode * 1000000 + value;
        if (key == shownSubKey) return;
        shownSubKey = key;

        if (mode == 1) subText.text = $"あと {value} 秒";
        else if (mode == 2) subText.text = $"次まで {value} 秒";
        else subText.text = $"{value}円";
    }

    // ───── 雨の見た目 ─────

    private void UpdateOverlay(float dt)
    {
        if (overlay == null || !overlay.activeSelf) return;

        // 降り始めと降りやみは少しずつ濃く・薄くする
        float target = IsRaining ? 1f : 0f;
        intensity = Mathf.MoveTowards(intensity, target, dt / FadeSeconds);
        overlayGroup.alpha = intensity;

        if (intensity <= 0f && !IsRaining)
        {
            overlay.SetActive(false);
            return;
        }

        Vector2 half = HalfArea();
        float halfW = half.x;
        float halfH = half.y;

        for (int i = 0; i < drops.Length; i++)
        {
            Vector2 p = drops[i].anchoredPosition + Fall * (dropSpeeds[i] * dt);

            // 下か左へ抜けたら、上か右から入り直す
            if (p.y < -halfH) { p.y += halfH * 2f; p.x = Random.Range(-halfW, halfW); }
            if (p.x < -halfW) p.x += halfW * 2f;

            drops[i].anchoredPosition = p;
        }
    }

    /// <summary>雨粒を画面全体にばらまく。降り始めに上から順に落ちてくるのを待たせない</summary>
    private void ScatterDrops()
    {
        Vector2 half = HalfArea();

        foreach (RectTransform d in drops)
        {
            d.anchoredPosition = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
        }
    }

    /// <summary>
    /// 雨粒を動かす範囲の半分の大きさ（画面中央が原点）。画面の外に雨粒 1 本ぶんの余白を取る。
    ///
    /// Canvas の矩形（rect）は有効にした直後だとまだ画面の大きさになっていないことがあるので、
    /// 画面の大きさを CanvasScaler が決めた倍率で割って求める。
    /// </summary>
    private Vector2 HalfArea()
    {
        float scale = Mathf.Max(0.0001f, overlayCanvas.scaleFactor);
        return new Vector2(Screen.width / scale / 2f + DropSize.y,
                           Screen.height / scale / 2f + DropSize.y);
    }

    private void HideOverlayNow()
    {
        intensity = 0f;
        if (overlayGroup != null) overlayGroup.alpha = 0f;
        if (overlay != null && overlay.activeSelf) overlay.SetActive(false);
    }
}
