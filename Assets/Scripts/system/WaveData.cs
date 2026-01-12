using System;
using UnityEngine;

[Serializable]
public class WaveData
{
    [Tooltip("このウェーブで出現させる敵の総数")]
    public int enemyCount = 10;

    [Tooltip("このウェーブが終わってから次のウェーブが始まるまでの待機時間")]
    public float wavesDelayTime = 10f;

    [Header("Spawn Rate Settings")] // ★追加
    [Tooltip("一定モードの場合のスポーン間隔 (秒)")]
    public float constantSpawnTime = 1f; // ★追加

    [Tooltip("ランダムモードの場合の最短スポーン間隔 (秒)")]
    public float minRandomDelay = 0.5f; // ★追加

    [Tooltip("ランダムモードの場合の最長スポーン間隔 (秒)")]
    public float maxRandomDelay = 1.5f; // ★追加

    [Tooltip("（オプション）このウェーブで出現させる敵の種類")]
    public GameObject enemyPrefab;
}
