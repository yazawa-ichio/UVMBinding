using NUnit.Framework;
using UnityEngine;
using UVMBinding.Core;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class DynamicPathLintTests
	{
		[DynamicPath]
		class DynamicViewModel : GeneralViewModel
		{
		}

		class StaticViewModel : ViewModel
		{
			[Bind] public bool Visible { get; set; }
		}

		class TestViewEvent : ViewEventBase
		{
			public override System.Type EventType() => null;
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

		View CreateView(System.Type sourceType)
		{
			m_Root = new GameObject("View");
			var view = m_Root.AddComponent<View>();
			var so = new UnityEditor.SerializedObject(view);
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = sourceType.FullName;
			so.ApplyModifiedPropertiesWithoutUndo();
			return view;
		}

		T AddChild<T>(string name) where T : Component
		{
			var go = new GameObject(name);
			go.transform.SetParent(m_Root.transform, false);
			return go.AddComponent<T>();
		}

		static void SetPath(Component binder, string path)
		{
			var so = new UnityEditor.SerializedObject(binder);
			so.FindProperty("m_Path").stringValue = path;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		static void SetEventName(Component viewEvent, string name)
		{
			var so = new UnityEditor.SerializedObject(viewEvent);
			so.FindProperty("m_EventName").stringValue = name;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		static LintReport Lint(View view)
		{
			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);
			return report;
		}

		[Test]
		public void 動的なパスは違反にならない()
		{
			var view = CreateView(typeof(GeneralViewModel));
			SetPath(AddChild<Binders.ActiveBinder>("Panel"), "実行時に作られるパス");

			var report = Lint(view);

			Assert.AreEqual(0, report.Violations.Count);
		}

		[Test]
		public void 部分検査として報告され検査済みには数えない()
		{
			var view = CreateView(typeof(GeneralViewModel));
			SetPath(AddChild<Binders.ActiveBinder>("Panel"), "Anything");

			var report = Lint(view);

			Assert.AreEqual(1, report.Partial.Count);
			Assert.AreEqual(0, report.CheckedViewCount, "全部検査したものと混ぜない");
			Assert.AreEqual(0, report.Skipped.Count, "検査不能ではない");
		}

		[Test]
		public void 動的なら購読されていないイベント名は違反にならない()
		{
			var view = CreateView(typeof(GeneralViewModel));
			SetEventName(AddChild<TestViewEvent>("A"), "OnTap");

			var report = Lint(view);

			Assert.AreEqual(0, report.Violations.Count);
		}

		[Test]
		public void 属性は派生に継承される()
		{
			var view = CreateView(typeof(DynamicViewModel));
			SetPath(AddChild<Binders.ActiveBinder>("Panel"), "Anything");

			var report = Lint(view);

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(1, report.Partial.Count);
		}

		[Test]
		public void 属性が無い型は従来どおり全部検査する()
		{
			var view = CreateView(typeof(StaticViewModel));
			SetPath(AddChild<Binders.ActiveBinder>("Panel"), "NotFound");

			var report = Lint(view);

			Assert.AreEqual(1, report.Violations.Count);
			Assert.AreEqual(LintRule.UnknownBindPath, report.Violations[0].Rule);
			Assert.AreEqual(0, report.Partial.Count);
			Assert.AreEqual(1, report.CheckedViewCount);
		}
	}
}
