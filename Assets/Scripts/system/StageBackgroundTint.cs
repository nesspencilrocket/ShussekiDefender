using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// StageData.backgroundTint を背景に流し込む。
///
/// 1限目＝朝、6限目＝夕方 というように時限で色調を変えることで、
/// 背景を 1 枚しか用意しなくても 6 ステージが別物に見える。
/// 新規に描く絵は 0 枚で済む。
///
/// 対象は明示的に指定する。財布や時計のアイコンまで色が変わると
/// 見づらくなるため、地面と建物だけを渡すこと。
/// </summary>
public class StageBackgroundTint : MonoBehaviour
{
    [Tooltip("色を掛ける背景スプライト（建物など）")]
    [SerializeField] private SpriteRenderer[] sprites;

    [Tooltip("色を掛けるタイルマップ（地面）")]
    [SerializeField] private Tilemap[] tilemaps;

    void Start()
    {
        // StageData は GameManager.Awake で解決済み
        StageData stage = (GameManager.Instance != null) ? GameManager.Instance.Stage : null;
        if (stage == null) return;

        Apply(stage.backgroundTint);
    }

    private void Apply(Color tint)
    {
        if (sprites != null)
        {
            foreach (SpriteRenderer sr in sprites)
            {
                if (sr != null) sr.color = tint;
            }
        }

        if (tilemaps != null)
        {
            foreach (Tilemap tm in tilemaps)
            {
                if (tm != null) tm.color = tint;
            }
        }
    }
}
