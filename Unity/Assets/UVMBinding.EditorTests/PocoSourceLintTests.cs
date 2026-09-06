using NUnit.Framework;
using UnityEngine;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class PocoSourceLintTests
	{
		public class SourceData
		{
			public bool Field;
			public bool Property { get; set; }
			public string Text { get; set; }
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

		View CreateView(string typeName, string path)
		{
			m_Root = new GameObject("View");
			var view = m_Root.AddComponent<View>();
			var so = new UnityEditor.SerializedObject(view);
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = typeName;
			so.ApplyModifiedPropertiesWithoutUndo();

			var child = new GameObject("Panel");
			child.transform.SetParent(m_Root.transform, false);
			var binder = child.AddComponent<Binders.ActiveBinder>();
			var binderSo = new UnityEditor.SerializedObject(binder);
			binderSo.FindProperty("m_Path").stringValue = path;
			binderSo.ApplyModifiedPropertiesWithoutUndo();
			return view;
		}

		static LintReport Lint(View view)
		{
			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);
			return report;
		}

		[Test]
		public void POCOのフィールドはパスとして認められる()
		{
			var report = Lint(CreateView(typeof(SourceData).FullName, "Field"));

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(0, report.Skipped.Count);
			Assert.AreEqual(1, report.CheckedViewCount);
		}

		[Test]
		public void POCOのプロパティはパスとして認められる()
		{
			var report = Lint(CreateView(typeof(SourceData).FullName, "Property"));

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(1, report.CheckedViewCount);
		}

		[Test]
		public void POCOに存在しないパスを検出できる()
		{
			var report = Lint(CreateView(typeof(SourceData).FullName, "NotFound"));

			Assert.AreEqual(1, report.Violations.Count);
			Assert.AreEqual(LintRule.UnknownBindPath, report.Violations[0].Rule);
		}

		[Test]
		public void POCOでも型不一致を検出できる()
		{
			var report = Lint(CreateView(typeof(SourceData).FullName, "Text"));

			Assert.AreEqual(1, report.Violations.Count);
			Assert.AreEqual(LintRule.UnconvertibleType, report.Violations[0].Rule);
		}

		[Test]
		public void 見つからない型は検査不能として報告される()
		{
			var report = Lint(CreateView("Not.Exist.Type", "Field"));

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(1, report.Skipped.Count);
			Assert.AreEqual(0, report.CheckedViewCount);
		}

		[Test]
		public void 型名の解決はIViewModel派生に限定されない()
		{
			Assert.AreEqual(typeof(SourceData), BindSourceType.Find(typeof(SourceData).FullName));
			Assert.AreEqual(typeof(GeneralViewModel), BindSourceType.Find(typeof(GeneralViewModel).FullName));
			Assert.IsNull(BindSourceType.Find("Not.Exist.Type"));
		}
	}
}
