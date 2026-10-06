using TMPro;

/// <summary>
/// 金額の表示をそろえるための小道具。
///
/// ゲーム内のお金は「円」。HUD の所持金は数字と「円」を別の文字で並べているが、
/// 購入カードや強化・売却の欄は数字だけだった。単位を付けて、どこでも同じ書き方にする。
/// </summary>
public static class Money
{
    /// <summary>
    /// 「120円」の形にする。単位は数字より少し小さくして、数字を読みやすくする
    /// </summary>
    public static string Yen(int amount)
    {
        return $"{amount}<size=70%>円</size>";
    }

    /// <summary>
    /// 金額を出す枠を、桁が増えても折り返さず、はみ出すときは縮める設定にする。
    /// 購入カードの枠は幅 100 しかなく、折り返しが有効だと「120円」の「円」が次の行に落ちる。
    /// </summary>
    public static void FitOneLine(TextMeshProUGUI text, float minSize = 20f)
    {
        if (text == null) return;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        if (!text.enableAutoSizing)
        {
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = minSize;
            text.enableAutoSizing = true;
        }
    }
}
