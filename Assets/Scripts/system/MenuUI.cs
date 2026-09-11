using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 成績確認・設定画面の UI をコードから組み立てるための道具箱。
///
/// 【なぜシーンに置かずコードで作るか】
/// スライダー 1 本でも Background / Fill Area / Fill / Handle Slide Area / Handle と
/// 5 階層の入れ子になり、シーンに手で組むと参照の付け替えが増えて壊れやすい。
/// 6 行 × 3 列の表も同じで、行を 1 つ増やすたびに手作業が発生する。
/// コードで組めば行数を変数で変えられるし、配置の根拠がその場に書ける。
///
/// 見た目の値は各画面の SerializeField から渡すので、色や文字サイズは
/// Inspector から調整できる。
/// </summary>
public static class MenuUI
{
    /// <summary>画面いっぱいに広がる Canvas を作る。参照解像度は 1920x1080</summary>
    public static Canvas CreateCanvas(GameObject host)
    {
        Canvas canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // 幅と高さの相乗平均。縦長にも横長にも寄りすぎない
        scaler.matchWidthOrHeight = 0.5f;

        host.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>親いっぱいに広がる矩形を作る</summary>
    public static RectTransform Stretch(Transform parent, string name)
    {
        RectTransform rt = new GameObject(name, typeof(RectTransform))
            .GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    /// <summary>中心基準で位置と大きさを指定した矩形を作る</summary>
    public static RectTransform Rect(Transform parent, string name,
                                     Vector2 pos, Vector2 size)
    {
        RectTransform rt = new GameObject(name, typeof(RectTransform))
            .GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>
    /// 背景を敷く。sprite があればそれを全面に広げ、上から黒を重ねて沈める。
    ///
    /// タイトル画面の絵をそのまま使うと写真が明るく、文字が読めない。
    /// かといって別の背景を用意すると画面ごとに世界観がばらけるので、
    /// 同じ絵を暗くして使い回す。scrimAlpha で濃さを調整できる。
    ///
    /// 背景と黒幕は最初に作ること。UI は階層の順に描かれるので、
    /// 後から作ると本文の上に乗ってしまう。
    /// </summary>
    public static void Background(Transform root, Sprite sprite, Color fallback, float scrimAlpha)
    {
        RectTransform holder = Stretch(root, "Background");

        if (sprite == null)
        {
            Image plain = holder.gameObject.AddComponent<Image>();
            plain.color = fallback;
            plain.raycastTarget = false;
            return;
        }

        // 画面いっぱいに引き伸ばすと縦横比が崩れる。
        // タイトル画面は SpriteRenderer で「高さを合わせて左右を切る」表示になっており、
        // それと揃えるために EnvelopeParent（はみ出す方に合わせて拡大）を使う。
        RectTransform imgRt = new GameObject("Image", typeof(RectTransform))
            .GetComponent<RectTransform>();
        imgRt.SetParent(holder, false);
        imgRt.anchorMin = new Vector2(0.5f, 0.5f);
        imgRt.anchorMax = new Vector2(0.5f, 0.5f);
        imgRt.pivot = new Vector2(0.5f, 0.5f);

        Image img = imgRt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = false;

        AspectRatioFitter fitter = imgRt.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = sprite.rect.width / sprite.rect.height;

        RectTransform scrimRt = Stretch(root, "Scrim");
        Image scrim = scrimRt.gameObject.AddComponent<Image>();
        scrim.color = new Color(0f, 0f, 0f, Mathf.Clamp01(scrimAlpha));
        scrim.raycastTarget = false;
    }

    /// <summary>単色の板</summary>
    public static Image Panel(Transform parent, string name,
                              Vector2 pos, Vector2 size, Color color)
    {
        Image img = Rect(parent, name, pos, size).gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>文字。align で左寄せ・中央・右寄せを決める</summary>
    public static TextMeshProUGUI Text(Transform parent, string name, string body,
                                       Vector2 pos, Vector2 size,
                                       float fontSize, Color color,
                                       TMP_FontAsset font,
                                       TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        TextMeshProUGUI t = Rect(parent, name, pos, size)
            .gameObject.AddComponent<TextMeshProUGUI>();
        t.text = body;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        // 1 行に収める。折り返すと行の高さが変わって表の行がずれる。
        // enableWordWrapping は廃止予定なので textWrappingMode を使う
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        if (font != null) t.font = font;
        return t;
    }

    /// <summary>文字を載せた押しボタン</summary>
    public static Button TextButton(Transform parent, string name, string label,
                                    Vector2 pos, Vector2 size,
                                    float fontSize,
                                    Color fill, Color textColor,
                                    TMP_FontAsset font)
    {
        RectTransform rt = Rect(parent, name, pos, size);

        Image bg = rt.gameObject.AddComponent<Image>();
        bg.color = fill;

        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;

        // 押した瞬間に色が変わらないと、反応したのか分からない
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        TextMeshProUGUI t = Text(rt, "Label", label, Vector2.zero, size,
                                 fontSize, textColor, font, TextAlignmentOptions.Center);
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = Vector2.zero;
        t.rectTransform.offsetMax = Vector2.zero;

        return btn;
    }

    /// <summary>
    /// 横向きのスライダー。
    /// Background / Fill / Handle の 3 枚を作って Slider に渡す。
    /// </summary>
    public static Slider HorizontalSlider(Transform parent, string name,
                                          Vector2 pos, Vector2 size,
                                          float min, float max, float value,
                                          Color track, Color fill, Color handle)
    {
        RectTransform rt = Rect(parent, name, pos, size);
        Slider slider = rt.gameObject.AddComponent<Slider>();

        // 溝
        RectTransform bg = Stretch(rt, "Background");
        Image bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = track;
        bg.anchorMin = new Vector2(0f, 0.5f);
        bg.anchorMax = new Vector2(1f, 0.5f);
        bg.sizeDelta = new Vector2(0f, 10f);
        bg.anchoredPosition = Vector2.zero;

        // 伸びる部分。Fill Area を挟まないと端で見切れる
        RectTransform fillArea = Stretch(rt, "Fill Area");
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(-20f, 10f);
        fillArea.anchoredPosition = Vector2.zero;

        RectTransform fillRt = Stretch(fillArea, "Fill");
        Image fillImg = fillRt.gameObject.AddComponent<Image>();
        fillImg.color = fill;
        fillRt.sizeDelta = new Vector2(10f, 0f);

        // つまみ
        RectTransform handleArea = Stretch(rt, "Handle Slide Area");
        handleArea.sizeDelta = new Vector2(-20f, 0f);

        RectTransform handleRt = Stretch(handleArea, "Handle");
        Image handleImg = handleRt.gameObject.AddComponent<Image>();
        handleImg.color = handle;
        handleRt.sizeDelta = new Vector2(28f, 40f);

        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.value = value;

        return slider;
    }
}
