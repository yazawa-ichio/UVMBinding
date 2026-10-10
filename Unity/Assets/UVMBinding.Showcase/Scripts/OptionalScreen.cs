using System;
using UnityEngine;
#if UVMB_LOCALIZATION
using UnityEngine.Localization.Settings;
using UVMBinding.Binders;
#endif

namespace UVMBinding.Showcase
{
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
				SetupLocalization(vm);
#endif
				vm.Availability = $"TMP : {tmp}   /   Localization : {loc}";
			});
		}

#if UVMB_LOCALIZATION
		const string DefaultCode = "en";

		void SetupLocalization(OptionalViewModel vm)
		{
			SelectLocale(DefaultCode);

			vm.TableEntry = LocalizeTextKey.GetEntry(ShowcaseLocalization.Table, ShowcaseLocalization.GreetingKey);
			vm.Parameterized = LocalizeTextKey
				.GetEntry(ShowcaseLocalization.Table, ShowcaseLocalization.PickupKey)
				.WithParam("count", 3);
			vm.RawText = LocalizeTextKey.GetText("生文字列（テーブルを引かない）");
			vm.CurrentLocale = Describe();

			vm.OnSwitchLocale = () =>
			{
				var next = LocalizationSettings.SelectedLocale?.Identifier.Code == DefaultCode ? "ja" : DefaultCode;
				SelectLocale(next);
				vm.SetDirty(nameof(vm.TableEntry));
				vm.SetDirty(nameof(vm.Parameterized));
				vm.CurrentLocale = Describe();
			};
		}

		static void SelectLocale(string code)
		{
			var locale = LocalizationSettings.AvailableLocales?.GetLocale(code);
			if (locale != null)
			{
				LocalizationSettings.SelectedLocale = locale;
			}
		}

		static string Describe()
		{
			var locale = LocalizationSettings.SelectedLocale;
			return locale != null ? locale.Identifier.Code : "-";
		}
#endif
	}
}
