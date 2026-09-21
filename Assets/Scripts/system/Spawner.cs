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

    [Header("湧き口")]
    // 敵が出る位置は Enemy.SnapToStartPoint() が movePoint.points[0] へ移すことで決まる。
    // SpawnRoute.spawnPoint は直後に上書きされるので結果に影響しない。
    // つまり経路さえあれば湧き口として成立するため、親を指定して自動で集める形にした。
    // 湧き口を増やすときは Route をこの親の下に複製するだけでよく、Inspector の一覧を触らずに済む。
    [Tooltip("MovePoint を子に持つ親。指定するとその子の MovePoint を全て湧き口として使う")]
    [SerializeField] private Transform routesParent;

    [Tooltip("routesParent が空のときだけ使う手動の一覧（移行用）")]
    [SerializeField] private List<SpawnRoute> spawnRoutes;

    [Header("経路の乗り換え")]
    // 同じ湧き口から出た敵が一列に並ぶのを崩す。
    // 湧いた直後に一定確率で他の経路の次の点へ向かわせる。詳細は RouteBranching。
    [SerializeField] private RouteBranching branching = new RouteBranching();

    // 波の内容は StageData から受け取る。シーンには持たせない。
    private List<WaveData> waves;

    private int currentWaveIndex = 0;
    private float spawnTimer;
    private float spawned;
    private ObjectPooler pooler;

    // 実際に使う湧き口。Start で routesParent か spawnRoutes から組み立てる
    private readonly List<MovePoint> routes = new List<MovePoint>();

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

        CollectRoutes();
        if (routes.Count == 0)
        {
            Debug.LogError("Spawner: 湧き口が 1 つもありません。"
                         + "Routes Parent の指定か、その下の MovePoint を確認してください。", this);
            return;
        }

        StartCoroutine(SpawnWaves());
    }

    /// <summary>
    /// 湧き口を集める。
    /// routesParent が指定されていればその子から、無ければ spawnRoutes から作る。
    /// 経由点が 1 つも無い MovePoint は湧き位置を決められないので除外する。
    /// </summary>
    private void CollectRoutes()
    {
        routes.Clear();

        if (routesParent != null)
        {
            foreach (MovePoint mp in routesParent.GetComponentsInChildren<MovePoint>())
            {
                if (IsUsable(mp)) routes.Add(mp);
            }
            return;
        }

        if (spawnRoutes == null) return;
        foreach (SpawnRoute sr in spawnRoutes)
        {
            if (sr != null && IsUsable(sr.targetRoute)) routes.Add(sr.targetRoute);
        }
    }

    private static bool IsUsable(MovePoint route)
    {
        return route != null && route.points != null && route.points.Length > 0;
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

        MovePoint route = GetRandomRoute();
        if (route == null)
        {
            newInstance.SetActive(false);
            return;
        }

        SetEnemy(newInstance, route, prefab);
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

    private void SetEnemy(GameObject newInstance, MovePoint route, GameObject prefabToSpawn)
    {
        Enemy enemy = newInstance.GetComponent<Enemy>();
        EnemyHP enemyHP = newInstance.GetComponent<EnemyHP>();

        if (enemyHP == null || enemy == null) return;

        enemyHP.originalPrefab = prefabToSpawn;
        enemy.movePoint = route;
        enemy.SetRoutes(routes, branching);
        enemy.ResetMovePoint();
        // 湧く位置は SnapToStartPoint が points[0] へ移して決める。
        // 以前はここで spawnPoint の位置も入れていたが、直後に上書きされる無駄な代入だった。
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
    private MovePoint GetRandomRoute()
    {
        if (routes.Count == 0) return null;

        int i = Random.Range(0, routes.Count);

        // 直前と同じなら、それ以外の中から選び直す
        if (i == lastRouteIndex && routes.Count > 1)
        {
            i = (lastRouteIndex + 1 + Random.Range(0, routes.Count - 1)) % routes.Count;
        }

        lastRouteIndex = i;
        return routes[i];
    }
}
