
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
        button = GetComponent<Button>();
        shownPrice = -1;
        RefreshPrice();
    }

    // 直前に出した値段と上限の状態。変わったときだけ文字を作り直す
    private int shownPrice = -1;
    private bool shownAtLimit;

    private Button button;

    // 買えるかどうかは持ち金で変わるので、購入パネルを開くたびと、開いている間に合わせ直す
    private void OnEnable()
    {
        RefreshPrice();
    }

    private void Update()
    {
        RefreshPrice();
    }

    /// <summary>
    /// 値段の表示と、押せるかどうかを合わせる。
    /// お金が足りないとき、またはその武器の置ける数の上限に達しているときは、
    /// カードを押せなくする（色が暗くなる）。上限のときは値段の代わりに「上限」と出す。
    /// </summary>
    private void RefreshPrice()
    {
        if (weaponSettings == null || weaponCost == null) return;

        int price = weaponSettings.TurretShopCost;
        WeaponShopManager shop = WeaponShopManager.Instance;
        bool atLimit = shop != null && shop.IsAtLimit(weaponSettings);

        if (price != shownPrice || atLimit != shownAtLimit)
        {
            shownPrice = price;
            shownAtLimit = atLimit;
            weaponCost.text = atLimit ? "上限" : Money.Yen(price);
        }

        if (button != null)
        {
            CurrencyManager wallet = CurrencyManager.instance;
            bool affordable = wallet != null && wallet.totalCoins >= price;
            button.interactable = GameManager.IsGameActive && !atLimit && affordable;
        }
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