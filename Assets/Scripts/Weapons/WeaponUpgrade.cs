using UnityEngine;
using System;

public class WeaponUpgrade : MonoBehaviour
{

    [SerializeField] private int upgradeCost;
    [SerializeField] private int addCost;//コスト増加
    [SerializeField] private float addDamage;//増加ダメージ
    [SerializeField] private float decreaseInterval;//インターバル減少

    private WeaponControl weaponControl;
    [NonSerialized] public int currentUpgradeCost;
    [NonSerialized] public int level;


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
        //コインがコストよりあるのか判定
        if (CurrencyManager.instance.totalCoins >= currentUpgradeCost)
        {
            //能力強化
            weaponControl.bulletDamage += addDamage;
            weaponControl.delay -= decreaseInterval;

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
