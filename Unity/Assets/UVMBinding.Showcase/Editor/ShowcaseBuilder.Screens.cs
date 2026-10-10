using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UVMBinding;
using UVMBinding.Arguments;
using UVMBinding.Binders;
using UVMBinding.Converters;
using UVMBinding.Events;
using UVMBinding.Showcase;

public static partial class ShowcaseBuilder
{
	static void BuildBasic(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "01 基本バインド", "表示系 Binder。Text / Image / RawImage / Active / Enabled。", out var c);
		AddView<BasicBindingViewModel>(screen);
		var driver = screen.AddComponent<BasicBindingScreen>();
		var icon = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
		SetRef(driver, "m_Icon", icon);
		SetRef(driver, "m_Preview", icon != null ? icon.texture : null);

		Section(c.transform, "テキスト");
		var r1 = Row(c.transform); Label(r1.transform, "TextBinder (Message)"); Value(r1.transform, "Message", 460);
		var r2 = Row(c.transform); Label(r2.transform, "TextBinder (CounterText)"); Value(r2.transform, "CounterText", 220);
		Button(r2.transform, "Increment", "OnIncrement");

		Section(c.transform, "画像");
		var r3 = Row(c.transform, "Row_AvatarIcon", 88); Label(r3.transform, "ImageSpriteBinder (AvatarIcon)");
		var iconGo = New("AvatarIcon", r3.transform); AddImage(iconGo, Color.white, false); Size(iconGo, 76, 76);
		SetPath(iconGo.AddComponent<ImageSpriteBinder>(), "AvatarIcon");

		var r4 = Row(c.transform, "Row_Preview", 88); Label(r4.transform, "RawImageTextureBinder (Preview)");
		var raw = New("Preview", r4.transform); raw.AddComponent<RawImage>().raycastTarget = false; Size(raw, 132, 76);
		SetPath(raw.AddComponent<RawImageTextureBinder>(), "Preview");

		Section(c.transform, "表示・活性");
		var r5 = Row(c.transform, "Row_Active", 64); Label(r5.transform, "ActiveBinder (ShowPanel)");
		var panel = New("TogglePanel", r5.transform); AddImage(panel, Panel, false); Size(panel, 260, 56);
		var pt = New("Text", panel.transform); Stretch(pt); AddText(pt, "表示中", 22, Ink, TextAnchor.MiddleCenter);
		SetPath(panel.AddComponent<ActiveBinder>(), "ShowPanel");
		Button(r5.transform, "Toggle Panel", "OnTogglePanel");

		var r6 = Row(c.transform); Label(r6.transform, "InteractableBinder (IsEnabled)");
		var tgt = Button(r6.transform, "対象ボタン", null);
		SetPath(tgt.AddComponent<InteractableBinder>(), "IsEnabled");
		Button(r6.transform, "Toggle Enabled", "OnToggleEnabled");

