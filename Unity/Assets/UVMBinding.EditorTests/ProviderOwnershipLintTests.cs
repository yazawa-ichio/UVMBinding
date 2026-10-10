using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UVMBinding.Core;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class ProviderOwnershipLintTests
	{
		class OwnerViewModel : ViewModel
		{
			[Bind] public bool Visible { get; set; }
		}

		class TestEvent : ViewEventBase
		{
			public override System.Type EventType() => null;
		}

		class TestProvider : ViewModelSourceProvider<OwnerViewModel>
		{
			public Component Only;

			public override bool IsTarget(Component component)
			{
				return Only == null || Only == component;
			}

			protected override OwnerViewModel GetSource() => new OwnerViewModel();
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

		GameObject CreateHost(out View view)
		{
			m_Root = new GameObject("Host");
			view = m_Root.AddComponent<View>();
			var child = new GameObject("Child");
			child.transform.SetParent(m_Root.transform, false);
			return child;
		}

		static List<IViewElement> GetElements(View view)
		{
			var elements = new List<IViewElement>();
			view.GetElements(elements);
			return elements;
		}

		[Test]
		public void Provider配下のイベントは親Viewから外れる()
		{
			var child = CreateHost(out var view);
			var viewEvent = child.AddComponent<TestEvent>();
			child.AddComponent<TestProvider>();

			CollectionAssert.DoesNotContain(GetElements(view), viewEvent);
		}

		[Test]
		public void Provider配下のBinderは親Viewから外れる()
		{
			var child = CreateHost(out var view);
			var binder = child.AddComponent<Binders.ActiveBinder>();
			child.AddComponent<TestProvider>();

			CollectionAssert.DoesNotContain(GetElements(view), binder);
		}

		[Test]
		public void 対象外の要素は親Viewが持つ()
		{
			var child = CreateHost(out var view);
			var targeted = child.AddComponent<TestEvent>();
			var other = child.AddComponent<TestEvent>();
			var provider = child.AddComponent<TestProvider>();
			provider.Only = targeted;

			var elements = GetElements(view);
			CollectionAssert.DoesNotContain(elements, targeted);
			CollectionAssert.Contains(elements, other, "対象外は親 View が持つ");
		}

		[Test]
		public void 検査も対象の要素だけを見る()
		{
			var child = CreateHost(out _);
			var targeted = child.AddComponent<Binders.ActiveBinder>();
			SetPath(targeted, "Visible");
			var other = child.AddComponent<Binders.ActiveBinder>();
			SetPath(other, "NotFound");
			var provider = child.AddComponent<TestProvider>();
			provider.Only = targeted;

			var report = new LintReport();
			ViewLinter.Lint(provider, "test", report);

			Assert.AreEqual(0, report.Violations.Count, "対象外の要素は Provider の検査に含めない");
		}

		static void SetPath(Component binder, string path)
		{
			var so = new UnityEditor.SerializedObject(binder);
			so.FindProperty("m_Path").stringValue = path;
			so.ApplyModifiedPropertiesWithoutUndo();
		}
	}
}
