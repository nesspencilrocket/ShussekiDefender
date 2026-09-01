using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教授の急病 ── 一定時間、画面上の敵をその場に止める。
///
/// コルーチンを使う見本。PlayerSkill はアセットなので自分では
/// StartCoroutine できない。SkillContext.Runner（発動側の MonoBehaviour）を
/// 借りて回す。
///
/// 待ち時間は WaitForSecondsRealtime にしていない。倍速中は
/// ゲーム内の時間が速く進むので、止まっている長さも同じ割合で
/// 短くなる方が体感として揃う。
/// </summary>
[CreateAssetMenu(menuName = "Shusseki/Skill/教授の急病")]
public class ProfessorIll : PlayerSkill
{
    [Tooltip("止めておく秒数")]
    [Min(0.1f)] public float duration = 3f;

    public override void Activate(SkillContext context)
    {
        if (context == null || context.Runner == null) return;

        context.Runner.StartCoroutine(Freeze(context));
    }

    private IEnumerator Freeze(SkillContext context)
    {
        // 発動時点で居た敵だけを止める。あとから湧いた敵は対象外。
        List<Enemy> targets = context.ActiveEnemies();

        foreach (Enemy e in targets)
        {
            if (e != null) e.StopMovement();
        }

        yield return new WaitForSeconds(duration);

        foreach (Enemy e in targets)
        {
            // 途中で倒された個体は動かさない。プールへ返却済みのものを
            // 動かすと、次に湧いた別の敵の速度を書き換えてしまう。
            if (e == null || !e.isActiveAndEnabled) continue;
            if (e.enemyHP == null || e.enemyHP.currentHP <= 0f) continue;

            e.SetMoveSpeed();
        }
    }
}
