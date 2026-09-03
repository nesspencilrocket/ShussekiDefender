using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveData
{
    [Tooltip("このウェーブで出現させる敵の総数")]
    public int enemyCount = 10;

    [Tooltip("このウェーブが終わってから次のウェーブが始まるまでの待機時間")]
    public float wavesDelayTime = 10f;

    [Header("Spawn Rate Settings")]
    [Tooltip("一定モードの場合のスポーン間隔 (秒)")]
    public float constantSpawnTime = 1f;

    [Tooltip("ランダムモードの場合の最短スポーン間隔 (秒)")]
    public float minRandomDelay = 0.5f;

    [Tooltip("ランダムモードの場合の最長スポーン間隔 (秒)")]
    public float maxRandomDelay = 1.5f;

    [Header("出現させる敵")]
    [Tooltip("敵の種類と割合。空なら下の enemyPrefab を使う（移行用）")]
    public List<SpawnEntry> spawnTable = new List<SpawnEntry>();

    [Tooltip("【旧形式】spawnTable が空のときだけ使われる。移行が済んだら削除する")]
    public GameObject enemyPrefab;

    /// <summary>spawnTable による抽選が使えるか</summary>
    public bool HasSpawnTable
    {
        get
        {
            if (spawnTable == null) return false;
            foreach (SpawnEntry e in spawnTable)
            {
                if (e != null && e.enemy != null && e.enemy.prefab != null) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// この波で必ず出す分を並べて返す。
    /// 重み抽選だけだと「6限目に学務課が必ず 1 体来る」を保証できないため、
    /// 確定分を先に配置してから残りを重みで埋める。
    /// </summary>
    public List<EnemyData> BuildGuaranteedList()
    {
        var list = new List<EnemyData>();
        if (spawnTable == null) return list;

        foreach (SpawnEntry e in spawnTable)
        {
            if (e == null || e.enemy == null || e.enemy.prefab == null) continue;
            for (int i = 0; i < e.guaranteedCount; i++) list.Add(e.enemy);
        }
        return list;
    }

    /// <summary>
    /// 重みに従って 1 体選ぶ。全部の重みが 0 なら null。
    /// </summary>
    public EnemyData PickWeighted()
    {
        if (spawnTable == null) return null;

        int total = 0;
        foreach (SpawnEntry e in spawnTable)
        {
            if (e == null || e.enemy == null || e.enemy.prefab == null) continue;
            total += Mathf.Max(0, e.weight);
        }
        if (total <= 0) return null;

        int roll = UnityEngine.Random.Range(0, total);
        foreach (SpawnEntry e in spawnTable)
        {
            if (e == null || e.enemy == null || e.enemy.prefab == null) continue;
            roll -= Mathf.Max(0, e.weight);
            if (roll < 0) return e.enemy;
        }
        return null;
    }
}
