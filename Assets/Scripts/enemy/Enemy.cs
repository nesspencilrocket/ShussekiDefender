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

    public Vector3 CurrentPointPosition => movePoint.GetMovePointPosition(currentMovePointIndex);
    public static Action OnReachedGoal;

    void Awake()
    {
        enemyHP = GetComponent<EnemyHP>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // ★★★ 修正点：警告が出ない新しい命令に変更 ★★★
        gameManager = FindFirstObjectByType<GameManager>();
    }

    void OnEnable()
    {
        currentMovePointIndex = 0;
        SetMoveSpeed();
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

        if (spriteRenderer != null)
        {
            if (direction.x < 0) spriteRenderer.flipX = true;
            else if (direction.x > 0) spriteRenderer.flipX = false;
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