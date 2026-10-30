#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

// 難易度監査専用。実際のシーン・経路・武器・購入処理を変更せずに反復する一時ドライバー。
[DefaultExecutionOrder(10000)]
public sealed class DifficultyAudit : MonoBehaviour
{
    [Serializable] public class Case
    {
        public string id;
        public int stage;
        public int seed;
        public int fps = 60;
        public string policy;
        public float delay = 5f;
        public float period = 2f;
        public int[] nodes;
    }
    [Serializable] public class Plan { public string output; public Case[] cases; }
    [Serializable] public class Point { public float x, y; public Point(Vector3 p) { x = p.x; y = p.y; } }
    [Serializable] public class Route { public string name; public Point[] points; public float length; }
    [Serializable] public class Socket { public string name; public int order; public Point position; }
    [Serializable] public class Geometry { public Socket[] nodes; public Route[] routes; }

    private const string Flag = "DifficultyAudit.Run";
    private Plan plan;
    private string output;
    private bool restored;
    private readonly Dictionary<string, int> progress = new Dictionary<string, int>();
    private readonly HashSet<string> existed = new HashSet<string>();
    private float oldSpeed, oldCapture, oldVolume;
    private int oldVsync, oldTargetRate;
    private Random.State oldRandom;
    private WeaponSettings normal, yellow;
    private Node[] nodes;
    private int kills, skills;
    private float firstPass, firstKill;
    private int oldPasses, wave;
    private float nextDecision;
    private int initialPurchases;
    private int actionCount;
    private int maxAlive;
    private float minDelta, maxDelta;
    private float currentElapsed;
    private StringBuilder actions;
    private StringBuilder timeline;
    private int errors;
    private EditorWindow gameView;
    private bool oldMaximized, oldBackground;

