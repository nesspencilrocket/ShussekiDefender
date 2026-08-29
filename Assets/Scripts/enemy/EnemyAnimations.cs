using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 敵のスプライトアニメーションを担当する。
///
/// 【なぜ Animator を使わないか】
/// ・4方向 × 歩行/被弾 を Animator で組むとクリップが 8 本以上必要になり、
///   敵の種類を増やすほど管理コストが跳ね上がる。
/// ・Animator はプールへ返却されたときに状態（色・コマ）を保持してしまい、
///   「被弾中に倒された敵が赤いまま再利用される」不具合の原因になっていた。
/// スプライト配列を直接めくる方式にすると、どちらも構造的に起きなくなる。
///
/// 【シートの並び】
/// ぴぽや系キャラチップと同じ 3 列 × 4 行。sprites には
/// 下(0,1,2) → 左(3,4,5) → 右(6,7,8) → 上(9,10,11) の順で 12 枚入れる。
/// </summary>
public class EnemyAnimations : MonoBehaviour
{
    /// <summary>シートの行順と一致させること</summary>
    public enum Facing
    {
        Down = 0,
        Left = 1,
        Right = 2,
        Up = 3,
    }

    [Header("スプライトシート")]
    [Tooltip("未設定なら同じ GameObject の SpriteRenderer を使う")]
    [SerializeField] private SpriteRenderer target;

    [Tooltip("下→左→右→上 の順に framesPerDirection 枚ずつ並べる（4方向 × 3コマ = 12枚）")]
    [SerializeField] private Sprite[] sprites = new Sprite[12];

    [Tooltip("1方向あたりのコマ数")]
    [SerializeField] private int framesPerDirection = 3;

    [Header("歩行アニメーション")]
    [Tooltip("1コマあたりの秒数。小さいほど速く歩く")]
    [SerializeField] private float secondsPerFrame = 0.16f;

    [Tooltip("コマの並び順。0→1→2→1 と往復させると足の運びが自然に見える")]
    [SerializeField] private int[] framePattern = { 0, 1, 2, 1 };

    [Header("被弾表現")]
    [Tooltip("被弾したときに一瞬かぶせる色")]
    [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Tooltip("被弾で足が止まる時間（秒）。撃破演出の待ち時間にも使う")]
    [SerializeField] private float hitStopTime = 0.15f;

    private Enemy enemy;
    private Facing facing = Facing.Down;
    private float frameTimer;
    private int patternIndex;
    private bool walking = true;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (target == null) target = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // プールから出るたびに初期状態へ戻す。
        // 色は Enemy.OnEnable でも白へ戻しているが、ここでも念のため揃える。
        facing = Facing.Down;
        patternIndex = 0;
        frameTimer = 0f;
        walking = true;
        if (target != null) target.color = Color.white;
        ApplyFrame();

        EnemyHP.OnEnemyHit += EnemyHit;
    }

    private void OnDisable()
    {
        EnemyHP.OnEnemyHit -= EnemyHit;
    }

    void Update()
    {
        if (!walking || framePattern == null || framePattern.Length == 0) return;

        frameTimer += Time.deltaTime;
        if (frameTimer < secondsPerFrame) return;

        frameTimer -= secondsPerFrame;
        patternIndex = (patternIndex + 1) % framePattern.Length;
        ApplyFrame();
    }

    /// <summary>
    /// 進行方向から向きを決める。Enemy.Move() から毎フレーム渡される。
    /// 横移動が縦移動より大きければ左右、そうでなければ上下を向く。
    /// </summary>
    public void SetDirection(Vector2 move)
    {
        if (move.sqrMagnitude < 0.0001f) return;

        Facing next = Mathf.Abs(move.x) >= Mathf.Abs(move.y)
            ? (move.x < 0f ? Facing.Left : Facing.Right)
            : (move.y < 0f ? Facing.Down : Facing.Up);

        if (next == facing) return;

        facing = next;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (target == null || sprites == null || framePattern == null || framePattern.Length == 0) return;

        int frame = framePattern[Mathf.Clamp(patternIndex, 0, framePattern.Length - 1)];
        int index = (int)facing * framesPerDirection + frame;

        if (index >= 0 && index < sprites.Length && sprites[index] != null)
        {
            target.sprite = sprites[index];
        }
    }

    /// <summary>
    /// 被弾で足が止まる時間。EnemyHP が撃破演出の待ち時間にも使う。
    /// </summary>
    public float StopTime()
    {
        return hitStopTime;
    }

    // ───── 被弾 ─────

    private void EnemyHit(Enemy hitEnemy)
    {
        // 自分への被弾で、かつまだ生きているときだけ反応する
        if (hitEnemy != enemy) return;
        if (enemy == null || enemy.enemyHP == null || enemy.enemyHP.currentHP <= 0) return;

        StopAllCoroutines();
        StartCoroutine(PlayHurt());
    }

    private IEnumerator PlayHurt()
    {
        enemy.StopMovement();
        walking = false;
        if (target != null) target.color = hitColor;

        yield return new WaitForSeconds(hitStopTime);

        if (target != null) target.color = Color.white;
        walking = true;
        enemy.SetMoveSpeed();
    }
}
