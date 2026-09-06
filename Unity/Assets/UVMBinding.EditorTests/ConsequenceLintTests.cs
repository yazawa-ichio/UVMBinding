using NUnit.Framework;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class ConsequenceLintTests
	{
		class ShopViewModel : ViewModel
		{
			[Event] public System.Action OnBuyItem { get; set; }
			[Event, Consequence(ConsequenceKind.Destructive)] public System.Action OnClearBag { get; set; }
			[Event] public System.Action OnOpen { get; set; }
		}

		static LintReport Run()
		{
			var report = new LintReport();
			report.AddDispatchedEvent(typeof(ShopViewModel), "OnBuyItem");
			ViewLinter.LintConsequences(report);
			return report;
		}

		[Test]
		public void 名前が結果を匂わせるのに注釈が無ければ警告になる()
		{
			var report = Run();

			Assert.AreEqual(1, report.Warnings.Count, report.Summarize());
			Assert.AreEqual(LintRule.MissingConsequence, report.Warnings[0].Rule);
			StringAssert.Contains("OnBuyItem", report.Warnings[0].Detail);
		}

		[Test]
		public void 違反ではなく警告として出る()
		{
			Assert.IsFalse(Run().HasViolation);
		}
	}
}
