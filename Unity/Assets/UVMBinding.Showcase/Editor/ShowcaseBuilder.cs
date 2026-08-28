using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using UVMBinding;
using UVMBinding.Arguments;
using UVMBinding.Binders;
using UVMBinding.Converters;
using UVMBinding.Events;
using UVMBinding.PropertyProvider;
using UVMBinding.Showcase;

//ショーケースシーンを生成する。手で組み直さず、常にこのスクリプトから再生成する。
public static partial class ShowcaseBuilder
{
	public const int Width = 1920;
	public const int Height = 1080;

	public static readonly Color Bg = new Color(0.13f, 0.14f, 0.17f, 1f);
	public static readonly Color Panel = new Color(0.20f, 0.22f, 0.27f, 1f);
	public static readonly Color Accent = new Color(0.30f, 0.55f, 0.90f, 1f);
	public static readonly Color TabIdle = new Color(0.22f, 0.24f, 0.29f, 1f);
	public static readonly Color Ink = new Color(0.94f, 0.95f, 0.97f, 1f);
	public static readonly Color Sub = new Color(0.65f, 0.68f, 0.74f, 1f);

	public const float LabelWidth = 360f;

	// ---------- 基本ヘルパ ----------

	public static GameObject New(string name, Transform parent)
	{
		var go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go;
	}

	public static RectTransform Rect(GameObject go) => (RectTransform)go.transform;

	public static void Stretch(GameObject go)
	{
		var r = Rect(go);
		r.anchorMin = Vector2.zero;
		r.anchorMax = Vector2.one;
		r.pivot = new Vector2(0.5f, 0.5f);
		r.offsetMin = Vector2.zero;
		r.offsetMax = Vector2.zero;
	}

	public static UnityEngine.UI.Image AddImage(GameObject go, Color color, bool raycast)
	{
		var img = go.AddComponent<UnityEngine.UI.Image>();
		img.color = color;
		img.raycastTarget = raycast;
		return img;
	}

	public static UnityEngine.UI.Text AddText(GameObject go, string content, int size, Color color, TextAnchor anchor)
	{
		var t = go.AddComponent<UnityEngine.UI.Text>();
		t.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		t.text = content;
		t.fontSize = size;
		t.color = color;
		t.alignment = anchor;
		t.raycastTarget = false;
		t.horizontalOverflow = HorizontalWrapMode.Overflow;
		t.verticalOverflow = VerticalWrapMode.Overflow;
		return t;
	}

	public static LayoutElement Size(GameObject go, float w, float h)
	{
		var le = go.GetComponent<LayoutElement>();
		if (le == null) le = go.AddComponent<LayoutElement>();
		if (w > 0) { le.minWidth = w; le.preferredWidth = w; }
		if (h > 0) { le.minHeight = h; le.preferredHeight = h; }
		return le;
	}

	//1 行。左にラベル、右に内容を並べる
	public static GameObject Row(Transform parent, string name = "Row", float height = 44f)
	{
		var go = New(name, parent);
		Size(go, 0, height);
		var h = go.AddComponent<HorizontalLayoutGroup>();
		h.childAlignment = TextAnchor.MiddleLeft;
		h.spacing = 14;
		//LayoutElement を効かせるため親が制御する
		h.childControlWidth = true;
		h.childControlHeight = true;
		h.childForceExpandWidth = false;
		h.childForceExpandHeight = false;
		return go;
	}

	public static GameObject Label(Transform parent, string text, float width = LabelWidth)
	{
		var go = New("Label", parent);
		AddText(go, text, 21, Sub, TextAnchor.MiddleLeft);
		Size(go, width, 0);
		return go;
	}

	public static GameObject Value(Transform parent, string path, float width = 300f, string initial = "-")
	{
		var go = New("Value", parent);
		AddText(go, initial, 23, Ink, TextAnchor.MiddleLeft);
		Size(go, width, 0);
		if (!string.IsNullOrEmpty(path)) SetPath(go.AddComponent<TextBinder>(), path);
		return go;
	}

