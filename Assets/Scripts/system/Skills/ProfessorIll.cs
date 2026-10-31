using UnityEngine;

/// <summary>
/// 時間停止（旧称：教授の急病）── 一定時間、すべての敵の動きを止める。
///
/// 画面上の敵だけでなく、止めている間に湧く敵も止まる。さらに既定では、新しい敵が湧く時刻表も
/// 止めた分だけ後ろへずれる（pauseSpawns）。時限の時計は止まらないので、
/// 止めた分だけ、その時限で湧く人数が減る。武器は止まっている敵を撃てる。
///
/// 止める・動かすは、時刻（Time.time）を 1 つ持つだけで済ませている。
/// 敵ごとに解除を覚えておく必要がなく、倍速でも、止まっている長さが同じ割合で変わる
/// （ゲーム内の時間で数える）。
///
/// クラス名（ProfessorIll）は、アセットの参照を壊さないために、そのまま残している。
/// </summary>
[CreateAssetMenu(menuName = "Shusseki/Skill/時間停止")]
public class ProfessorIll : PlayerSkill
{
    [Tooltip("止めておく秒数")]
    [Min(0.1f)] public float duration = 3f;

    [Tooltip("ON：止めている間は、新しい敵も湧かない（湧く時刻表を、止めた分だけ後ろへずらす）。"
           + "OFF：止まっている敵の列に、新しい敵が湧き続けて溜まる")]
    public bool pauseSpawns = true;

    public override void Activate(SkillContext context)
    {
        float until = Time.time + duration;

        // 重ねて使っても、長い方に合わせる（短くはならない）
        Enemy.TimeStopUntil = Mathf.Max(Enemy.TimeStopUntil, until);
        if (pauseSpawns) Spawner.SpawnPausedUntil = Mathf.Max(Spawner.SpawnPausedUntil, until);
    }
}
