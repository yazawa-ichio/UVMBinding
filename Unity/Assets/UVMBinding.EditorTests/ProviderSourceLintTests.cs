using NUnit.Framework;
using UnityEngine;
using UVMBinding.Core;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class ProviderSourceLintTests
	{
		class SourceViewModel : ViewModel
		{
			[Bind] public bool Visible { get; set; }
		}

		class SourceData
		{
			public bool Field;
			public bool Property { get; set; }
		}

		class TypedViewModelProvider : ViewModelSourceProvider<SourceViewModel>
		{
			protected override SourceViewModel GetSource() => new SourceViewModel();
		}

		class TypedDataProvider : DataSourceProvider<SourceData>
		{
			protected override SourceData GetSource() => new SourceData();
		}

		class UntypedProvider : ViewModelSourceProvider
		{
			public override IViewModel GetViewModel() => new SourceViewModel();
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

		T CreateProvider<T>(string path) where T : ViewModelSourceProvider
		{
			m_Root = new GameObject("Provider");
			var binder = m_Root.AddComponent<Binders.ActiveBinder>();
			SetPath(binder, path);
			return m_Root.AddComponent<T>();
		}

		static void SetPath(Component binder, string path)
		{
			var so = new UnityEditor.SerializedObject(binder);
			so.FindProperty("m_Path").stringValue = path;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		static LintReport Lint(ViewModelSourceProvider provider)
		{
			var report = new LintReport();
			ViewLinter.Lint(provider, "test", report);
			return report;
		}

		[Test]
		public void 宣言した型に存在するパスは違反にならない()
		{
			var report = Lint(CreateProvider<TypedViewModelProvider>("Visible"));

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(1, report.CheckedProviderCount);
		}

		[Test]
		public void 宣言した型に存在しないパスを検出できる()
		{
			var report = Lint(CreateProvider<TypedViewModelProvider>("NotFound"));

			Assert.AreEqual(1, report.Violations.Count);
			Assert.AreEqual(LintRule.UnknownBindPath, report.Violations[0].Rule);
		}

		[Test]
		public void POCOのフィールドはパスとして認められる()
		{
			var report = Lint(CreateProvider<TypedDataProvider>("Field"));

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(1, report.CheckedProviderCount);
		}

		[Test]
		public void POCOのプロパティはパスとして認められる()
		{
			var report = Lint(CreateProvider<TypedDataProvider>("Property"));

			Assert.AreEqual(0, report.Violations.Count);
		}

		[Test]
		public void POCOに存在しないパスを検出できる()
		{
			var report = Lint(CreateProvider<TypedDataProvider>("NotFound"));

			Assert.AreEqual(1, report.Violations.Count);
			Assert.AreEqual(LintRule.UnknownBindPath, report.Violations[0].Rule);
		}

		[Test]
		public void 非ジェネリック基底のProviderは検査不能として報告される()
		{
			var report = Lint(CreateProvider<UntypedProvider>("Visible"));

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(1, report.Skipped.Count);
			Assert.AreEqual(0, report.CheckedProviderCount);
		}

		[Test]
		public void 型引数がIViewModelでもDataSourceProviderとして扱える()
		{
			Assert.AreEqual(typeof(SourceData), ViewLinter.GetDeclaredSourceType(typeof(TypedDataProvider)));
			Assert.AreEqual(typeof(SourceViewModel), ViewLinter.GetDeclaredSourceType(typeof(TypedViewModelProvider)));
			Assert.IsNull(ViewLinter.GetDeclaredSourceType(typeof(UntypedProvider)));
		}
	}
}
