using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 臨時休講 ── 画面上のすべての敵にまとめてダメージを与える。
///
/// PlayerSkill の書き方の見本。SkillContext から「いま居る敵」を取り、
/// それぞれに ReduceHP を通すだけ。撃破の集計もコインの加算も
/// EnemyHP 側が既存の経路でやってくれるので、ここでは何もしなくていい。
/// </summary>
[CreateAssetMenu(menuName = "Shusseki/Skill/臨時休講")]
public class EmergencyClosure : PlayerSkill
{
    [Tooltip("画面上の敵 1 体あたりに与えるダメージ")]
    [Min(1f)] public float damage = 30f;

    public override void Activate(SkillContext context)
    {
        if (context == null) return;

        List<Enemy> enemies = context.ActiveEnemies();
        foreach (Enemy e in enemies)
        {
            if (e == null || e.enemyHP == null) continue;
            e.enemyHP.ReduceHP(damage);
        }
    }
}
