using NUnit.Framework;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class ProviderOwnershipTests
	{
		class OwnerViewModel : ViewModel
		{
			[Bind] public string OwnerText { get; set; }
		}

		class TestEvent : ViewEventBase
		{
			public override System.Type EventType() => null;

			public bool IsBound => m_Dispatcher != null;
		}

		class TestProvider : ViewModelSourceProvider<OwnerViewModel>
		{
			public Component Only;

			public override bool IsTarget(Component component)
			{
				return Only == null || Only == component;
			}

			protected override OwnerViewModel GetSource()
			{
				return new OwnerViewModel { OwnerText = "provider" };
			}
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

		[Test]
		public void Providerは対象の要素だけをバインドする()
		{
			m_Root = new GameObject("Provider");
			m_Root.SetActive(false);
			var targeted = m_Root.AddComponent<TestEvent>();
			var other = m_Root.AddComponent<TestEvent>();
			var provider = m_Root.AddComponent<TestProvider>();
			provider.Only = targeted;

			m_Root.SetActive(true);

			Assert.IsTrue(targeted.IsBound, "対象の要素はバインドされる");
			Assert.IsFalse(other.IsBound, "対象外の要素はバインドされない");
		}

		[Test]
		public void 対象が既定なら同じGameObjectの要素は全部バインドされる()
		{
			m_Root = new GameObject("Provider");
			m_Root.SetActive(false);
			var first = m_Root.AddComponent<TestEvent>();
			var second = m_Root.AddComponent<TestEvent>();
			m_Root.AddComponent<TestProvider>();

			m_Root.SetActive(true);

			Assert.IsTrue(first.IsBound);
			Assert.IsTrue(second.IsBound);
		}
	}
}
