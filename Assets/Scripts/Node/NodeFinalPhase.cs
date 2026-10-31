using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 時限の終盤に、一部の設置スポットを使えなくする。
///
/// ステージの StageData.finalPhaseSeconds（残りの秒数。0 なら何も起きない）になると、
/// Node.closesInFinalPhase が ON の設置スポットが、置いてある武器ごと消える。
/// 売ったときと違って、お金は戻らない。消える前に、画面上部のお知らせで予告する。
///
///   あと 10 秒で 右下・左下の設置場所が使用不可！
///   右下・左下の設置場所が使用不可！
///
/// どの設置スポットが消えるかはシーン（各 Node の設定）、いつ消えるかはステージ（StageData）で決める。
/// 6 つの時限は同じシーンを使うので、「場所はシーン、時刻はステージ」と分けてある。
///
/// 時間は GameManager.RemainingTime（ゲーム内の時間）で数えるので、倍速でも同じ場面で起きる。
/// シーンに置かなくても、Node が Ensure() で作る。
/// </summary>
public class NodeFinalPhase : MonoBehaviour
{
    /// <summary>画面に出すお知らせ（LevelBar が受け取る）</summary>
    public static Action<string> OnNotice;

    /// <summary>消える何秒前に予告するか</summary>
    private const float WarnSeconds = 10f;

    private static NodeFinalPhase instance;

    private readonly List<Node> nodes = new List<Node>();
    private bool warned;
    private bool closed;

    public static NodeFinalPhase Ensure()
    {
        if (instance != null) return instance;

        NodeFinalPhase found = FindFirstObjectByType<NodeFinalPhase>();
        if (found != null) return found;

        return new GameObject("NodeFinalPhase").AddComponent<NodeFinalPhase>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void Register(Node node)
    {
        if (node != null && !nodes.Contains(node)) nodes.Add(node);
    }

    private void Update()
    {
        if (closed || !GameManager.IsGameActive) return;

        GameManager manager = GameManager.Instance;
        StageData stage = (manager != null) ? manager.Stage : null;
        if (stage == null || stage.finalPhaseSeconds <= 0f) return;

        float remaining = manager.RemainingTime;

        if (!warned && remaining <= stage.finalPhaseSeconds + WarnSeconds)
        {
            warned = true;
            // 時限が短くて、始まった時点で予告の時間を過ぎているときは、予告を飛ばす
            if (remaining > stage.finalPhaseSeconds)
            {
                OnNotice?.Invoke($"あと {Mathf.CeilToInt(remaining - stage.finalPhaseSeconds)} 秒で {AreaNames()}の設置場所が使用不可！");
            }
        }

        if (remaining <= stage.finalPhaseSeconds)
        {
            closed = true;
            CloseAll();
        }
    }

    private void CloseAll()
    {
        string names = AreaNames();

        foreach (Node node in nodes)
        {
            if (node != null) node.CloseForFinalPhase();
        }

        // 開いていた購入・強化パネルは、消えた設置スポットのものかもしれないので閉じる
        if (UIManager.instance != null) UIManager.instance.CloseGameplayPanels();

        OnNotice?.Invoke($"{names}の設置場所が使用不可！");
    }

    // 「右下・左下」のように、消える場所の名前を並べる。名前が無ければ「一部」
    private string AreaNames()
    {
        var names = new List<string>();
        foreach (Node node in nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.AreaLabel)) continue;
            if (!names.Contains(node.AreaLabel)) names.Add(node.AreaLabel);
        }
        return names.Count > 0 ? string.Join("・", names) : "一部";
    }
}
