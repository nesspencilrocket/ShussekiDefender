using UnityEngine;
using UnityEditor;

/// <summary>
/// MovePoint の points をシーンビュー上でドラッグ編集できるようにするカスタムエディタ。
/// CustomEditor 属性で、どのコンポーネント用のエディタかを Unity に知らせる。
/// https://docs.unity3d.com/Manual/editor-CustomEditors.html
/// </summary>
[CustomEditor(typeof(MovePoint))]
public class MovePointEditor : Editor
{
    // Editor クラスが持つ target（object 型）を MovePoint 型として扱う
    MovePoint MovePoint => target as MovePoint;

    private void OnSceneGUI()
    {
        if (MovePoint == null || MovePoint.points == null) return;

        Handles.color = Color.green;

        for (int i = 0; i < MovePoint.points.Length; i++)
        {
            // このブロック内で値が変化したかを監視しはじめる
            EditorGUI.BeginChangeCheck();

            Vector3 currentWaypoint = MovePoint.points[i];

            // 球状の移動ハンドルを出す。第 3 引数は Ctrl 押下時のスナップ量。
            Vector3 newWaypoint = Handles.FreeMoveHandle(
                currentWaypoint,
                0.7f,
                new Vector3(0.3f, 0.3f, 0.3f),
                Handles.SphereHandleCap);

            // ハンドルの脇に通し番号を描く
            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = 20;
            textStyle.normal.textColor = Color.red;
            Vector3 textPos = Vector3.down * 0.35f + Vector3.right * 0.35f;
            Handles.Label(MovePoint.points[i] + textPos, $"{i + 1}", textStyle);

            // 【重要】EndChangeCheck は BeginChangeCheck 1 回につき 1 回だけ呼ぶ。
            // 以前はこの直前にもう 1 回呼んでおり、そちらが変更フラグを消費していたため、
            // 下の if が常に false になってドラッグ結果が保存されなかった。
            if (EditorGUI.EndChangeCheck())
            {
                // Undo に積む。これがないと Ctrl+Z で戻せない。
                Undo.RecordObject(target, "Move MovePoint");
                MovePoint.points[i] = newWaypoint;
                EditorUtility.SetDirty(target);
            }
        }
    }
}
