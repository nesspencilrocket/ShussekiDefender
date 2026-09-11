using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面全体に薄い幕をかけて明るさを変える。
///
/// 【なぜ Light2D や Post Processing を使わないか】
/// Light2D はシーンごとに置く必要があり、6 シーンに増えたときに設定漏れが起きる。
/// Post Processing は URP の Volume 設定が要るうえ、UI には掛からない。
/// 全画面の Image なら、シーンに何も置かずに全部の画面へ一様に効く。
///
/// 自分で自分を生成するので、どのシーンにもアタッチする必要がない。
/// </summary>
public class BrightnessOverlay : MonoBehaviour
{
    /// <summary>UI より手前に出す。他の Canvas は 0 なので十分に大きい値にする</summary>
    private const int SORTING_ORDER = 32000;

    /// <summary>暗くする側の最大濃度。1 にすると真っ暗になるので抑える</summary>
    private const float DARK_STRENGTH = 0.8f;

    /// <summary>明るくする側の最大濃度。白は乗せすぎると白飛びするので弱め</summary>
    private const float LIGHT_STRENGTH = 0.45f;

    private static BrightnessOverlay instance;
    private Image image;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject go = new GameObject("BrightnessOverlay");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BrightnessOverlay>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        Build();
        GameSettings.OnBrightnessChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        GameSettings.OnBrightnessChanged -= Refresh;
        if (instance == this) instance = null;
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SORTING_ORDER;

        GameObject imageObj = new GameObject("Veil");
        imageObj.transform.SetParent(transform, false);

        image = imageObj.AddComponent<Image>();
        // 幕がクリックを吸うと、下のボタンが一切押せなくなる
        image.raycastTarget = false;

        RectTransform rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Refresh()
    {
        if (image == null) return;

        float b = GameSettings.Brightness;

        if (b < 1f)
        {
            image.color = new Color(0f, 0f, 0f, (1f - b) * DARK_STRENGTH);
        }
        else if (b > 1f)
        {
            image.color = new Color(1f, 1f, 1f, (b - 1f) * LIGHT_STRENGTH);
        }
        else
        {
            image.color = Color.clear;
        }
    }
}
