using UnityEngine;
using TMPro; // TextMeshProを使うために必要

public class DescriptionManager : MonoBehaviour
{
    // 書き換えたいテキストオブジェクト（HierarchyにあるSetumeiTextを入れる場所）
    public TextMeshProUGUI targetText;

    // ボタンから呼び出される関数
    // 引数（string message）で、表示したい文章を受け取る
    public void ChangeText(string message)
    {
        targetText.text = message;
    }
}
