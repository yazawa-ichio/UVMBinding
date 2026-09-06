using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UVMBinding.Lint
{
	[CreateAssetMenu(fileName = "LintSettings", menuName = "UVMBinding/Lint Settings")]
	public class LintSettings : ScriptableObject
	{
		[SerializeField]
		List<string> m_ExcludeFolders = new List<string>();

		[SerializeField]
		List<LintRule> m_DisabledRules = new List<LintRule>();

		[SerializeField]
		LintBaseline m_Baseline = default;

		[SerializeField]
		List<string> m_GenericWords = new List<string>(DefaultGenericWords);

		[SerializeField]
		int m_MinNameLength = 3;

		[SerializeField]
		bool m_CheckMissingLabel = false;

		public static readonly string[] DefaultGenericWords =
		{
			"Button", "Btn", "Value", "Val", "Item", "Text", "Str", "Obj", "Object",
			"Panel", "Image", "Img", "Icon", "Data", "Info", "Flag", "Num", "Count",
			"Element", "Entry", "Node", "Target", "Temp", "Tmp", "Test", "Dummy",
			"Click", "Press", "Push", "Select", "Change", "Update", "Set", "Get",
		};

		public IReadOnlyList<string> GenericWords => m_GenericWords;

		public int MinNameLength => m_MinNameLength;

		public bool CheckMissingLabel => m_CheckMissingLabel;

		public LintBaseline Baseline => m_Baseline;

		public IReadOnlyList<string> ExcludeFolders => m_ExcludeFolders;

		public bool IsEnabled(LintRule rule)
		{
			return !m_DisabledRules.Contains(rule);
		}

		public bool IsExcluded(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				return false;
			}
			foreach (var folder in m_ExcludeFolders)
			{
				if (string.IsNullOrWhiteSpace(folder))
				{
					continue;
				}
				var prefix = folder.EndsWith("/") ? folder : folder + "/";
				if (assetPath.StartsWith(prefix, System.StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		public static LintSettings Find()
		{
			foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(LintSettings)))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var settings = AssetDatabase.LoadAssetAtPath<LintSettings>(path);
				if (settings != null)
				{
					return settings;
				}
			}
			return CreateInstance<LintSettings>();
		}
	}
}
