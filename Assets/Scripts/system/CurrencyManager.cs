using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{

    //初期コイン
    [SerializeField] private int InitialCoin;
    //所持コインをセーブするキー
    private string SAVE_COIN = "COIN";
    //所持コイン
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
        //コインの数が引き継がれないように消す
        PlayerPrefs.DeleteKey(SAVE_COIN);

        //読み込む
        LoadCoins();
    }

    private void LoadCoins()
    {
        //セーブされていればその数値を、
        //されていなければ第二引数を変数に格納
        totalCoins = PlayerPrefs.GetInt(SAVE_COIN, InitialCoin);
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        SaveCoins();
    }


    public void AddCoins()
    {
        AddCoins(10);
    }

    /// <summary>
    /// 引数分コインを減らす
    /// </summary>
    /// <param name="amount"></param>
    public void RemoveCoins(int amount)
    {
        if (totalCoins >= amount)
        {
            totalCoins -= amount;
            SaveCoins();
        }
    }

    // 【追加】GameManagerが所持コイン数を取得するための公開メソッド
    /// <summary>
    /// 現在の所持コイン数を返します
    /// </summary>
    /// <returns>所持コイン数</returns>
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


    private void SaveCoins()
    {
        //totalCoinsを保存する
        PlayerPrefs.SetInt(SAVE_COIN, totalCoins);
    }
}
