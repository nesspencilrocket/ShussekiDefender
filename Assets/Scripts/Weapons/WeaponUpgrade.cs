using UnityEngine;
using System;

public class WeaponUpgrade : MonoBehaviour
{
    [Header("強化の効果")]
    [SerializeField] private int upgradeCost;
    [SerializeField] private int addCost;//コスト増加
    [SerializeField] private float addDamage;//増加ダメージ
    [SerializeField] private float decreaseInterval;//インターバル減少

    [Header("上限")]
    [Tooltip("この回数まで強化できる。到達すると UI のボタンは効かなくなる")]
    [SerializeField] private int maxLevel = 5;

    [Tooltip("発射間隔の下限（秒）。これを下回らせない")]
    [SerializeField] private float minDelay = 0.08f;

    private WeaponControl weaponControl;
    [NonSerialized] public int currentUpgradeCost;
    [NonSerialized] public int level;

    /// <summary>これ以上強化できないか</summary>
    public bool IsMaxLevel => level >= maxLevel;

    /// <summary>UI 表示用（例：Level 3 / 5）</summary>
    public int MaxLevel => maxLevel;

    void Start()
    {
        //変数に格納
        weaponControl = GetComponent<WeaponControl>();
        //設定用の数値設定
        currentUpgradeCost = upgradeCost;
        //レベルの設定
        level = 1;
    }

    public void UpgradeWeapon()
    {
        // 【重要】上限に達していたら何もしない。
        // 上限が無いと delay が 0 以下まで下がり、WeaponControl が
        // 毎フレーム発射する状態になってゲームが成立しなくなる。
        if (IsMaxLevel) return;

        //コインがコストよりあるのか判定
        if (CurrencyManager.instance.totalCoins >= currentUpgradeCost)
        {
            //能力強化
            weaponControl.bulletDamage += addDamage;

            // 発射間隔は下限で止める。上限レベルと二重に守っているのは、
            // decreaseInterval を大きく設定してもゼロ割れしないようにするため。
            weaponControl.delay = Mathf.Max(minDelay, weaponControl.delay - decreaseInterval);

            UpdateUpgrade();
        }
    }

    private void UpdateUpgrade()
    {
        //コインを減らす
        CurrencyManager.instance.RemoveCoins(currentUpgradeCost);
        //次の強化にかかるコインを増やす
        currentUpgradeCost += addCost;
        //レベルの数値を上げる
        level++;
    }

    public int GetSellValue()
    {
        //int型に丸めて返す
        return Mathf.RoundToInt(currentUpgradeCost * 0.5f);
    }
}
