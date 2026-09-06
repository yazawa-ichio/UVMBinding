using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class BinderInfoTests
	{
		GameObject Create(string name)
		{
			return GameObject.Instantiate(Resources.Load<GameObject>(name));
		}

		static List<IBinderInfo> CollectBinders(IView view)
		{
			var elements = new List<IViewElement>();
			view.GetElements(elements);
			var ret = new List<IBinderInfo>();
			foreach (var element in elements)
			{
				if (element is IBinderInfo info)
				{
					ret.Add(info);
				}
			}
			return ret;
		}

		[UnityTest]
		public IEnumerator IViewElementからPathを取得できる()
		{
			var obj = Create("BinderTest1");
			var view = obj.GetComponent<View>();

			var binders = CollectBinders(view);

			Assert.IsNotEmpty(binders, "IBinderInfo へキャストできる要素がある");
			CollectionAssert.Contains(binders.ConvertAll(x => x.Path), "Text1");

			Object.Destroy(obj);
			yield return null;
		}

		[UnityTest]
		public IEnumerator BindTypeを取得できる()
		{
			var obj = Create("BinderTest1");
			var view = obj.GetComponent<View>();

			var text1 = CollectBinders(view).Find(x => x.Path == "Text1");

			Assert.IsNotNull(text1);
			Assert.AreEqual(typeof(string), text1.BindType);

			Object.Destroy(obj);
			yield return null;
		}

		[UnityTest]
		public IEnumerator バインド後に現在値を取得できる()
		{
			var vm = new BinderTestViewModel { Text1 = "Test1" };
			var obj = Create("BinderTest1");
			var view = obj.GetComponent<View>();
			view.Attach(vm);
			yield return null;

			var text1 = CollectBinders(view).Find(x => x.Path == "Text1");

			Assert.AreEqual("Test1", text1.GetBoundValue());

			Object.Destroy(obj);
			yield return null;
		}

		[UnityTest]
		public IEnumerator 未バインドではnullを返し例外にならない()
		{
			var obj = Create("BinderTest1");
			var view = obj.GetComponent<View>();

			var text1 = CollectBinders(view).Find(x => x.Path == "Text1");

			Assert.IsNotNull(text1);
			object value = null;
			Assert.DoesNotThrow(() => value = text1.GetBoundValue());
			Assert.IsNull(value);

			Object.Destroy(obj);
			yield return null;
		}
	}
}