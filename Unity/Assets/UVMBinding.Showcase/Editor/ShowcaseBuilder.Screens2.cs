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
	static void BuildCollection(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "03 コレクション", "CollectionBinder と Collection<T>。要素の追加・削除と CollectionIndexArgument。", out var c);
		AddView<CollectionViewModel>(screen);
		screen.AddComponent<CollectionScreen>();

		var ops = Row(c.transform, "Row_Ops", 56);
		Label(ops.transform, "操作");
		Button(ops.transform, "Add", "OnAdd", 140);
		Button(ops.transform, "Remove", "OnRemove", 140);
		Button(ops.transform, "Clear", "OnClear", 140);
		Value(ops.transform, "Summary", 300);

		Section(c.transform, "CollectionBinder (Rows)");
		var list = New("List", c.transform);
		var vlg = list.AddComponent<VerticalLayoutGroup>();
		vlg.spacing = 6; vlg.childControlWidth = true; vlg.childControlHeight = true;
		vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
		Size(list, 0, 60);
		var fitter = list.AddComponent<ContentSizeFitter>();
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		var item = New("RowTemplate", screen.transform);
		AddView<CollectionRowViewModel>(item);
		AddImage(item, Panel, true);
		Size(item, 520, 44);
		var ih = item.AddComponent<HorizontalLayoutGroup>();
		ih.padding = new RectOffset(14, 14, 0, 0); ih.spacing = 12;
		ih.childAlignment = TextAnchor.MiddleLeft;
		ih.childControlWidth = true; ih.childControlHeight = true;
		ih.childForceExpandWidth = false; ih.childForceExpandHeight = false;
		var itemLabel = New("Label", item.transform);
		AddText(itemLabel, "-", 21, Ink, TextAnchor.MiddleLeft);
		Size(itemLabel, 300, 0);
		SetPath(itemLabel.AddComponent<TextBinder>(), "Label");
		var pick = Button(item.transform, "Pick", "OnPick", 120);
		var ev = pick.GetComponent<EventButton>();
		SetManagedRef(ev, "m_Argument", new CollectionIndexArgument());
		item.SetActive(false);

		var binder = list.AddComponent<CollectionBinder>();
		SetPath(binder, "Rows");
		SetRef(binder, "m_Preafb", item);

		screens.Add(screen);
	}

	static void BuildConverter(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "04 Converter", "Binder に設定する変換。Cast / Comparison / BoolSwitch / Concatenate ほか。", out var c);
		AddView<ConverterViewModel>(screen);
		screen.AddComponent<ConverterScreen>();

		var ops = Row(c.transform, "Row_Ops", 56);
		Label(ops.transform, "元の値");
		var opsValue = New("Value", ops.transform);
		AddText(opsValue, "-", 23, Ink, TextAnchor.MiddleLeft);
		Size(opsValue, 120, 0);
		var opsBinder = opsValue.AddComponent<TextBinder>();
		SetPath(opsBinder, "IntValue");
		SetConverters(opsBinder, new ObjectToString());
		Button(ops.transform, "Increment", "OnIncrement", 160);
		Button(ops.transform, "Toggle Bool", "OnToggle", 160);

		Section(c.transform, "数値の型合わせ");
		var rAuto = Row(c.transform, "Row_ImplicitCast", 60);
		Label(rAuto.transform, "自動挿入 : byte → float（Converter 無し）", 420);
		var autoFill = New("Fill", rAuto.transform);
		var afi = AddImage(autoFill, Accent, false);
		afi.type = UnityEngine.UI.Image.Type.Filled;
		afi.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
		Size(autoFill, 240, 40);
		var autoBinder = autoFill.AddComponent<ImageFillAmountBinder>();
		SetPath(autoBinder, "ByteValue");

		var rCast = Row(c.transform, "Row_Cast", 60);
		Label(rCast.transform, "AllowCast : int → float（許可が要る）", 420);
		var castFill = New("Fill", rCast.transform);
		var cfi = AddImage(castFill, Accent, false);
		cfi.type = UnityEngine.UI.Image.Type.Filled;
		cfi.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
		Size(castFill, 240, 40);
		var castBinder = castFill.AddComponent<ImageFillAmountBinder>();
		SetPath(castBinder, "IntValue");
		SetAllowCast(castBinder, true);

		Section(c.transform, "比較・真偽");
		AddConverterRow(c.transform, "IntComparison : int → bool", "IntValue", new IntComparison());
		AddConverterRow(c.transform, "BoolInverse : bool → bool", "BoolValue", new BoolInverse());
		AddConverterRow(c.transform, "StringComparison : string → bool", "StringValue", new StringComparison());

		Section(c.transform, "文字列");
		AddConverterRow(c.transform, "ObjectToString : int → string", "IntValue", new ObjectToString());
		AddConverterRow(c.transform, "StringToLength : string → int", "StringValue", new StringToLength());

		Section(c.transform, "視覚効果（bool → 値）");
		var rSwitch = Row(c.transform, "Row_BoolSwitch", 52);
		Label(rSwitch.transform, "BoolSwitchTweenColor (BoolValue)");
		var sw = New("Target", rSwitch.transform); AddImage(sw, Color.white, false); Size(sw, 200, 40);
		var swBinder = sw.AddComponent<GraphicColorBinder>();
		SetPath(swBinder, "BoolValue");
		SetConverters(swBinder, new BoolSwitchTweenColor());

		var rDelay = Row(c.transform, "Row_Delay", 52);
		Label(rDelay.transform, "DelaySetBoolConverter (BoolValue)");
		var delayGo = New("Value", rDelay.transform); AddText(delayGo, "-", 23, Ink, TextAnchor.MiddleLeft); Size(delayGo, 240, 0);
		var delayBinder = delayGo.AddComponent<TextBinder>();
		SetPath(delayBinder, "BoolValue");
		SetConverters(delayBinder, new DelaySetBoolConverter(), new ObjectToString());

		Section(c.transform, "合成");
		var rConcat = Row(c.transform, "Row_Concat", 52);
		Label(rConcat.transform, "Concatenate : string → int → string");
		var concatGo = New("Value", rConcat.transform); AddText(concatGo, "-", 23, Ink, TextAnchor.MiddleLeft); Size(concatGo, 300, 0);
		var concatBinder = concatGo.AddComponent<TextBinder>();
		SetPath(concatBinder, "StringValue");
		SetConverters(concatBinder, new StringToLength(), new ObjectToString());

		screens.Add(screen);
	}

	static void AddConverterRow(Transform parent, string label, string path, object converter)
	{
		var row = Row(parent, "Row_" + label, 46);
		Label(row.transform, label, 420);
		var go = New("Value", row.transform);
		AddText(go, "-", 23, Ink, TextAnchor.MiddleLeft);
		Size(go, 260, 0);
		var binder = go.AddComponent<TextBinder>();
		SetPath(binder, path);
		if (converter is ObjectToString)
		{
			SetConverters(binder, converter);
		}
		else
		{
			SetConverters(binder, converter, new ObjectToString());
		}
	}

}
