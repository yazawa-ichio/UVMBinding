using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UVMBinding.Core;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class NamingLintTests
	{
		class NamingTestViewModel : ViewModel
		{
			[Bind] public bool Button1 { get; set; }
			[Bind] public bool HasItem0 { get; set; }
			[Bind] public bool Ok { get; set; }
			[Bind] public bool RemainingCount { get; set; }
			[Event] public System.Action OnDecide { get; set; }
		}

		[SemanticRole(SemanticRole.Button)]
		class PlainEvent : ViewEventBase
		{
			public override System.Type EventType() => null;
		}

		[SemanticRole(SemanticRole.Button)]
		class ArgumentEvent : ViewEventBase
		{
			public override System.Type EventType() => typeof(int);
		}

		class StaticLabel : MonoBehaviour
		{
			public string text { get; set; }
		}

		GameObject m_Root;
		LintSettings m_Settings;

		[SetUp]
		public void SetUp()
		{
			m_Settings = ScriptableObject.CreateInstance<LintSettings>();
		}

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null)
			{
				Object.DestroyImmediate(m_Root);
				m_Root = null;
			}
			if (m_Settings != null)
			{
				Object.DestroyImmediate(m_Settings);
				m_Settings = null;
			}
			ViewLinter.Settings = null;
		}

		static void SetPrivate(Object target, string field, System.Action<SerializedProperty> apply)
		{
			var so = new SerializedObject(target);
			apply(so.FindProperty(field));
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		LintReport LintName(string name)
		{
			var report = new LintReport();
			NamingLinter.LintName(name, "test", "obj", m_Settings, report);
			return report;
		}

		[Test]
		public void 語幹が汎用語の名前を検出する()
		{
			var report = LintName("Button1");

			Assert.AreEqual(1, report.Warnings.Count);
			Assert.AreEqual(LintRule.GenericName, report.Warnings[0].Rule);
			Assert.AreEqual(0, report.Violations.Count, "壊れてはいないので違反にしない");
		}

		[Test]
		public void 意味のある語幹に連番が付いていても検出しない()
		{
			CollectionAssert.IsEmpty(LintName("HasItem0").Warnings);
			CollectionAssert.IsEmpty(LintName("Slot3").Warnings);
			CollectionAssert.IsEmpty(LintName("Player2Score").Warnings);
		}

		[Test]
		public void 部分一致では検出しない()
		{
			CollectionAssert.IsEmpty(LintName("TitleText").Warnings);
			CollectionAssert.IsEmpty(LintName("ItemName").Warnings);
		}

		[Test]
		public void On接頭辞を外して判定する()
		{
			Assert.AreEqual(LintRule.GenericName, LintName("OnButton1").Warnings[0].Rule);
			CollectionAssert.IsEmpty(LintName("OnIncrement").Warnings);
			CollectionAssert.IsEmpty(LintName("OnceMore").Warnings, "On で始まるだけの語は外さない");
		}

		[Test]
		public void 短い名前を検出する()
		{
			Assert.AreEqual(LintRule.TooShortName, LintName("Ok").Warnings[0].Rule);
			CollectionAssert.IsEmpty(LintName("Age").Warnings);
		}

		[Test]
		public void ブロックリストを設定で変更できる()
		{
			SetPrivate(m_Settings, "m_GenericWords", p =>
			{
				p.arraySize = 1;
				p.GetArrayElementAtIndex(0).stringValue = "Slot";
			});

			Assert.AreEqual(LintRule.GenericName, LintName("Slot3").Warnings[0].Rule);
			CollectionAssert.IsEmpty(LintName("Button1").Warnings, "外した語は検出しない");
		}

		[Test]
		public void 引数を持つイベントは名前が汎用でも通す()
		{
			m_Root = new GameObject("Event");
			var plain = m_Root.AddComponent<PlainEvent>();
			var withArg = m_Root.AddComponent<ArgumentEvent>();
			SetPrivate(plain, "m_EventName", p => p.stringValue = "OnClick");
			SetPrivate(withArg, "m_EventName", p => p.stringValue = "OnClick");

			var report = new LintReport();
			NamingLinter.LintEventName(plain, "test", "obj", m_Settings, report);
			Assert.AreEqual(1, report.Warnings.Count, "引数が無ければ何を押したか分からない");

			var withArgReport = new LintReport();
			NamingLinter.LintEventName(withArg, "test", "obj", m_Settings, withArgReport);
			CollectionAssert.IsEmpty(withArgReport.Warnings, "引数が行き先を分けている");
			Assert.AreEqual(1, withArgReport.CheckedNameCount, "率のために数は数える");
		}

		[Test]
		public void ラベルの無いボタンを検出できる()
		{
			SetPrivate(m_Settings, "m_CheckMissingLabel", p => p.boolValue = true);
			ViewLinter.Settings = m_Settings;

			m_Root = new GameObject("View");
			var view = m_Root.AddComponent<View>();
			SetPrivate(view, View.ReservationViewModelFieldName,
				p => p.stringValue = typeof(NamingTestViewModel).FullName);
			var button = new GameObject("Button");
			button.transform.SetParent(m_Root.transform, false);
			var viewEvent = button.AddComponent<PlainEvent>();
			SetPrivate(viewEvent, "m_EventName", p => p.stringValue = "OnDecide");

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			Assert.AreEqual(1, report.Warnings.Count, report.Summarize());
			Assert.AreEqual(LintRule.MissingLabel, report.Warnings[0].Rule);
		}

		[Test]
		public void ラベルがあれば検出しない()
		{
			SetPrivate(m_Settings, "m_CheckMissingLabel", p => p.boolValue = true);
			ViewLinter.Settings = m_Settings;

			m_Root = new GameObject("View");
			var view = m_Root.AddComponent<View>();
			SetPrivate(view, View.ReservationViewModelFieldName,
				p => p.stringValue = typeof(NamingTestViewModel).FullName);
			var button = new GameObject("Button");
			button.transform.SetParent(m_Root.transform, false);
			var viewEvent = button.AddComponent<PlainEvent>();
			SetPrivate(viewEvent, "m_EventName", p => p.stringValue = "OnDecide");
			var label = new GameObject("Label");
			label.transform.SetParent(button.transform, false);
			label.AddComponent<StaticLabel>().text = "決定";

			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);

			CollectionAssert.IsEmpty(report.Warnings, report.Summarize());
		}

		[Test]
		public void ラベル検査は既定で出さない()
		{
			Assert.IsFalse(m_Settings.CheckMissingLabel);
		}
	}
}
