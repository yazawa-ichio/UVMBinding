#if UVMB_LOCALIZATION
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class ShowcaseLocalizationBuilder
{
	const string Parent = "Assets/UVMBinding.Showcase";
	const string Root = Parent + "/Localization";
	const string TablesFolder = Root + "/Tables";

	const string TableName = UVMBinding.Showcase.ShowcaseLocalization.Table;
	const string GreetingKey = UVMBinding.Showcase.ShowcaseLocalization.GreetingKey;
	const string PickupKey = UVMBinding.Showcase.ShowcaseLocalization.PickupKey;

	[MenuItem("UVMBinding/Showcase/Build Localization")]
	public static void BuildAll()
	{
		EnsureFolder(Parent, "Localization");
		EnsureFolder(Root, "Tables");

		var english = EnsureLocale(SystemLanguage.English, "English");
		var japanese = EnsureLocale(SystemLanguage.Japanese, "Japanese");
		EnsureSettings();

		var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
		if (collection == null)
		{
			collection = LocalizationEditorSettings.CreateStringTableCollection(
				TableName, TablesFolder, new List<Locale> { english, japanese });
		}

		SetEntry(collection, english, GreetingKey, "Hello UVMBinding");
		SetEntry(collection, japanese, GreetingKey, "ようこそ UVMBinding");
		SetEntry(collection, english, PickupKey, "You picked up {count} items.", smart: true);
		SetEntry(collection, japanese, PickupKey, "{count} 個 拾いました。", smart: true);

		EditorUtility.SetDirty(collection);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log($"Showcase localization built. table={TableName} locales=en,ja");
	}

	static void EnsureFolder(string parent, string name)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + name))
		{
			AssetDatabase.CreateFolder(parent, name);
		}
	}

	static Locale EnsureLocale(SystemLanguage language, string fileName)
	{
		var identifier = new LocaleIdentifier(language);
		var locale = LocalizationEditorSettings.GetLocale(identifier);
		if (locale != null)
		{
			return locale;
		}
		var created = Locale.CreateLocale(language);
		AssetDatabase.CreateAsset(created, $"{Root}/{fileName}.asset");
		LocalizationEditorSettings.AddLocale(created);
		return created;
	}

	static void EnsureSettings()
	{
		if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
		{
			return;
		}
		var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
		AssetDatabase.CreateAsset(settings, $"{Root}/LocalizationSettings.asset");
		LocalizationEditorSettings.ActiveLocalizationSettings = settings;
	}

	static void SetEntry(LocalizationTableCollection collection, Locale locale, string key, string value,
		bool smart = false)
	{
		if (!(collection.GetTable(locale.Identifier) is StringTable table))
		{
			Debug.LogWarning($"table not found: {locale.Identifier}");
			return;
		}
		var entry = table.GetEntry(key) ?? table.AddEntry(key, value);
		entry.Value = value;
		entry.IsSmart = smart;
		EditorUtility.SetDirty(table);
		EditorUtility.SetDirty(table.SharedData);
	}
}
#endif
