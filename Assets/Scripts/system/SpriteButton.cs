using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// ワールド空間のスプライトを押せるようにする。
///
/// タイトル画面の「成績確認」「設定」は Canvas ではなく SpriteRenderer で
/// 置かれており、Button も Collider も付いていなかったため押せなかった。
/// UI に作り直すとレイアウトを組み直すことになるので、
/// スプライトのまま押せるようにする方を選んでいる。
///
/// 当たり判定と Physics2DRaycaster は実行時に自分で用意するので、
/// シーン側はこのコンポーネントを足すだけでよい。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteButton : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("押したときに読み込むシーン名。空なら onClick だけ呼ぶ")]
    [SerializeField] private string sceneToLoad = "";

    [Tooltip("シーン遷移以外の処理を足したいとき用")]
    [SerializeField] private UnityEvent onClick;

    [Tooltip("カーソルを乗せたときの拡大率。1 で無効")]
    [SerializeField] private float hoverScale = 1.04f;

    [Tooltip("カーソルを乗せたときの明るさ")]
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 1f);

    private SpriteRenderer sr;
    private Vector3 baseScale;
    private Color baseColor;
    private bool clicked;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseColor = sr.color;

        EnsureCollider();
        EnsureRaycaster();
    }

    private void OnEnable()
    {
        clicked = false;
        transform.localScale = baseScale;
        if (sr != null) sr.color = baseColor;
    }

    /// <summary>
    /// スプライトの見た目どおりの当たり判定を付ける。
    /// 手で BoxCollider2D を置くと絵を差し替えたときにずれるため、実行時に測る。
    /// </summary>
    private void EnsureCollider()
    {
        if (GetComponent<Collider2D>() != null) return;
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError($"{name}: スプライトが未設定のため当たり判定を作れません。", this);
            return;
        }

        BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
        Bounds b = sr.sprite.bounds;
        box.size = b.size;
        box.offset = b.center;
    }

    /// <summary>
    /// カメラに Physics2DRaycaster が無いと、EventSystem がワールドの
    /// コライダーを拾えない。UI 側の Raycaster とは別物なので注意。
    /// </summary>
    private void EnsureRaycaster()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError($"{name}: MainCamera が見つからないため押せません。", this);
            return;
        }
        if (cam.GetComponent<Physics2DRaycaster>() == null)
        {
            cam.gameObject.AddComponent<Physics2DRaycaster>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = baseScale * hoverScale;
        if (sr != null) sr.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = baseScale;
        if (sr != null) sr.color = baseColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 遷移待ちの間に二重で押されると、同じシーンを 2 回読み込んでしまう
        if (clicked) return;
        clicked = true;

        onClick?.Invoke();

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            clicked = false;
            return;
        }

        // ビルド設定に無いシーンを LoadScene すると例外で止まる。
        // 遷移先を先に配線して、シーンを後から足す進め方をしているので、
        // 揃うまでは警告だけ出して押せる状態を保つ。
        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogWarning($"{name}: シーン '{sceneToLoad}' がビルド設定にありません。", this);
            clicked = false;
            return;
        }

        // 敗北時に止めたまま戻ってくると次の画面が固まる
        GameSpeed.Resume();
        SceneManager.LoadScene(sceneToLoad);
    }
}
