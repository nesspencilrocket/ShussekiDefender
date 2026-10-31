using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 設置スポット（Node）の選択を、画面の「位置」から決める。
///
/// 【なぜ UI のボタンをやめたか】
/// 以前は、各 Node に World Space の Canvas とボタンを付け、UI のクリックで選ばせていた。
/// この方式は、Node の上に重なった「クリックを受ける UI」に邪魔される。
/// 結果画面用の中身が空の文字（Text1〜Text6）が、画面の中ほどに置いたまま
/// クリックを受ける設定（raycastTarget）で残っていて、Node (2) の角が押せなかった。
/// 文字を大きくすると、Node (5) は 66% が押せなくなった。
/// 見えない UI を 1 つずつ探して直しても、また増えれば再発する。
///
/// そこで、クリックの位置を世界の座標に直し、Node の台座の範囲に入っているかを
/// 自分で調べる。UI がクリックを奪うのは、本当に操作できる UI だけにする。
///   ・ボタン・スライダーなど（Selectable）の上
///   ・武器の購入パネル、強化・売却パネルの上
/// 中身が空の文字や、見た目だけの画像は、Node のクリックを邪魔しない。
///
/// シーンに置かなくても、Node が Ensure() で作る。
/// </summary>
public class NodeClickInput : MonoBehaviour
{
    /// <summary>台座の外へ広げる当たりの余裕（世界の単位）。隣の台座まで届かない大きさ</summary>
    private const float Margin = 0.15f;

    private static NodeClickInput instance;

    private readonly List<Node> nodes = new List<Node>();
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    public static NodeClickInput Ensure()
    {
        if (instance != null) return instance;

        NodeClickInput found = FindFirstObjectByType<NodeClickInput>();
        if (found != null) return found;

        return new GameObject("NodeClickInput").AddComponent<NodeClickInput>();
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

    /// <summary>Node が増えたとき（Start）に呼ぶ。数えなおす</summary>
    public void Register(Node node)
    {
        if (node != null && !nodes.Contains(node)) nodes.Add(node);
    }

    private void Update()
    {
        // 開始前と結果表示中は、Node を選べない
        if (!GameManager.IsGameActive) return;
        if (!TryGetPress(out Vector2 screen)) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        // 黒帯の上は、ゲーム画面の外
        if (!cam.pixelRect.Contains(screen)) return;

        if (IsOverInteractiveUI(screen)) return;

        Vector3 world3 = cam.ScreenToWorldPoint(
            new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z)));
        Vector2 world = new Vector2(world3.x, world3.y);

        Node best = null;
        float bestDistance = float.MaxValue;
        foreach (Node node in nodes)
        {
            if (node == null || !node.isActiveAndEnabled) continue;
            if (!node.ContainsPoint(world, Margin)) continue;

            float d = ((Vector2)node.transform.position - world).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = node;
            }
        }

        if (best != null) best.SelectNode();
    }

    /// <summary>マウスの左ボタン、または指のタッチが、このフレームで押されたか</summary>
    private static bool TryGetPress(out Vector2 position)
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            position = mouse.position.ReadValue();
            return true;
        }

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            position = touch.primaryTouch.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }

    /// <summary>
    /// その位置に、本当に操作できる UI があるか。
    /// ボタンなど（Selectable）と、購入・強化パネルだけを数え、見えない文字などは数えない。
    /// </summary>
    private bool IsOverInteractiveUI(Vector2 screen)
    {
        EventSystem system = EventSystem.current;
        if (system == null) return false;

        // クリックのときだけなので、毎回作っても負担にならない
        PointerEventData pointerData = new PointerEventData(system) { position = screen };

        uiHits.Clear();
        system.RaycastAll(pointerData, uiHits);

        foreach (RaycastResult hit in uiHits)
        {
            GameObject target = hit.gameObject;
            if (target == null) continue;

            if (target.GetComponentInParent<Selectable>() != null) return true;
            if (UIManager.instance != null && UIManager.instance.IsGameplayPanel(target)) return true;
        }
        return false;
    }
}
