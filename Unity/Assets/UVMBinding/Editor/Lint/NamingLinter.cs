using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Lint
{
	public static class NamingLinter
	{
		public static void LintEventName(IViewEvent viewEvent, string assetPath, string objectPath, LintSettings settings, LintReport report)
		{
			if (viewEvent == null || string.IsNullOrEmpty(viewEvent.Name))
			{
				return;
			}
			if (viewEvent.EventType() != null)
			{
				report.CheckedNameCount++;
				return;
			}
			LintName(viewEvent.Name, assetPath, objectPath, settings, report);
		}

		public static void LintName(string name, string assetPath, string objectPath, LintSettings settings, LintReport report)
		{
			if (string.IsNullOrEmpty(name))
			{
				return;
			}
			report.CheckedNameCount++;

			if (name.Length <= settings.MinNameLength - 1)
			{
				report.Add(new LintViolation(assetPath, objectPath, LintRule.TooShortName,
					$"Name is too short to carry meaning: {name}"));
				return;
			}
			var stem = GetStem(name);
			foreach (var word in settings.GenericWords)
			{
				if (string.Equals(stem, word, System.StringComparison.OrdinalIgnoreCase))
				{
					report.Add(new LintViolation(assetPath, objectPath, LintRule.GenericName,
						$"Name stem is a generic word: {name} (stem {stem})"));
					return;
				}
			}
		}

		public static string GetStem(string name)
		{
			int end = name.Length;
			while (end > 0 && char.IsDigit(name[end - 1]))
			{
				end--;
			}
			var stem = name.Substring(0, end).TrimEnd('_');
			if (stem.Length > 2 && stem.StartsWith("On") && char.IsUpper(stem[2]))
			{
				stem = stem.Substring(2);
			}
			return stem;
		}

		public static bool IsInteractive(SemanticRole role)
		{
			switch (role)
			{
				case SemanticRole.Button:
				case SemanticRole.Link:
				case SemanticRole.TextBox:
				case SemanticRole.SearchBox:
				case SemanticRole.Checkbox:
				case SemanticRole.Switch:
				case SemanticRole.Slider:
				case SemanticRole.Scrollbar:
				case SemanticRole.ComboBox:
				case SemanticRole.Tab:
					return true;
				default:
					return false;
			}
		}

		public static bool HasLabel(Component element)
		{
			if (element == null)
			{
				return false;
			}
			foreach (var component in element.GetComponentsInChildren<Component>(true))
			{
				if (component == null || ReferenceEquals(component, element))
				{
					continue;
				}
				if (component is IBinderInfo binder && !string.IsNullOrWhiteSpace(binder.GetDisplayText()))
				{
					return true;
				}
				if (!string.IsNullOrWhiteSpace(ReadText(component)))
				{
					return true;
				}
			}
			return false;
		}

		static readonly Dictionary<System.Type, System.Reflection.PropertyInfo> s_TextProperties
			= new Dictionary<System.Type, System.Reflection.PropertyInfo>();

		static string ReadText(Component component)
		{
			var type = component.GetType();
			if (!s_TextProperties.TryGetValue(type, out var property))
			{
				property = type.GetProperty("text", typeof(string));
				s_TextProperties[type] = property;
			}
			if (property == null || !property.CanRead)
			{
				return null;
			}
			return property.GetValue(component) as string;
		}
	}
}
