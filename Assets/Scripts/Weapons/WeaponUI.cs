
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
        Money.FitOneLine(weaponCost);
        shownPrice = -1;
        RefreshPrice();
    }

    // 直前に出した値段。変わったときだけ文字を作り直す
    private int shownPrice = -1;

    // 値段は同じ武器を置くほど上がるので、購入パネルを開くたびと、開いている間に合わせ直す
    private void OnEnable()
    {
        RefreshPrice();
    }

    private void Update()
    {
        RefreshPrice();
    }

    private void RefreshPrice()
    {
        if (weaponSettings == null || weaponCost == null) return;

        WeaponShopManager shop = WeaponShopManager.Instance;
        int price = (shop != null) ? shop.PriceOf(weaponSettings) : weaponSettings.TurretShopCost;
        if (price == shownPrice) return;

        shownPrice = price;
        weaponCost.text = Money.Yen(price);
    }


    //ボタンに登録
    public void PressedWeaponUI()
    {
        // 支払いと設置の判断は WeaponShopManager にまとめた。
        // ここで先に代金を引くと、設置先のノードが無い・埋まっているときにも
        // お金だけ減ってしまう。
        OnPressedWeaponsUI?.Invoke(weaponSettings);
    }


}