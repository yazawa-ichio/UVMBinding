using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UVMBinding.Drawer;

namespace UVMBinding.EditorTests
{
	public class ConverterFilterTests
	{
		static void Groups(System.Type inputType, System.Type requiredType,
			out List<int> direct, out List<int> needsCast, out List<int> mismatch)
		{
			direct = new List<int>();
			needsCast = new List<int>();
			mismatch = new List<int>();
			ConverterFilter.GetGroups(inputType, requiredType, direct, needsCast, mismatch);
		}

		static bool Contains(List<int> group, string name)
		{
			return group.Exists(x => ConverterFilter.GetCandidateType(x).Name == name);
		}

		[Test]
		public void Binderの出力型が引ける()
		{
			Assert.AreEqual(typeof(bool), ConverterFilter.GetBinderValueType(typeof(Binders.ActiveBinder)));
			Assert.AreEqual(typeof(Vector3), ConverterFilter.GetBinderValueType(typeof(Binders.ScaleBinder)));
			Assert.AreEqual(typeof(Vector2), ConverterFilter.GetBinderValueType(typeof(Binders.AnchoredPositionBinder)));
			Assert.IsNull(ConverterFilter.GetBinderValueType(typeof(View)));
		}

		[Test]
		public void 候補は消えない()
		{
			int all = ConverterFilter.Count;
			foreach (var required in new[] { typeof(bool), typeof(string), typeof(float), typeof(Vector3) })
			{
				foreach (var input in new System.Type[] { null, typeof(int), typeof(string) })
				{
					Groups(input, required, out var direct, out var needsCast, out var mismatch);
					Assert.AreEqual(all, direct.Count + needsCast.Count + mismatch.Count,
						$"{Name(input)} -> {required.Name}");
				}
			}
		}

		[Test]
		public void 群は重複しない()
		{
			Groups(typeof(int), typeof(bool), out var direct, out var needsCast, out var mismatch);
			var seen = new HashSet<int>();
			foreach (var group in new[] { direct, needsCast, mismatch })
			{
				foreach (var index in group)
				{
					Assert.IsTrue(seen.Add(index), $"{ConverterFilter.GetCandidateType(index).Name} が複数の群に出ている");
				}
			}
		}

		[Test]
		public void そのまま繋がる候補が第1群に出る()
		{
			Groups(typeof(int), typeof(bool), out var direct, out _, out _);

			Assert.IsTrue(Contains(direct, "IntComparison"));
			Groups(typeof(byte), typeof(bool), out var fromByte, out _, out _);
			Assert.IsTrue(Contains(fromByte, "IntComparison"), "byte -> int は自動で挟まる");
		}

		[Test]
		public void 許可が要る候補が第2群に出る()
		{
			Groups(typeof(int), typeof(bool), out var direct, out var needsCast, out _);

			Assert.IsFalse(Contains(direct, "FloatComparison"), "そのままでは繋がらない");
			Assert.IsTrue(Contains(needsCast, "FloatComparison"), "許可すれば繋がる");
		}

		[Test]
		public void 型が合わない候補も必ず出る()
		{
			Groups(typeof(string), typeof(string), out var direct, out var needsCast, out var mismatch);

			Assert.IsFalse(Contains(direct, "BoolInverse"));
			Assert.IsFalse(Contains(needsCast, "BoolInverse"));
			Assert.IsTrue(Contains(mismatch, "BoolInverse"), "型が合わなくても選べる");
		}

		[Test]
		public void 入口の型が引けなければ出口だけで分ける()
		{
			Groups(null, typeof(string), out var direct, out _, out _);
			Groups(typeof(string), typeof(string), out var withInput, out _, out _);

			Assert.IsTrue(Contains(direct, "ObjectToString"));
			Assert.GreaterOrEqual(direct.Count, withInput.Count);
		}

		[Test]
		public void 出口は次段の入力型で判定する()
		{
			Groups(typeof(string), typeof(string), out var byBinderType, out _, out _);
			Groups(typeof(string), typeof(object), out var byNextInput, out _, out _);

			Assert.IsFalse(Contains(byBinderType, "StringToLength"), "T で判定すると第 1 群に出ない");
			Assert.IsTrue(Contains(byNextInput, "StringToLength"), "次段の入力型なら第 1 群に出る");
		}

		[Test]
		public void 出力が自動で挟まる変換で移せる候補は第1群に出る()
		{
			Groups(typeof(string), typeof(long), out var toLong, out _, out _);
			Assert.IsTrue(Contains(toLong, "StringToLength"));

			Groups(typeof(string), typeof(byte), out var toByte, out var needsCast, out _);
			Assert.IsFalse(Contains(toByte, "StringToLength"));
			Assert.IsTrue(Contains(needsCast, "StringToLength"), "許可すれば繋がる");
		}

		static string Name(System.Type type) => type != null ? type.Name : "null";
	}
}
