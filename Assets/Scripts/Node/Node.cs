using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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



    public static Action OnWeaponSold;


    void Start()
    {
        //画像の大きさを格納
        rangeSize = fireRange.GetComponent<SpriteRenderer>().bounds.size.y;
        //スケールを格納
        originalScale = fireRange.transform.localScale;
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