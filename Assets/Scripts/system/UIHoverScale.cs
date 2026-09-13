using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// カーソルを乗せたときに UI を少し拡大する。
///
/// タイトル画面のボタンはワールドのスプライトなので SpriteButton が拡大を担当しているが、
/// ステージ選択の時限ボタンは Canvas 上の UI Button なので同じ手が使えない。
/// 見え方を揃えるためにこちらを用意した。
///
/// Button の色変化（ColorTint）はそのまま残る。拡大だけを足す。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("カーソルを乗せたときの拡大率。1 で無効")]
    [SerializeField] private float hoverScale = 1.06f;

    private RectTransform rt;
    private Selectable selectable;
    private Vector3 baseScale;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        selectable = GetComponent<Selectable>();
        baseScale = rt.localScale;
    }

    private void OnEnable()
    {
        // 拡大したまま非表示になると、次に出したとき大きいまま残る
        if (rt != null) rt.localScale = baseScale;
    }

    private void OnDisable()
    {
        if (rt != null) rt.localScale = baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 未開放でグレーアウトしているボタンは反応させない
        if (selectable != null && !selectable.IsInteractable()) return;
        rt.localScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rt.localScale = baseScale;
    }
}
