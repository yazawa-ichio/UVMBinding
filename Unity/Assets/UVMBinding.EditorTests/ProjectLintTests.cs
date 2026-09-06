using NUnit.Framework;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class ProjectLintTests
	{
		[Test]
		public void プロジェクト全体に違反が無い()
		{
			var report = LintRunner.Run();

			if (report.HasViolation)
			{
				var lines = new System.Text.StringBuilder(report.Summarize());
				foreach (var violation in report.Violations)
				{
					lines.Append('\n').Append(violation);
				}
				Assert.Fail(lines.ToString());
			}
			UnityEngine.Debug.Log(report.Summarize());
		}
	}
}