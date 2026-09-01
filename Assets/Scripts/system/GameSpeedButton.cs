using UnityEngine;
using TMPro;

/// <summary>
/// 等速と ×1.2 を切り替えるボタン。
///
/// GameSpeed は静的クラスなので UnityEvent から直接は呼べない。
/// ボタンの OnClick はこのコンポーネントの Toggle() を指す。
/// </summary>
public class GameSpeedButton : MonoBehaviour
{
    [Tooltip("現在の速度を出すラベル。未設定なら子から探す")]
    [SerializeField] private TextMeshProUGUI label;

    [SerializeField] private string normalText = "×1.0";
    [SerializeField] private string fastText = "×1.5";

    void Awake()
    {
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        // 前のステージで選んだ速度が保持されているので、表示を合わせ直す
        Refresh();
    }

    /// <summary>ボタンの OnClick に登録する</summary>
    public void Toggle()
    {
        GameSpeed.Toggle();
        Refresh();
    }

    private void Refresh()
    {
        if (label != null)
        {
            label.text = GameSpeed.IsFast ? fastText : normalText;
        }
    }
}
