/// <summary>
/// 選択されたステージをシーン間で受け渡すための静的な入れ物。
/// DontDestroyOnLoad のオブジェクトを作るまでもないので static で持つ。
/// </summary>
public static class StageContext
{
    public static StageData Current { get; private set; }

    /// <summary>ステージ選択画面から呼ぶ</summary>
    public static void Select(StageData stage)
    {
        Current = stage;
    }

    /// <summary>タイトルに戻ったときなどに記憶を捨てる</summary>
    public static void Clear()
    {
        Current = null;
    }

    /// <summary>
    /// 選択画面を経由していれば Current を返す。
    /// エディタからステージシーンを直接再生した場合は fallback を採用する。
    /// これがないと、開発中に毎回選択画面から入り直すことになる。
    /// </summary>
    public static StageData Resolve(StageData fallback)
    {
        if (Current == null) Current = fallback;
        return Current;
    }
}
