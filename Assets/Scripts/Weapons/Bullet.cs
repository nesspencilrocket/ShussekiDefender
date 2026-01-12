using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{

    //弾の移動速度
    [SerializeField] private float moveSpeed = 10f;
    //ダメージ発生距離
    [SerializeField] private float damageDistance = 0.1f;
    //ターゲット格納
    private Enemy enemyTarget;
    //ダメージ
    private float damage;


    //この弾を管理するコンポーネント
    private WeaponControl bulletControl;

    private void Start()
    {
        // Startは安全のため空のまま
    }

    void Update()
    {
        // 【修正】ターゲットが有効か、そのトランスフォームがまだ存在するか確認
        if (enemyTarget != null && enemyTarget.transform != null)
        {
            //弾を動かす
            MoveBullet();
        }
        else
        {
            // ターゲットが消えた場合、弾を無効化してプールに戻す
            ResetAndReturnToPool();
        }
    }

    private void MoveBullet()
    {
        //現在地から目的地まで一定速度で移動
        transform.position = Vector2.MoveTowards(transform.position,
            enemyTarget.transform.position, moveSpeed * Time.deltaTime);

        //弾と敵の距離確認
        CheckDistance();
    }

    /// <summary>
    /// 弾と敵の距離を確認して近ければダメージ
    /// </summary>
    private void CheckDistance()
    {
        // ターゲットが有効か再確認
        if (enemyTarget == null)
        {
            ResetAndReturnToPool();
            return;
        }

        //敵との距離
        float distanceToTarget = (enemyTarget.transform.position -
            transform.position).magnitude;

        //十分近づいたら
        if (distanceToTarget < damageDistance)
        {
            // 【修正】enemyTarget.enemyHPがnullでないかチェック (重要)
            if (enemyTarget.enemyHP != null)
            {
                //ダメージ
                enemyTarget.enemyHP.ReduceHP(damage);
            }

            //弾の設定を初期化し、プールに戻す
            ResetAndReturnToPool();
        }
    }


    /// <summary>
    /// 攻撃対象を設定する
    /// </summary>
    /// <param name="enemy"></param>
    public void SetTargetEnemy(Enemy enemy)
    {
        enemyTarget = enemy;
    }


    /// <summary>
    /// 弾の初期設定(弾の管理者、ダメージ)
    /// </summary>
    public void BulletInitialization(WeaponControl weaponControl, float damage)
    {
        //引数を変数に格納
        bulletControl = weaponControl;
        this.damage = damage;

        // ResetBulletは外部からのTarget設定前に行うべきなので、
        // ここでは呼ばずに、ResetBulletWithTargetResetを使う
    }

    /// <summary>
    /// 弾をプールに戻す際の処理をまとめる
    /// </summary>
    private void ResetAndReturnToPool()
    {
        // 弾の設定を初期化
        if (bulletControl != null)
        {
            bulletControl.ResetBullet();
        }

        // ターゲットを解除
        enemyTarget = null;
        transform.localRotation = Quaternion.identity;

        // プールに戻す
        ObjectPooler.ReturnToPool(gameObject);
    }
}