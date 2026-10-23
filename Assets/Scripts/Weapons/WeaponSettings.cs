using UnityEngine;

[CreateAssetMenu(menuName = "Weapon")]
public class WeaponSettings : ScriptableObject
{
    //武器のプレファブ
    public GameObject TurretPrefab;
    //コスト（1 基目の値段）
    public int TurretShopCost;

    [Tooltip("同じ武器を 1 基置くごとに上がる値段の割合。基本の値段に掛ける。"
           + "0.5 なら 20 円の武器は 20 → 30 → 40 … と上がる")]
    [Range(0f, 2f)]
    public float costIncreaseRate = 0.5f;

    /// <summary>
    /// すでに placedCount 基置いてあるときの、次の 1 基の値段。
    /// 一番安い武器を並べるだけで勝てないよう、同じ武器ほど高くなる。
    /// </summary>
    public int PriceFor(int placedCount)
    {
        int step = Mathf.RoundToInt(TurretShopCost * costIncreaseRate);
        return TurretShopCost + step * Mathf.Max(0, placedCount);
    }
    //UI表示用の絵
    public Sprite TurretSprite;
}
