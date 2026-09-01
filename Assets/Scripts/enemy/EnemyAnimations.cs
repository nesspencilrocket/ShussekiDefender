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
    [Tooltip("ON: 進んだ距離でコマを送る（足が地面に合う）。OFF: 時間で送る")]
    [SerializeField] private bool advanceByDistance = true;

    // パターンを 1 周するあいだに進む距離。小さいほどコマ送りが速くなる。
    // 敵の身長が 1 単位・移動速度 3 のとき、2 コマ構成なら
    //   1.0 → 6 コマ/秒 ／ 0.5 → 12 コマ/秒 ／ 0.3 → 20 コマ/秒
    // 物理的な歩幅どおり（1.0）だと、小さいドット絵では歩いて見えにくい。
    [Tooltip("1 周で進む距離。小さいほどコマ送りが速くなる")]
    [Min(0.01f)] [SerializeField] private float distancePerCycle = 0.5f;

    [Tooltip("1コマあたりの秒数。advanceByDistance が OFF のときだけ使う")]
    [SerializeField] private float secondsPerFrame = 0.16f;

    [Tooltip("1フレームでこれ以上動いたらワープとみなし、コマを送らない")]
    [SerializeField] private float warpThreshold = 2f;

    // コマの並び順。
    //   {0, 2}       … 左足と右足だけの 2 コマ。歩いている感が強く出る（既定）
    //   {0, 1, 2, 1} … 中割りを挟む 4 コマ。滑らかだが動きは穏やか
    // どちらに変えても、歩幅は distancePerCycle 側で保たれる。
    [Tooltip("コマの並び順。{0, 2} は左足と右足のみ、{0, 1, 2, 1} は中割りを挟む")]
    [SerializeField] private int[] framePattern = { 0, 2 };

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

    // 距離ベースの送り用
    private Vector3 lastPosition;
    private float distanceAccum;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (target == null) target = GetComponent<SpriteRenderer>();

        Validate();
    }

    /// <summary>
    /// 設定漏れを黙って見逃さないための検査。
    ///
    /// スプライトが未設定でも「アニメーションしないだけ」で例外は出ないため、
    /// SpriteRenderer の初期スプライトのまま全員が同じ向きで固まる、という
    /// 紛らわしい症状になる。原因が一目で分かるよう Console に出しておく。
    /// </summary>
    private void Validate()
    {
        if (target == null)
        {
            Debug.LogError($"{name}: SpriteRenderer が見つかりません。", this);
            return;
        }

        int need = framesPerDirection * 4;   // 下・左・右・上 の 4 方向ぶん
        if (sprites == null || sprites.Length < need)
        {
            Debug.LogError(
                $"{name}: EnemyAnimations の sprites が {(sprites == null ? 0 : sprites.Length)} 枚しかありません。"
                + $"下→左→右→上 の順に {need} 枚（4方向 × {framesPerDirection}コマ）必要です。", this);
            return;
        }

        for (int i = 0; i < need; i++)
        {
            if (sprites[i] == null)
            {
                Debug.LogError($"{name}: EnemyAnimations の sprites[{i}] が未設定です。", this);
                return;
            }
        }
    }

    private void OnEnable()
    {
        // プールから出るたびに初期状態へ戻す。
        // 色は Enemy.OnEnable でも白へ戻しているが、ここでも念のため揃える。
        facing = Facing.Down;
        patternIndex = 0;
        frameTimer = 0f;
        walking = true;
        // Spawner は SetActive の前に位置を決めているので、ここで拾えば
        // 出現時のワープを距離としてカウントせずに済む
        lastPosition = transform.position;
        distanceAccum = 0f;
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
        if (framePattern == null || framePattern.Length == 0) return;

        if (advanceByDistance)
        {
            UpdateByDistance();
            return;
        }

        if (!walking) return;

        frameTimer += Time.deltaTime;
        if (frameTimer < secondsPerFrame) return;

        frameTimer -= secondsPerFrame;
        AdvanceFrame();
    }

    /// <summary>
    /// 実際に進んだ距離でコマを送る。
    ///
    /// 時間で送ると、移動速度や倍速を変えたときに足の運びと地面がずれて
    /// 滑って見える。距離で送れば、速度が変わっても歩幅は変わらない。
    /// 被弾や必殺技で止まっているあいだは距離が増えないので、
    /// 自然にその場で足も止まる。
    /// </summary>
    private void UpdateByDistance()
    {
        Vector3 now = transform.position;
        float moved = (now - lastPosition).magnitude;
        lastPosition = now;

        // 経路の切り替えなどで大きく飛んだ分は歩数に数えない
        if (moved > warpThreshold) return;

        // 1 周ぶんの距離をコマ数で割る。パターンを 2 コマにしても
        // 4 コマにしても、1 周で進む距離は変わらない＝歩幅が保たれる。
        float perFrame = distancePerCycle / framePattern.Length;

        distanceAccum += moved;
        if (distanceAccum < perFrame) return;

        // 1 フレームで複数コマ分進んだ場合も取りこぼさない
        while (distanceAccum >= perFrame)
        {
            distanceAccum -= perFrame;
            AdvanceFrame();
        }
    }

    private void AdvanceFrame()
    {
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
