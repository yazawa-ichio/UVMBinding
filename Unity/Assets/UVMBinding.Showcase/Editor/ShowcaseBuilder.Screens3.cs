using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UVMBinding;
using UVMBinding.Arguments;
using UVMBinding.Binders;
using UVMBinding.Converters;
using UVMBinding.Events;
using UVMBinding.PropertyProvider;
using UVMBinding.Showcase;

public static partial class ShowcaseBuilder
{
	// 05 派生値
	static void BuildProvider(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "05 派生値", "View 側で複数のパスから値を導出する。CombineBoolProvider / CombineStringProvider。", out var c);
		screen.AddComponent<View>();
		screen.AddComponent<ProviderScreen>();

		var ext = screen.AddComponent<ViewModelPropertyExtension>();
		var boolProvider = new CombineBoolProvider();
		SetPrivate(boolProvider, "m_ValueKey", "BothFlags");
		SetPrivate(boolProvider, "m_SourceKeys", new[] { "FlagA", "FlagB" });
		var strProvider = new CombineStringProvider();
		SetPrivate(strProvider, "m_ValueKey", "Caption");
		SetPrivate(strProvider, "m_Format", "{0} / Lv.{1}");
		SetPrivate(strProvider, "m_SourceKeys", new[] { "Name", "Level" });
		SetManagedArray(ext, "m_Properties", new object[] { boolProvider, strProvider });

		var r1 = Row(c.transform); Label(r1.transform, "FlagA / FlagB");
		Button(r1.transform, "Toggle A", "OnToggleA", 150);
		Button(r1.transform, "Toggle B", "OnToggleB", 150);

		Section(c.transform, "CombineBoolProvider（All）");
		var r2 = Row(c.transform, "Row_Both", 60); Label(r2.transform, "BothFlags = FlagA && FlagB");
		var panel = New("Panel", r2.transform); AddImage(panel, Accent, false); Size(panel, 240, 52);
		var pt = New("Text", panel.transform); Stretch(pt); AddText(pt, "両方 ON", 21, Color.white, TextAnchor.MiddleCenter);
		SetPath(panel.AddComponent<ActiveBinder>(), "BothFlags");

		Section(c.transform, "CombineStringProvider（書式）");
		var r3 = Row(c.transform); Label(r3.transform, "Caption = \"{0} / Lv.{1}\"");
		Value(r3.transform, "Caption", 360);

