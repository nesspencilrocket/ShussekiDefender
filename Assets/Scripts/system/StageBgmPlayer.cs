using UnityEngine;

/// <summary>
/// StageData.bgm を再生する。器だけ用意して未使用だったフィールドを
/// 実際に効かせるためのコンポーネント。
///
/// 【MusicByPanelState との住み分け】
/// あちらはリザルトパネルの表示状態を毎フレーム見て曲を切り替える仕組みで、
/// 勝敗ジングルを担当している。こちらはステージ開始時に一度だけ
/// 通常時の BGM を差し込む。あちらの defaultBGM を上書きする形になるので、
/// 両方を使う場合は MusicByPanelState の Default BGM を空にしておく。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class StageBgmPlayer : MonoBehaviour
{
    [Tooltip("StageData に bgm が設定されていないときに流す曲（任意）")]
    [SerializeField] private AudioClip fallbackBgm;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f;

    [SerializeField] private bool loop = true;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    void Start()
    {
        // StageData は GameManager.Awake で解決済み
        StageData stage = (GameManager.Instance != null) ? GameManager.Instance.Stage : null;
        AudioClip clip = (stage != null && stage.bgm != null) ? stage.bgm : fallbackBgm;

        if (clip == null || source == null) return;

        source.clip = clip;
        source.loop = loop;
        source.volume = volume;
        source.playOnAwake = false;
        source.Play();
    }
}
