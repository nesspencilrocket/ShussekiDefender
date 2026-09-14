using UnityEngine;

/// <summary>
/// 倒したときに追加でコインを落とす。
/// 硬い敵ほど見返りを大きくして、「弱い敵を大量に出す方が得」という
/// 逆転が起きないようにするための効果。
///
/// OnDeath を使う見本。状態を持たないので EnemyContext は参照だけに使う。
/// </summary>
[CreateAssetMenu(menuName = "Shusseki/Ability/撃破時にコイン追加")]
public class BonusCoinOnDeath : EnemyAbility
{
    [Tooltip("通常の報酬に上乗せするコイン")]
    [Min(0)] public int bonusCoin = 20;

    public override void OnDeath(EnemyContext context)
    {
        if (bonusCoin <= 0) return;
        if (CurrencyManager.instance == null) return;

        CurrencyManager.instance.AddCoins(bonusCoin);
    }
}
