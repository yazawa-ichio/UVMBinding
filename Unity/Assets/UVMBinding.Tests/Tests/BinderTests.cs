using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UVMBinding.Tests
{

	public class BinderTests
	{

		GameObject Create(string name)
		{
			return GameObject.Instantiate(Resources.Load<GameObject>(name));
		}

		[UnityTest]
		public IEnumerator Binder通常操作()
		{
			var vm = new BinderTestViewModel()
			{
				Text1 = "Test1",
				Text2 = 22.22222f,
			};
			bool onButton1 = false;
			string onButton2 = null;
			vm.OnButton1 += () => onButton1 = true;
			vm.OnButton2 += (x) => onButton2 = x;

			var obj = Create("BinderTest1");

			var text1 = obj.transform.Find("Text1").GetComponent<Text>();
			var text2 = obj.transform.Find("Text2").GetComponent<Text>();
			var button1 = obj.transform.Find("Button1").GetComponent<Button>();
			var button2 = obj.transform.Find("Button2").GetComponent<Button>();

			obj.GetComponent<View>().Attach(vm);

			Assert.AreEqual("", text1.text);
			Assert.AreEqual("", text2.text);
			Assert.AreEqual(false, onButton1);
			Assert.AreEqual(null, onButton2);

			yield return null;

			Assert.AreEqual(vm.Text1, text1.text);
			Assert.AreEqual($"{vm.Text2:F2}", text2.text);

			vm.Text1 = "Test1Test1";
			vm.Text2 = 20;

			yield return null;

			Assert.AreEqual("Test1Test1", text1.text);
			Assert.AreEqual("20.00", text2.text);

			button1.onClick.Invoke();
			button2.onClick.Invoke();

			Assert.AreEqual(true, onButton1);
			Assert.AreEqual(text1.text, onButton2);

			onButton1 = false;
			onButton2 = null;

			button1.onClick.Invoke();
			button2.onClick.Invoke();

			Assert.AreEqual(true, onButton1);
			Assert.AreEqual(text1.text, onButton2);

			GameObject.Destroy(obj);

			yield return null;
			yield return null;

		}

		[Test]
		public void Binder更新タイミング()
		{
			var vm1 = new BinderTestViewModel()
			{
				Text1 = "Test1",
				Text2 = 22.22222f,
			};
			var vm2 = new BinderTestViewModel()
			{
				Text1 = "Test2",
				Text2 = 15.5555f,
			};
			var obj = Create("BinderTest1");
			try
			{
				var text1 = obj.transform.Find("Text1").GetComponent<Text>();
				var text2 = obj.transform.Find("Text2").GetComponent<Text>();
				var view = obj.GetComponent<View>();
				view.Attach(vm1);
				view.TryUpdate();
				Assert.AreEqual(vm1.Text1, text1.text);
				Assert.AreEqual($"{vm1.Text2:F2}", text2.text);
				view.Attach(vm2);
				view.TryUpdate();
				Assert.AreEqual(vm2.Text1, text1.text);
				Assert.AreEqual($"{vm2.Text2:F2}", text2.text);

				view.Attach(null);

				vm2.Text1 = "None1";
				vm2.Text2 = 25;

				view.TryUpdate();
				Assert.AreNotEqual(vm2.Text1, text1.text);
				Assert.AreNotEqual($"{vm2.Text2:F2}", text2.text);

			}
			finally
			{
				GameObject.Destroy(obj);
			}
		}

		[Test]
		public void ネストしたBinder()
		{
			var vm1 = new BinderTestViewModel()
			{
				Text1 = "Test1",
				Text2 = 22.22222f,
			};
			vm1.Nest.NestText = "NestTest:BB";
			bool nestAction = false;
			vm1.Nest.NestAction += () =>
			{
				nestAction = true;
			};
			var obj = Create("BinderTest1");
			try
			{
				var view = obj.GetComponent<View>();
				var nestView = view.transform.Find("Nest").GetComponentInChildren<View>();
				Assert.AreEqual(true, nestView.UpdateOnAttach);
				Assert.AreEqual("", nestView.GetComponentInChildren<Text>().text);

				view.Attach(vm1);

				view.TryUpdate();

				Assert.AreEqual("NestTest:BB", nestView.GetComponentInChildren<Text>().text);

				Assert.IsFalse(nestAction);
				nestView.GetComponentInChildren<Button>().onClick.Invoke();
				Assert.IsTrue(nestAction);

			}
			finally
			{
				GameObject.Destroy(obj);
			}
		}

		[Test]
		public void コレクションを利用したBinder()
		{
			var vm = new CollectionTestViewModel();

			var obj = Create("CollectionTest");
			try
			{
				var view = obj.GetComponent<View>();
				view.Attach(vm);
				view.TryUpdate();
				int index = 0;
				CollectionItemViewModel item = null;
				var root = view.transform.Find("Root");
				vm.List.Add(new CollectionItemViewModel
				{
					No = 10,
					OnClick = (i, x) =>
					{
						index = i;
						item = x;
					}
				});
				view.TryUpdate();
				Assert.AreEqual(1, root.childCount);
				vm.List.Add(new CollectionItemViewModel
				{
					No = 20,
					OnClick = (i, x) =>
					{
						index = i;
						item = x;
					}
				});
				view.TryUpdate();
				Assert.AreEqual(2, root.childCount);
				int count = 0;
				foreach (var v in view.GetComponentsInChildren<View>())
				{
					if (v == view) continue;
					var itemVM = v.ViewModel as CollectionItemViewModel;
					Assert.AreEqual($"Button{itemVM.No}", v.GetComponentInChildren<Text>().text);
					v.GetComponentInChildren<Button>().onClick.Invoke();
					Assert.AreEqual(count++, index);
					Assert.AreEqual(itemVM, item);
				}
			}
			finally
			{
				GameObject.Destroy(obj);
			}
		}

		[Test]
		public void 最初が非アクティブでも正常に動作する()
		{
			var obj = Create("ActiveTest");
			try
			{
				var root1 = obj.transform.Find("Root1");
				var root2 = obj.transform.Find("Root2");
				var text = obj.GetComponentInChildren<Text>();

				Assert.AreEqual(true, root1.gameObject.activeSelf);
				Assert.AreEqual(false, root2.gameObject.activeSelf);
				Assert.AreEqual(true, text.enabled);

				var view = obj.GetComponent<View>();
				var vm = view.Attach<GeneralViewModel>((vm) =>
				{
					vm.Set<int>("Value", 10);
				});
				view.TryUpdate();

				Assert.AreEqual(false, root1.gameObject.activeSelf);
				Assert.AreEqual(true, root2.gameObject.activeSelf, "初期値がfalseでも正常に動作する");
				Assert.AreEqual(false, text.enabled);

				vm.Set("Value", -1);

				view.TryUpdate();

				Assert.AreEqual(false, root1.gameObject.activeSelf);
				Assert.AreEqual(false, root2.gameObject.activeSelf);
				Assert.AreEqual(true, text.enabled);

				vm.Set("Value", 1);

				view.TryUpdate();
				Assert.AreEqual(true, root1.gameObject.activeSelf);
				Assert.AreEqual(false, root2.gameObject.activeSelf);
				Assert.AreEqual(false, text.enabled);

				root1.gameObject.SetActive(false);
				vm.SetDirty("Value");
				view.TryUpdate();
				Assert.AreEqual(true, root1.gameObject.activeSelf);

			}
			finally
			{
				GameObject.Destroy(obj);
			}
		}

	}
}