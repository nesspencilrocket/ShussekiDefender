using UnityEngine;

[CreateAssetMenu(menuName = "Weapon")]
public class WeaponSettings : ScriptableObject
{
    //武器のプレファブ
    public GameObject TurretPrefab;
    //コスト（何基目でも同じ値段）
    public int TurretShopCost;

    [Tooltip("同時に置ける数の上限（全時限共通）。0 なら上限なし。売ると 1 つ空く")]
    [Min(0)] public int maxPlaced = 0;

    [Tooltip("ステージのレベルがこの値に届くたびに、置ける数が 1 つ増える（例：5 と 7 なら、Lv5 で +1、Lv7 でさらに +1）。"
           + "maxPlaced が 0（上限なし）のときは使わない")]
    public int[] extraPlacedAtStageLevel = new int[0];

    /// <summary>いまのステージのレベルで、同時に置ける数。0 なら上限なし</summary>
    public int MaxPlacedAt(int stageLevel)
    {
        if (maxPlaced <= 0) return 0;
        int max = maxPlaced;
        if (extraPlacedAtStageLevel != null)
        {
            foreach (int lv in extraPlacedAtStageLevel)
            {
                if (stageLevel >= lv) max++;
            }
        }
        return max;
    }
    //UI表示用の絵
    public Sprite TurretSprite;
}
