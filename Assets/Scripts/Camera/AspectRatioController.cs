using UnityEngine;

/// <summary>
/// 画面の縦横比が基準（既定 16:9）と違うとき、カメラの描画範囲を狭めて
/// 黒帯を付け、映る範囲を基準の比率に保つ。
///
/// ビルドはフルスクリーン（ウィンドウ）で起動するため、16:10 のノート PC や
/// ウルトラワイドのモニターでは画面の比率がそのままゲームの比率になる。
/// これを付けておくと、マップの見える範囲がどの画面でも同じになる。
///
/// Screen Space - Overlay の Canvas はカメラの描画範囲に関係なく画面全体に出るので、
/// 黒帯の対象はワールドに置いたスプライト（背景・敵・設置スポットなど）だけ。
/// </summary>
[RequireComponent(typeof(Camera))]
public class AspectRatioController : MonoBehaviour
{
    // 基準にしたいアスペクト比
    public float targetAspectWidth = 16.0f;
    public float targetAspectHeight = 9.0f;

    void Awake() // Start()より先に呼ばれるAwake()で実行
    {
        Camera camera = GetComponent<Camera>();
        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;

        // 基準の比率を保ったまま画面に収めたとき、高さが画面の何割になるか。
        // 以前は targetAspect / windowAspect と逆数になっており、
        // 16:9 以外の画面では黒帯が付く向きが逆で、比率がかえって崩れていた。
        float scaleHeight = windowAspect / targetAspect;

        // 画面が基準より縦長（例：4:3、16:10）
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
        // 画面が基準より横長（例：21:9）、または同じ
        else
        {
            // 左右に黒帯（ピラーボックス）。同じ比率なら scaleWidth が 1 になり帯は付かない
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
