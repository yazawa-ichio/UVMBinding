using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UVMBinding.Binders;
using UVMBinding.Core;
using AllocIs = UnityEngine.TestTools.Constraints.Is;

namespace UVMBinding.Tests
{
	public class CollectionDiffTests
	{
		public class RowViewModel : ViewModel
		{
			[Bind] public string Label { get; set; }
		}

		public class ListViewModel : ViewModel
		{
			public ListViewModel()
			{
				Rows = new Collection<RowViewModel>(nameof(Rows), this);
			}

			public Collection<RowViewModel> Rows { get; }
		}

		public class CountingRowBinder : Binder<string>, ICollectionIndex
		{
			public int BindCount;
			public int Index { get; set; }

			protected override void OnBind()
			{
				BindCount++;
			}

			protected override void UpdateValue(string value) { }
		}

		GameObject m_Root;
		GameObject m_Template;
		CollectionBinder m_Binder;

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null) Object.DestroyImmediate(m_Root);
			if (m_Template != null) Object.DestroyImmediate(m_Template);
			m_Root = null;
			m_Template = null;
		}

		ListViewModel Setup()
		{
			m_Template = new GameObject("Row");
			m_Template.AddComponent<View>();
			m_Template.AddComponent<CountingRowBinder>().ChangePath("Label");
			m_Template.SetActive(false);

			m_Root = new GameObject("Root");
			var view = m_Root.AddComponent<View>();
			var host = new GameObject("List");
			host.transform.SetParent(m_Root.transform, false);
			m_Binder = host.AddComponent<CollectionBinder>();
			m_Binder.ChangePath("Rows");
			SetPrivate(m_Binder, "m_Preafb", m_Template);

			var vm = view.Attach<ListViewModel>();
			return vm;
		}

		static void SetPrivate(object target, string name, object value)
		{
			var field = target.GetType().GetField(name,
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.IsNotNull(field, $"フィールドが見つからない: {name}");
			field.SetValue(target, value);
		}

		List<CountingRowBinder> Rows()
		{
			var list = new List<CountingRowBinder>();
			foreach (var binder in m_Binder.GetComponentsInChildren<CountingRowBinder>(true))
			{
				list.Add(binder);
			}
			return list;
		}

		List<CountingRowBinder> ActiveRows()
		{
			return Rows().FindAll(x => x.gameObject.activeSelf);
		}

		void Tick()
		{
			m_Root.GetComponent<View>().TryUpdate();
		}

		[Test]
		public void 追加しても既存の行は結び直さない()
		{
			var vm = Setup();
			for (int i = 0; i < 3; i++)
			{
				vm.Rows.Add(new RowViewModel { Label = "R" + i });
			}
			Tick();
			var rows = Rows();
			Assert.AreEqual(3, rows.Count);
			var before = rows.ConvertAll(x => x.BindCount);

			vm.Rows.Add(new RowViewModel { Label = "R3" });
			Tick();

			rows = ActiveRows();
			Assert.AreEqual(4, rows.Count, "1 行増える");
			for (int i = 0; i < before.Count; i++)
			{
				Assert.AreEqual(before[i], rows[i].BindCount, $"{i} 行目が結び直されていない");
			}
		}

		[Test]
		public void 削除した行はプールに戻る()
		{
			var vm = Setup();
			for (int i = 0; i < 3; i++)
			{
				vm.Rows.Add(new RowViewModel { Label = "R" + i });
			}
			Tick();
			var instances = Rows().ConvertAll(x => x.gameObject);
			Assert.AreEqual(0, m_Binder.PooledCount);

			vm.Rows.RemoveAt(2);
			Tick();

			Assert.AreEqual(1, m_Binder.PooledCount, "Destroy せずプールへ返す");
			Assert.IsNotNull(instances[2], "破棄されていない");
			Assert.IsFalse(instances[2].activeSelf, "見えないようにする");
		}

		[Test]
		public void 再追加でプールから使い回す()
		{
			var vm = Setup();
			vm.Rows.Add(new RowViewModel { Label = "A" });
			vm.Rows.Add(new RowViewModel { Label = "B" });
			Tick();
			var removed = Rows()[1].gameObject;
			vm.Rows.RemoveAt(1);
			Tick();
			Assert.AreEqual(1, m_Binder.PooledCount);

			vm.Rows.Add(new RowViewModel { Label = "C" });
			Tick();

			Assert.AreEqual(0, m_Binder.PooledCount, "プールから取り出す");
			var rows = ActiveRows();
			Assert.AreEqual(2, rows.Count);
			Assert.AreSame(removed, rows[1].gameObject, "同じ GameObject を使い回す");
		}

		[Test]
		public void 並べ替えで生成しない()
		{
			var vm = Setup();
			var first = new RowViewModel { Label = "A" };
			var second = new RowViewModel { Label = "B" };
			vm.Rows.Add(first);
			vm.Rows.Add(second);
			Tick();
			var instances = Rows().ConvertAll(x => x.gameObject);

			vm.Rows.RemoveAt(0);
			vm.Rows.Add(first);
			Tick();

			var after = ActiveRows().ConvertAll(x => x.gameObject);
			Assert.AreEqual(2, after.Count);
			CollectionAssert.AreEquivalent(instances, after, "同じ GameObject が使われる");
			Assert.AreEqual(0, m_Binder.PooledCount, "生成も回収もしない");
		}

		[Test]
		public void 各操作の後で添字が正しい()
		{
			var vm = Setup();
			for (int i = 0; i < 3; i++)
			{
				vm.Rows.Add(new RowViewModel { Label = "R" + i });
			}
			Tick();
			AssertIndices();

			vm.Rows.RemoveAt(0);
			Tick();
			AssertIndices();

			vm.Rows.Insert(0, new RowViewModel { Label = "X" });
			Tick();
			AssertIndices();
		}

		void AssertIndices()
		{
			var rows = ActiveRows();
			for (int i = 0; i < rows.Count; i++)
			{
				Assert.AreEqual(i, rows[i].Index, $"{i} 行目の添字");
			}
		}

		[Test]
		public void SetDirtyで全体が最新になる()
		{
			var vm = Setup();
			var row = new RowViewModel { Label = "A" };
			vm.Rows.Add(row);
			Tick();
			var before = Rows()[0].BindCount;

			vm.Rows.SetDirty();
			Tick();

			Assert.AreEqual(1, Rows().Count);
			Assert.AreEqual(before, Rows()[0].BindCount, "並びも中身も同じなら結び直さない");
		}

		[Test]
		public void 空にすると全部プールに戻る()
		{
			var vm = Setup();
			for (int i = 0; i < 3; i++)
			{
				vm.Rows.Add(new RowViewModel { Label = "R" + i });
			}
			Tick();

			vm.Rows.Clear();
			Tick();

			Assert.AreEqual(3, m_Binder.PooledCount);
			Assert.AreEqual(0, Rows().FindAll(x => x.gameObject.activeSelf).Count, "表示は 0 行");
			Assert.AreEqual(3, Rows().Count, "GameObject は残して使い回す");
		}

		[Test]
		public void 変更が無いフレームは何もしない()
		{
			var vm = Setup();
			vm.Rows.Add(new RowViewModel { Label = "A" });
			Tick();
			var before = Rows()[0].BindCount;

			Tick();
			Tick();

			Assert.AreEqual(before, Rows()[0].BindCount);
		}

		[Test]
		public void 要素100個に1個追加したときの生成と再バインドを測る()
		{
			var vm = Setup();
			for (int i = 0; i < 100; i++)
			{
				vm.Rows.Add(new RowViewModel { Label = "R" + i });
			}
			Tick();
			int createdBefore = Rows().Count;
			var bindsBefore = ActiveRows().ConvertAll(x => x.BindCount);

			vm.Rows.Add(new RowViewModel { Label = "X" });
			Tick();

			int created = Rows().Count - createdBefore;
			int rebound = 0;
			var rows = ActiveRows();
			for (int i = 0; i < bindsBefore.Count; i++)
			{
				rebound += rows[i].BindCount - bindsBefore[i];
			}
			Debug.Log($"[CollectionDiff] rows=100 add=1 instantiate={created} rebind={rebound}");
			Assert.AreEqual(1, created, "増えた分だけ生成する");
			Assert.AreEqual(0, rebound, "既存の 100 行は結び直さない");

			vm.Rows.RemoveAt(100);
			Tick();
			createdBefore = Rows().Count;
			vm.Rows.Add(new RowViewModel { Label = "Y" });
			Tick();
			Debug.Log($"[CollectionDiff] pooled add=1 instantiate={Rows().Count - createdBefore}");
			Assert.AreEqual(createdBefore, Rows().Count, "プールがあれば生成しない");
		}

		[Test]
		public void 追加と削除を繰り返したときの確保量を測る()
		{
			var vm = Setup();
			for (int i = 0; i < 100; i++)
			{
				vm.Rows.Add(new RowViewModel { Label = "R" + i });
			}
			Tick();

			var extra = new RowViewModel { Label = "X" };
			void Cycle()
			{
				vm.Rows.Add(extra);
				Tick();
				vm.Rows.Remove(extra);
				Tick();
			}

			for (int i = 0; i < 5; i++) Cycle();

			const int Loops = 50;
			void Idle()
			{
				vm.Rows.SetDirty();
				Tick();
			}
			for (int i = 0; i < 5; i++) Idle();
			long idleBefore = System.GC.GetTotalMemory(true);
			for (int i = 0; i < Loops; i++) Idle();
			long idleAfter = System.GC.GetTotalMemory(true);
			Debug.Log($"[CollectionDiff] rows=100 idle x{Loops} alloc={idleAfter - idleBefore} bytes"
				+ $" ({(idleAfter - idleBefore) / (float)Loops:0.0}/update)");

			long before = System.GC.GetTotalMemory(true);
			for (int i = 0; i < Loops; i++) Cycle();
			long after = System.GC.GetTotalMemory(true);
			Debug.Log($"[CollectionDiff] rows=100 addremove x{Loops} alloc={after - before} bytes"
				+ $" ({(after - before) / (float)Loops:0.0}/cycle) pooled={m_Binder.PooledCount}");

			Assert.AreEqual(100, ActiveRows().Count, "元の行数に戻る");
			Assert.AreEqual(1, m_Binder.PooledCount, "使い回した 1 行がプールに残る");
		}
	}
}
