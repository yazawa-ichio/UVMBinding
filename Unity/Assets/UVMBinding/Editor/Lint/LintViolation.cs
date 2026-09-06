using System.Collections.Generic;

namespace UVMBinding.Lint
{
	public enum LintRule
	{
		UnknownBindPath,
		UnknownSourcePath,
		UnknownEventName,
		IncompatibleTarget,
		UnconvertibleType,
		ConverterStageMismatch,
		DeclaredTypeMismatch,

		UnusedEventName,
		GenericName,
		TooShortName,
		MissingLabel,
		MissingConsequence,
	}

	public readonly struct LintViolation
	{
		public readonly string AssetPath;
		public readonly string ObjectPath;
		public readonly LintRule Rule;
		public readonly string Detail;
		public readonly UnityEngine.Object Source;

		public LintViolation(string assetPath, string objectPath, LintRule rule, string detail,
			UnityEngine.Object source = null)
		{
			AssetPath = assetPath;
			ObjectPath = objectPath;
			Rule = rule;
			Detail = detail;
			Source = source;
		}

		public override string ToString()
		{
			return $"{AssetPath}:{ObjectPath} [{Rule}] {Detail}";
		}
	}

	public readonly struct LintSkipped
	{
		public readonly string AssetPath;
		public readonly string ObjectPath;
		public readonly string Reason;

		public LintSkipped(string assetPath, string objectPath, string reason)
		{
			AssetPath = assetPath;
			ObjectPath = objectPath;
			Reason = reason;
		}

		public override string ToString()
		{
			return $"{AssetPath}:{ObjectPath} {Reason}";
		}
	}

	public class LintReport
	{
		public LintSettings Settings;
		public LintBaseline Baseline;

		public readonly Dictionary<System.Type, HashSet<string>> DispatchedEvents
			= new Dictionary<System.Type, HashSet<string>>();

		public readonly List<LintViolation> Violations = new List<LintViolation>();
		public readonly List<LintSkipped> Skipped = new List<LintSkipped>();
		public readonly List<LintSkipped> Ignored = new List<LintSkipped>();
		public readonly List<LintSkipped> Partial = new List<LintSkipped>();
		public readonly List<LintViolation> Warnings = new List<LintViolation>();
		public int CheckedNameCount;
		public int CheckedViewCount;
		public int CheckedProviderCount;
		public readonly List<LintViolation> Disabled = new List<LintViolation>();
		public readonly List<LintViolation> Baselined = new List<LintViolation>();
		public int ExcludedAssetCount;

		public bool HasViolation => Violations.Count > 0;

		public void Add(LintViolation violation)
		{
			if (Settings != null && !Settings.IsEnabled(violation.Rule))
			{
				Disabled.Add(violation);
				return;
			}
			if (Baseline != null && Baseline.Contains(violation))
			{
				Baselined.Add(violation);
				return;
			}
			(s_Warnings.Contains(violation.Rule) ? Warnings : Violations).Add(violation);
		}

		static readonly HashSet<LintRule> s_Warnings = new HashSet<LintRule>
		{
			LintRule.UnusedEventName,
			LintRule.GenericName,
			LintRule.TooShortName,
			LintRule.MissingLabel,
			LintRule.MissingConsequence,
		};

		public void AddDispatchedEvent(System.Type sourceType, string name)
		{
			if (sourceType == null || string.IsNullOrEmpty(name))
			{
				return;
			}
			if (!DispatchedEvents.TryGetValue(sourceType, out var names))
			{
				DispatchedEvents[sourceType] = names = new HashSet<string>();
			}
			names.Add(name);
		}

		static string Rate(int count, int total)
		{
			return total > 0 ? $"{count * 100f / total:0.0}%" : "-";
		}

		static readonly HashSet<LintRule> s_NamingRules = new HashSet<LintRule>
		{
			LintRule.GenericName,
			LintRule.TooShortName,
			LintRule.MissingLabel,
		};

		int CountNamingWarnings()
		{
			int count = 0;
			foreach (var warning in Warnings)
			{
				if (s_NamingRules.Contains(warning.Rule))
				{
					count++;
				}
			}
			return count;
		}

		public string Summarize()
		{
			return $"violations {Violations.Count} / warnings {Warnings.Count}"
				+ $" / ignored {Ignored.Count} / partial {Partial.Count} / skipped {Skipped.Count}"
				+ $" / disabled {Disabled.Count} / baselined {Baselined.Count}"
				+ $" / excluded {ExcludedAssetCount}"
				+ $" / checked View {CheckedViewCount} / Provider {CheckedProviderCount}"
				+ $" / event sources {DispatchedEvents.Count}"
				+ $" / names {CheckedNameCount} ({Rate(CountNamingWarnings(), CheckedNameCount)})";
		}
	}
}