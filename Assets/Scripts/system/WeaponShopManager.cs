using System;
using System.Collections;
using System.Collections.Generic; // List<T>を使用していないため、これは不要ですが、残します
using UnityEngine;

public class WeaponShopManager : MonoBehaviour
{

    //UIプレファブ
    [SerializeField] private GameObject turretCardPrefab;

    //生成したUIを格納する親オブジェクト
    [SerializeField] private Transform turretPanelContainer;

    //スクリプタブルオブジェクト格納
    [SerializeField] private WeaponSettings[] weapons; // 配列として宣言


    //現在選択中のノードを格納する
    private Node currentNodeSelected;



    void Start()
    {
        Debug.Log("DEBUG_CRASH_CHECK: WeaponShopManager.Start() 実行開始");

        // ★★★ ログ 1: 武器配列チェック前 ★★★
        Debug.Log("DEBUG_WHS: 1. 武器配列の長さチェック前。");
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogError("CRASH_A: 武器設定(weapons)が空です。");
            return;
        }

        // ★★★ ログ 2: UIプレハブチェック前 ★★★
        Debug.Log("DEBUG_WHS: 2. UIプレハブの Null チェック前。");
        if (turretCardPrefab == null)
        {
            Debug.LogError("CRASH_B: turretCardPrefab が未設定です。");
            return;
        }

        // ★★★ ログ 3: UIコンテナチェック前 ★★★
        Debug.Log("DEBUG_WHS: 3. コンテナの Null チェック前。");
        if (turretPanelContainer == null)
        {
            Debug.LogError("CRASH_C: turretPanelContainer が未設定です。");
            return;
        }

        // ★★★ ログ 4: ループ開始前 ★★★
        Debug.Log("DEBUG_WHS: 4. ループを開始します。");
        for (int i = 0; i < weapons.Length; i++)
        {
            // 【重要修正 2】: 配列の要素が null でないかチェック
            if (weapons[i] != null)
            {
                //UI生成
                CreateWeaponUI(weapons[i]);
            }
            else
            {
                Debug.LogWarning($"WeaponShopManager Warning: weapons配列の要素 {i} が空(None)です。スキップしました。");
            }
        }
    }

    /// <summary>
    /// 武器を生成するボタンUIを作成する
    /// </summary>
    /// <param name="weaponSettings"></param>
    private void CreateWeaponUI(WeaponSettings weaponSettings)
    {
        // Nullチェックを追加 (turretCardPrefabが未設定の場合のクラッシュ防止)
        if (turretCardPrefab == null || turretPanelContainer == null)
        {
            Debug.LogError("WeaponShopManager Error: turretCardPrefab または turretPanelContainer が未設定です。", this);
            return;
        }

        //インスタンス生成して格納
        GameObject newUI = Instantiate(turretCardPrefab,
            turretPanelContainer.position, Quaternion.identity);
        //親や大きさを設定
        newUI.transform.SetParent(turretPanelContainer);
        newUI.transform.localScale = Vector3.one;

        //コストや絵をUIに反映する
        WeaponUI weaponButton = newUI.GetComponent<WeaponUI>();

        // Nullチェックを追加 (WeaponUIコンポーネントの欠落によるクラッシュ防止)
        if (weaponButton != null)
        {
            weaponButton.SetupUI(weaponSettings);
        }
        else
        {
            Debug.LogError("WeaponShopManager Error: turretCardPrefabにWeaponUIコンポーネントがありません。", turretCardPrefab);
        }
    }


    // ... (NodeSelected, PressWeaponUI, OnEnable, OnDisable, WeaponSold メソッドは既存のまま)
    // ... (PressWeaponUI, OnEnable, OnDisable, WeaponSold は長文のため省略。既存のコードを使用してください。)
    // ...

    // --------------------------------------------------------------------------------------
    // 注意: 以下は提供されていないメソッドですが、上記のコードが動作するために存在すると仮定します。
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// 選択中のノードを変数に格納
    /// </summary>
    private void NodeSelected(Node nodeSelected)
    {
        currentNodeSelected = nodeSelected;
    }

    private void PressWeaponUI(WeaponSettings weapon)
    {
        //特定のノードが押されているなら
        if (currentNodeSelected != null)
        {
            //プレファブから武器オブジェクトを生成
            GameObject weaponInstance =
                Instantiate(weapon.TurretPrefab);

            //ノードの場所に設置
            weaponInstance.transform.localPosition =
                currentNodeSelected.transform.position;

            weaponInstance.transform.parent =
                currentNodeSelected.transform;

            //Nodeの変数に設置した武器格納
            Weapon turretPlaced =
                weaponInstance.GetComponent<Weapon>();

            currentNodeSelected.SetTurret(turretPlaced);
        }
    }

    private void OnEnable()
    {
        Node.OnNodeSelected += NodeSelected;
        WeaponUI.OnPressedWeaponsUI += PressWeaponUI;

        Node.OnWeaponSold += WeaponSold;
    }

    private void OnDisable()
    {
        Node.OnNodeSelected -= NodeSelected;
        WeaponUI.OnPressedWeaponsUI -= PressWeaponUI;

        Node.OnWeaponSold -= WeaponSold;
    }


    /// <summary>
    /// 売却時はcurrentNodeSelectedを空にして別の武器を設置できるようにする
    /// </summary>
    private void WeaponSold()
    {
        currentNodeSelected = null;
    }
}