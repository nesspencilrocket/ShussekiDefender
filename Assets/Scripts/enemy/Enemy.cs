using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Enemy : MonoBehaviour
{
    [NonSerialized] public EnemyHP enemyHP;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private float setMoveSpeed = 3f;
    [SerializeField] private float nextPointThreshold = 0.2f;

    private float moveSpeed;
    [NonSerialized] public MovePoint movePoint;
    private int currentMovePointIndex;

    private GameManager gameManager;
    private EnemyAnimations enemyAnimations;

    public Vector3 CurrentPointPosition => movePoint.GetMovePointPosition(currentMovePointIndex);
    public static Action OnReachedGoal;

    void Awake()
    {
        enemyHP = GetComponent<EnemyHP>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyAnimations = GetComponent<EnemyAnimations>();
        // ★★★ 修正点：警告が出ない新しい命令に変更 ★★★
        gameManager = FindFirstObjectByType<GameManager>();
    }

    void OnEnable()
    {
        currentMovePointIndex = 0;
        SetMoveSpeed();

        // 被弾表現で色を変えるため、プールから出るたびに白へ戻しておく。
        // 被弾中に倒されると色が戻らないまま返却されるため、ここが最後の砦になる。
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            // 4方向スプライトを使うので左右反転は不要。
            // 以前の名残が残っていると左向きが二重反転するため明示的に戻す。
            spriteRenderer.flipX = false;
        }
    }

    public void SetMoveSpeed()
    {
        moveSpeed = setMoveSpeed;
    }

    public void StopMovement()
    {
        moveSpeed = 0f;
    }

    void Update()
    {
        if (movePoint == null) return;
        Move();
        if (NextPointReached())
        {
            UpdatePointIndex();
        }
    }

    private void UpdatePointIndex()
    {
        if (currentMovePointIndex < movePoint.points.Length - 1)
        {
            currentMovePointIndex++;
        }
        else
        {
            ReachedGoal();
        }
    }

    private void ReachedGoal()
    {
        if (gameManager != null)
        {
            gameManager.RecordEnemyPass();
        }
        OnReachedGoal?.Invoke();
        ObjectPooler.ReturnToPool(gameObject);
    }

    public void ResetMovePoint()
    {
        currentMovePointIndex = 0;
    }

    private bool NextPointReached()
    {
        float distance = (transform.position - CurrentPointPosition).magnitude;
        return distance < nextPointThreshold;
    }

    private void Move()
    {
        Vector3 direction = CurrentPointPosition - transform.position;
        transform.position = Vector3.MoveTowards(
            transform.position,
            CurrentPointPosition,
            moveSpeed * Time.deltaTime);

        // 4方向スプライトへ進行方向を伝える。
        // 以前は flipX による左右反転だけだったため、上下へ移動しても
        // 正面（下向き）のまま歩いていた。
        if (enemyAnimations != null)
        {
            enemyAnimations.SetDirection(direction);
        }
    }

    public void SnapToStartPoint()
    {
        if (movePoint != null && movePoint.points.Length > 0)
        {
            transform.position = movePoint.points[0];
        }
    }
}