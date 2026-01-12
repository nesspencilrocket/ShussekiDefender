using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{

    //現在のウェーブ
    [NonSerialized] public int currentWave;


    //実際に値を変更させる変数（ゴールに到達した敵の数）
    [NonSerialized] public int enemiesReachedGoal;

    //▼▼▼ 修正点 ▼▼▼ 'private' を 'public' に変更！
    //ゲームオーバーになる敵の上限数
    [SerializeField] public int gameOverThreshold = 10;

    //シングルトン
    public static LevelManager instance;
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
        //現在のウェーブを設定
        currentWave = 1;

        //カウンターを0からスタートする
        enemiesReachedGoal = 0;
    }

    private void WaveCompleted()
    {
        currentWave++;
    }

    private void OnEnable()
    {
        Spawner.OnWaveCompleted += WaveCompleted;
        Enemy.OnReachedGoal += HandleEnemyReachedGoal;
    }

    private void OnDisable()
    {
        Spawner.OnWaveCompleted -= WaveCompleted;
        Enemy.OnReachedGoal -= HandleEnemyReachedGoal;
    }


    /// <summary>
    /// 敵がゴールに到達した時の処理
    /// </summary>
    private void HandleEnemyReachedGoal()
    {
        //カウンターを1増やす
        enemiesReachedGoal++;

        //カウンターが上限に達したか確認
        if (enemiesReachedGoal >= gameOverThreshold)
        {
            // 上限を超えないように調整（見た目のため）
            enemiesReachedGoal = gameOverThreshold;

            //ゲームオーバー
            Debug.Log("ゲームオーバー");
        }
    }
}