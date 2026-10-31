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

        // 選ぶ操作は NodeClickInput が、クリックの位置から決める。
        // 以前の「Node ごとの World Space Canvas とボタン」は、上に重なった見えない UI に
        // クリックを奪われて押せない場所ができたので、使わない（二重に反応しないよう消す）
        Canvas legacyCanvas = GetComponentInChildren<Canvas>(true);
        if (legacyCanvas != null) legacyCanvas.gameObject.SetActive(false);
        NodeClickInput.Ensure().Register(this);

        ApplyLock();
    }

    /// <summary>
    /// 世界の座標 point が、この台座の範囲（外側へ margin ずつ広げる）に入っているか。
    /// 選ぶ操作（NodeClickInput）が使う。
    /// </summary>
    public bool ContainsPoint(Vector2 point, float margin)
    {
        if (baseSprite == null) return false;
        Bounds b = baseSprite.bounds;
        return point.x >= b.min.x - margin && point.x <= b.max.x + margin
            && point.y >= b.min.y - margin && point.y <= b.max.y + margin;
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
        // 開始前と結果表示中は設置・強化を受け付けない。
        // 暗幕でもクリックを塞いでいるが、Canvas の重なり順に依存しない
        // よう、ここでも確実に止めておく。
        if (!GameManager.IsGameActive)
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
        if (!GameManager.IsGameActive || CurrencyManager.instance == null) return;
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