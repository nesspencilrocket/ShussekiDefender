using System;
using UnityEngine;

/// <summary>
/// ウェーブ 1 つの中で「どの敵をどれくらいの割合で出すか」を表す 1 行。
///
/// 【なぜ % ではなく重みなのか】
/// % で持つと合計を 100 に保つ制約が生まれ、敵を 1 種類足すたびに
/// 既存の数字を全部いじり直すことになる。6 ステージ × 5 波 = 30 箇所で
/// それが起きると調整が重い。
/// 重みなら「自転車を weight 3 で追加」だけで済み、他の行は触らなくていい。
/// 実際の出現率は「自分の weight ÷ 全 weight の合計」で決まる。
///
/// 【確定枠が要る理由】
/// 重み抽選だけでは「6限目に学務課が必ず 1 体来る」を保証できない。
/// 運が悪いと 1 体も出ない波が生まれ、難易度が安定しない。
/// guaranteedCount の分を先に配置し、残りを重みで埋める。
/// </summary>
[Serializable]
public class SpawnEntry
{
    [Tooltip("出現させる敵の種類")]
    public EnemyData enemy;

    [Tooltip("抽選の重み。大きいほど出やすい。0 なら確定枠でしか出ない")]
    [Min(0)] public int weight = 1;

    [Tooltip("この波で必ず出す数。ボスや初登場の敵に使う")]
    [Min(0)] public int guaranteedCount = 0;
}
