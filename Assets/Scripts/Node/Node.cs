using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Node : MonoBehaviour
{
    //ノード選択された時のイベント
    public static Action<Node> OnNodeSelected;
    //ノードに設置されている武器を格納する変数
    [NonSerialized] public Weapon weapon;



    [SerializeField] private GameObject fireRange;
    private float rangeSize;
    private Vector3 originalScale;

    [Header("解放")]
    [Tooltip("使えるようになる順番（1 から）。小さいほど早く開く。"
           + "何番目まで開くかはステージの StageData（initialNodes / nodesPerLevel / maxNodes）で決まる。"
           + "0 なら順番に関係なく常に使える")]
    [Min(0)]
    [SerializeField] private int unlockOrder = 0;

    [Tooltip("まだ使えないときの色。台座の色に掛ける")]
    [SerializeField] private Color lockedTint = new Color(0.3f, 0.3f, 0.3f, 0.55f);

    private SpriteRenderer baseSprite;
    private Color baseColor = Color.white;
    private Button button;


    public static Action OnWeaponSold;

    /// <summary>使えるようになる順番。0 は常に使える</summary>
    public int UnlockOrder => unlockOrder;

    /// <summary>
    /// 今使えるか。順番が今のレベルで開いている数以内なら使える。
    /// StageLevel が無いときは、順番が付いているスポットは使えない扱いにする
    /// </summary>
    public bool IsUnlocked
    {
        get
        {
            if (unlockOrder <= 0) return true;
            StageLevel level = StageLevel.Instance;
            return level != null && unlockOrder <= level.UnlockedCount;
        }
    }

    /// <summary>
    /// このステージのうちに使えるようになるか。maxNodes を超える順番のスポットは最後まで開かない
    /// </summary>
    public bool IsAvailableInStage
    {
        get
        {
            if (unlockOrder <= 0) return true;
            StageLevel level = StageLevel.Instance;
            return level == null || level.MaxNodes <= 0 || unlockOrder <= level.MaxNodes;
        }
    }


    void Start()
    {
        //画像の大きさを格納
        rangeSize = fireRange.GetComponent<SpriteRenderer>().bounds.size.y;
        //スケールを格納
        originalScale = fireRange.transform.localScale;

        // レベルの仕組みは、シーンに置いていなくてもここで用意される
        StageLevel.Ensure();

        baseSprite = GetComponent<SpriteRenderer>();
        if (baseSprite != null) baseColor = baseSprite.color;
        button = GetComponentInChildren<Button>(true);
        ApplyLock();
    }

    private void OnEnable()
    {
        StageLevel.OnLevelChanged += OnLevelChanged;
    }

    private void OnDisable()
    {
        StageLevel.OnLevelChanged -= OnLevelChanged;
    }

    private void OnLevelChanged(int level)
    {
        ApplyLock();
    }

    /// <summary>
    /// まだ使えないスポットは暗くし、押せなくする
    /// </summary>
    private void ApplyLock()
    {
        bool unlocked = IsUnlocked;
        if (baseSprite != null) baseSprite.color = unlocked ? baseColor : baseColor * lockedTint;
        if (button != null) button.interactable = unlocked;
    }


    /// <summary>
    /// このノードに武器をセット（変数に格納）
    /// </summary>
    /// <param name="weapon"></param>
    public void SetTurret(Weapon weapon)
    {
        this.weapon = weapon;
    }


    /// <summary>
    /// このノードは空か確認
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty()
    {
        return weapon == null;
    }


    //ボタンに設定
    public void SelectNode()
    {
        // 開始前カウントダウン中は設置・強化を受け付けない。
        // 暗幕でもクリックを塞いでいるが、Canvas の重なり順に依存しない
        // よう、ここでも確実に止めておく。
        if (GameManager.Instance != null && GameManager.Instance.IsCountingDown)
        {
            return;
        }

        // まだ解放されていないスポットは選べない
        if (!IsUnlocked)
        {
            return;
        }

        OnNodeSelected?.Invoke(this);


        if (!IsEmpty())
        {
            //攻撃範囲を表示
            ShowWeaponRange();
        }

    }

    /// <summary>
    /// 攻撃範囲を描写する
    /// </summary>
    private void ShowWeaponRange()
    {
        //400*400の画像の時だけ上手く行く
        //表示
        fireRange.SetActive(true);
        //サイズ調整
        fireRange.transform.localScale = originalScale * weapon.attackRange /
            (rangeSize / 2);
    }

    public void CloseAttackRange()
    {
        fireRange.SetActive(false);
    }



    /// <summary>
    /// 武器売却時の処理を呼ぶ
    /// </summary>
    public void SellWeapon()
    {
        if (!IsEmpty())
        {
            CurrencyManager.instance.AddCoins(weapon.weaponUpgrade.GetSellValue());
            Destroy(weapon.gameObject);
            weapon = null;
            CloseAttackRange();
            OnWeaponSold?.Invoke();
        }
    }
}