using UnityEngine;

/// <summary>
/// プレイヤーの必殺技を 1 つ表す抽象アセット。
///
/// 派生クラスを 1 本足すだけで技が増える。どの技を使えるかは
/// StageData に持たせる想定で、「3限目から解禁」といった構成を
/// 数値だけで作れるようにする。
///
/// 【状態を持たせてはいけない】
/// EnemyAbility と同じ理由。これは ScriptableObject＝アセットなので、
/// 実体は 1 つしかない。残りクールダウンなどの「いまどうなっているか」は
/// 発動側（PlayerSkillController）が持つこと。
/// ここに書いてよいのは「この技の仕様」だけ。
///
/// 【想定している技】
/// 妨害する側という設定に合わせると、こういう方向が噛み合う。
///   臨時休講 … 画面上の敵に大ダメージ
///   教授の急病 … 一定時間すべての敵を止める
///   一斉指導 … 全武器が即座に 1 発撃つ
/// 名前は後から決められるので、まず器だけ用意する。
/// </summary>
public abstract class PlayerSkill : ScriptableObject
{
    [Header("見せ方")]
    [Tooltip("ボタンに出す名前")]
    public string displayName = "必殺技";

    [TextArea(2, 4)]
    [Tooltip("効果の説明。UI のツールチップなどに使う")]
    public string description;

    [Tooltip("ボタンのアイコン")]
    public Sprite icon;

    [Header("使用条件")]
    [Tooltip("再使用までの待ち時間（秒）。0 なら制限なし")]
    [Min(0f)] public float cooldown = 30f;

    [Tooltip("発動に必要なコイン。0 なら無料")]
    [Min(0)] public int coinCost = 0;

    /// <summary>
    /// いま使えるか。コイン以外の条件を足したいときに override する。
    /// クールダウンの判定は発動側が持つので、ここでは見ない。
    /// </summary>
    public virtual bool CanActivate(SkillContext context)
    {
        if (coinCost <= 0) return true;
        return context != null
            && context.Currency != null
            && context.Currency.totalCoins >= coinCost;
    }

    /// <summary>
    /// 実際の効果。コインの支払いは発動側が済ませてから呼ぶ。
    /// </summary>
    public abstract void Activate(SkillContext context);
}
