using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	//各画面の駆動役。ViewModel を組み立てて View にアタッチする。

	[RequireComponent(typeof(View))]
	public class InputScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<InputViewModel>(vm =>
			{
				vm.OnToggleInteractable = () => vm.IsInteractable = !vm.IsInteractable;
				//双方向バインドの結果を別プロパティへ写して可視化する
				var p = ((Core.IViewModel)vm).Property;
				p.Subscribe<string>(nameof(InputViewModel.TextValue), x => vm.TextEcho = string.IsNullOrEmpty(x) ? "(未入力)" : x);
				p.Subscribe<float>(nameof(InputViewModel.SliderValue), x => vm.SliderEcho = x.ToString("0.00"));
				p.Subscribe<bool>(nameof(InputViewModel.ToggleValue), x => vm.ToggleEcho = x ? "ON" : "OFF");
				p.Subscribe<int>(nameof(InputViewModel.DropdownIndex), x => vm.DropdownEcho = x.ToString());
			});
		}
	}

	[RequireComponent(typeof(View))]
	public class CollectionScreen : MonoBehaviour
	{
		int m_Serial;

		void Start()
		{
			GetComponent<View>().Attach<CollectionViewModel>(vm =>
			{
				void Sync() => vm.Summary = $"{vm.Rows.Count} 件";

				void Add()
				{
					m_Serial++;
					var row = new CollectionRowViewModel { Label = $"Item {m_Serial:00}" };
					row.OnPick = index => vm.Summary = $"{vm.Rows.Count} 件 / 選択 {index}";
					vm.Rows.Add(row);
					Sync();
				}

				vm.OnAdd = Add;
				vm.OnRemove = () =>
				{
					if (vm.Rows.Count > 0)
					{
						vm.Rows.RemoveAt(vm.Rows.Count - 1);
						Sync();
					}
				};
				vm.OnClear = () => { vm.Rows.Clear(); Sync(); };

				for (int i = 0; i < 3; i++) Add();
			});
		}
	}

	[RequireComponent(typeof(View))]
	public class ConverterScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<ConverterViewModel>(vm =>
			{
				vm.OnIncrement = () =>
				{
					vm.IntValue++;
					vm.FloatValue = vm.IntValue;
				};
				vm.OnToggle = () => vm.BoolValue = !vm.BoolValue;
			});
		}
	}

	[RequireComponent(typeof(View))]
	public class ProviderScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<ProviderViewModel>(vm =>
			{
				vm.OnToggleA = () => vm.FlagA = !vm.FlagA;
				vm.OnToggleB = () => vm.FlagB = !vm.FlagB;
			});
		}
	}

	[RequireComponent(typeof(View))]
	public class CompositeScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<CompositeViewModel>(vm =>
			{
				bool a = true;
				vm.Child = new ChildViewModel { ChildText = "子 A" };
				vm.OnSwap = () =>
				{
					a = !a;
					vm.Child = new ChildViewModel { ChildText = a ? "子 A" : "子 B" };
				};
			});
		}
	}

	[RequireComponent(typeof(View))]
	public class VariantScreen : MonoBehaviour
	{
		[SerializeField]
		View m_ReflectionView;

		void Start()
		{
			int n = 0;
			GetComponent<View>().Attach<VariantViewModel>(vm =>
			{
				vm.Tracked.Value = "Property<string>";
				vm.OnChangeTracked = () => { n++; vm.Tracked.Value = $"Property<string> #{n}"; };
				vm.OnSetDirty = () => vm.SetDirty(nameof(VariantViewModel.DirtyTarget));
			});

			//素の POCO を ReflectionViewModel 経由でバインドする
			if (m_ReflectionView != null)
			{
				m_ReflectionView.Bind(new ReflectionSource());
			}
		}
	}

	[RequireComponent(typeof(View))]
	public class EffectScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<EffectViewModel>(vm =>
			{
				var rng = new System.Random(1);
				vm.OnToggleVisible = () => vm.Visible = !vm.Visible;
				vm.OnRandomize = () =>
				{
					vm.Alpha = 0.3f + (float)rng.NextDouble() * 0.7f;
					vm.Fill = (float)rng.NextDouble();
					vm.Tint = new Color((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble(), 1f);
					vm.Offset = new Vector3((float)(rng.NextDouble() - 0.5) * 120f, 0f, 0f);
					vm.Scale = Vector3.one * (0.6f + (float)rng.NextDouble() * 0.8f);
				};
			});
		}
	}

	[RequireComponent(typeof(View))]
	public class EventScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<EventViewModel>(vm =>
			{
				vm.OnPlain = () => vm.Log = "Action（引数なし）";
				vm.OnWithText = x => vm.Log = $"Action<string> : {x}";
				vm.OnWithColor = c => vm.Log = $"Action<Color> : {ColorUtility.ToHtmlStringRGB(c)}";
			});
		}
	}

	[RequireComponent(typeof(View))]
	public class OptionalScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<OptionalViewModel>(vm =>
			{
				string tmp = "未導入";
				string loc = "未導入";
#if UVMB_TMP
				tmp = "有効 (UVMB_TMP)";
#endif
#if UVMB_LOCALIZATION
				loc = "有効 (UVMB_LOCALIZATION)";
#endif
				vm.Availability = $"TMP : {tmp}   /   Localization : {loc}";
			});
		}
	}

	[RequireComponent(typeof(View))]
	public class MiscScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<MiscViewModel>(vm =>
			{
				bool right = false;
				vm.OnMove = () =>
				{
					right = !right;
					vm.Position = right ? new Vector3(160f, 0f, 0f) : Vector3.zero;
				};
			});
		}
	}
}
