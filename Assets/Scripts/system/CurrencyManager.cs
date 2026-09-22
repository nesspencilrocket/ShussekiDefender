using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    [Tooltip("StageData が取得できなかったときに使う初期コイン")]
    [SerializeField] private int InitialCoin = 100;

    [NonSerialized] public int totalCoins;

    public static CurrencyManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // コインはステージ内だけの一時的な資源なので保存しない。
        // 以前は PlayerPrefs に保存しては Start で消す、という誤用をしていた。
        // PlayerPrefs は StageProgress（進行状況の保存）に専念させる。
        StageData stage = (GameManager.Instance != null) ? GameManager.Instance.Stage : null;
        totalCoins = (stage != null) ? stage.initialCoin : InitialCoin;
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
    }

    /// <summary>
    /// EnemyHP.OnEnemyDead から呼ばれる。
    /// 報酬は倒された敵の EnemyData から取る。EnemyData が無い敵は
    /// EnemyHP 側の既定値（10）に落ちるので、従来と同じ額になる。
    /// </summary>
    private void OnEnemyDefeated(EnemyHP hp)
    {
        if (hp == null) return;
        AddCoins(hp.RewardCoin);
    }

    public void RemoveCoins(int amount)
    {
        if (totalCoins >= amount)
        {
            totalCoins -= amount;
        }
    }

    public int GetCurrentCurrency()
    {
        return totalCoins;
    }

    private void OnEnable()
    {
        EnemyHP.OnEnemyDead += OnEnemyDefeated;
    }

    private void OnDisable()
    {
        EnemyHP.OnEnemyDead -= OnEnemyDefeated;
    }
}
