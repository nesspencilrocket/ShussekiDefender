using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 設定画面。音量・明るさ・ゲーム速度を変える。
///
/// 値は GameSettings と GameSpeed が PlayerPrefs に持っているので、
/// この画面は表示と入力だけを担当し、状態を抱えない。
/// 変更はスライダーを動かした瞬間に反映されるので、決定ボタンは置いていない。
///
/// 効果音の音量は項目に出していない。現在シーンに効果音の AudioSource が
/// 1 つも無く、動かしても何も起きないスライダーになるため。
/// 効果音を入れたら GameSettings に SeVolume を足してここに 1 行増やす。
/// </summary>
public class SettingsScreen : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("日本語が出るフォント。未設定だと豆腐になる")]
    [SerializeField] private TMP_FontAsset font;

    [Header("戻り先")]
    [SerializeField] private string backSceneName = "StartMenu";

    [Header("背景")]
    [Tooltip("タイトル画面と同じ絵を敷く。未設定なら下の background 色で塗る")]
    [SerializeField] private Sprite backgroundImage;

    [Tooltip("背景に重ねる黒の濃さ。上げるほど暗くなり文字が読みやすくなる")]
    [Range(0f, 1f)]
    [SerializeField] private float scrimAlpha = 0.82f;

    [Header("配色")]
    [SerializeField] private Color background = new Color(0.055f, 0.078f, 0.118f, 1f);
    [SerializeField] private Color textMain = new Color(0.91f, 0.93f, 0.96f, 1f);
    [SerializeField] private Color textMuted = new Color(0.54f, 0.59f, 0.66f, 1f);
    [SerializeField] private Color accent = new Color(0.5f, 0.66f, 0.91f, 1f);
    [SerializeField] private Color track = new Color(1f, 1f, 1f, 0.13f);
    [SerializeField] private Color buttonFill = new Color(0.16f, 0.22f, 0.32f, 1f);

    // ───── 配置 ─────
    private const float ROW_TOP = 200f;
    private const float ROW_STEP = 130f;
    private const float COL_LABEL = -560f;
    private const float COL_CONTROL = 60f;
    private const float COL_VALUE = 470f;

    private TextMeshProUGUI masterValue;
    private TextMeshProUGUI bgmValue;
    private TextMeshProUGUI brightnessValue;
    private TextMeshProUGUI speedLabel;

    private Slider masterSlider;
    private Slider bgmSlider;
    private Slider brightnessSlider;

    private void Start()
    {
        GameSpeed.Resume();
        Build();
    }

    private void Build()
    {
        Canvas canvas = MenuUI.CreateCanvas(gameObject);
        Transform root = canvas.transform;

        MenuUI.Background(root, backgroundImage, background, scrimAlpha);

        MenuUI.Text(root, "Title", "設定", new Vector2(0f, 400f), new Vector2(900f, 110f),
                    72f, textMain, font, TextAlignmentOptions.Center);

        MenuUI.Panel(root, "HeadRule", new Vector2(0f, 320f), new Vector2(1360f, 2f),
                     new Color(1f, 1f, 1f, 0.16f));

        // 1行目 ── 全体の音量
        float y = ROW_TOP;
        Label(root, "全体の音量", y);
        masterSlider = MenuUI.HorizontalSlider(root, "MasterSlider",
            new Vector2(COL_CONTROL, y), new Vector2(560f, 44f),
            0f, 1f, GameSettings.MasterVolume, track, accent, textMain);
        masterValue = Value(root, "MasterValue", y);
        masterSlider.onValueChanged.AddListener(v =>
        {
            GameSettings.MasterVolume = v;
            masterValue.text = Percent(v);
        });
        masterValue.text = Percent(GameSettings.MasterVolume);

        // 2行目 ── BGM の音量
        y -= ROW_STEP;
        Label(root, "BGM の音量", y);
        bgmSlider = MenuUI.HorizontalSlider(root, "BgmSlider",
            new Vector2(COL_CONTROL, y), new Vector2(560f, 44f),
            0f, 1f, GameSettings.BgmVolume, track, accent, textMain);
        bgmValue = Value(root, "BgmValue", y);
        bgmSlider.onValueChanged.AddListener(v =>
        {
            GameSettings.BgmVolume = v;
            bgmValue.text = Percent(v);
        });
        bgmValue.text = Percent(GameSettings.BgmVolume);

        // 3行目 ── 明るさ
        y -= ROW_STEP;
        Label(root, "画面の明るさ", y);
        brightnessSlider = MenuUI.HorizontalSlider(root, "BrightnessSlider",
            new Vector2(COL_CONTROL, y), new Vector2(560f, 44f),
            GameSettings.MIN_BRIGHTNESS, GameSettings.MAX_BRIGHTNESS,
            GameSettings.Brightness, track, accent, textMain);
        brightnessValue = Value(root, "BrightnessValue", y);
        brightnessSlider.onValueChanged.AddListener(v =>
        {
            GameSettings.Brightness = v;
            brightnessValue.text = Percent(v);
        });
        brightnessValue.text = Percent(GameSettings.Brightness);

        // 4行目 ── ゲーム速度
        y -= ROW_STEP;
        Label(root, "ゲーム速度", y);
        Button speedButton = MenuUI.TextButton(root, "SpeedButton", "",
            new Vector2(COL_CONTROL - 140f, y), new Vector2(280f, 66f),
            30f, buttonFill, textMain, font);
        speedLabel = speedButton.GetComponentInChildren<TextMeshProUGUI>();
        speedButton.onClick.AddListener(() =>
        {
            GameSpeed.Toggle();
            RefreshSpeedLabel();
        });
        RefreshSpeedLabel();

        // 下段のボタン
        Button reset = MenuUI.TextButton(root, "ResetButton", "初期設定に戻す",
                                         new Vector2(-190f, -400f), new Vector2(320f, 76f),
                                         28f, buttonFill, textMain, font);
        reset.onClick.AddListener(ResetAll);

        Button back = MenuUI.TextButton(root, "BackButton", "戻る",
                                        new Vector2(190f, -400f), new Vector2(320f, 76f),
                                        32f, buttonFill, textMain, font);
        back.onClick.AddListener(() => SceneManager.LoadScene(backSceneName));
    }

    private void Label(Transform root, string body, float y)
    {
        MenuUI.Text(root, body, body, new Vector2(COL_LABEL, y), new Vector2(340f, 56f),
                    34f, textMain, font, TextAlignmentOptions.Left);
    }

    private TextMeshProUGUI Value(Transform root, string name, float y)
    {
        return MenuUI.Text(root, name, "", new Vector2(COL_VALUE, y), new Vector2(180f, 56f),
                           32f, accent, font, TextAlignmentOptions.Right);
    }

    private static string Percent(float v)
    {
        return Mathf.RoundToInt(v * 100f) + "%";
    }

    private void RefreshSpeedLabel()
    {
        if (speedLabel != null)
        {
            speedLabel.text = GameSpeed.IsFast ? "×1.5（早送り）" : "×1.0（標準）";
        }
    }

    /// <summary>
    /// スライダーを動かすと onValueChanged 経由で GameSettings にも書かれるので、
    /// ここでは値を戻すだけでよい。表示の更新もリスナ側で行われる。
    /// </summary>
    private void ResetAll()
    {
        GameSettings.ResetToDefault();
        GameSpeed.Select(GameSpeed.NORMAL);

        if (masterSlider != null) masterSlider.value = GameSettings.MasterVolume;
        if (bgmSlider != null) bgmSlider.value = GameSettings.BgmVolume;
        if (brightnessSlider != null) brightnessSlider.value = GameSettings.Brightness;
        RefreshSpeedLabel();
    }
}
