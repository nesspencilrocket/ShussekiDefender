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

    /// <summary>EnemyHP.OnEnemyDead から呼ばれる（引数なし版）</summary>
    public void AddCoins()
    {
        AddCoins(10);
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
        EnemyHP.OnEnemyDead += AddCoins;
    }

    private void OnDisable()
    {
        EnemyHP.OnEnemyDead -= AddCoins;
    }
}
