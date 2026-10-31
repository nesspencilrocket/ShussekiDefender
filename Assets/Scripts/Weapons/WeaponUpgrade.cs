using UnityEngine;
using System;

public class WeaponUpgrade : MonoBehaviour
{
    [Header("強化の効果")]
    [SerializeField] private int upgradeCost;
    [SerializeField] private int addCost;//コスト増加
    [SerializeField] private float addDamage;//増加ダメージ
    [SerializeField] private float decreaseInterval;//インターバル減少

    [Tooltip("Lv1からLv2への強化だけ、ダメージ増加と発射間隔短縮に掛ける倍率")]
    [Min(1f)]
    [SerializeField] private float firstUpgradeMultiplier = 1.2f;

    [Header("最後の強化（最大レベルの 1 つ前 → 最大）")]
    [Tooltip("最後の強化の値段に、さらに足す額。0 なら、ほかの強化と同じ増え方")]
    [Min(0)]
    [SerializeField] private int finalUpgradeExtraCost = 0;

    [Tooltip("最後の強化で、発射間隔をさらに短くする秒数。0 なら、ほかの強化と同じ。"
           + "マイナスなら、最後の強化だけ短縮を小さくする（最大レベルの連射を少し遅くする）")]
    [SerializeField] private float finalUpgradeExtraIntervalCut = 0f;

    [Header("上限")]
    [Tooltip("この回数まで強化できる。到達すると UI のボタンは効かなくなる")]
    [SerializeField] private int maxLevel = 5;

    [Tooltip("発射間隔の下限（秒）。これを下回らせない")]
    [SerializeField] private float minDelay = 0.08f;

    [Header("売却")]
    [Tooltip("売ったときに戻ってくる割合。購入代金と強化に払った額の合計に掛ける")]
    [Range(0f, 1f)]
    [SerializeField] private float sellRate = 0.5f;

    private WeaponControl weaponControl;
    [NonSerialized] public int currentUpgradeCost;
    [NonSerialized] public int level;

    // この武器に払った合計（購入代金＋強化代）。売却額の元になる
    private int totalSpent;

    /// <summary>これ以上強化できないか</summary>
    public bool IsMaxLevel => level >= maxLevel;

    /// <summary>UI 表示用（例：Level 3 / 5）</summary>
    public int MaxLevel => maxLevel;

    void Awake()
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
        // 結果表示後は、残っていた選択や連打から所持金・性能を変えさせない。
        if (!GameManager.IsGameActive || weaponControl == null || CurrencyManager.instance == null) return;
        // 【重要】上限に達していたら何もしない。
        // 上限が無いと delay が 0 以下まで下がり、WeaponControl が
        // 毎フレーム発射する状態になってゲームが成立しなくなる。
        if (IsMaxLevel) return;

        //コインがコストよりあるのか判定
        if (CurrencyManager.instance.totalCoins >= currentUpgradeCost)
        {
            // 最初の強化の手応えを増やし、Lv2以降は従来の増分で伸ばす。
            float effectMultiplier = level == 1 ? firstUpgradeMultiplier : 1f;

            //能力強化
            weaponControl.bulletDamage += addDamage * effectMultiplier;

            // 発射間隔は下限で止める。上限レベルと二重に守っているのは、
            // decreaseInterval を大きく設定してもゼロ割れしないようにするため。
            // 最後の強化だけ、発射間隔をさらに短くする（最大レベルの特別な伸び）
            float extraCut = (level + 1 == maxLevel) ? finalUpgradeExtraIntervalCut : 0f;
            weaponControl.delay = Mathf.Max(minDelay,
                weaponControl.delay - decreaseInterval * effectMultiplier - extraCut);

            UpdateUpgrade();
        }
    }

    private void UpdateUpgrade()
    {
        //コインを減らす
        CurrencyManager.instance.RemoveCoins(currentUpgradeCost);
        totalSpent += currentUpgradeCost;
        //次の強化にかかるコインを増やす
        currentUpgradeCost += addCost;
        //レベルの数値を上げる
        level++;
        // 次が最後の強化なら、値段をさらに上乗せする
        if (level == maxLevel - 1) currentUpgradeCost += finalUpgradeExtraCost;
    }

    /// <summary>
    /// 購入代金を記録する。設置した直後に WeaponShopManager が呼ぶ。
    /// </summary>
    public void RecordPurchase(int price)
    {
        totalSpent += price;
    }

    /// <summary>
    /// 売却額。払った合計に sellRate（既定 0.5）を掛ける。
    ///
    /// 以前は「次の強化費用の半分」を返していた。通常の武器は 20 で買えて
    /// 次の強化費用が 50 なので、置いてすぐ売ると 25 戻り、
    /// 買って売るだけで 5 ずつ増やせてしまっていた。
    /// </summary>
    public int GetSellValue()
    {
        //int型に丸めて返す
        return Mathf.RoundToInt(totalSpent * sellRate);
    }
}
