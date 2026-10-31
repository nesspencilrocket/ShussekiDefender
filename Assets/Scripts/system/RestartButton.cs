using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 本編の画面左下（いちばん左）に置く「リスタート」ボタン。今のステージを最初からやり直す。
///
/// 押し間違いで進行を失わないよう、2 回押して確定する。
///   1 回目 … 「もう一度押すとやり直す」に変わる（色も変わる）
///   2 回目 … 3 秒以内に押すとやり直す。3 秒たつと元に戻る
///
/// やり直しの中身は、結果画面の「Retry」と同じ GameManager.OnRetryButtonClicked。
/// UI は実行時に MenuUI で組み立てるので、シーンにはこのコンポーネントと
/// フォントの参照だけを置けばよい。本編中だけ表示し、カウントダウンとリザルトの間は隠す。
/// </summary>
public class RestartButton : MonoBehaviour
{
    [Header("見た目")]
    [Tooltip("日本語が出るフォント")]
    [SerializeField] private TMP_FontAsset font;

    [Tooltip("画面左下の角からの位置（ボタンの左下角）")]
    [SerializeField] private Vector2 position = new Vector2(40f, 40f);

    [Tooltip("1 回目を押してから、2 回目で確定できる秒数")]
    [Min(0.5f)] [SerializeField] private float confirmSeconds = 3f;

    /// <summary>
    /// ボタンの幅。右隣の必殺技ボタン（SkillBar）は、この幅と間隔の分だけ右へずらして置いてある
    /// </summary>
    public static readonly Vector2 Size = new Vector2(150f, 96f);

    private static readonly Color IdleColor = new Color(0.36f, 0.20f, 0.20f, 1f);
    private static readonly Color ArmedColor = new Color(0.80f, 0.42f, 0.12f, 1f);
    private static readonly Color TextColor = new Color(0.96f, 0.96f, 0.98f, 1f);

    private GameObject buttonRoot;
    private Button button;
    private Image background;
    private TextMeshProUGUI label;

    private bool armed;
    private float armedUntil;

    private void Start()
    {
        Build();
        Refresh();
    }

    private void Build()
    {
        Canvas canvas = MenuUI.CreateCanvas(gameObject);
        // リザルトや開始カウントダウンより下に置く。本編中だけの UI
        canvas.sortingOrder = -1;

        button = MenuUI.TextButton(transform, "RestartButton", "", Vector2.zero, Size,
                                   26f, IdleColor, TextColor, font);
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = position;

        background = button.GetComponent<Image>();
        label = button.GetComponentInChildren<TextMeshProUGUI>();
        // 2 行（「もう一度押すと / やり直す」）を折り返せるようにする
        label.textWrappingMode = TextWrappingModes.Normal;
        label.fontSize = 26f;

        buttonRoot = button.gameObject;
        button.onClick.AddListener(OnClicked);
        buttonRoot.SetActive(false);
    }

    private void Update()
    {
        if (buttonRoot == null) return;

        bool active = GameManager.IsGameActive;
        if (buttonRoot.activeSelf != active) buttonRoot.SetActive(active);

        // 時間切れで元に戻す。倍速でも体感の 3 秒になるよう、実時間で数える
        if (armed && Time.unscaledTime >= armedUntil)
        {
            armed = false;
            Refresh();
        }
    }

    private void OnClicked()
    {
        if (!GameManager.IsGameActive) return;

        if (!armed)
        {
            armed = true;
            armedUntil = Time.unscaledTime + confirmSeconds;
            Refresh();
            return;
        }

        armed = false;
        if (GameManager.Instance != null) GameManager.Instance.OnRetryButtonClicked();
    }

    private void Refresh()
    {
        if (label == null) return;
        label.text = armed ? "もう一度押すと\nやり直す" : "リスタート";
        if (armed) label.fontSize = 22f; else label.fontSize = 26f;
        background.color = armed ? ArmedColor : IdleColor;
    }
}
