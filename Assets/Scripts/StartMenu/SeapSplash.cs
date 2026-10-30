using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>エディタとビルドで同じ起動画面を表示する。</summary>
[DisallowMultipleComponent]
public sealed class SeapSplash : MonoBehaviour
{
    [SerializeField] private GameObject unityScreen;
    [SerializeField] private GameObject seapScreen;
    [SerializeField, Min(0.1f)] private float unityDuration = 2f;
    [SerializeField, Min(0.1f)] private float seapDuration = 2f;
    [Tooltip("暗くなる時間・明るくなる時間。それぞれにこの秒数を使う")]
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.8f;

    private Image fadeOverlay;

    public void ShowUnity()
    {
        if (unityScreen != null) unityScreen.SetActive(true);
        if (seapScreen != null) seapScreen.SetActive(false);
    }

    public void ShowSeap()
    {
        if (unityScreen != null) unityScreen.SetActive(false);
        if (seapScreen != null) seapScreen.SetActive(true);
    }

    private void Awake()
    {
        // 編集中にSEAPを表示していても、起動時には必ずUnityから始める。
        ShowUnity();

        // タイトルの読み込み中も幕を残し、新しい画面が一瞬だけ見えるのを防ぐ。
        DontDestroyOnLoad(gameObject);
        CreateFadeOverlay();
    }

    private IEnumerator Start()
    {
        if (unityScreen == null || seapScreen == null)
        {
            Debug.LogError("起動画面の参照がありません。Shusseki > 起動画面 > 作成・修復 を実行してください。", this);
            Destroy(gameObject);
            yield break;
        }

        // ポーズや倍速を持ち越しても、ロゴの表示秒数を一定にする。
        yield return FadeTo(0f);
        yield return new WaitForSecondsRealtime(unityDuration);
        yield return FadeTo(1f);
        ShowSeap();
        yield return FadeTo(0f);
        yield return new WaitForSecondsRealtime(seapDuration);
        yield return FadeTo(1f);
        yield return SceneManager.LoadSceneAsync("StartMenu");

        // タイトル側のStartがUIや明るさの設定を終えてから幕を開ける。
        yield return null;
        yield return FadeTo(0f);
        Destroy(gameObject);
    }

    private void CreateFadeOverlay()
    {
        var canvasObject = new GameObject("SplashFade", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 明るさ調整のCanvas（32000）よりも手前で、完全に黒く覆う。
        canvas.sortingOrder = short.MaxValue;

        var imageObject = new GameObject("BlackVeil", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        fadeOverlay = imageObject.GetComponent<Image>();
        fadeOverlay.color = Color.black;
        // タイトルが見え始めても、フェード完了まではクリックを受け付けない。
        fadeOverlay.raycastTarget = true;
        RectTransform rect = fadeOverlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeTo(float alpha)
    {
        float from = fadeOverlay.color.a;
        float seconds = Mathf.Max(0.1f, fadeDuration);
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            // 端でふわっと加減速させ、等速の明滅に見えないようにする。
            float eased = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, alpha, eased));
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
    }
}
