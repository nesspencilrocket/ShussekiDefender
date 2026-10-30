using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// マップとUIを同じ16:9の表示範囲に収め、余った部分を毎フレーム黒帯で覆う。
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-100)]
public class AspectRatioController : MonoBehaviour
{
    public float targetAspectWidth = 16f;
    public float targetAspectHeight = 9f;

    private Camera viewCamera;
    private Canvas barsCanvas;
    private RectTransform[] bars;
    private int lastWidth;
    private int lastHeight;
    private float lastAspect;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        CreateBars();
        RefreshViewport();
    }

    private void Start()
    {
        // 非表示の結果パネルも先に合わせ、開いた瞬間の位置ずれを防ぐ。
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas != barsCanvas && canvas.gameObject.scene == gameObject.scene)
                ConfigureCanvas(canvas, viewCamera);
        }
    }

    private void LateUpdate()
    {
        // 起動時だけでなく、ウィンドウのリサイズやモニター変更にも追従する。
        RefreshViewport();
    }

    private void OnEnable()
    {
        // 描画直前に解像度が切り替わる環境でも、古い比率を1フレーム残さない。
        Canvas.preWillRenderCanvases += RefreshViewport;
    }

    private void OnDisable()
    {
        Canvas.preWillRenderCanvases -= RefreshViewport;
    }

    /// <summary>実行時に作るレベル・雨・必殺技のUIも、シーンのHUDと同じ範囲に収める。</summary>
    public static void FitCanvas(Canvas canvas)
    {
        Camera camera = Camera.main;
        if (camera == null || camera.GetComponent<AspectRatioController>() == null) return;
        if (canvas.gameObject.scene != camera.gameObject.scene) return;
        ConfigureCanvas(canvas, camera);
    }

    private static void ConfigureCanvas(Canvas canvas, Camera camera)
    {
        if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) return;

        // Overlayの描画順やフォントを変えず、内容だけをカメラと同じ枠へ収める。
        GameViewportCanvas viewport = canvas.GetComponent<GameViewportCanvas>();
        if (viewport == null) viewport = canvas.gameObject.AddComponent<GameViewportCanvas>();
        viewport.Initialize(camera);
    }

    private void RefreshViewport()
    {
        int width = Screen.width;
        int height = Screen.height;
        if (width <= 0 || height <= 0) return;
        float aspect = Mathf.Max(0.01f, targetAspectWidth) / Mathf.Max(0.01f, targetAspectHeight);
        if (width == lastWidth && height == lastHeight && Mathf.Approximately(aspect, lastAspect)) return;

        lastWidth = width;
        lastHeight = height;
        lastAspect = aspect;

        float scaleHeight = (float)width / height / aspect;
        Rect viewport = scaleHeight < 1f
            ? new Rect(0f, (1f - scaleHeight) / 2f, 1f, scaleHeight)
            : new Rect((1f - 1f / scaleHeight) / 2f, 0f, 1f / scaleHeight, 1f);
        viewCamera.rect = viewport;

        SetBar(0, Vector2.zero, new Vector2(1f, viewport.yMin));
        SetBar(1, new Vector2(0f, viewport.yMax), Vector2.one);
        SetBar(2, new Vector2(0f, viewport.yMin), new Vector2(viewport.xMin, viewport.yMax));
        SetBar(3, new Vector2(viewport.xMax, viewport.yMin), new Vector2(1f, viewport.yMax));
    }

    private void CreateBars()
    {
        var root = new GameObject("LetterboxBars", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        barsCanvas = root.GetComponent<Canvas>();
        barsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        barsCanvas.targetDisplay = viewCamera.targetDisplay;
        // 明るさの幕（32000）より手前、起動フェード（32767）より奥に置く。
        barsCanvas.sortingOrder = 32001;
        bars = new RectTransform[4];
        for (int i = 0; i < bars.Length; i++)
        {
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(root.transform, false);
            Image image = bar.GetComponent<Image>();
            image.color = Color.black;
            // 黒帯の上では、後ろにあるUIを押せないようにする。
            image.raycastTarget = true;
            bars[i] = image.rectTransform;
        }
    }

    private void SetBar(int index, Vector2 min, Vector2 max)
    {
        RectTransform bar = bars[index];
        bar.anchorMin = min;
        bar.anchorMax = max;
        bar.offsetMin = Vector2.zero;
        bar.offsetMax = Vector2.zero;
        // 描画範囲外はカメラが消去しないため、黒いImageで毎フレーム塗り直す。
        bar.gameObject.SetActive(max.x > min.x && max.y > min.y);
    }
}
