using System;
using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.Showcase
{
	//画面 2「入力（双方向）」
	public class InputViewModel : ViewModel
	{
		[Bind] public string TextValue { get; set; } = "";
		[Bind] public string TextEcho { get; set; } = "(未入力)";
		[Bind] public float SliderValue { get; set; } = 0.4f;
		[Bind] public string SliderEcho { get; set; } = "0.40";
		[Bind] public bool ToggleValue { get; set; } = true;
		[Bind] public string ToggleEcho { get; set; } = "ON";
		[Bind] public float ScrollbarValue { get; set; } = 0.25f;
		[Bind] public int DropdownIndex { get; set; }
		[Bind] public string DropdownEcho { get; set; } = "0";
		[Bind] public bool IsInteractable { get; set; } = true;
		[Event] public Action OnToggleInteractable { get; set; }
	}

	//画面 3「コレクション」
	public class CollectionViewModel : ViewModel
	{
		[Bind] public Collection<CollectionRowViewModel> Rows { get; private set; }
		[Bind] public string Summary { get; set; } = "0 件";
		[Event] public Action OnAdd { get; set; }
		[Event] public Action OnRemove { get; set; }
		[Event] public Action OnClear { get; set; }
	}

	public class CollectionRowViewModel : ViewModel
	{
		[Bind] public string Label { get; set; } = "";
		[Event] public Action<int> OnPick { get; set; }
	}

	//画面 4「Converter」
	public class ConverterViewModel : ViewModel
	{
		[Bind] public int IntValue { get; set; } = 3;
		[Bind] public float FloatValue { get; set; } = 3f;
		[Bind] public bool BoolValue { get; set; } = true;
		[Bind] public string StringValue { get; set; } = "UVMBinding";
		[Event] public Action OnIncrement { get; set; }
		[Event] public Action OnToggle { get; set; }
	}

	//画面 5「派生値」
	public class ProviderViewModel : ViewModel
	{
		[Bind] public bool FlagA { get; set; } = true;
		[Bind] public bool FlagB { get; set; } = true;
		[Bind] public string Name { get; set; } = "Player";
		[Bind] public int Level { get; set; } = 12;
		[Event] public Action OnToggleA { get; set; }
		[Event] public Action OnToggleB { get; set; }
	}

	//画面 6「View 合成」
	public class CompositeViewModel : ViewModel
	{
		[Bind] public string Title { get; set; } = "親 ViewModel";
		[Bind] public ChildViewModel Child { get; set; }
		[Event] public Action OnSwap { get; set; }
	}

	public class ChildViewModel : ViewModel
	{
		[Bind] public string ChildText { get; set; } = "子 A";
	}

	//画面 7「ViewModel 種別」
	public class VariantViewModel : ViewModel
	{
		[Bind] public Property<string> Tracked { get; private set; }
		[Bind] public string DirtyTarget { get; set; } = "SetDirty で強制更新";
		[Event] public Action OnSetDirty { get; set; }
		[Event] public Action OnChangeTracked { get; set; }
	}

	//ReflectionViewModel 用の素の POCO
	public class ReflectionSource
	{
		public string Title = "ReflectionViewModel";
		public int Count = 7;
	}

	//画面 8「演出系 Binder」
	public class EffectViewModel : ViewModel
	{
		[Bind] public bool Visible { get; set; } = true;
		[Bind] public float Alpha { get; set; } = 1f;
		[Bind] public float Fill { get; set; } = 0.6f;
		[Bind] public Color Tint { get; set; } = Color.white;
		[Bind] public Vector3 Offset { get; set; } = Vector3.zero;
		[Bind] public Vector3 Scale { get; set; } = Vector3.one;
		[Event] public Action OnToggleVisible { get; set; }
		[Event] public Action OnRandomize { get; set; }
	}

	//画面 9「Event と引数」
	public class EventViewModel : ViewModel
	{
		[Bind] public string Log { get; set; } = "(イベント待ち)";
		[Bind] public string InputText { get; set; } = "";
		[Event] public Action OnPlain { get; set; }
		[Event] public Action<string> OnWithText { get; set; }
		[Event] public Action<Color> OnWithColor { get; set; }
	}

	//画面 10「任意パッケージ」
	public class OptionalViewModel : ViewModel
	{
		[Bind] public string TmpText { get; set; } = "TextMeshPro";
		[Bind] public string LocalizedText { get; set; } = "Localization";
		[Bind] public string Availability { get; set; } = "";
	}

	//画面 11「その他」
	public class MiscViewModel : ViewModel
	{
		[Bind] public string GeneralText { get; set; } = "GeneralBinder";
		[Bind] public Vector3 Position { get; set; } = Vector3.zero;
		[Event] public Action OnMove { get; set; }
	}
}
