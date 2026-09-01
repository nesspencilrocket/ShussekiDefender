using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 特殊効果（EnemyAbility）に渡す、敵 1 体ぶんの作業領域。
///
/// 【なぜこれが必要か】
/// EnemyAbility は ScriptableObject＝アセットなので、実体は全個体で 1 つしかない。
/// クールダウンの残り時間や「もう発動したか」といった値を SO のフィールドに
/// 置くと、1 体が発動しただけで全員が発動済みになってしまう。
///
/// そこで「何をするか」は SO（EnemyAbility）に、
/// 「その個体がいまどうなっているか」はここ（EnemyContext）に置いて分ける。
/// この分離を最初に決めておかないと、効果を増やすたびに同じ罠を踏む。
/// </summary>
public class EnemyContext
{
    public Enemy Enemy { get; private set; }
    public EnemyHP HP { get; private set; }
    public Transform Transform { get; private set; }

    // 個体ごとの状態。キーは「どの効果の分か」を表す
    private readonly Dictionary<EnemyAbility, float> cooldowns = new Dictionary<EnemyAbility, float>();
    private readonly Dictionary<EnemyAbility, object> states = new Dictionary<EnemyAbility, object>();

    public EnemyContext(Enemy enemy, EnemyHP hp)
    {
        Enemy = enemy;
        HP = hp;
        Transform = enemy != null ? enemy.transform : null;
    }

    // ───── クールダウン ─────

    /// <summary>この効果がいま使えるか</summary>
    public bool IsReady(EnemyAbility ability)
    {
        return !cooldowns.TryGetValue(ability, out float remain) || remain <= 0f;
    }

    /// <summary>この効果を使ったので、指定秒だけ待たせる</summary>
    public void StartCooldown(EnemyAbility ability, float seconds)
    {
        cooldowns[ability] = seconds;
    }

    /// <summary>毎フレーム 1 回、経過時間を渡して減らす</summary>
    public void TickCooldowns(float deltaTime)
    {
        if (cooldowns.Count == 0) return;

        // 走査中に書き換えられないよう、キーを複製してから回す
        var keys = new List<EnemyAbility>(cooldowns.Keys);
        foreach (var key in keys)
        {
            float remain = cooldowns[key] - deltaTime;
            cooldowns[key] = remain > 0f ? remain : 0f;
        }
    }

    // ───── 効果ごとの任意の状態 ─────

    /// <summary>
    /// 効果が自由に使える入れ物を取り出す。無ければ作る。
    /// 「分裂は 1 回だけ」のようなフラグをここに持たせる。
    /// </summary>
    public T GetState<T>(EnemyAbility ability) where T : class, new()
    {
        if (states.TryGetValue(ability, out object value) && value is T typed)
        {
            return typed;
        }

        T created = new T();
        states[ability] = created;
        return created;
    }

    /// <summary>
    /// プールから出し直すときに呼ぶ。前回の個体の状態を持ち越さないための後始末。
    /// これを忘れると「一度分裂した敵が二度と分裂しない」といった不具合になる。
    /// </summary>
    public void ResetAll()
    {
        cooldowns.Clear();
        states.Clear();
    }
}
