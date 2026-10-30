using UnityEngine;
using UnityEngine.UI;

/// <summary>Overlay UIの基準を、黒帯を除いた1920x1080の枠に揃える。</summary>
[RequireComponent(typeof(Canvas))]
[DefaultExecutionOrder(-90)]
public sealed class GameViewportCanvas : MonoBehaviour
{
    public Camera ViewCamera { get; private set; }
    public RectTransform Viewport { get; private set; }

    private Canvas canvas;

    public void Initialize(Camera camera)
    {
        ViewCamera = camera;
        if (Viewport != null) return;
        canvas = GetComponent<Canvas>();

        // CanvasScalerは画面全体で倍率を決めるので、カメラの表示範囲から決め直す。
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null) scaler.enabled = false;

        var root = new GameObject("GameViewport", typeof(RectTransform), typeof(RectMask2D));
        Viewport = root.GetComponent<RectTransform>();
        Viewport.SetParent(transform, false);
        Viewport.anchorMin = Viewport.anchorMax = new Vector2(0.5f, 0.5f);
        Viewport.pivot = new Vector2(0.5f, 0.5f);
        Viewport.sizeDelta = new Vector2(1920f, 1080f);
        RefreshLayout();
    }

    private void LateUpdate()
    {
        if (ViewCamera != null) RefreshLayout();
    }

    private void OnEnable()
    {
        // CanvasScalerと同じタイミングでも合わせ、リサイズ直後の文字のずれを防ぐ。
        Canvas.preWillRenderCanvases += LateUpdate;
    }

    private void OnDisable()
    {
        Canvas.preWillRenderCanvases -= LateUpdate;
    }

    private void RefreshLayout()
    {
        Rect pixels = ViewCamera.pixelRect;
        canvas.scaleFactor = Mathf.Max(0.0001f, Mathf.Min(pixels.width / 1920f, pixels.height / 1080f));
        Vector2 display = canvas.renderingDisplaySize;
        Viewport.anchoredPosition = (pixels.center - display / 2f) / canvas.scaleFactor;

        // Startで後から作られた雨・必殺技などの部品も、最初の描画前に枠へ移す。
        for (int i = 0; i < transform.childCount;)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null || child == Viewport)
            {
                i++;
                continue;
            }
            Vector2 min = child.anchorMin;
            Vector2 max = child.anchorMax;
            Vector2 size = child.sizeDelta;
            Vector3 position = child.anchoredPosition3D;
            child.SetParent(Viewport, false);
            // 親の大きさが変わっても、シーンで決めたアンカーと位置を維持する。
            child.anchorMin = min;
            child.anchorMax = max;
            child.sizeDelta = size;
            child.anchoredPosition3D = position;
        }
    }
}
