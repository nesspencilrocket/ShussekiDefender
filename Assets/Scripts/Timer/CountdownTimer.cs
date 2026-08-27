using UnityEngine;
using TMPro;

/// <summary>
/// 残り時間の表示だけを担当する。時間の管理は GameManager 側にある。
/// 独自にカウントすると、StageData.clearTime を変えたときに
/// 表示と実際のクリア時刻がずれるため、必ず GameManager から引く。
/// </summary>
public class CountdownTimer : MonoBehaviour
{
    [Tooltip("残り秒数を表示する TextMeshPro")]
    [SerializeField] private TextMeshProUGUI timerText;

    private int lastShown = -1;

    void Update()
    {
        if (timerText == null || GameManager.Instance == null) return;

        int seconds = Mathf.CeilToInt(GameManager.Instance.RemainingTime);

        // 秒が変わったときだけ文字列を組み立てる
        if (seconds == lastShown) return;
        lastShown = seconds;
        timerText.text = seconds.ToString();
    }
}
