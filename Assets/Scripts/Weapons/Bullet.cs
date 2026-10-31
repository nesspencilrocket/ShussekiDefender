using System;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float damageDistance = 0.1f;

    [Tooltip("ON: 当たり判定の半径を、見えている球の大きさ（スプライトの大きさ × スケール）から決める。"
           + "見た目と当たり判定がずれず、スケールを変えれば当たり判定も変わる。damageDistance は使わない")]
    [SerializeField] private bool hitRadiusFromSprite;

    [Tooltip("スプライトの外枠のうち、球として見えている部分の割合。余白の分だけ当たり判定を小さくする")]
    [Range(0.5f, 1f)] [SerializeField] private float visibleFill = 0.93f;
    [Min(0.1f)] [SerializeField] private float maxLifetime = 3f;

    private EnemyHP targetHP;
    private uint targetSpawnVersion;
    private WeaponControl bulletControl;
    private SpriteRenderer spriteRenderer;
    private float damage;
    private int maxHits = 1;
    private int hitCount;
    private float damageRetention = 1f;
    private float splashRadius;
    private float splashDamageRatio;
    private int remainingSplashTargets;
    private float maxDistance = 20f;
    private float travelled;
    private float elapsed;
    private Vector2 flightDirection;
    private bool piercing;

    // 配列の固定上限で密集時の敵を取りこぼさないよう、再利用できるリストを使う。
    private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>(16);
    private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
    private readonly List<EnemyHP> splashCandidates = new List<EnemyHP>(16);
    private readonly HashSet<EnemyHP> directHits = new HashSet<EnemyHP>();
    private readonly HashSet<EnemyHP> splashHits = new HashSet<EnemyHP>();
    private readonly ContactFilter2D collisionFilter = new ContactFilter2D { useTriggers = true };
    private Vector2 splashCenter;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 球の見た目と当たり判定を同じ大きさにそろえる。
        // 見た目はスプライトの大きさとスケールで決まるので、そこから半径を求める。
        if (hitRadiusFromSprite && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float halfSize = spriteRenderer.sprite.bounds.extents.x * Mathf.Abs(transform.lossyScale.x);
            damageDistance = halfSize * visibleFill;
        }
    }

    private void OnDisable()
    {
        ClearShot();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;
        elapsed += Time.deltaTime;
        if (elapsed >= maxLifetime || travelled >= maxDistance)
        {
            ResetAndReturnToPool();
            return;
        }

        if (piercing) MovePiercing();
        else MoveHoming();
    }

    private void MoveHoming()
    {
        if (targetHP == null || !targetHP.IsAlive || targetHP.SpawnVersion != targetSpawnVersion)
        {
            ResetAndReturnToPool();
            return;
        }

        Vector2 from = transform.position;
        Vector2 target = targetHP.transform.position;
        Vector2 direction = target - from;
        if (direction.sqrMagnitude > 0.000001f) flightDirection = direction.normalized;
        Vector2 next = Vector2.MoveTowards(from, target,
            Mathf.Min(moveSpeed * Time.deltaTime, maxDistance - travelled));

        // 狙った相手の手前にいる敵も拾うことで、敵の列を狙った配置が貫通に生きる。
        if (maxHits > 1 && SweepHits(from, Vector2.Distance(from, next))) return;
        transform.position = new Vector3(next.x, next.y, transform.position.z);
        travelled += Vector2.Distance(from, next);

        if (piercing) return;

        if (Vector2.Distance(next, target) > damageDistance) return;

        Hit(targetHP, target);
        if (hitCount >= maxHits)
        {
            ResetAndReturnToPool();
            return;
        }

        // 最初に命中した後は直進し、遠くの敵へ次々と追尾する連鎖弾にはしない。
        targetHP = null;
        piercing = true;
    }

    private void MovePiercing()
    {
        Vector2 from = transform.position;
        float distance = Mathf.Min(moveSpeed * Time.deltaTime, maxDistance - travelled);
        if (SweepHits(from, distance)) return;

        Vector2 next = from + flightDirection * distance;
        transform.position = new Vector3(next.x, next.y, transform.position.z);
        travelled += distance;
    }

    private bool SweepHits(Vector2 from, float distance)
    {
        castHits.Clear();
        Physics2D.CircleCast(from, damageDistance, flightDirection, collisionFilter, castHits, distance);
        castHits.Sort(CompareHitDistance);
        foreach (RaycastHit2D contact in castHits)
        {
            EnemyHP hp = contact.collider.GetComponentInParent<EnemyHP>();
            if (hp == null || !hp.IsAlive || directHits.Contains(hp)) continue;
            Hit(hp, hp.transform.position);
            targetHP = null;
            piercing = true;
            if (hitCount >= maxHits)
            {
                ResetAndReturnToPool();
                return true;
            }
        }

        return false;
    }

    private static int CompareHitDistance(RaycastHit2D a, RaycastHit2D b)
    {
        return a.distance.CompareTo(b.distance);
    }

    private void Hit(EnemyHP hp, Vector2 position)
    {
        float directDamage = damage * Mathf.Pow(damageRetention, hitCount);
        directHits.Add(hp);
        hitCount++;
        hp.ReduceHP(directDamage);
        ApplySplash(position, directDamage);
    }

    private void ApplySplash(Vector2 position, float directDamage)
    {
        if (splashRadius <= 0f || splashDamageRatio <= 0f) return;
        ProjectileSplashEffect.Show(position, splashRadius, spriteRenderer);
        if (remainingSplashTargets <= 0) return;

        overlaps.Clear();
        splashCandidates.Clear();
        Physics2D.OverlapCircle(position, splashRadius, collisionFilter, overlaps);
        foreach (Collider2D collider in overlaps)
        {
            EnemyHP hp = collider.GetComponentInParent<EnemyHP>();
            if (hp == null || !hp.IsAlive || directHits.Contains(hp) || splashHits.Contains(hp)
                || splashCandidates.Contains(hp)) continue;
            // 周囲のコライダーの大きさで範囲が広がらないよう、中心までの距離でも判定する。
            if (((Vector2)hp.transform.position - position).sqrMagnitude <= splashRadius * splashRadius)
                splashCandidates.Add(hp);
        }

        splashCenter = position;
        splashCandidates.Sort(CompareSplashDistance);
        foreach (EnemyHP hp in splashCandidates)
        {
            if (remainingSplashTargets <= 0) break;
            if (!hp.IsAlive) continue;
            splashHits.Add(hp);
            remainingSplashTargets--;
            hp.ReduceHP(directDamage * splashDamageRatio);
        }
    }

    private int CompareSplashDistance(EnemyHP a, EnemyHP b)
    {
        float aDistance = ((Vector2)a.transform.position - splashCenter).sqrMagnitude;
        float bDistance = ((Vector2)b.transform.position - splashCenter).sqrMagnitude;
        return aDistance.CompareTo(bDistance);
    }

    public void SetTargetEnemy(Enemy enemy)
    {
        targetHP = enemy != null ? enemy.GetComponent<EnemyHP>() : null;
        targetSpawnVersion = targetHP != null ? targetHP.SpawnVersion : 0;
        if (targetHP != null)
        {
            Vector2 direction = targetHP.transform.position - transform.position;
            if (direction.sqrMagnitude > 0.000001f) flightDirection = direction.normalized;
        }
    }

    public void BulletInitialization(WeaponControl weaponControl, float damage)
    {
        // OnEnableで消すと、プールから表示する直前に渡された設定まで失われる。
        ClearShot();
        bulletControl = weaponControl;
        this.damage = damage;
    }

    public void ConfigurePiercing(int hitLimit, float retention, float radius, float splashRatio,
        int splashTargetLimit, float travelLimit)
    {
        maxHits = Mathf.Max(1, hitLimit);
        damageRetention = Mathf.Clamp01(retention);
        splashRadius = Mathf.Max(0f, radius);
        splashDamageRatio = Mathf.Clamp01(splashRatio);
        remainingSplashTargets = Mathf.Max(0, splashTargetLimit);
        maxDistance = Mathf.Max(0.1f, travelLimit);
    }

    private void ClearShot()
    {
        targetHP = null;
        targetSpawnVersion = 0;
        bulletControl = null;
        damage = 0f;
        maxHits = 1;
        hitCount = 0;
        damageRetention = 1f;
        splashRadius = 0f;
        splashDamageRatio = 0f;
        remainingSplashTargets = 0;
        maxDistance = 20f;
        travelled = 0f;
        elapsed = 0f;
        flightDirection = Vector2.up;
        piercing = false;
        directHits.Clear();
        splashHits.Clear();
        castHits.Clear();
        overlaps.Clear();
        splashCandidates.Clear();
    }

    private void ResetAndReturnToPool()
    {
        if (bulletControl != null) bulletControl.ResetBullet();
        transform.localRotation = Quaternion.identity;
        ObjectPooler.ReturnToPool(gameObject);
    }
}
