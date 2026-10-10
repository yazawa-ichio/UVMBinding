using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UVMBinding.Core;
using UVMBinding.Lint;
using UVMBinding.PropertyProvider;

namespace UVMBinding.EditorTests
{
	public class UnusedBindLintTests
	{
		public class BindViewModel : ViewModel
		{
			[Bind] public string Title { get; set; }

			[Bind] public string Subtitle { get; set; }

			[Bind, LintIgnore("スクリプトから読む")]
			public string FromScript { get; set; }
		}

		public class DependsViewModel : ViewModel
		{
			[Bind] public string First { get; set; }

			[Bind] public string Last { get; set; }

			[Bind, DependsOn(nameof(First), nameof(Last))]
			public string FullName => $"{First} {Last}";
		}

		public class ExtensionViewModel : ViewModel
		{
			[Bind] public bool A { get; set; }

			[Bind] public bool B { get; set; }
		}

		[DynamicPath]
		public class DynamicBindViewModel : ViewModel
		{
			[Bind] public string Title { get; set; }
		}

		public class TextRecordBinder : Binder<string>
		{
			protected override void UpdateValue(string value) { }
		}

		readonly List<GameObject> m_Roots = new List<GameObject>();

		[TearDown]
		public void TearDown()
		{
			foreach (var root in m_Roots)
			{
				if (root != null)
				{
					Object.DestroyImmediate(root);
				}
			}
			m_Roots.Clear();
		}

		View CreateView(System.Type type)
		{
			var root = new GameObject("View");
			m_Roots.Add(root);
			var view = root.AddComponent<View>();
			var so = new UnityEditor.SerializedObject(view);
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = type.FullName;
			so.ApplyModifiedPropertiesWithoutUndo();
			return view;
		}

		void AddBinder(View view, string path)
		{
			var go = new GameObject("Element");
			go.transform.SetParent(view.transform, false);
			go.AddComponent<TextRecordBinder>().ChangePath(path);
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

		void AddProvider(View view, string valueKey, string[] sources)
		{
			var extension = view.gameObject.AddComponent<ViewModelPropertyExtension>();
			var provider = new CombineBoolProvider();
			SetField(provider, "m_ValueKey", valueKey);
			SetField(provider, "m_SourceKeys", sources);
			var field = typeof(ViewModelPropertyExtension).GetField("m_Properties",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var array = System.Array.CreateInstance(field.FieldType.GetElementType(), 1);
			array.SetValue(provider, 0);
			field.SetValue(extension, array);
		}

		static LintReport Lint(params View[] views)
		{
			var report = new LintReport();
			foreach (var view in views)
			{
				ViewLinter.Lint(view, "test", report);
			}
			ViewLinter.LintUnusedBinds(report);
			return report;
		}

		static List<string> Unused(LintReport report)
		{
			var paths = new List<string>();
			foreach (var warning in report.Warnings)
			{
				if (warning.Rule == LintRule.UnusedBindPath)
				{
					paths.Add(warning.Detail.Substring(warning.Detail.LastIndexOf(' ') + 1));
				}
			}
			return paths;
		}

		[Test]
		public void 束ねられていないBindを警告として拾う()
		{
			var view = CreateView(typeof(BindViewModel));
			AddBinder(view, "Title");

			var report = Lint(view);

			CollectionAssert.AreEqual(new[] { "Subtitle" }, Unused(report), report.Summarize());
			Assert.IsFalse(report.HasViolation, "違反ではなく警告にする");
		}

		[Test]
		public void Binderを1つも置いていないViewの型も対象にする()
		{
			var view = CreateView(typeof(BindViewModel));

			var report = Lint(view);

			CollectionAssert.AreEquivalent(new[] { "Title", "Subtitle" }, Unused(report), report.Summarize());
		}

		[Test]
		public void 複数のViewで分け持つBindは合算する()
		{
			var first = CreateView(typeof(BindViewModel));
			AddBinder(first, "Title");
			var second = CreateView(typeof(BindViewModel));
			AddBinder(second, "Subtitle");

			var report = Lint(first, second);

			CollectionAssert.IsEmpty(Unused(report), report.Summarize());
		}

		[Test]
		public void LintIgnoreを付けたBindは除外され件数に出る()
		{
			var view = CreateView(typeof(BindViewModel));
			AddBinder(view, "Title");
			AddBinder(view, "Subtitle");

			var report = Lint(view);

			CollectionAssert.IsEmpty(Unused(report), report.Summarize());
			Assert.IsTrue(report.Ignored.Exists(x => x.ObjectPath.EndsWith("FromScript")), report.Summarize());
		}

		[Test]
		public void 派生値が束ねられていれば依存元は出ない()
		{
			var view = CreateView(typeof(DependsViewModel));
			AddBinder(view, "FullName");

			var report = Lint(view);

			CollectionAssert.IsEmpty(Unused(report), report.Summarize());
		}

		[Test]
		public void 派生値が束ねられていなければ依存元も出る()
		{
			var view = CreateView(typeof(DependsViewModel));

			var report = Lint(view);

			CollectionAssert.AreEquivalent(new[] { "First", "Last", "FullName" }, Unused(report), report.Summarize());
		}

		[Test]
		public void PropertyExtensionの依存元は使われているとみなす()
		{
			var view = CreateView(typeof(ExtensionViewModel));
			AddProvider(view, "Combined", new[] { "A", "B" });

			var report = Lint(view);

			CollectionAssert.IsEmpty(Unused(report), report.Summarize());
		}

		[Test]
		public void DynamicPathの型は対象にしない()
		{
			var view = CreateView(typeof(DynamicBindViewModel));

			var report = Lint(view);

			CollectionAssert.IsEmpty(Unused(report), report.Summarize());
		}

		[Test]
		public void Viewが1つも無い型は対象にしない()
		{
			var report = new LintReport();
			ViewLinter.LintUnusedBinds(report);

			CollectionAssert.IsEmpty(Unused(report));
		}
	}
}
