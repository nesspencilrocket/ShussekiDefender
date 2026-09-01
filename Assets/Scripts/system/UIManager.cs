using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    //武器の設置UI
    [SerializeField] private GameObject weaponShopPanel;
    //選択中のノードを格納
    private Node currentNodeSelected;


    [SerializeField] private TextMeshProUGUI totalCoinsText;
    // ▼▼▼ 変更点 ▼▼▼ 変数名を分かりやすく変更
    [SerializeField] private TextMeshProUGUI enemiesReachedGoalText;


    [SerializeField] private GameObject nodeUIPanel;
    [SerializeField] private TextMeshProUGUI sellText;

    //レベル、コスト
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private TextMeshProUGUI upgradeText;

    [Tooltip("強化ボタン。上限に達したら押せなくする（未設定でも動く）")]
    [SerializeField] private UnityEngine.UI.Button upgradeButton;


    public static UIManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

    }

    // 直前に表示した値。変わったときだけ文字列を作り直す
    private int lastCoins = int.MinValue;
    private int lastPasses = int.MinValue;

    void Update()
    {
        //体力などのUIを更新
        UpdateUI();
    }

    /// <summary>
    /// //体力,コインのUIを更新
    /// </summary>
    private void UpdateUI()
    {
        // Nullチェックを追加し、クラッシュを防ぐ
        // 毎フレーム文字列を作り直すと、変化していなくても新しい string が
        // 生成されてゴミが増える。値が変わったときだけ組み立てる。
        if (totalCoinsText != null && CurrencyManager.instance != null)
        {
            int coins = CurrencyManager.instance.totalCoins;
            if (coins != lastCoins)
            {
                lastCoins = coins;
                totalCoinsText.text = coins.ToString();
            }
        }

        // 通過数のカウンタは GameManager に一本化した。
        // 以前は LevelManager 側にも別のカウンタがあり、二重管理になっていた
        // （しかも LevelManager 側は Debug.Log を出すだけで敗北処理をしていなかった）。
        if (enemiesReachedGoalText != null && GameManager.Instance != null)
        {
            // 例：「5 / 50」のように表示する
            int passes = GameManager.Instance.EnemiesPassed;
            if (passes != lastPasses)
            {
                lastPasses = passes;
                enemiesReachedGoalText.text =
                    $"{passes} / {GameManager.Instance.MaxEnemyPasses}";
            }
        }
    }


    /// <summary>
    /// どのWeapon設置するか決めるUIを非表示にする
    /// </summary>
    public void CloseTurretShopPanel()
    {
        weaponShopPanel.SetActive(false);
    }

    // 【修正】メソッド名を変更したり、引数を変えたりしていないか確認し、
    // イベント登録で参照できるようにします。元の名前を維持。
    private void NodeSelected(Node nodeSelected) // メソッド自体は問題ありません
    {
        //currentNodeSelected選択中のノードを格納
        currentNodeSelected = nodeSelected;

        //ノードが空か判定
        if (currentNodeSelected.IsEmpty())
        {
            //UI表示
            weaponShopPanel.SetActive(true);
        }
        else
        {
            ShowNodeUI();
        }

    }


    private void OnEnable()
    {
        // 【CS0103の修正】: メソッド名 'NodeSelected' は存在するため、
        // Node.OnNodeSelected の Actionの引数とメソッドの引数が一致しているかを確認。
        // コード上は問題ないため、エラーはコンパイラの解釈ミスまたは隠しバグの可能性が高い。
        // そのまま登録を維持します。
        Node.OnNodeSelected += NodeSelected;
    }


    private void OnDisable()
    {
        Node.OnNodeSelected -= NodeSelected;
    }


    //後々ボタンに登録する
    public void CloseNodeUIPanel()
    {
        //攻撃範囲を非表示にする
        // Nullチェックを追加
        if (currentNodeSelected != null)
        {
            currentNodeSelected.CloseAttackRange();
        }

        //武器の強化や販売のUIを非表示
        nodeUIPanel.SetActive(false);
    }

    /// <summary>
    /// 強化、売却 UI
    /// </summary>
    private void ShowNodeUI()
    {
        nodeUIPanel.SetActive(true);
        //UIを更新する
        UpdateUpgradeText();
        UpdateWeaponLevel();
        UpdateSellValue();
    }

    /// <summary>
    /// 武器強化ボタン：
    /// </summary>
    public void UpgradeWeapon()
    {
        // Nullチェックを追加
        if (currentNodeSelected != null && currentNodeSelected.weapon != null)
        {
            currentNodeSelected.weapon.weaponUpgrade.UpgradeWeapon();
            //UI更新
            UpdateUpgradeText();
            UpdateWeaponLevel();
            UpdateSellValue();
        }
    }


    private void UpdateSellValue()
    {
        // Nullチェックを追加
        if (currentNodeSelected != null && currentNodeSelected.weapon != null && sellText != null)
        {
            //売却時の値段を更新する
            int sellAmount = currentNodeSelected.weapon.weaponUpgrade.GetSellValue();
            sellText.text = sellAmount.ToString();
        }
    }

    private void UpdateWeaponLevel()
    {
        // Nullチェックを追加
        if (currentNodeSelected != null && currentNodeSelected.weapon != null && weaponLevelText != null)
        {
            WeaponUpgrade up = currentNodeSelected.weapon.weaponUpgrade;
            // 上限が分かると、あと何回強化できるかが読める
            weaponLevelText.text = $"Level {up.level} / {up.MaxLevel}";
        }
    }

    private void UpdateUpgradeText()
    {
        // Nullチェックを追加
        if (currentNodeSelected != null && currentNodeSelected.weapon != null && upgradeText != null)
        {
            WeaponUpgrade up = currentNodeSelected.weapon.weaponUpgrade;
            // 上限では数字ではなく MAX と出す。
            // 数字のままだと、押しても何も起きない理由が分からない。
            upgradeText.text = up.IsMaxLevel ? "MAX" : up.currentUpgradeCost.ToString();
        }

        UpdateUpgradeButtonState();
    }

    /// <summary>
    /// 上限に達した武器では強化ボタンを押せなくする。
    /// ボタンが未設定でも表示だけは正しくなるようにしてある。
    /// </summary>
    private void UpdateUpgradeButtonState()
    {
        if (upgradeButton == null) return;

        upgradeButton.interactable =
            currentNodeSelected != null &&
            currentNodeSelected.weapon != null &&
            !currentNodeSelected.weapon.weaponUpgrade.IsMaxLevel;
    }

    //ボタンに登録する
    public void SellWeapon()
    {
        if (currentNodeSelected != null && nodeUIPanel != null)
        {
            currentNodeSelected.SellWeapon();
            currentNodeSelected = null;
            nodeUIPanel.SetActive(false);
        }
    }
}