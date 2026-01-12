using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class ObjectPooler : MonoBehaviour
{
    // =================================================================
    // 【修正箇所 1】シングルトンインスタンスの追加
    // =================================================================
    public static ObjectPooler Instance { get; private set; }

    [Serializable]
    public class PoolItem
    {
        [Tooltip("生成するアイテムのプレハブ")]
        public GameObject prefab;
        [Tooltip("初期生成数")]
        public int size;
        [NonSerialized] public List<GameObject> pool;
    }

    [Header("Pool Settings")]
    [Tooltip("管理するすべての敵のプレハブ設定")]
    [SerializeField]
    private List<PoolItem> poolItems = new List<PoolItem>();

    private Dictionary<GameObject, GameObject> instanceToPrefabMap;

    private GameObject poolContainer;

    private void Awake()
    {
        // 【修正】: シングルトン化をAwakeの最初に確実に実行する
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this; // ★★★ ObjectPooler.Instance が設定される ★★★

        // インスタンス化 (プール機能の初期化)
        instanceToPrefabMap = new Dictionary<GameObject, GameObject>();

        // オブジェクト生成して名前つけて変数に格納
        poolContainer = new GameObject("Object Pool Container");

        // 【重要】: プールオブジェクトの生成はStart()に移動し、参照ミスによるクラッシュを防ぐ
        // CreatePooler(); // <-- ここから削除
    }

    private void Start()
    {
        // 【追加】: プール生成をStart()に移動する
        CreatePooler();
        Debug.Log("DEBUG_POOL: ObjectPoolerがプール生成を完了しました。");
    }

    private void CreatePooler()
    {
        foreach (PoolItem item in poolItems) // foreachで直接itemを操作可能に
        {
            if (item.prefab == null) continue;

            item.pool = new List<GameObject>(); // リストを初期化

            for (int j = 0; j < item.size; j++)
            {
                item.pool.Add(CreateObject(item.prefab));
            }
        }
    }

    private GameObject CreateObject(GameObject prefab)
    {
        if (prefab == null) return null;

        GameObject newInstance = Instantiate(prefab);

        newInstance.transform.SetParent(poolContainer.transform);

        newInstance.SetActive(false);

        instanceToPrefabMap.Add(newInstance, prefab);

        return newInstance;
    }

    public GameObject GetObjectFromPool(GameObject prefab)
    {
        // 指定されたプレハブのPoolItemを探すためのループ
        foreach (PoolItem item in poolItems) // foreachでアクセス
        {
            if (item.prefab == prefab)
            {
                List<GameObject> currentPool = item.pool;

                // プール内の非表示オブジェクトを探す
                for (int j = 0; j < currentPool.Count; j++)
                {
                    if (currentPool[j] != null && !currentPool[j].activeInHierarchy)
                    {
                        return currentPool[j];
                    }
                }

                // 足りない場合は生成し、プールに追加して返す
                GameObject newInstance = CreateObject(prefab);
                currentPool.Add(newInstance);

                // itemがclassになったため、この行（構造体で必要だった更新処理）は不要になります
                // poolItems[i] = currentItem; 

                return newInstance;
            }
        }

        // プール設定が見つからなかった場合
        Debug.LogError($"Pool setting for prefab {prefab.name} not found.");
        return null;
    }

    public GameObject GetOriginalPrefab(GameObject instance)
    {
        if (instanceToPrefabMap.ContainsKey(instance))
        {
            return instanceToPrefabMap[instance];
        }
        return null;
    }

    public static void ReturnToPool(GameObject instance)
    {
        instance.SetActive(false);
    }
}
