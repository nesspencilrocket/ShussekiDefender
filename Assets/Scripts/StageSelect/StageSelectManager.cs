using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StageSelectManager : MonoBehaviour
{
    [Header("UI パーツの割り当て")]
    [Tooltip("説明文の表示エリア")]
    public TextMeshProUGUI descriptionText;
    [Tooltip("「行く」ボタン本体（表示 / 非表示の切り替えに使う）")]
    public GameObject startButtonObj;
    [Tooltip("「行く」ボタンの中の文字")]
    public TextMeshProUGUI startButtonLabel;

    [Header("ステージ一覧")]
    [Tooltip("StageCatalog アセットを割り当てる")]
    public StageCatalog catalog;

    [Header("時限ボタン（catalog.stages と同じ順に並べる）")]
    public List<Button> stageButtons = new List<Button>();

    private StageData selected;

    void Start()
    {
        // ステージ選択に来た時点で timeScale を戻す（敗北直後に戻ってきた場合の保険）
        Time.timeScale = 1f;

        if (descriptionText != null) descriptionText.text = "";
        if (startButtonObj != null) startButtonObj.SetActive(false);

        RefreshLocks();
    }

    /// <summary>
    /// 未開放のステージはボタンを押せなくする
    /// </summary>
    private void RefreshLocks()
    {
        if (catalog == null)
        {
            Debug.LogError("StageSelectManager: catalog が未設定です。", this);
            return;
        }

        for (int i = 0; i < stageButtons.Count && i < catalog.stages.Count; i++)
        {
            if (stageButtons[i] == null || catalog.stages[i] == null) continue;
            stageButtons[i].interactable = StageProgress.IsUnlocked(catalog.stages[i].stageNumber);
        }
    }

    /// <summary>
    /// 各時限ボタンの OnClick に登録し、引数で 0〜5 を渡す
    /// </summary>
    public void OnStageImageClicked(int index)
    {
        if (catalog == null || index < 0 || index >= catalog.stages.Count) return;

        StageData data = catalog.stages[index];
        if (data == null) return;
        if (!StageProgress.IsUnlocked(data.stageNumber)) return;

        selected = data;

        if (descriptionText != null) descriptionText.text = data.description;
        if (startButtonLabel != null) startButtonLabel.text = $"{data.displayName}へ行く";
        if (startButtonObj != null) startButtonObj.SetActive(true);
    }

    /// <summary>
    /// 「行く」ボタンの OnClick に登録する
    /// </summary>
    public void OnStartButtonClick()
    {
        if (selected == null) return;

        if (string.IsNullOrEmpty(selected.sceneName))
        {
            Debug.LogWarning($"{selected.name} の sceneName が空です。Inspector を確認してください。");
            return;
        }

        // 選んだステージを次のシーンへ引き継ぐ
        StageContext.Select(selected);
        SceneManager.LoadScene(selected.sceneName);
    }
}
