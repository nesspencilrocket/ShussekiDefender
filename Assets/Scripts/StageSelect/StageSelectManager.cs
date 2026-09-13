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

    [Header("ステージの紹介映像")]
    [Tooltip("映像を映している RawImage の GameObject。時限を選ぶまで隠しておく")]
    public GameObject previewObj;

    [Header("タイトルへ戻るボタン")]
    [Tooltip("実行時に作るボタンの文字。日本語が出るフォントを指定する")]
    [SerializeField] private TMP_FontAsset backButtonFont;

    [SerializeField] private string titleSceneName = "StartMenu";

    private StageData selected;

    void Start()
    {
        // 敗北直後に戻ってきた場合の保険。止まったままだとボタンが押せない
        GameSpeed.Resume();

        // 説明文・行くボタン・映像は、時限を選ぶまで出さない。
        // 何も選んでいないのに 1限目の映像が出ていると、選択済みだと誤解される。
        if (descriptionText != null) descriptionText.text = "";
        if (startButtonObj != null) startButtonObj.SetActive(false);
        if (previewObj != null) previewObj.SetActive(false);

        RefreshLocks();
        CreateBackButton();
    }

    /// <summary>
    /// タイトルへ戻るボタンを実行時に作る。
    /// 時限ボタンと同じ Canvas に置くので、シーン側の配置は不要。
    /// </summary>
    private void CreateBackButton()
    {
        Canvas canvas = null;
        foreach (Button b in stageButtons)
        {
            if (b == null) continue;
            canvas = b.GetComponentInParent<Canvas>();
            if (canvas != null) break;
        }
        if (canvas == null)
        {
            Debug.LogWarning("StageSelectManager: Canvas が見つからず戻るボタンを作れません。", this);
            return;
        }

        // 6限目ボタンの下（画面左下）に置く
        Button back = MenuUI.TextButton(canvas.transform, "BackToTitleButton", "タイトルへ戻る",
                                        new Vector2(-540f, -462f), new Vector2(300f, 76f),
                                        30f,
                                        new Color(0.12f, 0.16f, 0.24f, 0.92f),
                                        Color.white,
                                        backButtonFont);
        back.gameObject.AddComponent<UIHoverScale>();
        back.onClick.AddListener(() =>
        {
            GameSpeed.Resume();
            SceneManager.LoadScene(titleSceneName);
        });
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
        if (previewObj != null) previewObj.SetActive(true);
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
