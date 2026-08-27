using UnityEngine;

public class MusicByPanelState : MonoBehaviour
{
    [Header("★ メインのAudioSource (唯一の再生プレイヤー)")]
    [Tooltip("シーン内の唯一のAudioSourceを指定してください。")]
    [SerializeField] private AudioSource mainMusicSource;

    // --- デフォルトBGM設定 ---
    [Header("★ 通常時のBGM設定 (パネル非表示時)")]
    [Tooltip("どのパネルも出ていないときに再生するBGMのAudioClip。")]
    [SerializeField] private AudioClip defaultBGM;
    [Tooltip("Default BGMをループ再生するかどうか。")]
    [SerializeField] private bool isLooping_Default;

    // --- パネルグループ 1 設定 ---
    [Header("★ パネルグループ 1 の設定 (優先度: 低)")]
    [Tooltip("この配列内のパネルが一つでもアクティブになったら Music 1 が流れます。")]
    [SerializeField] private GameObject[] group1Panels;
    [Tooltip("Panel Group 1 がアクティブのときに再生するBGMのAudioClip。")]
    [SerializeField] private AudioClip musicClip1;
    [Tooltip("Music 1 をループ再生するかどうか。")]
    [SerializeField] private bool isLooping_Music1;

    // --- パネルグループ 2 設定 ---
    [Header("★ パネルグループ 2 の設定 (優先度: 高)")]
    [Tooltip("この配列内のパネルが一つでもアクティブになったら Music 2 が流れます。")]
    [SerializeField] private GameObject[] group2Panels;
    [Tooltip("Panel Group 2 がアクティブのときに再生するBGMのAudioClip。")]
    [SerializeField] private AudioClip musicClip2;
    [Tooltip("Music 2 をループ再生するかどうか。")]
    [SerializeField] private bool isLooping_Music2;

    private AudioClip currentTargetClip;
    private bool currentLoopSetting;

    void Update()
    {
        if (mainMusicSource == null) return;

        // 制御パネルの状態をチェック
        bool isGroup1Active = IsAnyPanelActive(group1Panels);
        bool isGroup2Active = IsAnyPanelActive(group2Panels);

        AudioClip targetClip = defaultBGM;
        bool targetLoop = isLooping_Default;

        // 優先度が高い Music 2 からチェック
        if (isGroup2Active && musicClip2 != null)
        {
            targetClip = musicClip2;
            targetLoop = isLooping_Music2;
        }
        // 次に Music 1 をチェック
        else if (isGroup1Active && musicClip1 != null)
        {
            targetClip = musicClip1;
            targetLoop = isLooping_Music1;
        }
        // それ以外の場合は defaultBGM (初期値) のまま

        // ターゲットとなるBGMを再生
        PlayMusic(targetClip, targetLoop);
    }

    /// <summary>
    /// 指定されたAudioClipとループ設定で再生します。
    /// </summary>
    private void PlayMusic(AudioClip targetClip, bool targetLoop)
    {
        // ターゲットクリップが設定されていない場合は、停止
        if (targetClip == null)
        {
            if (mainMusicSource.isPlaying)
            {
                mainMusicSource.Stop();
            }
            // ループ設定をリセット
            mainMusicSource.loop = false;
            currentTargetClip = null;
            return;
        }

        // 1. クリップが切り替わるか、ループ設定が変わる場合は、新しい設定で再生
        if (mainMusicSource.clip != targetClip || mainMusicSource.loop != targetLoop)
        {
            mainMusicSource.Stop();
            mainMusicSource.clip = targetClip;
            mainMusicSource.loop = targetLoop; // ★★★ ループ設定を適用 ★★★
            mainMusicSource.Play();
            currentTargetClip = targetClip;
            currentLoopSetting = targetLoop;
        }
        // 2. クリップが同じで、再生が停止している場合は再開 (ループではない単発音が終わった場合など)
        else if (!mainMusicSource.isPlaying)
        {
            // Note: ループ設定が false の場合、再生が終わるとここに入ります。
            // targetLoop が true なら、Play()で再開。targetLoop が false なら、そのままにして音が鳴り終わった状態を維持します。
            if (targetLoop)
            {
                mainMusicSource.Play();
            }
        }
    }

    /// <summary>
    /// 指定された配列内のGameObjectが一つでもアクティブかどうかを判定する
    /// </summary>
    private bool IsAnyPanelActive(GameObject[] panels)
    {
        if (panels == null || panels.Length == 0)
        {
            return false;
        }

        foreach (GameObject panel in panels)
        {
            if (panel != null && panel.activeInHierarchy)
            {
                return true;
            }
        }
        return false;
    }
}