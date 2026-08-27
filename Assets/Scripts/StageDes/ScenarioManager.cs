using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScenarioManager : MonoBehaviour
{
    // --- インスペクターで設定するUIパーツ ---
    [Header("UIパーツの割り当て")]
    public Text mainTextField;           // 文章を表示するテキスト
    public Button[] actionButtons;       // 6つのボタン配列
    public Text[] buttonLabels;          // 各ボタンのラベルテキスト（画像のみなら不要可）

    // --- シナリオデータの構造定義 ---
    [System.Serializable]
    public class ButtonOption
    {
        public string buttonText;        // ボタンに表示する文字
        public int nextScenarioIndex;    // このボタンを押した時に飛ぶシナリオID
        public Sprite buttonImage;       // ボタンの画像を変えたい場合（オプション）
    }

    [System.Serializable]
    public class ScenarioData
    {
        [TextArea(3, 5)]
        public string mainText;          // 表示されるメインの文章
        public List<ButtonOption> options; // この場面で有効なボタン設定リスト
    }

    // --- データ本体 ---
    [Header("シナリオデータ設定")]
    public List<ScenarioData> scenarioList;

    // 現在の進行状況
    private int currentIndex = 0;

    void Start()
    {
        // 最初にID 0 のシナリオを表示
        ShowScenario(0);
    }

    // シナリオを表示・更新する関数
    public void ShowScenario(int index)
    {
        // インデックスが範囲外ならエラー回避
        if (index < 0 || index >= scenarioList.Count)
        {
            Debug.LogError("指定されたシナリオIDが存在しません: " + index);
            return;
        }

        currentIndex = index;
        ScenarioData currentData = scenarioList[index];

        // 1. メインテキストの更新
        mainTextField.text = currentData.mainText;

        // 2. ボタンの更新（6つのボタンをループして設定）
        for (int i = 0; i < actionButtons.Length; i++)
        {
            // データリストに設定がある場合だけボタンを有効化
            if (i < currentData.options.Count)
            {
                actionButtons[i].gameObject.SetActive(true);

                // ボタンの文字更新
                if (buttonLabels.Length > i && buttonLabels[i] != null)
                {
                    buttonLabels[i].text = currentData.options[i].buttonText;
                }

                // ボタンの画像を変えたい場合の処理例（Spriteが設定されていれば）
                if (currentData.options[i].buttonImage != null)
                {
                    actionButtons[i].GetComponent<Image>().sprite = currentData.options[i].buttonImage;
                }

                // ボタンクリック時の動作を登録
                // 注意: ループ内の変数をクロージャでキャプチャするためにローカル変数に置く
                int nextIndex = currentData.options[i].nextScenarioIndex;
                actionButtons[i].onClick.RemoveAllListeners(); // 前のイベントを削除
                actionButtons[i].onClick.AddListener(() => OnOptionClicked(nextIndex));
            }
            else
            {
                // 設定がないボタンは非表示にする
                actionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // ボタンが押された時の処理
    void OnOptionClicked(int nextIndex)
    {
        // 指定された次のシナリオIDへ移動
        ShowScenario(nextIndex);
    }
}