using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UVMBinding.Core;
using AllocIs = UnityEngine.TestTools.Constraints.Is;

namespace UVMBinding.Tests
{
	public class AllocationTests
	{
		const int Count = 1000000;

		static void WithoutTraceLog(System.Action action)
		{
			var level = Logger.Log.Level;
			Logger.Log.Level = Logger.LogLevel.Error;
			try { action(); }
			finally { Logger.Log.Level = level; }
		}

		[Test]
		public void 完全一致の書き込みでアロケーションしない()
		{
			WithoutTraceLog(() =>
			{
				var vm = new GeneralViewModel();
				vm.Set("P1", 0);
				vm.Set("P1", 1);

				int next = 2;
				TestDelegate act = () => { vm.Set("P1", next); };
				act(); act(); act();
				Assert.That(act, AllocIs.Not.AllocatingGCMemory());
			});
		}

		[Test]
		public void 購読ありの書き込みでアロケーションしない()
		{
			WithoutTraceLog(() =>
			{
				var vm = new GeneralViewModel();
				IViewModel ivm = vm;
				vm.Set("P1", 0);
				int received = 0;
				ivm.Property.Subscribe<int>("P1", x => received = x);
				vm.Set("P1", 1);

				int next = 2;
				TestDelegate act = () => { vm.Set("P1", next); };
				act(); act(); act();
				Assert.That(act, AllocIs.Not.AllocatingGCMemory());
				Assert.AreEqual(2, received);
			});
		}

		static long MinElapsed(int repeat, System.Action<int> loop)
		{
			long min = long.MaxValue;
			var watch = new Stopwatch();
			for (int r = 0; r < repeat; r++)
			{
				watch.Reset();
				watch.Start();
				loop(Count);
				watch.Stop();
				if (watch.ElapsedTicks < min)
				{
					min = watch.ElapsedTicks;
				}
			}
			return min * 1000 / Stopwatch.Frequency;
		}
	}
}