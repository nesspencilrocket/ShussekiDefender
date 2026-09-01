using UnityEngine;
using UnityEditor;

/// <summary>
/// MovePoint の points をシーンビュー上で編集するカスタムエディタ。
/// これから 5 枚分のマップで経路を引くので、ここの使い勝手が
/// そのまま作業時間に効いてくる。
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

        DrawHandles();
        DrawInsertButtons();
    }

    /// <summary>各ウェイポイントを掴んで動かせるようにする</summary>
    private void DrawHandles()
    {
        Handles.color = Color.green;

        for (int i = 0; i < MovePoint.points.Length; i++)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 current = MovePoint.points[i];

            Vector3 moved = Handles.FreeMoveHandle(
                current,
                0.7f,
                new Vector3(0.3f, 0.3f, 0.3f),
                Handles.SphereHandleCap);

            // 通し番号
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.red;
            Vector3 offset = Vector3.down * 0.35f + Vector3.right * 0.35f;
            Handles.Label(current + offset, $"{i + 1}", style);

            // 【重要】EndChangeCheck は BeginChangeCheck 1 回につき 1 回だけ。
            // 以前はこの直前にもう 1 回呼んでおり、そちらが変更フラグを
            // 消費していたため、ドラッグ結果が保存されなかった。
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Move MovePoint");
                MovePoint.points[i] = moved;
                EditorUtility.SetDirty(target);
            }
        }
    }

    /// <summary>区間の中点に「＋」を出し、そこへ点を差し込めるようにする</summary>
    private void DrawInsertButtons()
    {
        Handles.color = Color.cyan;

        for (int i = 0; i < MovePoint.points.Length - 1; i++)
        {
            Vector3 mid = (MovePoint.points[i] + MovePoint.points[i + 1]) * 0.5f;
            float size = HandleUtility.GetHandleSize(mid) * 0.12f;

            if (Handles.Button(mid, Quaternion.identity, size, size, Handles.DotHandleCap))
            {
                InsertAt(i + 1, mid);
                return;   // 配列を変えたので今フレームの描画はここまで
            }
        }
    }

    private void InsertAt(int index, Vector3 position)
    {
        Undo.RecordObject(target, "Insert MovePoint");

        var list = new System.Collections.Generic.List<Vector3>(MovePoint.points);
        list.Insert(index, position);
        MovePoint.points = list.ToArray();

        EditorUtility.SetDirty(target);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (MovePoint == null || MovePoint.points == null) return;

        EditorGUILayout.Space();

        // 経路の総距離。難易度の目安になる（長いほど武器の射程に入る時間が延びる）
        float length = 0f;
        for (int i = 0; i < MovePoint.points.Length - 1; i++)
        {
            length += Vector3.Distance(MovePoint.points[i], MovePoint.points[i + 1]);
        }
        EditorGUILayout.LabelField("経路の総距離", $"{length:F2}");

        EditorGUILayout.HelpBox(
            "シーンビューの緑の球をドラッグで移動。\n"
            + "区間の中点にある水色の点を押すと、そこに経由点を差し込めます。",
            MessageType.Info);

        if (GUILayout.Button("末尾に経由点を追加"))
        {
            Vector3 tail = MovePoint.points.Length > 0
                ? MovePoint.points[MovePoint.points.Length - 1] + Vector3.up
                : MovePoint.transform.position;
            InsertAt(MovePoint.points.Length, tail);
        }
    }
}
