using UnityEngine;

/// <summary>小さな範囲攻撃の広さが分かるよう、短い輪だけを表示する。</summary>
public sealed class ProjectileSplashEffect : MonoBehaviour
{
    private const float Duration = 0.18f;
    private LineRenderer ring;
    private float elapsed;

    public static void Show(Vector2 position, float radius, SpriteRenderer source)
    {
        if (source == null || radius <= 0f) return;
        var host = new GameObject("SmallSplash");
        host.transform.position = new Vector3(position.x, position.y, source.transform.position.z);
        var effect = host.AddComponent<ProjectileSplashEffect>();
        effect.ring = host.AddComponent<LineRenderer>();
        effect.ring.sharedMaterial = source.sharedMaterial;
        effect.ring.sortingLayerID = source.sortingLayerID;
        effect.ring.sortingOrder = source.sortingOrder + 1;
        effect.ring.useWorldSpace = false;
        effect.ring.loop = true;
        effect.ring.widthMultiplier = 0.04f;
        effect.ring.positionCount = 32;
        for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.PI * 2f / 32;
            effect.ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
        effect.Draw(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= Duration) Destroy(gameObject);
        else Draw(elapsed / Duration);
    }

    private void Draw(float progress)
    {
        transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, progress);
        Color tint = new Color(1f, 0.8f, 0.15f, 0.65f * (1f - progress));
        ring.startColor = tint;
        ring.endColor = tint;
    }
}
