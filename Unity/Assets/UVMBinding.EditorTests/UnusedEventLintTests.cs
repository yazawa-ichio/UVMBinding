using NUnit.Framework;
using UnityEngine;
using UVMBinding.Core;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class UnusedEventLintTests
	{
		public class EventViewModel : ViewModel
		{
			[Bind] public string Label { get; set; }

			[Event] public void OnUsed() { }

			[Event] public void OnNeverDispatched() { }

			[Event, LintIgnore("スクリプトから dispatch する")]
			public void OnFromScript() { }
		}

		public class RecordViewEvent : ViewEventBase
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

		View CreateView()
		{
			m_Root = new GameObject("View");
			var view = m_Root.AddComponent<View>();
			var so = new UnityEditor.SerializedObject(view);
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = typeof(EventViewModel).FullName;
			so.ApplyModifiedPropertiesWithoutUndo();
			return view;
		}

		void AddEvent(View view, string name)
		{
			var go = new GameObject("Event");
			go.transform.SetParent(view.transform, false);
			var element = go.AddComponent<RecordViewEvent>();
			var so = new UnityEditor.SerializedObject(element);
			so.FindProperty("m_EventName").stringValue = name;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		static LintReport Lint(View view)
		{
			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);
			ViewLinter.LintUnusedEvents(report);
			return report;
		}

		static System.Collections.Generic.List<LintViolation> Unused(LintReport report)
		{
			return report.Warnings.FindAll(x => x.Rule == LintRule.UnusedEventName);
		}

		[Test]
		public void dispatchされていないイベントを警告として拾う()
		{
			var view = CreateView();
			AddEvent(view, "OnUsed");

			var report = Lint(view);
			var found = Unused(report);

			Assert.AreEqual(1, found.Count, report.Summarize());
			StringAssert.Contains("OnNeverDispatched", found[0].Detail);
		}

		[Test]
		public void 違反ではなく警告にする()
		{
			var view = CreateView();
			AddEvent(view, "OnUsed");

			var report = Lint(view);

			Assert.IsFalse(report.HasViolation, report.Summarize());
		}

		[Test]
		public void dispatchされていればイベントは出ない()
		{
			var view = CreateView();
			AddEvent(view, "OnUsed");
			AddEvent(view, "OnNeverDispatched");

			var report = Lint(view);

			Assert.AreEqual(0, Unused(report).Count, report.Summarize());
		}

		[Test]
		public void LintIgnoreを付けたイベントは除外され件数に出る()
		{
			var view = CreateView();
			AddEvent(view, "OnUsed");

			var report = Lint(view);

			Assert.IsFalse(Unused(report).Exists(x => x.Detail.Contains("OnFromScript")), "除外されている");
			Assert.IsTrue(report.Ignored.Exists(x => x.ObjectPath.EndsWith("OnFromScript")),
				report.Summarize());
		}

		[Test]
		public void dispatchが1つも無い型は対象にしない()
		{
			var report = new LintReport();
			ViewLinter.LintUnusedEvents(report);

			Assert.AreEqual(0, Unused(report).Count);
		}
	}
}
