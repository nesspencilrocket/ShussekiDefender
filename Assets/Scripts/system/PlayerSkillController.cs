using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 必殺技の発動役。ステージの skills を読み、クールダウンとコイン支払いを管理する。
///
/// PlayerSkill はアセットなので「残りクールダウン」のような状態を持てない。
/// 個体ごとの状態はすべてここに置く（PlayerSkill のコメント参照）。
///
/// シーンに置かなくても Ensure() で作られる。UI（SkillBar）や
/// 入力側はこれを通じて発動する。
/// </summary>
public class PlayerSkillController : MonoBehaviour
{
    public static PlayerSkillController Instance { get; private set; }

    /// <summary>発動したとき。UI が演出やボタンの更新に使う</summary>
    public static Action<int> OnActivated;

    private readonly List<PlayerSkill> skills = new List<PlayerSkill>();

    // 各技が次に使える時刻（Time.time）。倍速中は早く回復するが、
    // 敵の動きも速いので体感の長さは揃う（ProfessorIll と同じ考え方）
    private float[] readyAt = new float[0];

    // 各技を、このステージで何回使ったか。コストと待ち時間が、使うたびに増える技のために数える
    private int[] uses = new int[0];
    // いま進んでいる待ち時間の長さ（使ったときに決まる）。幕の縮み方の割合に使う
    private float[] cooldownLength = new float[0];

    private SkillContext context;

    public IReadOnlyList<PlayerSkill> Skills => skills;

    /// <summary>
    /// シーンに居なければ作って返す。
    /// 各ステージのシーンに手で置かなくて済むようにするため。
    /// </summary>
    public static PlayerSkillController Ensure()
    {
        if (Instance != null) return Instance;

        PlayerSkillController found = FindFirstObjectByType<PlayerSkillController>();
        if (found != null) return found;

        return new GameObject("PlayerSkillController").AddComponent<PlayerSkillController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        StageData stage = (GameManager.Instance != null) ? GameManager.Instance.Stage : null;
        Load(stage);
    }

    /// <summary>
    /// ステージの技一覧を取り込む。null の要素は飛ばす。
    /// Start より前に UI が読みたいときのために公開してある。
    /// </summary>
    public void Load(StageData stage)
    {
        skills.Clear();
        if (stage != null && stage.skills != null)
        {
            foreach (PlayerSkill s in stage.skills)
            {
                if (s != null) skills.Add(s);
            }
        }

        readyAt = new float[skills.Count];
        uses = new int[skills.Count];
        cooldownLength = new float[skills.Count];
        context = new SkillContext(this, CurrencyManager.instance);
    }

    /// <summary>残りクールダウン（秒）。使えるなら 0</summary>
    public float CooldownRemaining(int index)
    {
        if (index < 0 || index >= skills.Count) return 0f;
        return Mathf.Max(0f, readyAt[index] - Time.time);
    }

    /// <summary>残りクールダウンの割合。1 で使ったばかり、0 で使える</summary>
    public float CooldownRatio(int index)
    {
        if (index < 0 || index >= skills.Count) return 0f;
        float cd = cooldownLength[index];
        if (cd <= 0f) return 0f;
        return Mathf.Clamp01(CooldownRemaining(index) / cd);
    }

    /// <summary>あと何回使えるか。回数の制限がない技は -1</summary>
    public int RemainingUses(int index)
    {
        if (index < 0 || index >= skills.Count) return 0;
        if (skills[index].maxUses <= 0) return -1;
        return Mathf.Max(0, skills[index].maxUses - uses[index]);
    }

    /// <summary>いま発動するのに必要なコイン（使うたびに増える分を含む）</summary>
    public int CurrentCost(int index)
    {
        if (index < 0 || index >= skills.Count) return 0;
        return skills[index].CostAfter(uses[index]);
    }

    /// <summary>
    /// いま発動できるか。ゲームが動いていて、クールダウンが明けていて、
    /// 技側の条件（コインなど）も満たしていること。
    /// </summary>
    public bool CanActivate(int index)
    {
        if (index < 0 || index >= skills.Count) return false;
        if (!GameManager.IsGameActive) return false;
        // 回数を使い切った技は、使えない
        if (skills[index].IsExhausted(uses[index])) return false;
        if (CooldownRemaining(index) > 0f) return false;
        return skills[index].CanActivate(context, CurrentCost(index));
    }

    /// <summary>
    /// 発動を試みる。成功したらコインを払い、クールダウンを開始して true。
    /// </summary>
    public bool TryActivate(int index)
    {
        if (!CanActivate(index)) return false;

        PlayerSkill skill = skills[index];
        int cost = CurrentCost(index);
        if (cost > 0 && context.Currency != null)
        {
            context.Currency.RemoveCoins(cost);
        }

        skill.Activate(context);

        // この回の待ち時間は、使った回数に応じた長さ。そのあと、使った回数を 1 つ増やす
        cooldownLength[index] = skill.CooldownAfter(uses[index]);
        readyAt[index] = Time.time + cooldownLength[index];
        uses[index]++;
        OnActivated?.Invoke(index);
        return true;
    }
}
