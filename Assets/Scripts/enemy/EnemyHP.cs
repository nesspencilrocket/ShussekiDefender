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

    [SerializeField] private GameObject hpBar;

    [SerializeField] private Transform barPos;



    private Image hpBarImage;

    private GameObject instantiatedHpBar;



    private ScoreManager scoreManager;

    public static Action<Enemy> OnEnemyHit;

    private Enemy enemy;

    private EnemyAnimations enemyAnimations;

    public static Action OnEnemyDead;



    private void Awake()

    {

        enemy = GetComponent<Enemy>();

        enemyAnimations = GetComponent<EnemyAnimations>();

        scoreManager = FindFirstObjectByType<ScoreManager>();

        CreateHealthBar();

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



        // OnEnemyHitはDeathCheckの前に呼び出すのが安全です

        OnEnemyHit?.Invoke(enemy);



        DeathCheck();

    }



    private void DeathCheck()

    {

        if (currentHP <= 0)

        {

            // ▼▼▼ 最重要修正点 ▼▼▼

            // HPが0になったら、他の何よりも先にまず動きを止める！

            enemy.StopMovement();



            currentHP = 0;

            if (enemyAnimations != null && enemyAnimations.isActiveAndEnabled)

            {

                Invoke("Die", enemyAnimations.StopTime());

            }

            else

            {

                Die();

            }

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