	public static GameObject Button(Transform parent, string caption, string eventName, float width = 200f)
	{
		var go = New("Button_" + caption, parent);
		AddImage(go, Accent, true);
		go.AddComponent<UnityEngine.UI.Button>();
		Size(go, width, 44);

		var label = New("Text", go.transform);
		Stretch(label);
		AddText(label, caption, 20, Color.white, TextAnchor.MiddleCenter);

		if (!string.IsNullOrEmpty(eventName))
		{
			var ev = go.AddComponent<EventButton>();
			SetProp(ev, "m_EventName", eventName);
		}
		return go;
	}

	// ---------- SerializedObject ヘルパ ----------

	public static void SetProp(Component c, string prop, string value)
	{
		var so = new SerializedObject(c);
		so.FindProperty(prop).stringValue = value;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	public static void SetPath(Component binder, string path)
	{
		SetProp(binder, "m_Path", path);
	}

	public static void SetRef(Component c, string prop, Object value)
	{
		var so = new SerializedObject(c);
		so.FindProperty(prop).objectReferenceValue = value;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	//[SerializeReference] に値を入れる
	public static void SetManagedRef(Component c, string prop, object value)
	{
		var so = new SerializedObject(c);
		so.FindProperty(prop).managedReferenceValue = value;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	// ---------- ScrollView ----------

	//ScrollRect の必須構成。Viewport に RectMask2D、Content に VLG + ContentSizeFitter。
	public static GameObject ScrollView(Transform parent, out GameObject content)
	{
		var root = New("ScrollView", parent);
		var scroll = root.AddComponent<ScrollRect>();
		scroll.horizontal = false;
		scroll.vertical = true;
		scroll.movementType = ScrollRect.MovementType.Clamped;
		scroll.scrollSensitivity = 40f;

		var viewport = New("Viewport", root.transform);
		Stretch(viewport);
		//矩形クリップなので RectMask2D を使う。Mask はステンシルを使い、
		//Image が必須なうえ描画が破綻するケースがあった。
		AddImage(viewport, new Color(1, 1, 1, 0.004f), true);
		viewport.AddComponent<RectMask2D>();

		content = New("Content", viewport.transform);
		var cr = Rect(content);
		//上端に固定して下へ伸ばす
		cr.anchorMin = new Vector2(0f, 1f);
		cr.anchorMax = new Vector2(1f, 1f);
		cr.pivot = new Vector2(0.5f, 1f);
		cr.offsetMin = new Vector2(0f, 0f);
		cr.offsetMax = new Vector2(0f, 0f);

		var vlg = content.AddComponent<VerticalLayoutGroup>();
		vlg.padding = new RectOffset(0, 24, 0, 24);
		vlg.spacing = 8;
		vlg.childAlignment = TextAnchor.UpperLeft;
		vlg.childControlWidth = true;
		vlg.childControlHeight = true;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;

		var fitter = content.AddComponent<ContentSizeFitter>();
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

		scroll.viewport = Rect(viewport);
		scroll.content = cr;
		return root;
	}

	//画面の器。見出し + 説明 + スクロール領域を返す
	public static GameObject Screen(Transform parent, string title, string description, out GameObject content)
	{
		var screen = New(title, parent);
		Stretch(screen);
		var vlg = screen.AddComponent<VerticalLayoutGroup>();
		vlg.padding = new RectOffset(56, 24, 24, 24);
		vlg.spacing = 6;
		vlg.childAlignment = TextAnchor.UpperLeft;
		vlg.childControlWidth = true;
		vlg.childControlHeight = true;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;

		var header = New("Header", screen.transform);
		AddText(header, title, 34, Ink, TextAnchor.MiddleLeft);
		Size(header, 0, 46);

		var desc = New("Description", screen.transform);
		AddText(desc, description, 20, Sub, TextAnchor.MiddleLeft);
		Size(desc, 0, 28);

		var scroll = ScrollView(screen.transform, out content);
		var le = scroll.AddComponent<LayoutElement>();
		le.flexibleHeight = 1;
		le.minHeight = 700;
		return screen;
	}

	//見出し（グループ区切り）
	public static void Section(Transform parent, string text)
	{
		var go = New("Section", parent);
		AddText(go, text, 20, Accent, TextAnchor.LowerLeft);
		Size(go, 0, 40);
	}
}
