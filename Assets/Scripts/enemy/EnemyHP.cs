using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHP : MonoBehaviour
{
    [NonSerialized] public GameObject originalPrefab;

    [SerializeField] private float hp = 10f;
    [NonSerialized] public float currentHP;

    [Tooltip("EnemyData が指定されていないときに使う獲得コイン")]
    [SerializeField] private int defaultRewardCoin = 10;

    [SerializeField] private GameObject hpBar;
    [SerializeField] private Transform barPos;

    private Image hpBarImage;
    private GameObject instantiatedHpBar;

    private ScoreManager scoreManager;
    private Enemy enemy;
    private EnemyAnimations enemyAnimations;

    public static Action<Enemy> OnEnemyHit;
    public static Action OnEnemyDead;

    /// <summary>最大 HP。残量の割合を出したいときに使う</summary>
    public float MaxHP => hp;

    /// <summary>この個体の設定。倒したときの報酬や集計に使う</summary>
    public EnemyData Data { get; private set; }

    /// <summary>獲得コイン。EnemyData が無ければ従来どおりの固定値</summary>
    public int RewardCoin => (Data != null) ? Data.rewardCoin : defaultRewardCoin;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyAnimations = GetComponent<EnemyAnimations>();
        scoreManager = FindFirstObjectByType<ScoreManager>();

        CreateHealthBar();
    }

    /// <summary>
    /// EnemyData の値でプレハブ側の設定を上書きする。
    /// OnEnable より先に呼ばれる必要があるため、Spawner は
    /// SetActive(true) の前にこれを呼ぶ。
    /// </summary>
    public void Apply(EnemyData data)
    {
        Data = data;
        if (data == null) return;

        hp = data.maxHP;
        currentHP = hp;
    }

    private void OnEnable()
    {
        currentHP = hp;

        if (hpBarImage != null)
        {
            hpBarImage.fillAmount = 1f;
        }
    }

    private void CreateHealthBar()
    {
        if (hpBar == null || barPos == null) return;
        if (instantiatedHpBar != null) return;

        instantiatedHpBar = Instantiate(hpBar, barPos.position, Quaternion.identity);
        instantiatedHpBar.transform.SetParent(transform);

        EnemyHPBar healthBar = instantiatedHpBar.GetComponent<EnemyHPBar>();
        if (healthBar != null)
        {
            hpBarImage = healthBar.hpBarImage;
        }
    }

    void Update()
    {
        if (hpBarImage == null) return;

        hpBarImage.fillAmount = Mathf.Lerp(hpBarImage.fillAmount, currentHP / hp, Time.deltaTime * 10f);
    }

    public void ReduceHP(float damage)
    {
        currentHP -= damage;

        // OnEnemyHit は DeathCheck の前に呼ぶ。
        // 致命傷のときは currentHP が既に 0 以下なので、
        // 購読側（EnemyAnimations）が被弾演出を始めないようになっている。
        OnEnemyHit?.Invoke(enemy);

        DeathCheck();
    }

    private void DeathCheck()
    {
        if (currentHP > 0) return;

        // HP が 0 になったら、他の何よりも先にまず動きを止める
        enemy.StopMovement();

        currentHP = 0;

        if (enemyAnimations != null && enemyAnimations.isActiveAndEnabled)
        {
            // 撃破演出を見せてから返却する
            Invoke(nameof(Die), enemyAnimations.StopTime());
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        if (scoreManager != null && originalPrefab != null)
        {
            scoreManager.RecordDefeat(originalPrefab);
        }

        OnEnemyDead?.Invoke();
        ObjectPooler.ReturnToPool(gameObject);
    }
}
