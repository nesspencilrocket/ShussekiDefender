using UnityEngine;

/// <summary>
/// ステージの進行状況を PlayerPrefs に保存する。
/// コインはステージ内だけの一時的な資源なので、ここでは扱わない。
/// </summary>
public static class StageProgress
{
    private const string KEY_CLEARED = "CLEARED_STAGE";
    private const string KEY_SCORE = "BEST_SCORE_";

    /// <summary>クリア済みの最大ステージ番号。未クリアなら 0</summary>
    public static int ClearedUpTo => PlayerPrefs.GetInt(KEY_CLEARED, 0);

    /// <summary>1限目は常に開放。以降は 1 つ前をクリアしていれば開放</summary>
    public static bool IsUnlocked(int stageNumber)
    {
        return stageNumber <= ClearedUpTo + 1;
    }

    public static void MarkCleared(int stageNumber)
    {
        if (stageNumber > ClearedUpTo)
        {
            PlayerPrefs.SetInt(KEY_CLEARED, stageNumber);
        }
        PlayerPrefs.Save();
    }

    public static int GetBestScore(int stageNumber)
    {
        return PlayerPrefs.GetInt(KEY_SCORE + stageNumber, 0);
    }

    public static void SubmitScore(int stageNumber, int score)
    {
        if (score > GetBestScore(stageNumber))
        {
            PlayerPrefs.SetInt(KEY_SCORE + stageNumber, score);
            PlayerPrefs.Save();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// テスト用。解放を実装すると毎回 1限目からやり直しになるため、
    /// メニューから初期化できるようにしておく。
    /// </summary>
    [UnityEditor.MenuItem("Shusseki/進行状況をリセット")]
    private static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(KEY_CLEARED);
        for (int i = 1; i <= 6; i++) PlayerPrefs.DeleteKey(KEY_SCORE + i);
        PlayerPrefs.Save();
        Debug.Log("進行状況をリセットしました");
    }
#endif
}
