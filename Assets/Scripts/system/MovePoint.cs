using UnityEngine;

// 【追加】クラス定義が必要です
public class MovePoint : MonoBehaviour
{
    // 【追加】points変数の宣言が必要です
    public Vector3[] points;

    // 【元のコード】OnDrawGizmos() メソッド
    private void OnDrawGizmos()
    {
        // 【修正】配列が null または 要素が0 の場合は、処理をスキップして抜ける
        if (points == null || points.Length == 0)
        {
            return;
        }

        //配列に格納されている数値分処理をする
        for (int i = 0; i < points.Length; i++)
        {
            //色を指定
            Gizmos.color = Color.blue;

            //ポジション、半径
            Gizmos.DrawWireSphere(points[i], 0.5f);

            // 【追加】次のポイントとの間に線を描画 (オプション: ルートが分かりやすくなります)
            if (i < points.Length - 1)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(points[i], points[i + 1]);
            }
        }
    }

    /// <summary>
    /// 引数で渡された数値と同じポイントの位置を返す
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Vector3 GetMovePointPosition(int index)
    {
        // 【修正・重要】インデックスが有効範囲内かチェック
        if (points == null || index < 0 || index >= points.Length)
        {
            // エラーログを出力して、安全な値（現在のオブジェクトの位置など）を返す
            Debug.LogError("MovePoint: 無効なインデックス (" + index + ") が指定されました。");

            // 敵の移動が停止しないよう、最後のポイントを返すか、エラーとして Vector3.zero を返します。
            // ここでは安全のため、最後の有効なポイントを返すロジックを採用します。
            if (points != null && points.Length > 0)
            {
                return points[points.Length - 1];
            }
            return transform.position; // それも無理なら、この MovePoint オブジェクト自身の位置を返す
        }

        //敵の移動に必要なので先に記述しておく
        return points[index];
    }
}
