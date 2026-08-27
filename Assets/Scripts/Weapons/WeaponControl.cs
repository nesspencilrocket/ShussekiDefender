using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WeaponControl : MonoBehaviour
{
    //弾を生成する位置
    [SerializeField] private Transform bulletSpawnPos;
    //武器を格納
    private Weapon weapon;
    //設定用の弾ダメージ
    [SerializeField] private float damage = 2f;
    //アップグレードする際はこちらの変数の数値を変更する
    [NonSerialized] public float bulletDamage;

    //生成用
    [SerializeField] public GameObject fireBullet;

    //発射の間隔
    [SerializeField] private float firingInterval = 2f;
    private float nextFireTime;
    [NonSerialized] public float delay;

    //弾のプール
    private ObjectPooler pooler;

    private void Start()
    {
        weapon = GetComponent<Weapon>();
        pooler = ObjectPooler.Instance;

        if (pooler == null)
        {
            Debug.LogError("グローバルな ObjectPooler がシーンに見つかりません。設置を確認してください。");
        }

        bulletDamage = damage;
        delay = firingInterval;
    }

    private void Update()
    {
        // ★★★ ロジックを大幅にシンプル化 ★★★
        // 1. 次の発射時間が来ているか？
        // 2. 攻撃対象の敵は存在するか？
        if (Time.time > nextFireTime && weapon.currentEnemyTarget != null)
        {
            // 上記２つを満たしたら、発射処理を呼び出す
            Fire();
            // 次の発射時間を更新
            nextFireTime = Time.time + delay;
        }
    }

    /// <summary>
    /// ★★★ 新しい発射メソッド ★★★
    /// 弾をプールから取得し、設定して発射するまでを一度に行う
    /// </summary>
    private void Fire()
    {
        // プレハブやプーラーが設定されていなければ処理を中断
        if (fireBullet == null || pooler == null) // ← ここにブレークポイント
        {
            return;
        }

        // プールから弾を取得
        GameObject newBulletObject = pooler.GetObjectFromPool(fireBullet);
        if (newBulletObject == null) return;

        // 弾の位置と向きを、発射口(bulletSpawnPos)に合わせる
        newBulletObject.transform.position = bulletSpawnPos.position;
        newBulletObject.transform.rotation = bulletSpawnPos.rotation;

        // 弾のコンポーネントを取得
        Bullet bullet = newBulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            // 弾の初期設定を行い、ターゲットをセットする
            bullet.BulletInitialization(this, bulletDamage);
            bullet.SetTargetEnemy(weapon.currentEnemyTarget);
            // 弾を表示する
            newBulletObject.SetActive(true);
        }
    }

    // ResetBulletメソッドはBulletスクリプトから呼び出される可能性があるため、残しておきます。
    // 中身が空でも問題ありません。
    public void ResetBullet()
    {
        // このメソッドは以前のロジックで使われていましたが、
        // 新しいロジックでは不要になりました。
    }
}
