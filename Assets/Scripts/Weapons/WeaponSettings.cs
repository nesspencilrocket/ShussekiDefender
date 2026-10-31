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
    //UI表示用の絵
    public Sprite TurretSprite;
}
