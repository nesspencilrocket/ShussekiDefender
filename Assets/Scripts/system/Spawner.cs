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

    // 直前に使ったスポーン地点。連続で同じ場所から湧かせないために覚えておく
    private int lastRouteIndex = -1;

    // 今の波でまだ出していない確定枠。先頭から消化する
    private readonly List<EnemyData> pendingGuaranteed = new List<EnemyData>();

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

        // 開始前カウントダウン（3・2・1・GO）が明けるまで湧かせない
        yield return new WaitUntil(() => GameManager.IsGameActive);

        while (currentWaveIndex < waves.Count)
        {
            WaveData currentWave = waves[currentWaveIndex];
            spawned = 0;

            // 確定枠を先に積んでおく。総数を超えていたら頭から切り詰める
            pendingGuaranteed.Clear();
            if (currentWave.HasSpawnTable)
            {
                pendingGuaranteed.AddRange(currentWave.BuildGuaranteedList());
                if (pendingGuaranteed.Count > currentWave.enemyCount)
                {
                    pendingGuaranteed.RemoveRange(currentWave.enemyCount,
                        pendingGuaranteed.Count - currentWave.enemyCount);
                }
            }

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
        EnemyData data = PickEnemy(currentWave);

        // spawnTable が未設定の波は旧形式のプレハブで湧かせる
        GameObject prefab = (data != null) ? data.prefab : currentWave.enemyPrefab;
        if (prefab == null) return;

        GameObject newInstance = pooler.GetObjectFromPool(prefab);
        if (newInstance == null) return;

        SpawnRoute selectedRoute = GetRandomSpawnRoute();
        if (selectedRoute.targetRoute == null)
        {
            newInstance.SetActive(false);
            return;
        }

        SetEnemy(newInstance, selectedRoute.spawnPoint, selectedRoute.targetRoute, prefab);
        newInstance.SetActive(true);
    }

    /// <summary>
    /// この 1 体をどの敵にするか決める。
    /// 確定枠が残っていればそちらを優先し、無くなったら重みで抽選する。
    /// spawnTable が空の波では null を返し、呼び出し側が旧形式へ落ちる。
    /// </summary>
    private EnemyData PickEnemy(WaveData currentWave)
    {
        if (!currentWave.HasSpawnTable) return null;

        if (pendingGuaranteed.Count > 0)
        {
            EnemyData first = pendingGuaranteed[0];
            pendingGuaranteed.RemoveAt(0);
            return first;
        }

        return currentWave.PickWeighted();
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

    /// <summary>
    /// スポーン地点を選ぶ。直前と同じ地点は避けるので、
    /// 同じ場所から連続で湧くことがなくなる。
    /// 範囲スポーンを実装しなくても、体感上の問題はこれで解消する。
    /// </summary>
    private SpawnRoute GetRandomSpawnRoute()
    {
        if (spawnRoutes == null || spawnRoutes.Count == 0)
        {
            return new SpawnRoute { spawnPoint = null, targetRoute = null };
        }

        int i = Random.Range(0, spawnRoutes.Count);

        // 直前と同じなら、それ以外の中から選び直す
        if (i == lastRouteIndex && spawnRoutes.Count > 1)
        {
            i = (lastRouteIndex + 1 + Random.Range(0, spawnRoutes.Count - 1)) % spawnRoutes.Count;
        }

        lastRouteIndex = i;
        return spawnRoutes[i];
    }
}
