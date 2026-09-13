using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 成績確認画面。1限目〜6限目のベストスコアと、そのときの処分を並べる。
///
/// 【何を出すか】
/// 保存しているのは StageProgress のベストスコアとクリア到達点だけなので、
/// 出せるのはこの 2 つから導ける情報に限られる。撃破数や生存時間は
/// ステージ内でしか集計しておらず、記録に残していない。
///
/// スコアの数字だけだと大小しか分からないため、処分名を主役にしている。
/// このゲームはスコアを稼ぐほど重い処分になるので、
/// 「6限目で退学処分を取れた」の方がプレイヤーの記憶に残る。
/// あわせて次の処分までの不足点も出し、もう一度遊ぶ理由を作る。
/// </summary>
public class RecordsScreen : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("StageCatalog アセット。時限名と処分の閾値をここから読む")]
    [SerializeField] private StageCatalog catalog;

    [Tooltip("日本語が出るフォント。未設定だと豆腐になる")]
    [SerializeField] private TMP_FontAsset font;

    [Header("戻り先")]
    [SerializeField] private string backSceneName = "StartMenu";

    [Header("背景")]
    [Tooltip("タイトル画面と同じ絵を敷く。未設定なら下の background 色で塗る")]
    [SerializeField] private Sprite backgroundImage;

    [Tooltip("背景に重ねる黒の濃さ。上げるほど暗くなり文字が読みやすくなる")]
    [Range(0f, 1f)]
    [SerializeField] private float scrimAlpha = 0.82f;

    [Header("配色")]
    [SerializeField] private Color background = new Color(0.055f, 0.078f, 0.118f, 1f);
    [SerializeField] private Color rowEven = new Color(1f, 1f, 1f, 0.035f);
    [SerializeField] private Color textMain = new Color(0.91f, 0.93f, 0.96f, 1f);
    [SerializeField] private Color textMuted = new Color(0.54f, 0.59f, 0.66f, 1f);
    [SerializeField] private Color accent = new Color(0.5f, 0.66f, 0.91f, 1f);
    [SerializeField] private Color buttonFill = new Color(0.16f, 0.22f, 0.32f, 1f);

    [Tooltip("処分の色。軽い順に 訓告 / 厳重注意 / 1週間停学 / 無期限停学 / 退学処分")]
    [SerializeField]
    private Color[] rankColors =
    {
        new Color(0.60f, 0.64f, 0.70f, 1f),
        new Color(0.55f, 0.76f, 0.85f, 1f),
        new Color(0.90f, 0.82f, 0.45f, 1f),
        new Color(0.94f, 0.63f, 0.35f, 1f),
        new Color(0.93f, 0.40f, 0.36f, 1f),
    };

    // ───── 配置 ─────
    private const float ROW_TOP = 210f;
    private const float ROW_STEP = 84f;
    private const float COL_STAGE = -540f;
    private const float COL_RANK = -170f;
    private const float COL_SCORE = 280f;
    private const float COL_NEXT = 540f;

    private void Start()
    {
        // 敗北で止めたまま戻ってくると、ボタンのアニメーションが動かない
        GameSpeed.Resume();
        Build();
    }

    private void Build()
    {
        Canvas canvas = MenuUI.CreateCanvas(gameObject);
        Transform root = canvas.transform;

        MenuUI.Background(root, backgroundImage, background, scrimAlpha);

        MenuUI.Text(root, "Title", "成績確認", new Vector2(0f, 400f), new Vector2(900f, 110f),
                    72f, textMain, font, TextAlignmentOptions.Center);

        // 見出し行
        MenuUI.Text(root, "HeadStage", "時限", new Vector2(COL_STAGE, 300f), new Vector2(220f, 44f),
                    26f, textMuted, font, TextAlignmentOptions.Left);
        MenuUI.Text(root, "HeadRank", "最高記録の処分", new Vector2(COL_RANK, 300f), new Vector2(440f, 44f),
                    26f, textMuted, font, TextAlignmentOptions.Center);
        MenuUI.Text(root, "HeadScore", "ベストスコア", new Vector2(COL_SCORE, 300f), new Vector2(260f, 44f),
                    26f, textMuted, font, TextAlignmentOptions.Right);

        MenuUI.Panel(root, "HeadRule", new Vector2(0f, 272f), new Vector2(1360f, 2f),
                     new Color(1f, 1f, 1f, 0.16f));

        if (catalog == null || catalog.stages == null || catalog.stages.Count == 0)
        {
            MenuUI.Text(root, "NoData", "StageCatalog が設定されていません",
                        new Vector2(0f, 60f), new Vector2(1200f, 60f),
                        32f, textMuted, font, TextAlignmentOptions.Center);
            BuildBackButton(root);
            Debug.LogError("RecordsScreen: catalog が未設定です。", this);
            return;
        }

        int total = 0;
        string heaviest = null;
        int heaviestIndex = -1;

        for (int i = 0; i < catalog.stages.Count; i++)
        {
            StageData stage = catalog.stages[i];
            if (stage == null) continue;

            float y = ROW_TOP - i * ROW_STEP;

            // 1 行おきに薄く敷いて、横に目が滑らないようにする
            if (i % 2 == 0)
            {
                MenuUI.Panel(root, $"RowBg{i}", new Vector2(0f, y), new Vector2(1360f, ROW_STEP - 8f), rowEven);
            }

            bool unlocked = StageProgress.IsUnlocked(stage.stageNumber);
            int best = StageProgress.GetBestScore(stage.stageNumber);

            string label = string.IsNullOrEmpty(stage.displayName)
                ? $"{stage.stageNumber}限目" : stage.displayName;

            MenuUI.Text(root, $"Stage{i}", label, new Vector2(COL_STAGE, y), new Vector2(220f, 56f),
                        36f, unlocked ? textMain : textMuted, font, TextAlignmentOptions.Left);

            if (!unlocked)
            {
                MenuUI.Text(root, $"Rank{i}", "未開放", new Vector2(COL_RANK, y), new Vector2(440f, 56f),
                            30f, textMuted, font, TextAlignmentOptions.Center);
                MenuUI.Text(root, $"Score{i}", "—", new Vector2(COL_SCORE, y), new Vector2(260f, 56f),
                            32f, textMuted, font, TextAlignmentOptions.Right);
                continue;
            }

            if (best <= 0)
            {
                MenuUI.Text(root, $"Rank{i}", "記録なし", new Vector2(COL_RANK, y), new Vector2(440f, 56f),
                            30f, textMuted, font, TextAlignmentOptions.Center);
                MenuUI.Text(root, $"Score{i}", "—", new Vector2(COL_SCORE, y), new Vector2(260f, 56f),
                            32f, textMuted, font, TextAlignmentOptions.Right);
                continue;
            }

            total += best;

            string rank = stage.RankOf(best);
            int rankIndex = System.Array.IndexOf(StageData.Ranks, rank);
            if (rankIndex > heaviestIndex)
            {
                heaviestIndex = rankIndex;
                heaviest = rank;
            }

            MenuUI.Text(root, $"Rank{i}", rank, new Vector2(COL_RANK, y), new Vector2(440f, 56f),
                        38f, RankColor(rankIndex), font, TextAlignmentOptions.Center);

            // 右寄せにしておくと桁が縦に揃い、ステージ間で比べやすい
            MenuUI.Text(root, $"Score{i}", best.ToString("N0"),
                        new Vector2(COL_SCORE, y), new Vector2(260f, 56f),
                        36f, textMain, font, TextAlignmentOptions.Right);

            int next = stage.NextRankScore(best);
            if (next > 0)
            {
                MenuUI.Text(root, $"Next{i}", $"あと {next - best:N0}",
                            new Vector2(COL_NEXT, y), new Vector2(240f, 56f),
                            24f, textMuted, font, TextAlignmentOptions.Left);
            }
            else
            {
                MenuUI.Text(root, $"Next{i}", "最高評価", new Vector2(COL_NEXT, y), new Vector2(240f, 56f),
                            24f, accent, font, TextAlignmentOptions.Left);
            }
        }

        // 合計
        float footY = ROW_TOP - catalog.stages.Count * ROW_STEP - 30f;
        MenuUI.Panel(root, "FootRule", new Vector2(0f, footY + 40f), new Vector2(1360f, 2f),
                     new Color(1f, 1f, 1f, 0.16f));

        MenuUI.Text(root, "TotalLabel", "合計スコア", new Vector2(COL_STAGE, footY), new Vector2(300f, 56f),
                    28f, textMuted, font, TextAlignmentOptions.Left);
        MenuUI.Text(root, "TotalValue", total.ToString("N0"), new Vector2(COL_SCORE, footY), new Vector2(260f, 56f),
                    38f, accent, font, TextAlignmentOptions.Right);

        string reached = StageProgress.ClearedUpTo <= 0
            ? "まだクリアしていません"
            : $"{StageProgress.ClearedUpTo}限目までクリア";
        string heaviestNote = string.IsNullOrEmpty(heaviest) ? "" : $"　最も重い処分：{heaviest}";
        MenuUI.Text(root, "Summary", reached + heaviestNote,
                    new Vector2(0f, footY - 56f), new Vector2(1360f, 50f),
                    26f, textMuted, font, TextAlignmentOptions.Center);

        BuildBackButton(root);
    }

    private Color RankColor(int index)
    {
        if (rankColors == null || rankColors.Length == 0) return textMain;
        return rankColors[Mathf.Clamp(index, 0, rankColors.Length - 1)];
    }

    private void BuildBackButton(Transform root)
    {
        Button back = MenuUI.TextButton(root, "BackButton", "戻る",
                                        new Vector2(0f, -450f), new Vector2(320f, 84f),
                                        34f, buttonFill, textMain, font);
        back.onClick.AddListener(() => SceneManager.LoadScene(backSceneName));
    }
}
