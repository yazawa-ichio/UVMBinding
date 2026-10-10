using System.Collections.Generic;

namespace UVMBinding.Sample
{
	public enum Difficulty
	{
		Easy,
		Normal,
		Hard,
	}

	public class DifficultyRule
	{
		public readonly Difficulty Level;
		public readonly string Label;
		public readonly string Summary;
		public readonly int EncounterScale;
		public readonly int Deadline;

		DifficultyRule(Difficulty level, string label, string summary, int encounterScale, int deadline)
		{
			Level = level;
			Label = label;
			Summary = summary;
			EncounterScale = encounterScale;
			Deadline = deadline;
		}

		public static IReadOnlyList<DifficultyRule> All { get; } = new[]
		{
			new DifficultyRule(Difficulty.Easy, "気楽な旅", "敵は少ない。日数にも余裕がある", 60, 35),
			new DifficultyRule(Difficulty.Normal, "ふつうの旅", "ほどよく敵が出る", 100, 30),
			new DifficultyRule(Difficulty.Hard, "険しい旅", "敵が多い。日数も足りない", 140, 24),
		};

		public static DifficultyRule Of(Difficulty level)
		{
			foreach (var rule in All)
			{
				if (rule.Level == level) return rule;
			}
			return All[1];
		}
	}
}
