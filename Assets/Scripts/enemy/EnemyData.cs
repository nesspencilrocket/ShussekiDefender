using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵 1 種類の定義。プレハブと数値をここに集約する。
///
/// ステージのルールを StageData に出したのと同じ考え方で、敵の強さも
/// アセット側へ出しておく。こうすると「6限目だけ歩行者を固くする」
/// といった調整が、プレハブを増やさずに数値だけでできる。
///
/// 【prefab を 1 つに統一しない理由】
/// 当たり判定のサイズが種類ごとに違い（自転車は横長、学務課は大きい）、
/// 実行時に Collider や見た目を差し替えるのはプールと相性が悪い。
/// ObjectPooler は複数プレハブに対応済みなので、見た目の違う敵は
/// Enemy.prefab の Prefab Variant として作り、ここから参照する。
/// </summary>
[CreateAssetMenu(menuName = "Shusseki/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("見せ方")]
    [Tooltip("リザルトなどに出す名前（歩行者 / 自転車 / 学務課）")]
    public string displayName = "歩行者";

    [Tooltip("Enemy.prefab から派生させた Prefab Variant")]
    public GameObject prefab;

    [Tooltip("リザルト一覧に並べるアイコン（任意）")]
    public Sprite icon;

    [Header("基本値")]
    [Tooltip("最大 HP。プレハブ側の値を上書きする")]
    public float maxHP = 10f;

    [Tooltip("移動速度。プレハブ側の値を上書きする")]
    public float moveSpeed = 3f;

    [Tooltip("倒したときに得られるコイン。現在は全種一律 10 になっている")]
    public int rewardCoin = 10;

    [Tooltip("倒したときのスコア。現在は全種一律 50 で計算している")]
    public int scoreValue = 50;
}
