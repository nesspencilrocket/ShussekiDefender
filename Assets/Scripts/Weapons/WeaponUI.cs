
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;


public class WeaponUI : MonoBehaviour
{

    [SerializeField] private Image weaponImage;
    [SerializeField] private TextMeshProUGUI weaponCost;

    //表示している武器の情報を格納（コストや絵）
    private WeaponSettings weaponSettings;


    //武器UIを押した
    public static Action<WeaponSettings> OnPressedWeaponsUI;


    /// <summary>
    /// カードから絵とコストテキストを設定
    /// </summary>
    /// <param name="weaponSettings"></param>
    public void SetupUI(WeaponSettings weapon)
    {
        //スクリプタブルオブジェクトを変数に格納
        weaponSettings = weapon;

        //絵とコストテキストを設定
        weaponImage.sprite = weaponSettings.TurretSprite;
        weaponCost.text = weaponSettings.TurretShopCost.ToString();
    }


    //ボタンに登録
    public void PressedWeaponUI()
    {

        //所持コイン ＞= 設置コスト
        if (CurrencyManager.instance.totalCoins >=
            weaponSettings.TurretShopCost)
        {

            CurrencyManager.instance.RemoveCoins(weaponSettings.TurretShopCost);

            UIManager.instance.CloseTurretShopPanel();

            OnPressedWeaponsUI?.Invoke(weaponSettings);
        }

    }


}