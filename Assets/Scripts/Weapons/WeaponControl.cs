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

    [Header("黄色いタワーの貫通強化")]
    [SerializeField] private bool levelledPiercingShots;

    [Tooltip("Lv2以降は複数体に当たるため、1体目のダメージをこの割合に抑える")]
    [Range(0f, 1f)] [SerializeField] private float upgradedShotDamageScale = 0.8f;

    [Tooltip("1体貫通するごとに残るダメージの割合")]
    [Range(0f, 1f)] [SerializeField] private float pierceDamageRetention = 0.65f;

    [Min(0f)] [SerializeField] private float splashRadiusAtLevel2 = 0.45f;
    [Min(0f)] [SerializeField] private float splashRadiusPerLevel = 0.05f;
    [Range(0f, 1f)] [SerializeField] private float splashDamageAtLevel2 = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float splashDamagePerLevel = 0.01f;

    [Tooltip("1発の弾が範囲ダメージを与えられる追加の敵数。貫通1回ごとではなく1発全体で数える")]
    [Min(0)] [SerializeField] private int maxSplashTargets = 2;

    //弾のプール
    private ObjectPooler pooler;

    private void Awake()
    {
        // 購入したフレームで強化されても、Startで強化値を上書きしない。
        weapon = GetComponent<Weapon>();
        bulletDamage = damage;
        delay = firingInterval;
    }

    private void Start()
    {
        pooler = ObjectPooler.Instance;

        if (pooler == null)
        {
            Debug.LogError("グローバルな ObjectPooler がシーンに見つかりません。設置を確認してください。");
        }

    }

    private void Update()
    {
        if (!GameManager.IsGameActive) return;
        // ★★★ ロジックを大幅にシンプル化 ★★★
        // 1. 次の発射時間が来ているか？
        // 2. 攻撃対象の敵は存在するか？
        if (!HasLiveTarget())
        {
            return;
        }

        // 長く撃っていなかった（標的がいなかった）あとは、溜まった分を撃たず、今から数える
        if (Time.time - nextFireTime > delay) nextFireTime = Time.time;

        // 発射の時刻を時刻表（nextFireTime）で管理する。
        // 以前は「撃った時刻 + 間隔」まで待っていたので、1 フレームに 1 発しか撃てず、
        // 間隔が短いほど（強化したほど）・倍速やフレームレートが低いほど、間隔が長めに丸められて
        // 連射が遅くなっていた（間隔 0.134 秒・60fps で約 12%、倍速 ×1.5 ではさらに）。
        // 時刻に遅れた分は、同じフレームで続けて撃つ。前の弾がまだ飛んでいても、撃ってよい。
        int shots = 0;
        while (Time.time >= nextFireTime && shots < MaxShotsPerFrame && HasLiveTarget())
        {
            Fire();
            nextFireTime += delay;
            shots++;
        }
    }

    // 1 フレームに続けて撃てる最大の数（フレームが大きく遅れたときの暴発を防ぐ）
    private const int MaxShotsPerFrame = 3;

    private bool HasLiveTarget()
    {
        Enemy target = weapon.currentEnemyTarget;
        return target != null && target.enemyHP != null && target.enemyHP.IsAlive;
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
            int level = weapon.weaponUpgrade != null ? Mathf.Max(1, weapon.weaponUpgrade.level) : 1;
            bool upgradedPiercing = levelledPiercingShots && level >= 2;
            bullet.BulletInitialization(this,
                bulletDamage * (upgradedPiercing ? upgradedShotDamageScale : 1f));
            if (upgradedPiercing)
            {
                // 発射時点の性能を弾へ渡し、飛んでいる途中の強化や売却に影響されないようにする。
                bullet.ConfigurePiercing(level, pierceDamageRetention,
                    splashRadiusAtLevel2 + splashRadiusPerLevel * (level - 2),
                    splashDamageAtLevel2 + splashDamagePerLevel * (level - 2),
                    maxSplashTargets, weapon.attackRange + 2f);
            }
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
