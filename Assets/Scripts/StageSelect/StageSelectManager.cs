using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使うため必須
using UnityEngine.SceneManagement;

public class StageSelectManager : MonoBehaviour
{
    // --- 1セットのデータを定義（ここが1対1対応の肝です） ---
    [System.Serializable]
    public class StageData
    {
        public string stageName;        // 管理用メモ（例：1限目）

        [TextArea(3, 5)]
        public string description;      // 表示する説明文（例：数学の授業です）

        public string buttonLabelText;  // 【追加】文字ボタンに表示する文字（例：1限目へ行く）

        public string nextSceneName;    // 遷移先のシーン名（例：Stage1）
    }

    // --- 画面上のUIパーツ（書き換える場所） ---
    [Header("UIパーツの割り当て")]
    public TextMeshProUGUI descriptionText; // 説明文の表示エリア

    public GameObject startButtonObj;       // ボタン本体（表示/非表示用）
    public TextMeshProUGUI startButtonLabel;// 【追加】ボタンの中にある文字エリア

    // --- データリスト ---
    [Header("ステージ設定リスト")]
    public List<StageData> stageList;       // ここに6つ分のデータを登録

    // 現在選んでいるデータ番号
    private int currentSelectedIndex = -1;

    void Start()
    {
        // 最初は説明文とボタンを隠しておく
        descriptionText.text = "";
        startButtonObj.SetActive(false);
    }

    // 画像ボタンから呼ばれる (0〜5の番号を受け取る)
    public void OnStageImageClicked(int index)
    {
        if (index < 0 || index >= stageList.Count) return;

        currentSelectedIndex = index;
        UpdateUI();
    }

    // 画面を更新する処理
    void UpdateUI()
    {
        // 選ばれた番号のデータをリストから取り出す
        StageData data = stageList[currentSelectedIndex];

        // 1. 説明文を書き換える
        descriptionText.text = data.description;

        // 2. ボタンの文字を書き換える（ここが追加機能）
        startButtonLabel.text = data.buttonLabelText;

        // 3. ボタンを表示する
        startButtonObj.SetActive(true);
    }

    // 文字ボタンが押されたら実行
    public void OnStartButtonClick()
    {
        if (currentSelectedIndex != -1)
        {
            // 選ばれたデータのシーン名へ移動
            string sceneName = stageList[currentSelectedIndex].nextSceneName;

            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("シーン名が空です！Inspectorを確認してください");
            }
        }
    }
}