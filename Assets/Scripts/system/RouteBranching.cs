using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵が経由点を踏んだとき、一定確率で別の経路へ乗り換える規則。
///
/// 同じ湧き口から出た敵は同じ経路をなぞるので、間隔を空けて出しても
/// 一列に並んで同じ方向へ歩く。これを崩すため、湧いた直後（既定では
/// 1 番目の点）に、他の経路の次の点へ向かわせる。乗り換えた後は
/// その経路をそのまま最後までたどる。
///
/// Spawner が 1 つ持ち、全ての敵に同じ規則を渡す。
/// </summary>
[Serializable]
public class RouteBranching
{
    [Tooltip("乗り換える確率。0 で無効")]
    [Range(0f, 1f)] public float chance = 0.5f;

    [Tooltip("何番目の点を踏んだときに乗り換えるか（0 始まり。0 なら湧いた直後）")]
    [Min(0)] public int atIndex = 0;

    [Tooltip("乗り換え先の候補は、踏んだ点からこの距離以内に同じ番号の点を持つ経路に限る。"
           + "反対側の湧き口へ向かって画面を横切るのを防ぐ。0 で全経路を候補にする")]
    [Min(0f)] public float candidateRadius = 5f;

    // 抽選のたびに List を作らないための使い回し
    private readonly List<MovePoint> candidates = new List<MovePoint>();

    /// <summary>
    /// 乗り換え先を返す。乗り換えないときは null。
    /// </summary>
    /// <param name="current">いま歩いている経路</param>
    /// <param name="reachedIndex">踏んだ点の番号</param>
    /// <param name="routes">乗り換え先の候補になる全経路</param>
    public MovePoint Pick(MovePoint current, int reachedIndex, IReadOnlyList<MovePoint> routes)
    {
        if (reachedIndex != atIndex || chance <= 0f) return null;
        if (current == null || current.points == null || routes == null) return null;
        if (reachedIndex >= current.points.Length) return null;
        if (UnityEngine.Random.value >= chance) return null;

        int next = reachedIndex + 1;
        Vector3 origin = current.points[reachedIndex];
        float radiusSq = candidateRadius * candidateRadius;

        candidates.Clear();
        for (int i = 0; i < routes.Count; i++)
        {
            MovePoint r = routes[i];
            if (r == null || r == current) continue;
            if (r.points == null || r.points.Length <= next) continue;

            if (candidateRadius > 0f
                && (r.points[reachedIndex] - origin).sqrMagnitude > radiusSq) continue;

            candidates.Add(r);
        }

        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
}