		screens.Add(screen);
	}

	static void BuildInput(Transform parent, List<GameObject> screens)
	{
		var screen = Screen(parent, "02 入力（双方向）", "入力系 Binder。View の変更が ViewModel に戻ることを確認する。", out var c);
		AddView<InputViewModel>(screen);
		screen.AddComponent<InputScreen>();

		Section(c.transform, "テキスト入力");
		var r1 = Row(c.transform, "Row_Input", 56); Label(r1.transform, "InputFieldBinder (TextValue)");
		var input = New("InputField", r1.transform); AddImage(input, Panel, true); Size(input, 320, 48);
		var text = New("Text", input.transform); Stretch(text);
		var t = AddText(text, "", 20, Ink, TextAnchor.MiddleLeft); t.supportRichText = false;
		var ph = New("Placeholder", input.transform); Stretch(ph); AddText(ph, "入力してください", 20, Sub, TextAnchor.MiddleLeft);
		var f = input.AddComponent<InputField>(); f.textComponent = t; f.placeholder = ph.GetComponent<UnityEngine.UI.Text>();
		SetPath(input.AddComponent<InputFieldBinder>(), "TextValue");
		Value(r1.transform, "TextEcho", 260);

		Section(c.transform, "スライダー / トグル");
		var r2 = Row(c.transform, "Row_Slider", 56); Label(r2.transform, "SliderBinder (SliderValue)");
		var slider = BuildSlider(r2.transform, out var sliderComp); SetPath(slider.AddComponent<SliderBinder>(), "SliderValue");
		Value(r2.transform, "SliderEcho", 160);

		var r3 = Row(c.transform, "Row_Toggle", 52); Label(r3.transform, "ToggleBinder (ToggleValue)");
		var toggle = New("Toggle", r3.transform); Size(toggle, 56, 44);
		var bgGo = New("Background", toggle.transform); Stretch(bgGo); AddImage(bgGo, Panel, true);
		var check = New("Checkmark", bgGo.transform); Stretch(check); AddImage(check, Accent, false);
		var tg = toggle.AddComponent<Toggle>(); tg.targetGraphic = bgGo.GetComponent<UnityEngine.UI.Image>(); tg.graphic = check.GetComponent<UnityEngine.UI.Image>();
		SetPath(toggle.AddComponent<ToggleBinder>(), "ToggleValue");
		Value(r3.transform, "ToggleEcho", 160);

		var r4 = Row(c.transform, "Row_Scrollbar", 52); Label(r4.transform, "ScrollbarBinder (ScrollbarValue)");
		var sb = New("Scrollbar", r4.transform); AddImage(sb, Panel, true); Size(sb, 300, 26);
		var sbArea = New("SlidingArea", sb.transform); Stretch(sbArea);
		var sbHandle = New("Handle", sbArea.transform); AddImage(sbHandle, Accent, true);
		var sbhr = Rect(sbHandle);
		sbhr.anchorMin = Vector2.zero; sbhr.anchorMax = Vector2.one;
		sbhr.sizeDelta = Vector2.zero;
		var sbc = sb.AddComponent<Scrollbar>(); sbc.handleRect = Rect(sbHandle); sbc.targetGraphic = sbHandle.GetComponent<UnityEngine.UI.Image>();
		SetPath(sb.AddComponent<ScrollbarBinder>(), "ScrollbarValue");

		Section(c.transform, "選択 / 活性");
		var r5 = Row(c.transform, "Row_Dropdown", 56); Label(r5.transform, "DropdownSelectBinder (DropdownIndex)");
		var dd = BuildDropdown(r5.transform); SetPath(dd.AddComponent<DropdownSelectBinder>(), "DropdownIndex");
		Value(r5.transform, "DropdownEcho", 160);

		var r6 = Row(c.transform); Label(r6.transform, "InteractableBinder (IsInteractable)");
		var target = Button(r6.transform, "対象ボタン", null);
		SetPath(target.AddComponent<InteractableBinder>(), "IsInteractable");
		Button(r6.transform, "Toggle", "OnToggleInteractable", 160);

		screens.Add(screen);
	}

	static GameObject BuildSlider(Transform parent, out Slider slider)
	{
		const float HandleWidth = 24f;
		var go = New("Slider", parent); Size(go, 300, 32);
		var bg = New("Background", go.transform); Stretch(bg); AddImage(bg, Panel, true);

		var fillArea = New("Fill Area", go.transform);
		var far = Rect(fillArea);
		far.anchorMin = Vector2.zero; far.anchorMax = Vector2.one;
		far.offsetMin = new Vector2(HandleWidth * 0.5f, 0f);
		far.offsetMax = new Vector2(-HandleWidth * 0.5f, 0f);
		var fill = New("Fill", fillArea.transform); AddImage(fill, Accent, false);
		var fr = Rect(fill);
		fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
		fr.sizeDelta = Vector2.zero;

		var handleArea = New("Handle Slide Area", go.transform);
		var har = Rect(handleArea);
		har.anchorMin = Vector2.zero; har.anchorMax = Vector2.one;
		har.offsetMin = new Vector2(HandleWidth * 0.5f, 0f);
		har.offsetMax = new Vector2(-HandleWidth * 0.5f, 0f);
		var handle = New("Handle", handleArea.transform); AddImage(handle, Ink, true);
		var hr = Rect(handle);
		hr.anchorMin = new Vector2(0f, 0f); hr.anchorMax = new Vector2(0f, 1f);
		hr.pivot = new Vector2(0.5f, 0.5f);
		hr.sizeDelta = new Vector2(HandleWidth, 0f);

		slider = go.AddComponent<Slider>();
		slider.fillRect = Rect(fill);
		slider.handleRect = Rect(handle);
		slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
		slider.minValue = 0f; slider.maxValue = 1f;
		return go;
	}

	static GameObject BuildDropdown(Transform parent)
	{
		var go = New("Dropdown", parent); AddImage(go, Panel, true); Size(go, 260, 48);
		var label = New("Label", go.transform); Stretch(label);
		var lt = AddText(label, "Alpha", 20, Ink, TextAnchor.MiddleLeft);

		var template = New("Template", go.transform);
		var tr = Rect(template);
		tr.anchorMin = new Vector2(0f, 0f); tr.anchorMax = new Vector2(1f, 0f); tr.pivot = new Vector2(0.5f, 1f);
		tr.sizeDelta = new Vector2(0, 150);
		AddImage(template, Panel, true);
		template.SetActive(false);
		var viewport = New("Viewport", template.transform); Stretch(viewport);
		AddImage(viewport, new Color(1, 1, 1, 0.004f), true); viewport.AddComponent<RectMask2D>();
		var content = New("Content", viewport.transform);
		var cr = Rect(content); cr.anchorMin = new Vector2(0f, 1f); cr.anchorMax = new Vector2(1f, 1f); cr.pivot = new Vector2(0.5f, 1f); cr.sizeDelta = new Vector2(0, 40);
		var item = New("Item", content.transform);
		var ir = Rect(item); ir.anchorMin = new Vector2(0f, 0.5f); ir.anchorMax = new Vector2(1f, 0.5f); ir.sizeDelta = new Vector2(0, 40);
		var itemBg = New("Item Background", item.transform); Stretch(itemBg); AddImage(itemBg, Panel, true);
		var itemLabel = New("Item Label", item.transform); Stretch(itemLabel);
		var ilt = AddText(itemLabel, "Option", 20, Ink, TextAnchor.MiddleLeft);
		var itemToggle = item.AddComponent<Toggle>(); itemToggle.targetGraphic = itemBg.GetComponent<UnityEngine.UI.Image>();
		var sr = template.AddComponent<ScrollRect>(); sr.content = cr; sr.viewport = Rect(viewport); sr.horizontal = false;

		var dd = go.AddComponent<Dropdown>();
		dd.captionText = lt; dd.template = tr; dd.itemText = ilt;
		dd.options = new List<Dropdown.OptionData>
		{
			new Dropdown.OptionData("Alpha"),
			new Dropdown.OptionData("Beta"),
			new Dropdown.OptionData("Gamma"),
		};
		return go;
	}
}
