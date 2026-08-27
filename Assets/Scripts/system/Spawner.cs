using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum SpawnModes
{
    constant,
    Random
}

public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private SpawnModes spawnMode = SpawnModes.constant;

    [Tooltip("スポーン地点と経路の組。シーン内の Transform を指すのでアセットには出せない")]
    [SerializeField] private List<SpawnRoute> spawnRoutes;

    // 波の内容は StageData から受け取る。シーンには持たせない。
    private List<WaveData> waves;

    private int currentWaveIndex = 0;
    private float spawnTimer;
    private float spawned;
    private ObjectPooler pooler;

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
            Debug.LogError("Spawner: ObjectPooler が見つかりません。", this);
            return;
        }

        // GameManager.Awake で解決済みの StageData から波表を受け取る
        StageData stage = (GameManager.Instance != null) ? GameManager.Instance.Stage : null;
        if (stage == null)
        {
            Debug.LogError("Spawner: StageData を取得できませんでした。"
                         + "GameManager の Fallback Stage を確認してください。", this);
            return;
        }

        waves = stage.waves;
        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        if (waves == null || waves.Count == 0) yield break;

        while (currentWaveIndex < waves.Count)
        {
            WaveData currentWave = waves[currentWaveIndex];
            spawned = 0;

            // 指定数の敵をすべてスポーンさせる
            while (spawned < currentWave.enemyCount)
            {
                spawned++;
                SpawnEnemy(currentWave);
                spawnTimer = GetSpawnDelay(currentWave);
                yield return new WaitForSeconds(spawnTimer);
            }

            // 敵の全滅は待たない。波は「スポーンの時刻表」として扱う。
            OnWaveCompleted?.Invoke();
            currentWaveIndex++;

            if (currentWaveIndex < waves.Count)
            {
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
