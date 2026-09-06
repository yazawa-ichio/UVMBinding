using NUnit.Framework;
using UnityEngine;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class ViewLinterTests
	{
		class TestViewEvent : Core.ViewEventBase
		{
			public override System.Type EventType() => null;
		}

		class LintTestViewModel : ViewModel
		{
			[Bind] public bool Visible { get; set; }
			[Bind] public string Label { get; set; }
			[Event] public System.Action OnClick { get; set; }
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

		View CreateView(string viewModelName)
		{
			m_Root = new GameObject("View");
			var view = m_Root.AddComponent<View>();
			SetReservation(view, viewModelName);
			return view;
		}

		static void SetReservation(View view, string name)
		{
			var so = new UnityEditor.SerializedObject(view);
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = name;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		static T AddChild<T>(GameObject parent, string name) where T : Component
		{
			var go = new GameObject(name);
			go.transform.SetParent(parent.transform, false);
			return go.AddComponent<T>();
		}

		static void SetReason(LintIgnore ignore, string reason)
		{
			var so = new UnityEditor.SerializedObject(ignore);
			so.FindProperty("m_Reason").stringValue = reason;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		static void SetEventName(Component viewEvent, string name)
		{
			var so = new UnityEditor.SerializedObject(viewEvent);
			so.FindProperty("m_EventName").stringValue = name;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		static void SetPath(Component binder, string path)
		{
			var so = new UnityEditor.SerializedObject(binder);
			so.FindProperty("m_Path").stringValue = path;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		[Test]
		public void 存在するバインドパスは違反にならない()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			var binder = AddChild<Binders.ActiveBinder>(m_Root, "Panel");
			SetPath(binder, "Visible");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.IsFalse(report.HasViolation, report.Summarize());
			Assert.AreEqual(1, report.CheckedViewCount);
		}

		[Test]
		public void 存在しないバインドパスを検出する()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			var binder = AddChild<Binders.ActiveBinder>(m_Root, "Panel");
			SetPath(binder, "NotExist");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(1, report.Violations.Count, report.Summarize());
			Assert.AreEqual(LintRule.UnknownBindPath, report.Violations[0].Rule);
			StringAssert.Contains("NotExist", report.Violations[0].Detail);
		}

		[Test]
		public void 存在しないイベント名を検出する()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			var button = AddChild<TestViewEvent>(m_Root, "Button");
			SetEventName(button, "NotExist");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(1, report.Violations.Count, report.Summarize());
			Assert.AreEqual(LintRule.UnknownEventName, report.Violations[0].Rule);
		}

		[Test]
		public void 存在するイベント名は違反にならない()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			var button = AddChild<TestViewEvent>(m_Root, "Button");
			SetEventName(button, "OnClick");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.IsFalse(report.HasViolation, report.Summarize());
		}

		[Test]
		public void 同じイベント名を複数の要素が持っても違反にならない()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			SetEventName(AddChild<TestViewEvent>(m_Root, "Button1"), "OnClick");
			SetEventName(AddChild<TestViewEvent>(m_Root, "Button2"), "OnClick");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(0, report.Violations.Count, report.Summarize());
		}

		[Test]
		public void LintIgnoreを付けた要素は除外され件数に出る()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			var binder = AddChild<Binders.ActiveBinder>(m_Root, "Panel");
			SetPath(binder, "NotExist");
			var ignore = binder.gameObject.AddComponent<LintIgnore>();
			SetReason(ignore, "検査対象外の理由");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.IsFalse(report.HasViolation, report.Summarize());
			Assert.AreEqual(1, report.Ignored.Count, "黙って消さず件数に出す");
			StringAssert.Contains("検査対象外の理由", report.Ignored[0].Reason);
		}

		[Test]
		public void 理由の無いLintIgnoreは効かない()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			var binder = AddChild<Binders.ActiveBinder>(m_Root, "Panel");
			SetPath(binder, "NotExist");
			binder.gameObject.AddComponent<LintIgnore>();

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(1, report.Violations.Count);
			Assert.AreEqual(0, report.Ignored.Count);
		}

		[Test]
		public void LintIgnoreは子孫にも効く()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			var group = new GameObject("Group");
			group.transform.SetParent(m_Root.transform, false);
			var ignore = group.AddComponent<LintIgnore>();
			SetReason(ignore, "まとめて対象外");
			var binder = AddChild<Binders.ActiveBinder>(group, "Panel");
			SetPath(binder, "NotExist");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.IsFalse(report.HasViolation, report.Summarize());
			Assert.AreEqual(1, report.Ignored.Count);
		}

		[Test]
		public void 型が一致していれば違反にならない()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			var binder = AddChild<Binders.ActiveBinder>(m_Root, "Panel");
			SetPath(binder, "Visible");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.IsFalse(report.HasViolation, report.Summarize());
		}

		[Test]
		public void 変換手段が無い型不一致を検出する()
		{
			var view = CreateView(typeof(LintTestViewModel).FullName);
			var binder = AddChild<Binders.ActiveBinder>(m_Root, "Panel");
			SetPath(binder, "Label");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(1, report.Violations.Count, report.Summarize());
			Assert.AreEqual(LintRule.UnconvertibleType, report.Violations[0].Rule);
			StringAssert.Contains("Label", report.Violations[0].Detail);
		}

		[Test]
		public void 型宣言が無いViewは検査不能として報告する()
		{
			var view = CreateView("");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.IsFalse(report.HasViolation);
			Assert.AreEqual(1, report.Skipped.Count);
			Assert.AreEqual(0, report.CheckedViewCount, "検査した数には数えない");
		}

		[Test]
		public void 見つからない型を宣言したViewは検査不能として報告する()
		{
			var view = CreateView("Not.Exist.ViewModel");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(1, report.Skipped.Count);
			StringAssert.Contains("Not.Exist.ViewModel", report.Skipped[0].Reason);
		}
	}
}