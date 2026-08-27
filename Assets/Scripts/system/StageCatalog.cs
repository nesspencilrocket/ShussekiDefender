using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全ステージを順番に並べたアセット。
/// ステージ選択画面の一覧と「次の時限へ」の遷移先が、
/// この 1 本のリストから来るので順番がずれない。
/// </summary>
[CreateAssetMenu(menuName = "Shusseki/Stage Catalog")]
public class StageCatalog : ScriptableObject
{
    [Tooltip("1限目から順に並べる")]
    public List<StageData> stages = new List<StageData>();

    /// <summary>ステージ番号から StageData を引く</summary>
    public StageData Get(int stageNumber)
    {
        return stages.Find(s => s != null && s.stageNumber == stageNumber);
    }

    /// <summary>次のステージを返す。最後なら null</summary>
    public StageData Next(StageData current)
    {
        if (current == null) return null;

        int i = stages.IndexOf(current);
        return (i >= 0 && i + 1 < stages.Count) ? stages[i + 1] : null;
    }
}
