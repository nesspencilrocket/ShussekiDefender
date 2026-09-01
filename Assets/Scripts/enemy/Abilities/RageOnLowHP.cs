using UnityEngine;

/// <summary>
/// HP が一定割合を下回ると、被弾で足が止まらなくなる。
/// 「あと一撃で倒せるはずが押し切られる」緊張を作るための効果。
///
/// EnemyAbility の書き方の見本も兼ねている。
/// 状態（もう発動したか）を EnemyContext 側へ預けている点に注目してほしい。
/// このクラス自身はアセットなので、フィールドに持たせると
/// 1 体が発動しただけで全員が発動済みになってしまう。
/// </summary>
[CreateAssetMenu(menuName = "Shusseki/Ability/瀕死で怯まなくなる")]
public class RageOnLowHP : EnemyAbility
{
    [Tooltip("この割合を下回ると発動する（0.3 なら残り3割）")]
    [Range(0.05f, 0.9f)] public float threshold = 0.3f;

    /// <summary>個体ごとの状態。EnemyContext に預ける</summary>
    private class State { public bool fired; }

    public override void OnDamaged(EnemyContext context, float amount)
    {
        if (context == null || context.HP == null || context.Enemy == null) return;

        State state = context.GetState<State>(this);
        if (state.fired) return;

        float max = context.HP.MaxHP;
        if (max <= 0f) return;
        if (context.HP.currentHP / max > threshold) return;

        state.fired = true;

        // 被弾で止められた足を即座に戻す。
        // 以降も EnemyAnimations が止めにくるが、そのたびに打ち消される。
        context.Enemy.SetMoveSpeed();
    }
}
