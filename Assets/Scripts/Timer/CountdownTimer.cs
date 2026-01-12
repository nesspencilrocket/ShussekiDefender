using UnityEngine;
using TMPro; // TextMesh Proを使用するために必要

public class CountdownTimerTMP : MonoBehaviour
{
    // 公開変数: Inspectorから TextMeshProUGUI コンポーネントをアタッチできるようにする
    public TextMeshProUGUI timerText;

    // ★★★ 修正箇所 ★★★
    // [SerializeField] をつけることで、private変数でありながらInspectorから値を設定できるようになります。
    [SerializeField]
    [Tooltip("カウントダウン開始時の初期時間（秒）を設定します。")]
    private float startTime = 30f; // カウントダウン開始時の時間（秒）

    private float currentTime;     // 現在の時間

    void Start()
    {
        // ゲーム開始時に現在の時間を初期値に設定
        currentTime = startTime;

        // 初期のテキスト表示を更新
        UpdateTimerDisplay();
    }

    void Update()
    {
        // 毎フレーム、経過時間（Time.deltaTime）を減算する
        if (currentTime > 0)
        {
            // 時間の減算
            currentTime -= Time.deltaTime;

            // 負の値にならないように0でクランプ（制限）する
            if (currentTime < 0)
            {
                currentTime = 0;
            }

            // UI表示の更新
            UpdateTimerDisplay();
        }
        else
        {
            // カウントが0になった時の処理（ここに必要な処理を追加します）
            // Debug.Log("カウントダウン終了！");
        }
    }

    // タイマーの表示を更新する関数
    void UpdateTimerDisplay()
    {
        // 少数点以下を切り上げて整数にする（例: 30.9秒 → 31）
        int seconds = Mathf.CeilToInt(currentTime);

        // TextMeshProの text プロパティに表示
        timerText.text = seconds.ToString();
    }
}