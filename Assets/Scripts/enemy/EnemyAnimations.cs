using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimations : MonoBehaviour
{
    private Animator animator;
    private Enemy enemy;

    void Awake()
    {
        animator = GetComponent<Animator>();
        enemy = GetComponent<Enemy>();
    }

    private void PlayHitAnimation()
    {
        animator.SetTrigger("Hit");
    }

    private void EnemyHit(Enemy enemy)
    {
        if (this.enemy == enemy && this.enemy.enemyHP.currentHP > 0)
        {
            StartCoroutine(PlayHurt());
        }
    }

    private IEnumerator PlayHurt()
    {
        enemy.StopMovement();
        PlayHitAnimation();
        yield return new WaitForSeconds(StopTime());
        enemy.SetMoveSpeed();
    }

    public float StopTime()
    {
        // 以前調整した値
        return animator.GetCurrentAnimatorStateInfo(0).length + 0.1f;
    }

    //イベントに関数を登録・解除
    private void OnEnable()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Hit");

            // ▼▼▼ 修正点 ▼▼▼
            // "Walk" を正しい名前の "walk" (小文字) に変更
            animator.Play("walk", 0, 0f);
        }

        EnemyHP.OnEnemyHit += EnemyHit;
    }

    private void OnDisable()
    {
        EnemyHP.OnEnemyHit -= EnemyHit;
    }
}