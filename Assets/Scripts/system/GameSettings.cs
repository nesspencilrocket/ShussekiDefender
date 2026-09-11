using UnityEngine;

/// <summary>
/// 音量と明るさを PlayerPrefs に保存する。
///
/// GameSpeed と同じ形にしてある。設定はシーンをまたいで保持したいが、
/// シーンに置いた MonoBehaviour だと遷移のたびに消えるため static にする。
///
/// 【音量の効かせ方】
/// 全体音量は AudioListener.volume なので、何も配線しなくても必ず効く。
/// BGM 音量は係数として公開するだけで、実際に掛けるのは BGM を鳴らす側
/// （MusicByPanelState / StageBgmPlayer）。効果音は現在シーンに AudioSource が
/// 無いため項目を出していない。追加するときはここに SeVolume を足す。
/// </summary>
public static class GameSettings
{
    private const string KEY_MASTER = "SET_MASTER_VOLUME";
    private const string KEY_BGM = "SET_BGM_VOLUME";
    private const string KEY_BRIGHTNESS = "SET_BRIGHTNESS";

    public const float DEFAULT_MASTER = 0.8f;
    public const float DEFAULT_BGM = 0.6f;
    public const float DEFAULT_BRIGHTNESS = 1f;

    /// <summary>明るさの下限。これ以上暗くすると敵が見えなくなる</summary>
    public const float MIN_BRIGHTNESS = 0.5f;
    public const float MAX_BRIGHTNESS = 1.5f;

    private static bool loaded;
    private static float master;
    private static float bgm;
    private static float brightness;

    /// <summary>明るさが変わったときに BrightnessOverlay が拾う</summary>
    public static System.Action OnBrightnessChanged;

    private static void Load()
    {
        if (loaded) return;
        master = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MASTER, DEFAULT_MASTER));
        bgm = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_BGM, DEFAULT_BGM));
        brightness = Mathf.Clamp(PlayerPrefs.GetFloat(KEY_BRIGHTNESS, DEFAULT_BRIGHTNESS),
                                 MIN_BRIGHTNESS, MAX_BRIGHTNESS);
        loaded = true;
    }

    /// <summary>全体の音量。AudioListener に直接掛かる</summary>
    public static float MasterVolume
    {
        get { Load(); return master; }
        set
        {
            Load();
            master = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_MASTER, master);
            PlayerPrefs.Save();
            Apply();
        }
    }

    /// <summary>BGM の音量。鳴らす側がこの値を掛ける</summary>
    public static float BgmVolume
    {
        get { Load(); return bgm; }
        set
        {
            Load();
            bgm = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_BGM, bgm);
            PlayerPrefs.Save();
        }
    }

    /// <summary>1 で標準。1 未満で暗く、1 超で明るくなる</summary>
    public static float Brightness
    {
        get { Load(); return brightness; }
        set
        {
            Load();
            brightness = Mathf.Clamp(value, MIN_BRIGHTNESS, MAX_BRIGHTNESS);
            PlayerPrefs.SetFloat(KEY_BRIGHTNESS, brightness);
            PlayerPrefs.Save();
            OnBrightnessChanged?.Invoke();
        }
    }

    /// <summary>
    /// 保存済みの設定を実際に反映する。
    /// シーンが変わっても AudioListener は作り直されるため、起動時に一度呼ぶ。
    /// </summary>
    public static void Apply()
    {
        Load();
        AudioListener.volume = master;
    }

    public static void ResetToDefault()
    {
        MasterVolume = DEFAULT_MASTER;
        BgmVolume = DEFAULT_BGM;
        Brightness = DEFAULT_BRIGHTNESS;
    }

    /// <summary>
    /// シーンに何も置かなくても設定が効くようにする。
    /// 設定画面を経由せずタイトルから始めた場合でも、保存値が反映される。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        loaded = false;
        Apply();
    }
}
