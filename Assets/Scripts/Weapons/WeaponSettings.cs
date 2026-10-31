using UnityEngine;

[CreateAssetMenu(menuName = "Weapon")]
public class WeaponSettings : ScriptableObject
{
    //武器のプレファブ
    public GameObject TurretPrefab;
    //コスト（何基目でも同じ値段）
    public int TurretShopCost;
    //UI表示用の絵
    public Sprite TurretSprite;
}
