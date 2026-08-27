using UnityEngine;
using System;
using System.Collections.Generic;

// 【重要】[Serializable] はここに一度だけ記述します。
[Serializable]
public class SpawnRoute
{
    // 敵がスポーンする場所 (位置)
    public Transform spawnPoint;

    // 敵がたどるルート (MovePointコンポーネネント)
    public MovePoint targetRoute;
}
