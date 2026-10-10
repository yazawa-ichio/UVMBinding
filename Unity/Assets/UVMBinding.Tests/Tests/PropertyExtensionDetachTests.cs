using NUnit.Framework;
using UnityEngine;
using UVMBinding.PropertyProvider;

namespace UVMBinding.Tests
{
	public class PropertyExtensionDetachTests
	{
		public class DetachViewModel : ViewModel
		{
			[Bind] public bool FlagA { get; set; } = true;
			[Bind] public bool FlagB { get; set; } = true;
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

		View Build()
		{
			m_Root = new GameObject("Screen");
			var view = m_Root.AddComponent<View>();
			var extension = m_Root.AddComponent<ViewModelPropertyExtension>();
			var provider = new CombineBoolProvider();
			SetField(provider, "m_ValueKey", "BothFlags");
			SetField(provider, "m_SourceKeys", new[] { "FlagA", "FlagB" });
			SetField(extension, "m_Properties", new IPropertyProvider[] { provider });
			return view;
		}

		static void SetField(object target, string name, object value)
		{
			const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance
				| System.Reflection.BindingFlags.NonPublic;
			for (var type = target.GetType(); type != null; type = type.BaseType)
			{
				var field = type.GetField(name, Flags);
				if (field != null)
				{
					field.SetValue(target, value);
					return;
				}
			}
			Assert.Fail($"field not found: {name}");
		}

		[Test]
		public void ViewModelを外しても落ちない()
		{
			var view = Build();
			view.Attach(new DetachViewModel());
			view.TryUpdate();

			Assert.DoesNotThrow(() => view.Attach(null));
		}

		[Test]
		public void 外した後にもう一度繋げる()
		{
			var view = Build();
			view.Attach(new DetachViewModel());
			view.TryUpdate();
			view.Attach(null);

			var second = new DetachViewModel { FlagA = false };
			Assert.DoesNotThrow(() => view.Attach(second));
			view.TryUpdate();
			Assert.AreSame(second, view.ViewModel);
		}
	}
}
