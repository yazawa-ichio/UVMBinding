using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UVMBinding.Binders;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class CollectionReleaseHookTests
	{
		public class RowViewModel : ViewModel
		{
			[Bind] public string Label { get; set; }

			public int UnbindCount;

			protected override void OnUnbind()
			{
				UnbindCount++;
			}
		}

		public class ListViewModel : ViewModel
		{
			public ListViewModel()
			{
				Rows = new Collection<RowViewModel>(nameof(Rows), this);
			}

			public Collection<RowViewModel> Rows { get; }
		}

		public class HookBinder : CollectionBinder
		{
			public readonly List<GameObject> Started = new();
			public readonly List<IViewModel> StartedModels = new();
			public readonly List<int> StartedSibling = new();
			public readonly List<TaskCompletionSource<bool>> Gates = new();
			public int Canceled;
			public bool ThrowOnStart;

			protected override Task OnBeforeReleaseAsync(GameObject instance, IViewModel model,
				CancellationToken cancellationToken)
			{
				Started.Add(instance);
				StartedModels.Add(model);
				StartedSibling.Add(instance.transform.GetSiblingIndex());
				if (ThrowOnStart)
				{
					throw new InvalidOperationException("hook");
				}
				var gate = new TaskCompletionSource<bool>();
				Gates.Add(gate);
				cancellationToken.Register(() =>
				{
					Canceled++;
					gate.TrySetCanceled();
				});
				return gate.Task;
			}

			public void CompleteAll()
			{
				var gates = Gates.ToArray();
				Gates.Clear();
				foreach (var gate in gates)
				{
					gate.TrySetResult(true);
				}
			}

			public void FaultAll()
			{
				var gates = Gates.ToArray();
				Gates.Clear();
				foreach (var gate in gates)
				{
					gate.TrySetException(new InvalidOperationException("hook"));
				}
			}
		}

		GameObject m_Root;
		GameObject m_Template;
		View m_View;
		CollectionBinder m_Binder;

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null) UnityEngine.Object.DestroyImmediate(m_Root);
			if (m_Template != null) UnityEngine.Object.DestroyImmediate(m_Template);
			m_Root = null;
			m_Template = null;
			m_View = null;
			m_Binder = null;
		}

		ListViewModel Setup<T>() where T : CollectionBinder
		{
			m_Template = new GameObject("Row");
			m_Template.AddComponent<View>();
			m_Template.SetActive(false);

			m_Root = new GameObject("Root");
			m_View = m_Root.AddComponent<View>();
			var host = new GameObject("List");
			host.transform.SetParent(m_Root.transform, false);
			m_Binder = host.AddComponent<T>();
			m_Binder.ChangePath("Rows");
			SetPrivate(m_Binder, "m_Preafb", m_Template);

			return m_View.Attach<ListViewModel>();
		}

		HookBinder Hook => (HookBinder)m_Binder;

		static void SetPrivate(object target, string name, object value)
		{
			var field = typeof(CollectionBinder).GetField(name,
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.IsNotNull(field, $"フィールドが見つからない: {name}");
			field.SetValue(target, value);
		}

		void Tick()
		{
			m_View.TryUpdate();
		}

		ListViewModel Fill<T>(params string[] labels) where T : CollectionBinder
		{
			var vm = Setup<T>();
			foreach (var label in labels)
			{
				vm.Rows.Add(new RowViewModel { Label = label });
			}
			Tick();
			return vm;
		}

		int ActiveRows()
		{
			int count = 0;
			foreach (Transform child in m_Binder.transform)
			{
				if (child.gameObject.activeSelf) count++;
			}
			return count;
		}

		static Regex HookError = new Regex("OnBeforeReleaseAsync");

		[Test]
		public void フックを使わなければその場で回収する()
		{
			var vm = Fill<CollectionBinder>("A", "B");

			vm.Rows.RemoveAt(0);
			Tick();

			Assert.AreEqual(0, m_Binder.PendingReleaseCount, "待ちを挟まない");
			Assert.AreEqual(1, m_Binder.PooledCount, "その場でプールへ返す");
			Assert.AreEqual(1, ActiveRows());
		}

		[Test]
		public void 並べ替えではフックを呼ばない()
		{
			var vm = Setup<HookBinder>();
			var first = new RowViewModel { Label = "A" };
			var second = new RowViewModel { Label = "B" };
			vm.Rows.Add(first);
			vm.Rows.Add(second);
			Tick();

			vm.Rows.RemoveAt(0);
			vm.Rows.Add(first);
			Tick();

			CollectionAssert.IsEmpty(Hook.Started, "同じ行の並べ替えでは削除扱いにしない");
			Assert.AreEqual(0, m_Binder.PooledCount);
		}

		[UnityTest]
		public IEnumerator 待機中は回収せず完了後に回収する()
		{
			var vm = Fill<HookBinder>("A", "B");
			var removed = vm.Rows[0];

			vm.Rows.RemoveAt(0);
			Tick();

			Assert.AreEqual(1, Hook.Started.Count);
			Assert.AreSame(removed, Hook.StartedModels[0], "削除対象の旧 ViewModel を渡す");
			Assert.IsTrue(Hook.Started[0].activeSelf, "待機中は非 active にしない");
			Assert.AreEqual(0, m_Binder.PooledCount, "待機中はプールへ返さない");
			Assert.AreEqual(1, m_Binder.PendingReleaseCount);

			Hook.CompleteAll();
			yield return null;

			Assert.AreEqual(0, m_Binder.PendingReleaseCount);
			Assert.AreEqual(1, m_Binder.PooledCount);
			Assert.IsFalse(Hook.Started[0].activeSelf);
		}

		[Test]
		public void 待機中の行はViewModelから切り離される()
		{
			var vm = Fill<HookBinder>("A");
			var removed = vm.Rows[0];

			vm.Rows.Clear();
			Tick();

			Assert.AreEqual(1, m_Binder.PendingReleaseCount);
			Assert.IsNull(Hook.Started[0].GetComponent<View>().ViewModel, "待機中の行は更新されない");
			Assert.AreEqual(1, removed.UnbindCount, "OnUnbindまで通す");
		}

		[UnityTest]
		public IEnumerator 複数行の削除を同時に待機できる()
		{
			var vm = Fill<HookBinder>("A", "B", "C");

			vm.Rows.Clear();
			Tick();

			Assert.AreEqual(3, Hook.Started.Count);
			Assert.AreEqual(3, m_Binder.PendingReleaseCount);
			Assert.AreEqual(0, m_Binder.PooledCount);

			Hook.CompleteAll();
			yield return null;

			Assert.AreEqual(0, m_Binder.PendingReleaseCount);
			Assert.AreEqual(3, m_Binder.PooledCount);
		}

		[UnityTest]
		public IEnumerator 待機中の行は再利用に回さない()
		{
			var vm = Fill<HookBinder>("A");
			var removed = vm.Rows[0];

			vm.Rows.RemoveAt(0);
			Tick();
			Assert.AreEqual(1, m_Binder.PendingReleaseCount);
			var pendingInstance = Hook.Started[0];

			vm.Rows.Add(removed);
			Tick();

			Assert.AreEqual(2, ActiveRows(), "待機中の行とは別に新しい行を出す");
			Assert.AreEqual(1, m_Binder.PendingReleaseCount, "同じ ViewModel の再追加でも待機は続く");
			Assert.AreEqual(1, Hook.Started.Count, "再追加は削除扱いにしない");

			Hook.CompleteAll();
			yield return null;

			Assert.AreEqual(1, m_Binder.PooledCount);
			Assert.IsFalse(pendingInstance.activeSelf);
			Assert.AreEqual(1, ActiveRows());
		}

		[UnityTest]
		public IEnumerator 待機中でも他の行の追加を止めない()
		{
			var vm = Fill<HookBinder>("A", "B");

			vm.Rows.RemoveAt(0);
			Tick();
			vm.Rows.Add(new RowViewModel { Label = "C" });
			Tick();

			Assert.AreEqual(3, ActiveRows(), "待機中の 1 行と有効な 2 行");
			Assert.AreEqual(1, m_Binder.PendingReleaseCount);

			Hook.CompleteAll();
			yield return null;

			Assert.AreEqual(2, ActiveRows());
		}

		[Test]
		public void フックは並べ替えの前に呼ばれる()
		{
			var vm = Fill<HookBinder>("A", "B", "C");

			vm.Rows.RemoveAt(0);
			Tick();

			Assert.AreEqual(1, Hook.Started.Count);
			Assert.AreEqual(0, Hook.StartedSibling[0], "削除前の位置を取得できる");
		}

		[UnityTest]
		public IEnumerator Binding解除で取消して回収する()
		{
			var vm = Fill<HookBinder>("A");

			vm.Rows.Clear();
			Tick();
			Assert.AreEqual(1, m_Binder.PendingReleaseCount);

			m_View.Attach(null);

			Assert.AreEqual(1, Hook.Canceled, "待機中の処理へ取消を通知する");
			Assert.AreEqual(0, m_Binder.PendingReleaseCount, "終了を待たせない");
			Assert.AreEqual(1, m_Binder.PooledCount);

			yield return null;
			Assert.AreEqual(1, m_Binder.PooledCount, "遅れて完了しても二重に返さない");
		}

		[UnityTest]
		public IEnumerator 非active化で取消して回収する()
		{
			var vm = Fill<HookBinder>("A");

			vm.Rows.Clear();
			Tick();
			Assert.AreEqual(1, m_Binder.PendingReleaseCount);

			m_Binder.gameObject.SetActive(false);

			Assert.AreEqual(1, Hook.Canceled);
			Assert.AreEqual(0, m_Binder.PendingReleaseCount);
			Assert.AreEqual(1, m_Binder.PooledCount);

			yield return null;
			Assert.AreEqual(1, m_Binder.PooledCount);
		}

		[UnityTest]
		public IEnumerator 取消に応じない処理でも終了を待たせない()
		{
			var vm = Fill<HookBinder>("A");

			vm.Rows.Clear();
			Tick();
			var gate = Hook.Gates[0];
			Hook.Gates.Clear();

			m_View.Attach(null);

			Assert.AreEqual(0, m_Binder.PendingReleaseCount, "取消に応じなくても待たない");
			Assert.AreEqual(1, m_Binder.PooledCount);

			gate.TrySetResult(true);
			yield return null;

			Assert.AreEqual(1, m_Binder.PooledCount, "遅れて完了しても回収は一度だけ");
		}

		[Test]
		public void フックが同期的に例外を投げても回収する()
		{
			var vm = Fill<HookBinder>("A");
			Hook.ThrowOnStart = true;

			LogAssert.Expect(LogType.Error, HookError);
			vm.Rows.Clear();
			Tick();

			Assert.AreEqual(0, m_Binder.PendingReleaseCount);
			Assert.AreEqual(1, m_Binder.PooledCount, "行が残留しない");
		}

		[UnityTest]
		public IEnumerator フックが失敗しても行が残留しない()
		{
			var vm = Fill<HookBinder>("A");

			vm.Rows.Clear();
			Tick();
			Assert.AreEqual(1, m_Binder.PendingReleaseCount);

			LogAssert.Expect(LogType.Error, HookError);
			Hook.FaultAll();
			yield return null;

			Assert.AreEqual(0, m_Binder.PendingReleaseCount);
			Assert.AreEqual(1, m_Binder.PooledCount);
		}

		[UnityTest]
		public IEnumerator 破棄しても待機が残らない()
		{
			var vm = Fill<HookBinder>("A");
			var binder = Hook;

			vm.Rows.Clear();
			Tick();
			Assert.AreEqual(1, m_Binder.PendingReleaseCount);

			UnityEngine.Object.DestroyImmediate(m_Root);
			m_Root = null;
			m_View = null;

			Assert.AreEqual(1, binder.Canceled);
			yield return null;
		}
	}
}
