using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UVMBinding.Lint;
using UVMBinding.PropertyProvider;

namespace UVMBinding.EditorTests
{
	public class ProviderLintTests
	{
		class ProviderTestViewModel : ViewModel
		{
			[Bind] public bool A { get; set; }
			[Bind] public bool B { get; set; }
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
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = typeof(ProviderTestViewModel).FullName;
			so.ApplyModifiedPropertiesWithoutUndo();
			return view;
		}

		static void SetField(object target, string name, object value)
		{
			for (var type = target.GetType(); type != null; type = type.BaseType)
			{
				var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				if (field != null)
				{
					field.SetValue(target, value);
					return;
				}
			}
			Assert.Fail($"フィールドが見つからない: {name}");
		}

		ViewModelPropertyExtension AddProvider(string[] sources)
		{
			var extension = m_Root.AddComponent<ViewModelPropertyExtension>();
			var provider = new CombineBoolProvider();
			SetField(provider, "m_ValueKey", "Combined");
			SetField(provider, "m_SourceKeys", sources);
			var field = typeof(ViewModelPropertyExtension).GetField("m_Properties",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var array = System.Array.CreateInstance(field.FieldType.GetElementType(), 1);
			array.SetValue(provider, 0);
			field.SetValue(extension, array);
			return extension;
		}

		[Test]
		public void 存在する依存元は違反にならない()
		{
			var view = CreateView();
			AddProvider(new[] { "A", "B" });

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.IsFalse(report.HasViolation, report.Summarize());
		}

		[Test]
		public void 存在しない依存元を検出する()
		{
			var view = CreateView();
			AddProvider(new[] { "A", "NotExist" });

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(1, report.Violations.Count, report.Summarize());
			Assert.AreEqual(LintRule.UnknownSourcePath, report.Violations[0].Rule);
			StringAssert.Contains("NotExist", report.Violations[0].Detail);
		}

		[Test]
		public void 派生値そのものはバインド先として認められる()
		{
			var view = CreateView();
			AddProvider(new[] { "A" });
			var binder = new GameObject("Panel");
			binder.transform.SetParent(m_Root.transform, false);
			var component = binder.AddComponent<Binders.ActiveBinder>();
			var so = new UnityEditor.SerializedObject(component);
			so.FindProperty("m_Path").stringValue = "Combined";
			so.ApplyModifiedPropertiesWithoutUndo();

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.IsFalse(report.HasViolation, report.Summarize());
		}
	}
}