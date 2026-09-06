using System;
using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.Showcase
{
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

	public class CollectionViewModel : ViewModel
	{
		[Bind] public Collection<CollectionRowViewModel> Rows { get; private set; }
		[Bind] public string Summary { get; set; } = "0 件";
		[Event, Consequence(ConsequenceKind.Safe)] public Action OnAdd { get; set; }
		[Event, Consequence(ConsequenceKind.Destructive)] public Action OnRemove { get; set; }
		[Event, Consequence(ConsequenceKind.Destructive)] public Action OnClear { get; set; }
	}

	public class CollectionRowViewModel : ViewModel
	{
		[Bind] public string Label { get; set; } = "";
		[Event] public Action<int> OnPick { get; set; }
	}

	public class ConverterViewModel : ViewModel
	{
		[Bind] public int IntValue { get; set; } = 3;
		[Bind] public float FloatValue { get; set; } = 3f;
		[Bind] public byte ByteValue { get; set; } = 1;
		[Bind] public bool BoolValue { get; set; } = true;
		[Bind] public string StringValue { get; set; } = "UVMBinding";
		[Event] public Action OnIncrement { get; set; }
		[Event] public Action OnToggle { get; set; }
	}

	public class ProviderViewModel : ViewModel
	{
		[Bind] public bool FlagA { get; set; } = true;
		[Bind] public bool FlagB { get; set; } = true;
		[Bind] public string Name { get; set; } = "Player";
		[Bind] public int Level { get; set; } = 12;
		[Event] public Action OnToggleA { get; set; }
		[Event] public Action OnToggleB { get; set; }
	}

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

	public class VariantViewModel : ViewModel
	{
		[Bind] public Property<string> Tracked { get; private set; }
		[Bind] public string DirtyTarget { get; set; } = "SetDirty で強制更新";
		[Event] public Action OnSetDirty { get; set; }
		[Event] public Action OnChangeTracked { get; set; }
	}

	public class ReflectionSource
	{
		public string Title = "ReflectionViewModel";
		public int ItemCount = 7;
	}

	public class EffectViewModel : ViewModel
	{
		[Bind] public bool Visible { get; set; } = true;
		[Bind] public float Alpha { get; set; } = 1f;
		[Bind] public float Fill { get; set; } = 0.6f;
		[Bind] public Color Tint { get; set; } = Color.white;
		[Bind] public Vector3 Offset { get; set; } = Vector3.zero;
		[Bind] public Vector3 Scale { get; set; } = Vector3.one;
		[Bind] public Quaternion Rotation { get; set; } = Quaternion.identity;
		[Event] public Action OnToggleVisible { get; set; }
		[Event] public Action OnRandomize { get; set; }
	}

	public class EventViewModel : ViewModel
	{
		[Bind] public string Log { get; set; } = "(イベント待ち)";
		[Bind] public string InputText { get; set; } = "";
		[Event] public Action OnPlain { get; set; }
		[Event] public Action<string> OnWithText { get; set; }
		[Event] public Action<Color> OnWithColor { get; set; }
	}

	public class OptionalViewModel : ViewModel
	{
		[Bind] public string TmpText { get; set; } = "TextMeshPro";
		[Bind] public string LocalizedText { get; set; } = "Localization";
		[Bind] public string Availability { get; set; } = "";
		[Bind] public string CurrentLocale { get; set; } = "-";
		[Event] public Action OnSwitchLocale { get; set; }
#if UVMB_LOCALIZATION
		[Bind] public Binders.LocalizeTextKey TableEntry { get; set; }
		[Bind] public Binders.LocalizeTextKey Parameterized { get; set; }
		[Bind] public Binders.LocalizeTextKey RawText { get; set; }
#endif
	}

	public class MiscViewModel : ViewModel
	{
		[Bind] public string GeneralText { get; set; } = "GeneralBinder";
		[Bind] public Vector2 AnchoredPosition { get; set; } = new Vector2(40f, 0f);
		[Bind] public ShowcaseEnum EnumValue { get; set; } = ShowcaseEnum.Beta;
		[Bind] public string EnumEcho { get; set; } = "Beta";
		[Event] public Action OnMove { get; set; }
	}
}
