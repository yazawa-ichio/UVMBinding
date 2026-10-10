using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using UVMBinding.Showcase;

public static partial class ShowcaseBuilder
{
	const string ScenePath = "Assets/UVMBinding.Showcase/Scenes/Showcase.unity";

	static readonly string[] TabNames =
	{
		"01 基本", "02 入力", "03 コレクション", "04 Converter", "05 派生値", "06 View 合成",
		"07 VM 種別", "08 演出", "09 Event", "10 任意", "11 その他",
	};

	[MenuItem("UVMBinding/Showcase/Build All")]
	public static void BuildAll()
	{
		var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
		if (scene.path != ScenePath)
		{
			scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
		}
		foreach (var root in scene.GetRootGameObjects())
		{
			Object.DestroyImmediate(root);
		}

		var canvasGo = new GameObject("Canvas", typeof(RectTransform));
		var canvas = canvasGo.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		var scaler = canvasGo.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(Width, Height);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 0.5f;
		canvasGo.AddComponent<GraphicRaycaster>();

		var es = new GameObject("EventSystem");
		es.AddComponent<EventSystem>();
		es.AddComponent<StandaloneInputModule>();

		var bg = New("Background", canvasGo.transform);
		Stretch(bg);
		AddImage(bg, Bg, false);

		var tabBar = New("TabBar", canvasGo.transform);
		var tr = Rect(tabBar);
		tr.anchorMin = new Vector2(0f, 0f);
		tr.anchorMax = new Vector2(0f, 1f);
		tr.pivot = new Vector2(0f, 0.5f);
		tr.sizeDelta = new Vector2(300f, 0f);
		tr.anchoredPosition = Vector2.zero;
		AddImage(tabBar, new Color(0.10f, 0.11f, 0.14f, 1f), false);
		var tvl = tabBar.AddComponent<VerticalLayoutGroup>();
		tvl.padding = new RectOffset(16, 16, 24, 24);
		tvl.spacing = 6;
		tvl.childAlignment = TextAnchor.UpperCenter;
		tvl.childControlWidth = true;
		tvl.childControlHeight = true;
		tvl.childForceExpandWidth = true;
		tvl.childForceExpandHeight = false;

		var title = New("Title", tabBar.transform);
		AddText(title, "UVMBinding\nShowcase", 24, Ink, TextAnchor.MiddleCenter);
		Size(title, 0, 84);

		var body = New("Body", canvasGo.transform);
		var br = Rect(body);
		br.anchorMin = new Vector2(0f, 0f);
		br.anchorMax = new Vector2(1f, 1f);
		br.pivot = new Vector2(0.5f, 0.5f);
		br.offsetMin = new Vector2(300f, 0f);
		br.offsetMax = Vector2.zero;

		var screens = new List<GameObject>();
		BuildBasic(body.transform, screens);
		BuildInput(body.transform, screens);
		BuildCollection(body.transform, screens);
		BuildConverter(body.transform, screens);
		BuildProvider(body.transform, screens);
		BuildComposite(body.transform, screens);
		BuildVariant(body.transform, screens);
		BuildEffect(body.transform, screens);
		BuildEvent(body.transform, screens);
		BuildOptional(body.transform, screens);
		BuildMisc(body.transform, screens);

		var buttons = new List<UnityEngine.UI.Button>();
		for (int i = 0; i < TabNames.Length && i < screens.Count; i++)
		{
			var go = New("Tab_" + i, tabBar.transform);
			AddImage(go, TabIdle, true);
			buttons.Add(go.AddComponent<UnityEngine.UI.Button>());
			Size(go, 0, 46);
			var label = New("Text", go.transform);
			Stretch(label);
			AddText(label, TabNames[i], 19, Ink, TextAnchor.MiddleCenter);
		}

		var tabs = canvasGo.AddComponent<ShowcaseTabs>();
		var so = new SerializedObject(tabs);
		var sp = so.FindProperty("m_Screens");
		sp.arraySize = screens.Count;
		for (int i = 0; i < screens.Count; i++) sp.GetArrayElementAtIndex(i).objectReferenceValue = screens[i];
		var bp = so.FindProperty("m_Tabs");
		bp.arraySize = buttons.Count;
		for (int i = 0; i < buttons.Count; i++) bp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
		so.ApplyModifiedPropertiesWithoutUndo();

		es.AddComponent<ShowcaseSelfCheck>();

		UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
		UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
		Debug.Log($"Showcase built. screens={screens.Count}");
	}
}
