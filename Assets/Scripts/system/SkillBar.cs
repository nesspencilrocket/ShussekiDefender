using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 画面左下に必殺技のボタンを並べ、クールダウンを見せる。
///
/// ボタンは実行時に MenuUI で組み立てる。ステージが 6 枚に増えても
/// シーンごとに UI を作り直さなくて済むようにするため。
/// 技の一覧はステージの StageData.skills から PlayerSkillController が読む。
///
/// 数字キー（1, 2, …）でも発動できる。武器の設置でマウスが忙しいときに
/// キーで撃てる方が扱いやすい。
/// </summary>
public class SkillBar : MonoBehaviour
{
    [Header("見た目")]
    [Tooltip("ボタンの文字。日本語が出るフォントを指定する")]
    [SerializeField] private TMP_FontAsset font;

    [Tooltip("左下からの位置")]
    [SerializeField] private Vector2 origin = new Vector2(40f, 40f);

    [SerializeField] private Vector2 buttonSize = new Vector2(230f, 96f);
    [SerializeField] private float spacing = 16f;

    private PlayerSkillController controller;
    private GameObject bar;

    private readonly List<Button> buttons = new List<Button>();
    private readonly List<Image> cooldownMasks = new List<Image>();
    private readonly List<TextMeshProUGUI> cooldownLabels = new List<TextMeshProUGUI>();

    // 発動に使うキー。技の並び順と対応する
    private static readonly Key[] Hotkeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };

    private void Start()
    {
        controller = PlayerSkillController.Ensure();

        // Controller の Start より先に走った場合に備えて自分で読み込む
        StageData stage = (GameManager.Instance != null) ? GameManager.Instance.Stage : null;
        if (controller.Skills.Count == 0) controller.Load(stage);

        Build();
    }

    private void Build()
    {
        Canvas canvas = MenuUI.CreateCanvas(gameObject);
        // リザルトや開始カウントダウンより下に置く。技は本編中だけの UI
        canvas.sortingOrder = -1;

        bar = new GameObject("Bar", typeof(RectTransform));
        RectTransform barRt = bar.GetComponent<RectTransform>();
        barRt.SetParent(transform, false);
        barRt.anchorMin = Vector2.zero;
        barRt.anchorMax = Vector2.zero;
        barRt.pivot = Vector2.zero;
        barRt.anchoredPosition = origin;

        IReadOnlyList<PlayerSkill> skills = controller.Skills;
        for (int i = 0; i < skills.Count; i++)
        {
            CreateButton(barRt, i, skills[i]);
        }
    }

    private void CreateButton(Transform parent, int index, PlayerSkill skill)
    {
        // 左下原点で右へ並べる。pivot が左下なので pos は左下角の位置
        Vector2 pos = new Vector2(index * (buttonSize.x + spacing), 0f);
        string hint = (index < Hotkeys.Length) ? $"[{index + 1}] " : "";
        string label = hint + skill.displayName;
        if (skill.coinCost > 0) label += $"\n{skill.coinCost} コイン";

        Button btn = MenuUI.TextButton(parent, $"Skill_{index}", label,
                                       Vector2.zero, buttonSize, 28f,
                                       new Color(0.16f, 0.12f, 0.24f, 0.92f),
                                       Color.white, font);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = pos;

        // ボタンの文字は複数行になりうるので折り返しを許す
        TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.textWrappingMode = TextWrappingModes.Normal;

        btn.gameObject.AddComponent<UIHoverScale>();

        // クールダウンの幕。上から下へ縮んで、明けたら消える
        RectTransform maskRt = MenuUI.Stretch(rt, "Cooldown");
        Image mask = maskRt.gameObject.AddComponent<Image>();
        mask.color = new Color(0f, 0f, 0f, 0.65f);
        mask.raycastTarget = false;
        mask.type = Image.Type.Filled;
        mask.fillMethod = Image.FillMethod.Vertical;
        mask.fillOrigin = (int)Image.OriginVertical.Bottom;
        mask.fillAmount = 0f;

        TextMeshProUGUI remain = MenuUI.Text(rt, "Remain", "", Vector2.zero, buttonSize,
                                             36f, Color.white, font, TextAlignmentOptions.Center);
        remain.rectTransform.anchorMin = Vector2.zero;
        remain.rectTransform.anchorMax = Vector2.one;
        remain.rectTransform.offsetMin = Vector2.zero;
        remain.rectTransform.offsetMax = Vector2.zero;

        int captured = index;
        btn.onClick.AddListener(() => controller.TryActivate(captured));

        buttons.Add(btn);
        cooldownMasks.Add(mask);
        cooldownLabels.Add(remain);
    }

    private void Update()
    {
        if (controller == null || bar == null) return;

        // 本編中だけ出す。カウントダウン中とリザルト中は隠す
        bool show = GameManager.IsGameActive;
        if (bar.activeSelf != show) bar.SetActive(show);
        if (!show) return;

        for (int i = 0; i < buttons.Count; i++)
        {
            float ratio = controller.CooldownRatio(i);
            cooldownMasks[i].fillAmount = ratio;

            float remaining = controller.CooldownRemaining(i);
            string label = (remaining > 0f) ? Mathf.CeilToInt(remaining).ToString() : "";
            if (cooldownLabels[i].text != label) cooldownLabels[i].text = label;

            buttons[i].interactable = controller.CanActivate(i);
        }

        ReadHotkeys();
    }

    private void ReadHotkeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        int n = Mathf.Min(buttons.Count, Hotkeys.Length);
        for (int i = 0; i < n; i++)
        {
            if (kb[Hotkeys[i]].wasPressedThisFrame)
            {
                controller.TryActivate(i);
            }
        }
    }
}
