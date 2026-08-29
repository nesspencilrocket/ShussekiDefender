using UnityEngine;

/// <summary>
/// 敵の特殊効果を 1 つ表す抽象アセット。
///
/// 派生クラスを 1 本足すだけで新しい効果が増える。既存の Enemy / EnemyHP を
/// 書き換える必要はなく、EnemyData の abilities に並べるだけで組み合わせられる。
///
/// 【状態を持たせてはいけない】
/// これは ScriptableObject＝アセットなので、実体は全個体で 1 つしかない。
/// 「残りクールダウン」「もう発動したか」といった値をこのクラスの
/// フィールドに置くと、1 体が発動しただけで全員が発動済みになる。
/// 個体ごとの値は必ず EnemyContext 側へ置くこと。
///
/// 【想定している使い方】
///   [CreateAssetMenu(menuName = "Shusseki/Ability/HPが減ると加速")]
///   public class RageOnLowHP : EnemyAbility
///   {
///       [SerializeField] private float threshold = 0.3f;
///       [SerializeField] private float speedScale = 1.8f;
///
///       public override void OnDamaged(EnemyContext c, float amount)
///       {
///           var state = c.GetState&lt;Flag&gt;(this);   // ← 状態は context 側に置く
///           if (state.done) return;
///           if (残 HP の割合 &gt; threshold) return;
///           state.done = true;
///           // ここで移動速度を上げる
///       }
///
///       private class Flag { public bool done; }
///   }
/// </summary>
public abstract class EnemyAbility : ScriptableObject
{
    [Header("共通")]
    [Tooltip("Inspector で見分けるための名前。挙動には影響しない")]
    public string displayName;

    [Tooltip("再発動までの待ち時間（秒）。使わない効果は 0 のままでよい")]
    [Min(0f)] public float cooldown = 0f;

    /// <summary>敵がプールから出て動き出すとき</summary>
    public virtual void OnSpawn(EnemyContext context) { }

    /// <summary>毎フレーム。deltaTime は倍速の影響を受けた値が渡る</summary>
    public virtual void OnTick(EnemyContext context, float deltaTime) { }

    /// <summary>ダメージを受けたとき。amount は今回の被ダメージ量</summary>
    public virtual void OnDamaged(EnemyContext context, float amount) { }

    /// <summary>倒されたとき。分裂やアイテム落としはここ</summary>
    public virtual void OnDeath(EnemyContext context) { }

    /// <summary>ゴールへ到達したとき</summary>
    public virtual void OnReachedGoal(EnemyContext context) { }
}