		screens.Add(screen);
	}

	// 06 View 合成
	static void BuildComposite(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "06 View 合成", "View の入れ子と ViewBinder。子 ViewModel を差し替える。", out var c);
		screen.AddComponent<View>();
		screen.AddComponent<CompositeScreen>();

		var r1 = Row(c.transform); Label(r1.transform, "親 View (Title)"); Value(r1.transform, "Title", 300);
		Button(r1.transform, "子を差し替え", "OnSwap", 200);

		Section(c.transform, "ViewBinder（子 View に ViewModel を流す）");
		var childHost = New("ChildView", c.transform);
		AddImage(childHost, Panel, false);
		Size(childHost, 0, 90);
		var ch = childHost.AddComponent<HorizontalLayoutGroup>();
		ch.padding = new RectOffset(20, 20, 0, 0); ch.spacing = 14;
		ch.childAlignment = TextAnchor.MiddleLeft;
		ch.childControlWidth = true; ch.childControlHeight = true;
		ch.childForceExpandWidth = false; ch.childForceExpandHeight = false;
		//子 View。親とは別の ViewModel がバインドされる
		childHost.AddComponent<View>();
		var vb = childHost.AddComponent<ViewBinder>();
		SetPath(vb, "Child");
		Label(childHost.transform, "子 View (ChildText)", 260);
		Value(childHost.transform, "ChildText", 260);

		screens.Add(screen);
	}

	// 07 ViewModel 種別
	static void BuildVariant(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "07 ViewModel 種別", "Property<T> / SetDirty / ReflectionViewModel。", out var c);
		screen.AddComponent<View>();
		var driver = screen.AddComponent<VariantScreen>();

		Section(c.transform, "Property<T>");
		var r1 = Row(c.transform); Label(r1.transform, "Property<string> (Tracked)");
		Value(r1.transform, "Tracked", 320);
		Button(r1.transform, "変更", "OnChangeTracked", 150);

		Section(c.transform, "SetDirty（値を変えずに再通知）");
		var r2 = Row(c.transform); Label(r2.transform, "DirtyTarget");
		Value(r2.transform, "DirtyTarget", 320);
		Button(r2.transform, "SetDirty", "OnSetDirty", 150);

		Section(c.transform, "ReflectionViewModel（素の POCO をバインド）");
		var refHost = New("ReflectionView", c.transform);
		AddImage(refHost, Panel, false);
		Size(refHost, 0, 90);
		var rh = refHost.AddComponent<HorizontalLayoutGroup>();
		rh.padding = new RectOffset(20, 20, 0, 0); rh.spacing = 14;
		rh.childAlignment = TextAnchor.MiddleLeft;
		rh.childControlWidth = true; rh.childControlHeight = true;
		rh.childForceExpandWidth = false; rh.childForceExpandHeight = false;
		var refView = refHost.AddComponent<View>();
		Label(refHost.transform, "public フィールド Title / Count", 320);
		Value(refHost.transform, "Title", 260);
		var countGo = New("Count", refHost.transform);
		AddText(countGo, "-", 23, Ink, TextAnchor.MiddleLeft); Size(countGo, 120, 0);
		var countBinder = countGo.AddComponent<TextBinder>();
		SetPath(countBinder, "Count");
		SetManagedRef(countBinder, "m_Converter", new ObjectToString());
		SetRef(driver, "m_ReflectionView", refView);

		screens.Add(screen);
	}

	// 08 演出系 Binder
	static void BuildEffect(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "08 演出系 Binder", "SemanticRole.None に分類される表示効果。走査対象から除外される予定。", out var c);
		screen.AddComponent<View>();
		screen.AddComponent<EffectScreen>();

		var ops = Row(c.transform); Label(ops.transform, "操作");
		Button(ops.transform, "Toggle Visible", "OnToggleVisible", 200);
		Button(ops.transform, "Randomize", "OnRandomize", 200);

		Section(c.transform, "CanvasGroup / Image");
		var r1 = Row(c.transform, "Row_Alpha", 76); Label(r1.transform, "CanvasGroupAlphaBinder (Alpha)");
		var cg = New("Group", r1.transform); AddImage(cg, Accent, false); Size(cg, 200, 64);
		cg.AddComponent<CanvasGroup>();
		SetPath(cg.AddComponent<CanvasGroupAlphaBinder>(), "Alpha");

		var r2 = Row(c.transform, "Row_Fill", 76); Label(r2.transform, "ImageFillAmountBinder (Fill)");
		var fill = New("Fill", r2.transform);
		var fi = AddImage(fill, Accent, false); fi.type = UnityEngine.UI.Image.Type.Filled; fi.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
		Size(fill, 240, 40);
		SetPath(fill.AddComponent<ImageFillAmountBinder>(), "Fill");

		var r3 = Row(c.transform, "Row_Tint", 76); Label(r3.transform, "GraphicColorBinder (Tint)");
		var tint = New("Tint", r3.transform); AddImage(tint, Color.white, false); Size(tint, 200, 64);
		SetPath(tint.AddComponent<GraphicColorBinder>(), "Tint");

		Section(c.transform, "Transform");
		var r4 = Row(c.transform, "Row_Pos", 90); Label(r4.transform, "Position3DBinder (Offset)");
		var posHost = New("PosHost", r4.transform); Size(posHost, 320, 80);
		var pos = New("Moving", posHost.transform); AddImage(pos, Ink, false);
		//Position3DBinder は localPosition に書く。RectTransform の localPosition は
		//端アンカーだと親の解決済み幅に依存するため、中央アンカーにして依存を外す。
		//端アンカーのまま使う場合は AnchoredPositionBinder を選ぶ（画面 11・TASK-027）
		var pr = Rect(pos); pr.anchorMin = new Vector2(0.5f, 0.5f); pr.anchorMax = new Vector2(0.5f, 0.5f); pr.pivot = new Vector2(0.5f, 0.5f);
		pr.sizeDelta = new Vector2(48, 48); pr.anchoredPosition = Vector2.zero;
		SetPath(pos.AddComponent<Position3DBinder>(), "Offset");

		var r5 = Row(c.transform, "Row_Scale", 90); Label(r5.transform, "ScaleBinder (Scale)");
		var scaleHost = New("ScaleHost", r5.transform); Size(scaleHost, 200, 80);
		var scale = New("Scaling", scaleHost.transform); AddImage(scale, Accent, false);
		var scr = Rect(scale); scr.anchorMin = new Vector2(0.5f, 0.5f); scr.anchorMax = new Vector2(0.5f, 0.5f); scr.sizeDelta = new Vector2(56, 56);
		SetPath(scale.AddComponent<ScaleBinder>(), "Scale");

		var r6 = Row(c.transform, "Row_Visible", 76); Label(r6.transform, "ActiveBinder (Visible)");
		var vis = New("Visible", r6.transform); AddImage(vis, Panel, false); Size(vis, 200, 64);
		var vt = New("Text", vis.transform); Stretch(vt); AddText(vt, "表示中", 21, Ink, TextAnchor.MiddleCenter);
		SetPath(vis.AddComponent<ActiveBinder>(), "Visible");

		screens.Add(screen);
	}

	// 09 Event と引数
	static void BuildEvent(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "09 Event と引数", "EventButton と EventArgument。引数付きイベントの発火。", out var c);
		screen.AddComponent<View>();
		screen.AddComponent<EventScreen>();

		var r1 = Row(c.transform); Label(r1.transform, "受信ログ"); Value(r1.transform, "Log", 480);

		Section(c.transform, "引数なし / 引数あり");
		var r2 = Row(c.transform); Label(r2.transform, "Action");
		Button(r2.transform, "Plain", "OnPlain", 180);

		//EventArgument は m_Target から値を読む。未設定だと GetValue で例外になる
		var r3 = Row(c.transform); Label(r3.transform, "Action<string> (TextArgument)");
		var payload = New("Payload", r3.transform);
		AddImage(payload, Panel, false);
		Size(payload, 220, 44);
		var payloadText = New("Text", payload.transform);
		Stretch(payloadText);
		var payloadLabel = AddText(payloadText, "送る文字列", 20, Ink, TextAnchor.MiddleCenter);
		var withText = Button(r3.transform, "Send Text", "OnWithText", 200);
		var textArg = new TextArgument();
		SetPrivate(textArg, "m_Target", payloadLabel);
		SetManagedRef(withText.GetComponent<EventButton>(), "m_Argument", textArg);

		var r4 = Row(c.transform); Label(r4.transform, "Action<Color> (GraphicColorArgument)");
		var swatch = New("Swatch", r4.transform);
		var swatchImage = AddImage(swatch, new Color(0.95f, 0.45f, 0.25f, 1f), false);
		Size(swatch, 220, 44);
		var withColor = Button(r4.transform, "Send Color", "OnWithColor", 200);
		var colorArg = new GraphicColorArgument();
		SetPrivate(colorArg, "m_Target", swatchImage);
		SetManagedRef(withColor.GetComponent<EventButton>(), "m_Argument", colorArg);

		screens.Add(screen);
	}

	// 10 任意パッケージ
	static void BuildOptional(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "10 任意パッケージ", "TMP / Localization。defineConstraints により未導入でもコンパイルできる。", out var c);
		screen.AddComponent<View>();
		screen.AddComponent<OptionalScreen>();

		var r1 = Row(c.transform); Label(r1.transform, "導入状況"); Value(r1.transform, "Availability", 620);

		Section(c.transform, "備考");
		var note = New("Note", c.transform);
		AddText(note,
			"TMP / Localization 向けの Binder は別アセンブリにあり、versionDefines で自動的に有効化される。\n" +
			"未導入の環境ではアセンブリごと無効になるため、この画面はコンパイルエラーにならない。",
			19, Sub, TextAnchor.UpperLeft);
		Size(note, 0, 70);

		screens.Add(screen);
	}

	// 11 その他
	static void BuildMisc(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "11 その他", "GeneralBinder と ComponentSetter。任意コンポーネントのプロパティを更新する。", out var c);
		screen.AddComponent<View>();
		screen.AddComponent<MiscScreen>();

		Section(c.transform, "GeneralBinder（任意のプロパティ）");
		var r1 = Row(c.transform); Label(r1.transform, "対象 Text.text へ直接反映");
		var target = New("Target", r1.transform);
		AddText(target, "-", 23, Ink, TextAnchor.MiddleLeft);
		Size(target, 320, 0);
		var gb = target.AddComponent<GeneralBinder>();
		SetPath(gb, "GeneralText");
		SetRef(gb, "m_Target", target.GetComponent<UnityEngine.UI.Text>());
		SetProp(gb, "m_Property", "text");

		Section(c.transform, "AnchoredPositionBinder + 操作");
		var r2 = Row(c.transform, "Row_Move", 90); Label(r2.transform, "AnchoredPositionBinder (AnchoredPosition)");
		var host = New("Host", r2.transform); Size(host, 320, 80);
		var mover = New("Mover", host.transform); AddImage(mover, Accent, false);
		//端アンカーのまま置く。Position3DBinder ではここで結果が安定しない（TASK-027）。
		//AnchoredPositionBinder は親の幅に依存しないため成立する
		var mr = Rect(mover); mr.anchorMin = new Vector2(0f, 0.5f); mr.anchorMax = new Vector2(0f, 0.5f);
		mr.sizeDelta = new Vector2(48, 48); mr.anchoredPosition = new Vector2(40, 0);
		SetPath(mover.AddComponent<AnchoredPositionBinder>(), "AnchoredPosition");
		Button(r2.transform, "Move", "OnMove", 150);

		screens.Add(screen);
	}

	// ---------- private フィールド設定 ----------

	static void SetPrivate(object target, string field, object value)
	{
		var f = target.GetType().GetField(field,
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		if (f != null) f.SetValue(target, value);
	}

	static void SetManagedArray(Component c, string prop, object[] values)
	{
		var so = new SerializedObject(c);
		var p = so.FindProperty(prop);
		p.arraySize = values.Length;
		for (int i = 0; i < values.Length; i++)
		{
			p.GetArrayElementAtIndex(i).managedReferenceValue = values[i];
		}
		so.ApplyModifiedPropertiesWithoutUndo();
	}
}
