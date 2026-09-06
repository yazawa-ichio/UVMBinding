using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class VersionGateTests
	{
		public class GateViewModel : ViewModel
		{
			[Bind] public string Text { get; set; } = "A";
			[Bind] public bool Visible { get; set; } = true;
		}

		public class CountingBinder : Binder<string>
		{
			public int UpdateCount;

			protected override void UpdateValue(string value)
			{
				UpdateCount++;
			}
		}

		readonly List<GameObject> m_Objects = new List<GameObject>();

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_Objects)
			{
				if (obj != null) Object.DestroyImmediate(obj);
			}
			m_Objects.Clear();
		}

		View CreateView(out CountingBinder binder)
		{
			var root = new GameObject("Root");
			m_Objects.Add(root);
			var view = root.AddComponent<View>();
			var child = new GameObject("Element");
			child.transform.SetParent(root.transform, false);
			binder = child.AddComponent<CountingBinder>();
			binder.ChangePath("Text");
			return view;
		}

		[Test]
		public void 値を変更しないフレームは走査しない()
		{
			var view = CreateView(out var binder);
			var vm = view.Attach<GateViewModel>();
			view.TryUpdate();
			var before = binder.UpdateCount;

			view.TryUpdate();
			view.TryUpdate();

			Assert.AreEqual(before, binder.UpdateCount, "変更が無ければ走査しない");

			vm.Text = "B";
			view.TryUpdate();
			Assert.AreEqual(before + 1, binder.UpdateCount, "変更があれば走査する");
		}

		[Test]
		public void 同じViewModelを2つのViewで共有しても両方が受け取る()
		{
			var first = CreateView(out var firstBinder);
			var second = CreateView(out var secondBinder);
			var vm = new GateViewModel();
			first.Attach(vm);
			second.Attach(vm);
			first.TryUpdate();
			second.TryUpdate();
			var beforeFirst = firstBinder.UpdateCount;
			var beforeSecond = secondBinder.UpdateCount;

			vm.Text = "B";
			first.TryUpdate();
			second.TryUpdate();

			Assert.AreEqual(beforeFirst + 1, firstBinder.UpdateCount);
			Assert.AreEqual(beforeSecond + 1, secondBinder.UpdateCount, "2 つ目も受け取る");
		}

		[Test]
		public void 非表示の間の変更は再表示で反映される()
		{
			var view = CreateView(out var binder);
			var vm = view.Attach<GateViewModel>();
			view.TryUpdate();
			var before = binder.UpdateCount;

			view.gameObject.SetActive(false);
			vm.Text = "B";
			view.TryUpdate();
			Assert.AreEqual(before, binder.UpdateCount, "非表示の間は走査しない");

			view.gameObject.SetActive(true);
			view.TryUpdate();
			Assert.AreEqual(before + 1, binder.UpdateCount, "再表示で拾い直す");
		}

		[Test]
		public void 非表示でもViewUpdaterから外れない()
		{
			var view = CreateView(out _);
			view.Attach<GateViewModel>();

			view.gameObject.SetActive(false);

			Assert.IsTrue(((IAutoViewUpdate)view).IsActive, "生きている扱いのままにする");
		}

		[Test]
		public void ChangePath後に再バインドされる()
		{
			var view = CreateView(out var binder);
			var vm = view.Attach<GateViewModel>();
			vm.Text = "B";
			view.TryUpdate();
			var before = binder.UpdateCount;

			binder.ChangePath("Visible");
			view.TryUpdate();

			Assert.AreEqual(before, binder.UpdateCount, "繋ぎ直しても落ちない");
		}

		[Test]
		public void 派生値の変更が反映される()
		{
			var root = new GameObject("Root");
			m_Objects.Add(root);
			var view = root.AddComponent<View>();
			var extension = root.AddComponent<PropertyProvider.ViewModelPropertyExtension>();
			var child = new GameObject("Element");
			child.transform.SetParent(root.transform, false);
			var binder = child.AddComponent<CountingBinder>();
			binder.ChangePath("Derived");

			var vm = view.Attach<GateViewModel>();
			view.TryUpdate();
			var before = binder.UpdateCount;

			((PropertyProvider.IViewModelPropertyExtension)extension).Set("Derived", "X");
			view.TryUpdate();

			Assert.AreEqual(before + 1, binder.UpdateCount, "派生値の変更も拾う");
		}
	}
}
