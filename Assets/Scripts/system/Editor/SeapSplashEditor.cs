using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[CustomEditor(typeof(SeapSplash))]
public sealed class SeapSplashEditor : Editor
{
    private const string ScenePath = "Assets/Scenes/SeapSplash.unity";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        if (GUILayout.Button("Unity画面を表示")) ((SeapSplash)target).ShowUnity();
        if (GUILayout.Button("SEAP画面を表示")) ((SeapSplash)target).ShowSeap();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("起動演出を最初からプレビュー")) Preview();
        }
    }

    [MenuItem("Shusseki/起動画面/プレビュー", false, 0)]
    private static void Preview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Shusseki/起動画面/開く", false, 1)]
    private static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        Selection.activeGameObject = Object.FindFirstObjectByType<SeapSplash>().gameObject;
    }

    [MenuItem("Shusseki/起動画面/作成・修復", false, 20)]
    private static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // Unity自身に参照を保存させ、手書きYAMLによるMissing Scriptを防ぐ。
        AssetDatabase.ImportAsset("Assets/Scripts/StartMenu/SeapSplash.cs", ImportAssetOptions.ForceUpdate);
        Sprite unityLogo = AssetDatabase.GetBuiltinExtraResource<Sprite>("SplashScreen/UnitySplash-Light.png");
        Sprite seapLogo = AssetDatabase.LoadAllAssetsAtPath("Assets/SEAP/SEAP2.png").OfType<Sprite>().FirstOrDefault();
        if (unityLogo == null || seapLogo == null)
            throw new System.InvalidOperationException("UnityまたはSEAP2のロゴを読み込めません。");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        Canvas canvas = new GameObject("SplashCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image unityScreen = CreateImage("UnityScreen", canvas.transform, Color.black);
        Image unityImage = CreateImage("UnityLogo", unityScreen.transform, Color.white);
        unityImage.sprite = unityLogo;
        unityImage.preserveAspect = true;
        unityImage.rectTransform.anchorMin = new Vector2(0.2f, 0.3f);
        unityImage.rectTransform.anchorMax = new Vector2(0.8f, 0.7f);

        Image seapScreen = CreateImage("SeapScreen", canvas.transform, Color.white);
        Image seapImage = CreateImage("SEAP2", seapScreen.transform, Color.white);
        seapImage.sprite = seapLogo;
        seapImage.preserveAspect = true;

        SeapSplash controller = new GameObject("SeapSplash").AddComponent<SeapSplash>();
        SerializedObject fields = new SerializedObject(controller);
        fields.FindProperty("unityScreen").objectReferenceValue = unityScreen.gameObject;
        fields.FindProperty("seapScreen").objectReferenceValue = seapScreen.gameObject;
        fields.ApplyModifiedPropertiesWithoutUndo();
        controller.ShowUnity();
        EditorSceneManager.SaveScene(scene, ScenePath);

        // ビルドもこのシーンを入口にし、標準スプラッシュとの二重表示を避ける。
        PlayerSettings.SplashScreen.show = false;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
            .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = controller.gameObject;
        Debug.Log("起動画面を作成しました。Shusseki > 起動画面 > プレビュー で Unity → SEAP → タイトル を再生できます。");
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;
        return image;
    }
}
