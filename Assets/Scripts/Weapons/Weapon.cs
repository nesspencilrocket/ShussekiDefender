using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    // この武器の攻撃範囲
    public float attackRange = 3f;

    // エネミーを格納するリスト
    private List<Enemy> enemies;
    // 攻撃範囲内にいるターゲットを1体格納する
    [NonSerialized] public Enemy currentEnemyTarget;

    public WeaponUpgrade weaponUpgrade;

    void Start()
    {
        // プレイ開始時に数値を合わせる
        GetComponent<CircleCollider2D>().radius = attackRange;

        enemies = new List<Enemy>();

        weaponUpgrade = GetComponent<WeaponUpgrade>();

        // 【追加】イベントの購読を開始
        EnemyHP.OnEnemyDead += RemoveInvalidEnemy;
        Enemy.OnReachedGoal += RemoveInvalidEnemy;
    }

    // 【追加】シーンを移動する際にイベント購読を解除 (メモリリーク防止)
    private void OnDestroy()
    {
        // Nullチェックはイベントの呼び出し側で安全に処理されていることが多いが、明示的に解除する
        EnemyHP.OnEnemyDead -= RemoveInvalidEnemy;
        Enemy.OnReachedGoal -= RemoveInvalidEnemy;
    }

    void Update()
    {
        // 【重要】ゲームがアクティブでない場合は処理しない
        if (!GameManager.IsGameActive) return;

        // ターゲットを取得する
        GetCurrentTarget();
    }

    private void GetCurrentTarget()
    {
        // 【修正】無効な敵（HP 0以下など）をリストから排除してから、ターゲットを選ぶ
        CleanEnemyList();

        // リストに敵がいない?
        if (enemies.Count <= 0)
        {
            // 設定をnullに
            currentEnemyTarget = null;
            return;
        }

        // 【修正】リストの最初の敵を設定 (最前線にいる敵を攻撃するロジック)
        currentEnemyTarget = enemies[0];
    }

    /// <summary>
    /// 【追加】死亡または非アクティブな敵をリストから削除する
    /// </summary>
    private void CleanEnemyList()
    {
        // リストから無効な敵をすべて削除する
        // 1. Enemyコンポーネントがnullになっている (プールに戻された)
        // 2. EnemyHPがnull、またはHPが0以下である

        // リストを逆順にチェックして、削除してもインデックスが狂わないようにする
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];

            if (enemy == null || enemy.enemyHP == null || enemy.enemyHP.currentHP <= 0)
            {
                enemies.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 【追加】敵が倒された（死亡/ゴール）イベントが発生したときにリストをクリーンアップ
    /// </summary>
    private void RemoveInvalidEnemy()
    {
        // イベント発生時に即座にターゲットリセットとリストクリーンアップを試みる
        CleanEnemyList();

        // 現在のターゲットが無効になっていたら、即座にnullにする
        if (currentEnemyTarget == null || currentEnemyTarget.enemyHP == null || currentEnemyTarget.enemyHP.currentHP <= 0)
        {
            currentEnemyTarget = null;
        }
    }

    private void OnDrawGizmos()
    {
        //円のギズモ（発生位置：半径）
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        //攻撃範囲に入ったときの処理
        if (collision.CompareTag("Enemy"))
        {
            //リストに格納する
            Enemy enemy = collision.GetComponent<Enemy>();
            // 【修正】Nullチェックを追加
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //攻撃範囲から出たときの処理
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();

            //リストの中に引数の要素があるか判定
            if (enemies.Contains(enemy))
            {
                //いるならリストから削除
                enemies.Remove(enemy);
            }
        }
    }
}