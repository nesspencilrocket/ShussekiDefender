using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    // --- 【Inspectorで設定する統計表示設定】 ---

    [Serializable]
    public struct EnemyStatsSetting
    {
        [Tooltip("統計を記録・表示したい敵のプレハブ")]
        public GameObject enemyPrefab;

        [Tooltip("この敵の討伐数を表示するUIテキスト (オプション)")]
        public TextMeshProUGUI killCountDisplay;

        [NonSerialized] public int killCount; // 討伐数を保持する変数
    }

    [Header("Enemy Stats Settings (Display Order)")]
    [Tooltip("討伐数を記録・表示する敵のプレハブとUIのリスト")]
    [SerializeField]
    private List<EnemyStatsSetting> enemyStatsSettings = new List<EnemyStatsSetting>();

    // ---------------------------------------------

    // --- 【ランタイムデータ】 ---
    private Dictionary<GameObject, int> defeatedCounts = new Dictionary<GameObject, int>();
    public int TotalKills { get; private set; } = 0;

    void Awake()
    {
        // 辞書を初期化
        foreach (var setting in enemyStatsSettings)
        {
            // ★【修正箇所】: enemyPrefab が null でないかチェックし、クラッシュを防ぐ
            if (setting.enemyPrefab != null)
            {
                // 設定されたプレハブをキーとして0で初期化
                defeatedCounts[setting.enemyPrefab] = 0;
            }
            else
            {
                Debug.LogError("ScoreManager Error: Enemy Stats Settings リストに未設定の Prefab が含まれています。", this);
            }
        }
    }

    /// <summary>
    /// 敵が倒されたときに呼ばれる。討伐数を記録する。
    /// </summary>
    /// <param name="enemyPrefab">倒された敵の元のプレハブ</param>
    public void RecordDefeat(GameObject enemyPrefab)
    {
        // 辞書にキーが存在するかチェック（Inspectorで設定されている敵か）
        if (defeatedCounts.ContainsKey(enemyPrefab))
        {
            defeatedCounts[enemyPrefab]++;
            TotalKills++;
        }
        else
        {
            // Inspectorに設定されていない敵が倒された場合は、ログに記録するが統計には含めない
            Debug.LogWarning($"ScoreManager: 未登録のプレハブ ({enemyPrefab.name}) が倒されました。");
        }
    }

    /// <summary>
    /// 統計UIを更新し、必要な統計情報をGameManagerに提供する。
    /// </summary>
    public void UpdateStatsUI()
    {
        // 総合スコア計算のために使用する値
        TotalKills = 0;

        for (int i = 0; i < enemyStatsSettings.Count; i++)
        {
            EnemyStatsSetting setting = enemyStatsSettings[i];

            // ★【修正箇所】: UI更新時にも null チェックを行う (ゲームが落ちるのを防ぐ)
            if (setting.enemyPrefab == null) continue;

            int kills = defeatedCounts.ContainsKey(setting.enemyPrefab)
                         ? defeatedCounts[setting.enemyPrefab]
                         : 0;

            // UIを直接更新
            if (setting.killCountDisplay != null)
            {
                setting.killCountDisplay.text = kills.ToString();
            }

            TotalKills += kills;
        }
    }

    /// <summary>
    /// 特定のプレハブの討伐数を取得する
    /// </summary>
    public int GetKillsByPrefab(GameObject prefab)
    {
        // ★【修正箇所】: 検索前に null チェック
        if (prefab == null) return 0;

        return defeatedCounts.ContainsKey(prefab) ? defeatedCounts[prefab] : 0;
    }

    /// <summary>
    /// 統計情報設定のリストを公開（GameManagerがUI更新に使用できるように）
    /// </summary>
    public List<EnemyStatsSetting> GetStatsSettings()
    {
        return enemyStatsSettings;
    }
}
