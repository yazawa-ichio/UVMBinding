using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class RuntimeLintTests
	{
		public class RuntimeViewModel : ViewModel
		{
			[Bind] public bool Visible { get; set; }
			[Bind] public string Caption { get; set; }
		}

		public class ProvidedViewModel : ViewModel
		{
			[Bind] public bool Enabled { get; set; }
		}

		public class DerivedViewModel : RuntimeViewModel { }

		public class SourceData
		{
			public bool Field;
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

		View CreateView(string path)
		{
			m_Root = new GameObject("View");
			var view = m_Root.AddComponent<View>();
			var child = new GameObject("Panel");
			child.transform.SetParent(m_Root.transform, false);
			var binder = child.AddComponent<Binders.ActiveBinder>();
			var so = new SerializedObject(binder);
			so.FindProperty("m_Path").stringValue = path;
			so.ApplyModifiedPropertiesWithoutUndo();
			return view;
		}

		static LintReport Lint(View view)
		{
			var report = new LintReport();
			ViewLinter.LintRuntime(view, report);
			return report;
		}

		[Test]
		public void 未解決バインドを検出できる()
		{
			var view = CreateView("NotFound");
			view.Attach<RuntimeViewModel>();

			var report = Lint(view);

			Assert.AreEqual(1, report.Violations.Count, report.Summarize());
			Assert.AreEqual(LintRule.UnknownBindPath, report.Violations[0].Rule);
		}

		[Test]
		public void 型不一致を検出できる()
		{
			var view = CreateView("Caption");
			view.Attach<RuntimeViewModel>();

			var report = Lint(view);

			Assert.AreEqual(1, report.Violations.Count, report.Summarize());
			Assert.AreEqual(LintRule.UnconvertibleType, report.Violations[0].Rule);
		}

		[Test]
		public void 一覧から該当オブジェクトを選択できる()
		{
			var view = CreateView("NotFound");
			view.Attach<RuntimeViewModel>();

			var report = Lint(view);

			Assert.IsNotNull(report.Violations[0].Source, "選択できるように元を持つ");
			Assert.AreEqual("Panel", ((Component)report.Violations[0].Source).name);
		}

		[Test]
		public void 宣言が無い構成でも検査できる()
		{
			var view = CreateView("NotFound");
			view.ViewModel = new ProvidedViewModel();

			var staticReport = new LintReport();
			ViewLinter.Lint(view, "test", staticReport);
			Assert.AreEqual(1, staticReport.Skipped.Count, "静的検査では届かない");

			var report = Lint(view);
			Assert.AreEqual(1, report.Violations.Count, "実行時なら検査できる");
			Assert.AreEqual(LintRule.UnknownBindPath, report.Violations[0].Rule);
		}

		[Test]
		public void 素のPOCOでも検査できる()
		{
			var view = CreateView("Field");
			view.Bind(new SourceData());

			var report = Lint(view);

			CollectionAssert.IsEmpty(report.Violations, report.Summarize());

			var missing = CreateMissing();
			Assert.AreEqual(LintRule.UnknownBindPath, Lint(missing).Violations[0].Rule);
		}

		View CreateMissing()
		{
			Object.DestroyImmediate(m_Root);
			var view = CreateView("NotFound");
			view.Bind(new SourceData());
			return view;
		}

		[Test]
		public void 動的なパスの型は部分検査になる()
		{
			var view = CreateView("実行時に作られるパス");
			view.Attach<GeneralViewModel>();

			var report = Lint(view);

			CollectionAssert.IsEmpty(report.Violations);
			Assert.AreEqual(1, report.Partial.Count);
		}

		[Test]
		public void 繋がっている型が読める()
		{
			var view = CreateView("Visible");
			Assert.IsNull(view.ViewModel, "Attach 前は無い");

			view.Attach<RuntimeViewModel>();
			Assert.AreEqual(typeof(RuntimeViewModel), view.ViewModel.GetType());

			view.Bind(new SourceData());
			Assert.AreEqual(typeof(ReflectionViewModel), view.ViewModel.GetType());
		}

		[Test]
		public void 宣言と実型の食い違いを検出できる()
		{
			var view = CreateView("Visible");
			var so = new SerializedObject(view);
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = typeof(ProvidedViewModel).FullName;
			so.ApplyModifiedPropertiesWithoutUndo();
			view.Attach<RuntimeViewModel>();

			var report = Lint(view);

			var mismatch = report.Violations.Find(x => x.Rule == LintRule.DeclaredTypeMismatch);
			Assert.AreNotEqual(default(LintViolation), mismatch, report.Summarize());
			Assert.AreSame(view, mismatch.Source);
		}

		[Test]
		public void 代入できるなら食い違いにしない()
		{
			var view = CreateView("Visible");
			var so = new SerializedObject(view);
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = typeof(RuntimeViewModel).FullName;
			so.ApplyModifiedPropertiesWithoutUndo();
			view.Attach<DerivedViewModel>();

			var report = Lint(view);

			Assert.IsFalse(report.Violations.Exists(x => x.Rule == LintRule.DeclaredTypeMismatch),
				report.Summarize());
		}

		[Test]
		public void ViewModelが無ければ何もしない()
		{
			var view = CreateView("NotFound");

			var report = Lint(view);

			CollectionAssert.IsEmpty(report.Violations);
			CollectionAssert.IsEmpty(report.Skipped);
		}
	}
}
