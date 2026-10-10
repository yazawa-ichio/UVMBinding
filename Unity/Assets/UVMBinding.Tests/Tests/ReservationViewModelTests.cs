using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UVMBinding.Tests
{
	public class ReservationViewModelTests
	{
		[UnityTest]
		public IEnumerator 宣言された型名を取得できる()
		{
			var obj = GameObject.Instantiate(Resources.Load<GameObject>("BinderTest1"));
			var view = obj.GetComponent<View>();

			Assert.IsNotNull(view.ReservationViewModelName);
			Assert.IsNotEmpty(view.ReservationViewModelName, "prefab に型宣言がある");

			Object.Destroy(obj);
			yield return null;
		}

		[Test]
		public void 宣言が無いViewは空文字を返し例外にならない()
		{
			var obj = new GameObject("View");
			var view = obj.AddComponent<View>();

			string name = null;
			Assert.DoesNotThrow(() => name = view.ReservationViewModelName);
			Assert.IsTrue(string.IsNullOrEmpty(name));

			Object.DestroyImmediate(obj);
		}
	}
}