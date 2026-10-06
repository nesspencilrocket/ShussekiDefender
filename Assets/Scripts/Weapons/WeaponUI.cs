
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
    private bool shownFull;

    private Button button;

    // 値段は同じ武器を置くほど上がるので、購入パネルを開くたびと、開いている間に合わせ直す
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
    /// ステージの設置上限に達していたら「上限」と出し、
    /// お金が足りないときと合わせてカードを押せなくする（色が暗くなる）。
    /// </summary>
    private void RefreshPrice()
    {
        if (weaponSettings == null || weaponCost == null) return;

        WeaponShopManager shop = WeaponShopManager.Instance;
        int price = (shop != null) ? shop.PriceOf(weaponSettings) : weaponSettings.TurretShopCost;
        bool full = shop != null && shop.IsAtCapacity;

        if (price != shownPrice || full != shownFull)
        {
            shownPrice = price;
            shownFull = full;
            weaponCost.text = full ? "上限" : Money.Yen(price);
        }

        if (button != null)
        {
            CurrencyManager wallet = CurrencyManager.instance;
            bool affordable = wallet != null && wallet.totalCoins >= price;
            button.interactable = !full && affordable;
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