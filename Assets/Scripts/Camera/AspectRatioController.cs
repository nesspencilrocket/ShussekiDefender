using UnityEngine;

// MainCameraにアタッチされていることを要求する
[RequireComponent(typeof(Camera))]
public class AspectRatioController : MonoBehaviour
{
    // ① 基準にしたいアスペクト比を設定
    public float targetAspectWidth = 16.0f;
    public float targetAspectHeight = 9.0f;

    void Awake() // Start()より先に呼ばれるAwake()で実行
    {
        Camera camera = GetComponent<Camera>();
        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = targetAspect / windowAspect;

        // 実際の画面比率が基準よりも横長の場合
        if (scaleHeight < 1.0f)
        {
            // 上下に黒帯（レターボックス）
            Rect rect = camera.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            camera.rect = rect;
        }
        // 実際の画面比率が基準よりも縦長の場合
        else
        {
            // 左右に黒帯（ピラーボックス）
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = camera.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            camera.rect = rect;
        }
    }
}