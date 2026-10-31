using UnityEngine;

[CreateAssetMenu(menuName = "Weapon")]
public class WeaponSettings : ScriptableObject
{
    //武器のプレファブ
    public GameObject TurretPrefab;
    //コスト（1 基目の値段）
    public int TurretShopCost;

    [Tooltip("同じ武器を 1 基置くごとに上がる値段の割合。基本の値段に掛ける。"
           + "0.5 なら 30 円の武器は 30 → 45 → 60 … と上がる。0 なら上がらない（一律）")]
    [Range(0f, 2f)]
    public float costIncreaseRate = 0.5f;

    /// <summary>
    /// すでに placedCount 基置いてあるときの、次の 1 基の値段。
    /// 一番安い武器を並べるだけで勝てないよう、同じ武器ほど高くなる。
    /// 売ると置いてある数が減るので、値段も下がる。
    /// </summary>
    public int PriceFor(int placedCount)
    {
        int step = Mathf.RoundToInt(TurretShopCost * costIncreaseRate);
        return TurretShopCost + step * Mathf.Max(0, placedCount);
    }

    [Tooltip("同時に置ける数の上限（全時限共通）。0 なら上限なし。売ると 1 つ空く")]
    [Min(0)] public int maxPlaced = 0;
    //UI表示用の絵
    public Sprite TurretSprite;
}