    [InitializeOnLoadMethod]
    private static void AutoLaunch()
    {
        EditorApplication.update -= PollRequest;
        EditorApplication.update += PollRequest;
        if (!File.Exists("Temp/DifficultyAudit/autorun.request")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                AutoLaunch();
                return;
            }
            File.Delete("Temp/DifficultyAudit/autorun.request");
            try { Launch(); }
            catch (Exception error)
            {
                SessionState.SetBool(Flag, false);
                File.WriteAllText("Temp/DifficultyAudit/autorun-error.txt", error.ToString());
            }
        };
    }

    private static void PollRequest()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling
            && !EditorApplication.isUpdating && File.Exists("Temp/DifficultyAudit/autorun.request")) AutoLaunch();
    }

    [MenuItem("Shusseki/難易度を監査")]
    private static void Launch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().name != "Stage_1" || SceneManager.GetActiveScene().isDirty)
            throw new Exception("保存済みStage_1から監査を実行してください");
        if (!File.Exists("Temp/DifficultyAudit/plan.json")) throw new Exception("監査プランがありません");
        SessionState.SetBool(Flag, true);
        EditorApplication.isPlaying = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Inject()
    {
        if (SessionState.GetBool(Flag, false)) new GameObject("DifficultyAudit").AddComponent<DifficultyAudit>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        oldSpeed = PlayerPrefs.GetFloat("GAME_SPEED", 1f);
        if (PlayerPrefs.HasKey("GAME_SPEED")) existed.Add("GAME_SPEED");
        foreach (string key in new[] { "CLEARED_STAGE", "BEST_SCORE_1", "BEST_SCORE_2", "BEST_SCORE_3", "BEST_SCORE_4", "BEST_SCORE_5", "BEST_SCORE_6" })
        {
            progress[key] = PlayerPrefs.GetInt(key);
            if (PlayerPrefs.HasKey(key)) existed.Add(key);
        }
        oldCapture = Time.captureDeltaTime;
        oldVsync = QualitySettings.vSyncCount;
        oldTargetRate = Application.targetFrameRate;
        oldVolume = AudioListener.volume;
        oldRandom = Random.state;
        oldBackground = Application.runInBackground;
        Application.runInBackground = true;
        gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        oldMaximized = gameView.maximized;
        gameView.maximized = true;
        plan = JsonUtility.FromJson<Plan>(File.ReadAllText("Temp/DifficultyAudit/plan.json"));
        output = plan.output;
        Directory.CreateDirectory(output);
        File.WriteAllText(output + "/results.csv", "id,stage,seed,fps,policy,delay,clear,elapsed,passed,limit,kills,gold,towers,upgrades,skills,spawned,wave,maxAlive,firstPass,firstKill,minDelta,maxDelta,wallSeconds,errors\n");
        normal = AssetDatabase.LoadAssetAtPath<WeaponSettings>("Assets/ScriptableObject/Weapon.asset");
        yellow = AssetDatabase.LoadAssetAtPath<WeaponSettings>("Assets/ScriptableObject/StrongWeapon.asset");
        SceneManager.sceneLoaded += OnLoaded;
        Application.logMessageReceived += OnLog;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        AudioListener.volume = 0f;
        GameSpeed.Select(GameSpeed.NORMAL);
        GameSpeed.Resume();
    }

    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errors++;
            EditorApplication.delayCall += () => { Restore(); EditorApplication.isPlaying = false; };
            File.AppendAllText(output + "/errors.txt", message + "\n" + stack + "\n");
        }
    }

    private static FieldInfo Private(object host, string field) => host.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);

    private void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        GameManager gm = GameManager.Instance;
        // 操作できない3.6秒の開始演出だけを省く。GO後のルールや経済は元の実装のまま。
        Private(gm, "countdownSeconds").SetValue(gm, 0f);
        Private(gm, "goDuration").SetValue(gm, 0f);
    }

    private IEnumerator Start()
    {
        yield return null;
        double auditStart = Time.realtimeSinceStartupAsDouble;
        for (int index = 0; index < plan.cases.Length; index++)
        {
            Case test = plan.cases[index];
            Time.captureDeltaTime = 1f / test.fps;
            GameSpeed.Select(GameSpeed.NORMAL);
            GameSpeed.Resume();
            StageData stage = AssetDatabase.LoadAssetAtPath<StageData>($"Assets/ScriptableObject/Stages/Stage_{test.stage}.asset");
            StageContext.Select(stage);
            Random.InitState(test.seed);
            kills = skills = oldPasses = initialPurchases = actionCount = maxAlive = errors = 0;
            firstPass = firstKill = -1f;
            minDelta = float.MaxValue;
            maxDelta = 0f;
            actions = new StringBuilder("time,action,node,cost,goldAfter\n");
            timeline = new StringBuilder("time,wave,spawned,kills,passed,alive,gold,towers,unlocked\n");
            EnemyHP.OnEnemyDead += OnDead;
            SceneManager.LoadScene("Stage_1");
            yield return null;
            nodes = FindObjectsByType<Node>(FindObjectsSortMode.None).OrderBy(n => n.UnlockOrder).ToArray();
            if (!File.Exists(output + "/geometry.json")) DumpGeometry();
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            if (Camera.main != null) Camera.main.cullingMask = 0;
            while (!GameManager.IsGameActive) yield return null;
            double wallStart = Time.realtimeSinceStartupAsDouble;
            nextDecision = test.delay;
            float nextSample = 0f;
            GameManager gm = GameManager.Instance;
            Spawner spawner = FindFirstObjectByType<Spawner>();
            File.WriteAllText(output + "/status.txt", $"START {index + 1}/{plan.cases.Length} {test.id}\n");
            while (GameManager.IsGameActive)
            {
                currentElapsed = stage.clearTime - gm.RemainingTime;
                minDelta = Mathf.Min(minDelta, Time.deltaTime);
                maxDelta = Mathf.Max(maxDelta, Time.deltaTime);
                if (gm.EnemiesPassed != oldPasses)
                {
                    if (firstPass < 0f) firstPass = currentElapsed;
                    oldPasses = gm.EnemiesPassed;
                }
                if (currentElapsed >= nextDecision)
                {
                    Decide(test);
                    nextDecision += test.period;
                }
                if (currentElapsed >= nextSample)
                {
                    int alive = FindObjectsByType<EnemyHP>(FindObjectsSortMode.None).Count(hp => hp.IsAlive);
                    maxAlive = Mathf.Max(maxAlive, alive);
                    timeline.AppendLine(FormattableString.Invariant($"{currentElapsed:F3},{Wave(spawner) + 1},{Spawned(spawner, stage)},{kills},{gm.EnemiesPassed},{alive},{CurrencyManager.instance.totalCoins},{WeaponShopManager.Instance.TotalPlaced()},{StageLevel.Instance.UnlockedCount}"));
                    nextSample += 1f;
                }
                if (Time.realtimeSinceStartupAsDouble - auditStart > 1800d) throw new Exception("監査が30分を超えました");
                yield return null;
            }
            EnemyHP.OnEnemyDead -= OnDead;
            currentElapsed = stage.clearTime - gm.RemainingTime;
            int upgraded = nodes.Where(n => n.weapon != null).Sum(n => Mathf.Max(0, n.weapon.weaponUpgrade.level - 1));
            string row = FormattableString.Invariant($"{test.id},{test.stage},{test.seed},{test.fps},{test.policy},{test.delay:F1},{gm.IsGameClear},{currentElapsed:F3},{gm.EnemiesPassed},{gm.MaxEnemyPasses},{kills},{CurrencyManager.instance.totalCoins},{WeaponShopManager.Instance.TotalPlaced()},{upgraded},{skills},{Spawned(spawner, stage)},{Wave(spawner) + 1},{maxAlive},{firstPass:F3},{firstKill:F3},{minDelta:F6},{maxDelta:F6},{Time.realtimeSinceStartupAsDouble - wallStart:F3},{errors}");
            File.AppendAllText(output + "/results.csv", row + "\n");
            File.WriteAllText(output + "/" + test.id + "-actions.csv", actions.ToString());
            File.WriteAllText(output + "/" + test.id + "-timeline.csv", timeline.ToString());
            File.WriteAllText(output + "/status.txt", $"DONE {index + 1}/{plan.cases.Length} {test.id}\nwallTotal={Time.realtimeSinceStartupAsDouble - auditStart:F1}\n" + row);
        }
        File.AppendAllText(output + "/status.txt", "\nCOMPLETE\n");
        Restore();
        EditorApplication.isPlaying = false;
    }

    private void OnDead(EnemyHP hp)
    {
        kills++;
        if (firstKill < 0f && GameManager.Instance != null)
            firstKill = GameManager.Instance.Stage.clearTime - GameManager.Instance.RemainingTime;
    }

    private static int Wave(Spawner spawner) => (int)Private(spawner, "currentWaveIndex").GetValue(spawner);

    private static int Spawned(Spawner spawner, StageData stage)
    {
        // 波間の待機中にはSpawner.spawnedが直前の波の値を保つため、そこからは総数を作れない。
        // プール作成時の初回OnEnableを除き、実際に再出現した回数だけを数える。
        var items = (List<ObjectPooler.PoolItem>)Private(ObjectPooler.Instance, "poolItems").GetValue(ObjectPooler.Instance);
        int total = 0;
        foreach (ObjectPooler.PoolItem item in items)
        {
            if (item.prefab == null || item.prefab.GetComponent<EnemyHP>() == null || item.pool == null) continue;
            foreach (GameObject instance in item.pool)
            {
                if (instance == null) continue;
                EnemyHP hp = instance.GetComponent<EnemyHP>();
                total += Mathf.Max(0, (int)hp.SpawnVersion - 1);
            }
        }
        return total;
    }

    private IEnumerable<Node> OrderedNodes(Case test)
    {
        if (test.nodes != null && test.nodes.Length > 0)
            return test.nodes.Select(order => nodes.FirstOrDefault(n => n.UnlockOrder == order)).Where(n => n != null);
        return nodes;
    }

    private void Decide(Case test)
    {
        if (test.policy == "Idle") return;
        if (test.policy == "SkillOnly")
        {
            if (currentElapsed >= 20f) UseSkill();
            return;
        }
        bool mixed = test.policy.Contains("Mixed");
        bool reactive = test.policy.StartsWith("Reactive");
        if (test.policy == "ReactiveMixedSkill") UseSkill();
        if (!reactive && initialPurchases >= 3) return;
        WeaponShopManager shop = WeaponShopManager.Instance;
        int placed = shop.TotalPlaced();
        Node empty = OrderedNodes(test).FirstOrDefault(n => n.IsUnlocked && n.IsEmpty());
        WeaponSettings buy = mixed && placed % 2 == 1 ? yellow : normal;
        if (empty != null && !shop.IsAtCapacity && (placed < 4 || !reactive))
        {
            if (Purchase(empty, buy)) initialPurchases++;
            return;
        }
        if (!reactive) return;
        Node upgrade = OrderedNodes(test).Where(n => n.weapon != null && !n.weapon.weaponUpgrade.IsMaxLevel)
            .OrderBy(n => n.weapon.weaponUpgrade.level).ThenBy(n => n.UnlockOrder).FirstOrDefault();
        if (upgrade != null && upgrade.weapon.weaponUpgrade.level < 2)
        {
            Upgrade(upgrade);
            return;
        }
        if (empty != null && !shop.IsAtCapacity)
        {
            if (Purchase(empty, buy)) return;
        }
        if (upgrade != null) Upgrade(upgrade);
    }

    private bool Purchase(Node node, WeaponSettings buy)
    {
        int before = CurrencyManager.instance.totalCoins;
        int price = WeaponShopManager.Instance.PriceOf(buy);
        if (before < price) return false;
        node.SelectNode();
        WeaponUI.OnPressedWeaponsUI?.Invoke(buy);
        if (node.weapon == null) throw new Exception("購入が失敗しました");
        actions.AppendLine(FormattableString.Invariant($"{currentElapsed:F3},buy{(buy == yellow ? "Yellow" : "Normal")},{node.UnlockOrder},{before - CurrencyManager.instance.totalCoins},{CurrencyManager.instance.totalCoins}"));
        actionCount++;
        return true;
    }

    private void Upgrade(Node node)
    {
        int before = CurrencyManager.instance.totalCoins;
        WeaponUpgrade up = node.weapon.weaponUpgrade;
        if (before < up.currentUpgradeCost) return;
        int level = up.level;
        node.SelectNode();
        UIManager.instance.UpgradeWeapon();
        if (up.level != level + 1) throw new Exception("強化が失敗しました");
        actions.AppendLine(FormattableString.Invariant($"{currentElapsed:F3},upgradeTo{up.level},{node.UnlockOrder},{before - CurrencyManager.instance.totalCoins},{CurrencyManager.instance.totalCoins}"));
        actionCount++;
    }

    private void UseSkill()
    {
        PlayerSkillController controller = PlayerSkillController.Instance;
        if (controller == null || !controller.CanActivate(0)) return;
        int count = FindObjectsByType<EnemyHP>(FindObjectsSortMode.None).Count(hp => hp.IsAlive);
        if (count < 6) return;
        if (!controller.TryActivate(0)) return;
        skills++;
        actions.AppendLine(FormattableString.Invariant($"{currentElapsed:F3},skill0,-1,0,{CurrencyManager.instance.totalCoins}"));
    }

    private void DumpGeometry()
    {
        Geometry geometry = new Geometry();
        geometry.nodes = nodes.Select(n => new Socket { name = n.name, order = n.UnlockOrder, position = new Point(n.transform.position) }).ToArray();
        geometry.routes = FindObjectsByType<MovePoint>(FindObjectsSortMode.None).Select(route =>
        {
            float length = 0f;
            for (int i = 1; i < route.points.Length; i++) length += Vector3.Distance(route.points[i - 1], route.points[i]);
            return new Route { name = route.name, points = route.points.Select(p => new Point(p)).ToArray(), length = length };
        }).ToArray();
        File.WriteAllText(output + "/geometry.json", JsonUtility.ToJson(geometry, true));
    }

    private void Restore()
    {
        if (restored) return;
        restored = true;
        SceneManager.sceneLoaded -= OnLoaded;
        Application.logMessageReceived -= OnLog;
        EnemyHP.OnEnemyDead -= OnDead;
        foreach (var item in progress)
        {
            if (existed.Contains(item.Key)) PlayerPrefs.SetInt(item.Key, item.Value);
            else PlayerPrefs.DeleteKey(item.Key);
        }
        GameSpeed.Select(oldSpeed);
        if (existed.Contains("GAME_SPEED")) PlayerPrefs.SetFloat("GAME_SPEED", oldSpeed);
        else PlayerPrefs.DeleteKey("GAME_SPEED");
        PlayerPrefs.Save();
        GameSpeed.Resume();
        Time.captureDeltaTime = oldCapture;
        QualitySettings.vSyncCount = oldVsync;
        Application.targetFrameRate = oldTargetRate;
        Application.runInBackground = oldBackground;
        if (gameView != null) gameView.maximized = oldMaximized;
        AudioListener.volume = oldVolume;
        Random.state = oldRandom;
        StageContext.Clear();
        SessionState.SetBool(Flag, false);
        File.AppendAllText(output + "/status.txt", "\nRESTORED\n");
    }

    private void OnDestroy() { Restore(); }
}
#endif
