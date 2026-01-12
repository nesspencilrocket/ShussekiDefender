using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// ★★★ 修正点：削除されていた定義を元に戻しました ★★★
public enum SpawnModes
{
    constant,
    Random
}

public class Spawner : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private List<WaveData> waves = new List<WaveData>();

    private int currentWaveIndex = 0;

    [SerializeField] private SpawnModes spawnMode = SpawnModes.constant;
    [SerializeField] private List<SpawnRoute> spawnRoutes;

    private float spawnTimer;
    private float spawned;
    private ObjectPooler pooler;
    private int enemiesRemaining; // ウェーブ進行制御では不要になりましたが、他の用途のため残しています
    public static Action OnWaveCompleted;

    private void Awake()
    {
        pooler = ObjectPooler.Instance;
    }

    private void Start()
    {
        if (pooler == null) pooler = ObjectPooler.Instance;
        if (pooler == null)
        {
            Debug.LogError("CRITICAL ERROR: ObjectPoolerの参照が取得できませんでした。");
            return;
        }
        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        if (waves == null || waves.Count == 0) yield break;

        while (currentWaveIndex < waves.Count)
        {
            WaveData currentWave = waves[currentWaveIndex];
            // 敵の全滅待ちをしないため、この行はウェーブ進行制御には影響しません
            enemiesRemaining = currentWave.enemyCount;
            spawned = 0;

            // ★★★ このwhileループで指定数の敵をすべてスポーンさせます ★★★
            while (spawned < currentWave.enemyCount)
            {
                spawned++;
                SpawnEnemy(currentWave);
                spawnTimer = GetSpawnDelay(currentWave);
                yield return new WaitForSeconds(spawnTimer);
            }

            // ★★★ 修正箇所: 敵が全滅するのを待つ行を削除しました ★★★
            // yield return new WaitUntil(() => enemiesRemaining <= 0);

            // スポーンが完了したらすぐに次の処理へ
            OnWaveCompleted?.Invoke();
            currentWaveIndex++;

            if (currentWaveIndex < waves.Count)
            {
                // 次のウェーブまでのインターバル時間
                yield return new WaitForSeconds(waves[currentWaveIndex - 1].wavesDelayTime);
            }
        }
    }

    private void SpawnEnemy(WaveData currentWave)
    {
        if (currentWave.enemyPrefab == null) return;
        GameObject newInstance = pooler.GetObjectFromPool(currentWave.enemyPrefab);
        if (newInstance == null) return;

        SpawnRoute selectedRoute = GetRandomSpawnRoute();
        if (selectedRoute.targetRoute == null)
        {
            newInstance.SetActive(false);
            return;
        }

        SetEnemy(newInstance, selectedRoute.spawnPoint, selectedRoute.targetRoute, currentWave.enemyPrefab);
        newInstance.SetActive(true);
    }

    private void SetEnemy(GameObject newInstance, Transform spawnTransform, MovePoint route, GameObject prefabToSpawn)
    {
        Enemy enemy = newInstance.GetComponent<Enemy>();
        EnemyHP enemyHP = newInstance.GetComponent<EnemyHP>();

        if (enemyHP == null || enemy == null) return;

        enemyHP.originalPrefab = prefabToSpawn;
        enemy.movePoint = route;
        enemy.ResetMovePoint();
        enemy.transform.position = spawnTransform.position;
        enemy.SnapToStartPoint();
        enemy.SetMoveSpeed();
    }

    private float GetSpawnDelay(WaveData currentWave)
    {
        return spawnMode == SpawnModes.constant
            ? currentWave.constantSpawnTime
            : Random.Range(currentWave.minRandomDelay, currentWave.maxRandomDelay);
    }

    private SpawnRoute GetRandomSpawnRoute()
    {
        if (spawnRoutes == null || spawnRoutes.Count == 0)
        {
            return new SpawnRoute { spawnPoint = null, targetRoute = null };
        }
        return spawnRoutes[Random.Range(0, spawnRoutes.Count)];
    }
}