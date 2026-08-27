using UnityEngine;
using TMPro;

/// <summary>
/// カウントダウン表示。
/// 【重要】クラス名はファイル名 CountdownTimer と一致させること。
/// ※ STEP 11 で GameManager が残り時間を持つようになったら、
///    このクラスは GameManager.RemainingTime を映すだけの表示専用に作り替える。
/// </summary>
public class CountdownTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    [SerializeField]
    [Tooltip("カウントダウン開始時の初期時間（秒）")]
    private float startTime = 30f;

    private float currentTime;

    void Start()
    {
        currentTime = startTime;
        UpdateTimerDisplay();
    }

    void Update()
    {
        if (currentTime <= 0) return;

        currentTime -= Time.deltaTime;
        if (currentTime < 0) currentTime = 0;

        UpdateTimerDisplay();
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        timerText.text = Mathf.CeilToInt(currentTime).ToString();
    }
}
