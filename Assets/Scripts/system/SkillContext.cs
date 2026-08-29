using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 必殺技（PlayerSkill）に渡す、発動時の周辺情報。
///
/// PlayerSkill は ScriptableObject＝アセットなので、シーン内の敵や武器を
/// 直接参照できない。発動のたびに「いま何が居るか」をここから取らせる。
///
/// 敵と武器の取得を毎フレームではなく発動時だけに限っているのは、
/// 必殺技が数十秒に一度しか使われないため。常時監視する仕組みを
/// 増やすより、その瞬間に数えた方が単純で速い。
/// </summary>
public class SkillContext
{
    /// <summary>コルーチンを回したいときの実行主体（PlayerSkillController）</summary>
    public MonoBehaviour Runner { get; private set; }

    /// <summary>コスト支払いや報酬付与に使う</summary>
    public CurrencyManager Currency { get; private set; }

    public SkillContext(MonoBehaviour runner, CurrencyManager currency)
    {
        Runner = runner;
        Currency = currency;
    }

    /// <summary>
    /// いま画面に出ている敵。プールで非アクティブな個体は含まれない。
    /// 「画面上の敵に大ダメージ」「全体を一定時間止める」などで使う。
    /// </summary>
    public List<Enemy> ActiveEnemies()
    {
        var found = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        var result = new List<Enemy>(found.Length);

        foreach (var e in found)
        {
            if (e == null) continue;
            if (e.enemyHP == null || e.enemyHP.currentHP <= 0f) continue;
            result.Add(e);
        }
        return result;
    }

    /// <summary>
    /// 設置済みの武器。「全武器が即座に1発撃つ」などで使う。
    /// </summary>
    public List<Weapon> PlacedWeapons()
    {
        var found = Object.FindObjectsByType<Weapon>(FindObjectsSortMode.None);
        return new List<Weapon>(found);
    }
}
