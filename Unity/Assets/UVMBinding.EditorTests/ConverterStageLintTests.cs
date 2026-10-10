using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UVMBinding.Converters;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class ConverterStageLintTests
	{
		public class StageViewModel : ViewModel
		{
			[Bind] public string Label { get; set; }
			[Bind] public int Amount { get; set; }
		}

		public class TextRecordBinder : Binder<string>
		{
			protected override void UpdateValue(string value) { }
		}

		public class BoolRecordBinder : Binder<bool>
		{
			protected override void UpdateValue(bool value) { }
		}

		GameObject m_Root;

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null)
			{
				Object.DestroyImmediate(m_Root);
				m_Root = null;
			}
		}

		View CreateView()
		{
			m_Root = new GameObject("View");
			var view = m_Root.AddComponent<View>();
			var so = new UnityEditor.SerializedObject(view);
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = typeof(StageViewModel).FullName;
			so.ApplyModifiedPropertiesWithoutUndo();
			return view;
		}

		TBinder AddBinder<TBinder, TValue>(View view, string path, params object[] converters)
			where TBinder : Binder<TValue>
		{
			var go = new GameObject("Element");
			go.transform.SetParent(view.transform, false);
			var binder = go.AddComponent<TBinder>();
			binder.ChangePath(path);
			var field = typeof(Binder<TValue>).GetField("m_Converters",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			var elementType = field.FieldType.GetElementType();
			var array = System.Array.CreateInstance(elementType, converters.Length);
			for (int i = 0; i < converters.Length; i++)
			{
				array.SetValue(converters[i], i);
			}
			field.SetValue(binder, array);
			return binder;
		}

		static List<LintViolation> StageViolations(LintReport report)
		{
			return report.Violations.FindAll(x => x.Rule == LintRule.ConverterStageMismatch);
		}

		[Test]
		public void 段間が繋がる組は違反にしない()
		{
			var view = CreateView();
			AddBinder<BoolRecordBinder, bool>(view, "Label", new StringToLength(), new IntComparison());

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(0, StageViolations(report).Count, report.Summarize());
		}

		[Test]
		public void 段間の型が繋がらない組を拾う()
		{
			var view = CreateView();
			AddBinder<BoolRecordBinder, bool>(view, "Label", new StringToLength(), new BoolInverse());

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			var found = StageViolations(report);
			Assert.AreEqual(1, found.Count, report.Summarize());
			StringAssert.Contains("Int32 -> Boolean", found[0].Detail);
		}

		[Test]
		public void 自動で挟まる組は違反にしない()
		{
			var view = CreateView();
			AddBinder<BoolRecordBinder, bool>(view, "Label", new StringToLength(), new IntComparison());

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(0, StageViolations(report).Count, report.Summarize());
		}

		[Test]
		public void 最終段の出力がTと繋がらない場合も拾う()
		{
			var view = CreateView();
			AddBinder<BoolRecordBinder, bool>(view, "Label", new StringToLength());

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			var found = StageViolations(report);
			Assert.AreEqual(1, found.Count, report.Summarize());
			StringAssert.Contains("output", found[0].Detail);
		}

		[Test]
		public void Converterが無ければ段の検査はしない()
		{
			var view = CreateView();
			AddBinder<TextRecordBinder, string>(view, "Label");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(0, StageViolations(report).Count, report.Summarize());
		}

		[Test]
		public void 段を足しただけで中身が無い場合に落ちない()
		{
			var view = CreateView();
			AddBinder<TextRecordBinder, string>(view, "Label", null, new ObjectToString());

			var report = new LintReport();
			Assert.DoesNotThrow(() => ViewLinter.Lint(view, "test", report));

			var found = StageViolations(report);
			Assert.AreEqual(1, found.Count, report.Summarize());
			StringAssert.Contains("stage 0 is empty", found[0].Detail);
		}

		[Test]
		public void 検査はBinderと同じ解決を見る()
		{
			var view = CreateView();
			var binder = AddBinder<BoolRecordBinder, bool>(view, "Label",
				new StringToLength(), new IntComparison());

			var stages = new System.Collections.Generic.List<Core.ConverterStage>();
			((Core.IBinderInfo)binder).GetConverterStages(stages);

			Assert.AreEqual(2, stages.Count);
			Assert.AreEqual(typeof(string), stages[0].InputType);
			Assert.AreEqual(typeof(int), stages[0].OutputType);
			Assert.AreEqual(typeof(int), stages[1].InputType);
			Assert.AreEqual(typeof(bool), stages[1].OutputType);

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);
			Assert.AreEqual(0, StageViolations(report).Count, report.Summarize());
		}

	}
}
