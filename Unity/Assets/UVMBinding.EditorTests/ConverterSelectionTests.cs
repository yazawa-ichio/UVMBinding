using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UVMBinding.Converters;
using UVMBinding.Drawer;

namespace UVMBinding.EditorTests
{
	public class ConverterSelectionTests
	{
		public class IntRecordBinder : Binder<int>
		{
			protected override void UpdateValue(int value) { }
		}

		static System.Type ListType =>
			typeof(BinderDrawer).Assembly.GetType("UVMBinding.Drawer.ConverterList");

		const BindingFlags Flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

		static MethodInfo Method(string name, params System.Type[] args)
		{
			var m = ListType.GetMethod(name, Flags, null, args, null);
			Assert.IsNotNull(m, $"method not found: {name}");
			return m;
		}

		static int GetIndex(string name) =>
			(int)Method("GetIndex", typeof(string)).Invoke(null, new object[] { name });

		static System.Type GetType(int index) =>
			(System.Type)Method("GetType", typeof(int)).Invoke(null, new object[] { index });

		static string[] DispNames() =>
			(string[])Method("GetDispNames").Invoke(null, null);

		GameObject m_Root;

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null) Object.DestroyImmediate(m_Root);
			m_Root = null;
		}

		[Test]
		public void 段を挿しても直前の要素が複製されない()
		{
			m_Root = new GameObject("B");
			var binder = m_Root.AddComponent<IntRecordBinder>();
			var so = new UnityEditor.SerializedObject(binder);
			var list = so.FindProperty("m_Converters");
			list.arraySize = 1;
			list.GetArrayElementAtIndex(0).managedReferenceValue = new IntComparison();
			so.ApplyModifiedProperties();

			so.Update();
			list = so.FindProperty("m_Converters");
			list.InsertArrayElementAtIndex(1);
			list.GetArrayElementAtIndex(1).managedReferenceValue = new ObjectToString();
			so.ApplyModifiedProperties();

			var reloaded = new UnityEditor.SerializedObject(binder).FindProperty("m_Converters");
			Assert.AreEqual(2, reloaded.arraySize);
			Assert.AreEqual(typeof(IntComparison),
				GetType(GetIndex(reloaded.GetArrayElementAtIndex(0).managedReferenceFullTypename)));
			Assert.AreEqual(typeof(ObjectToString),
				GetType(GetIndex(reloaded.GetArrayElementAtIndex(1).managedReferenceFullTypename)),
				"直前の要素の複製になっていない");
		}

		[Test]
		public void 選んだConverterが保存され同じものとして読み戻せる()
		{
			m_Root = new GameObject("B");
			var binder = m_Root.AddComponent<IntRecordBinder>();
			var all = DispNames();

			foreach (var converter in new object[]
				{ new BoolSwitchInt(), new ObjectToString(), new StringToLength(), new IntComparison() })
			{
				var so = new UnityEditor.SerializedObject(binder);
				var list = so.FindProperty("m_Converters");
				list.arraySize = 1;
				list.GetArrayElementAtIndex(0).managedReferenceValue = converter;
				so.ApplyModifiedProperties();

				var reloaded = new UnityEditor.SerializedObject(binder);
				var typename = reloaded.FindProperty("m_Converters")
					.GetArrayElementAtIndex(0).managedReferenceFullTypename;
				var index = GetIndex(typename);

				Assert.AreNotEqual(0, index,
					$"None に戻っている: {converter.GetType().Name} typename={typename}");
				Assert.AreEqual(converter.GetType(), GetType(index), all[index]);
			}
		}

		[Test]
		public void 群に分けた候補の添字が型と一致する()
		{
			var all = DispNames();
			foreach (var (inputType, requiredType) in new[]
			{
				((System.Type)null, (System.Type)typeof(int)),
				(typeof(int), typeof(bool)),
				(typeof(bool), typeof(int)),
				(typeof(string), typeof(string)),
				(typeof(int), typeof(float)),
			})
			{
				var direct = new System.Collections.Generic.List<int>();
				var needsCast = new System.Collections.Generic.List<int>();
				var mismatch = new System.Collections.Generic.List<int>();
				ConverterFilter.GetGroups(inputType, requiredType, direct, needsCast, mismatch);

				var picks = new System.Collections.Generic.List<int>();
				picks.AddRange(direct);
				picks.AddRange(needsCast);
				picks.AddRange(mismatch);

				foreach (var index in picks)
				{
					Assert.AreNotEqual(0, index, "None は候補に出さない");
					var type = GetType(index);
					Assert.IsNotNull(type,
						$"input={inputType?.Name} required={requiredType.Name} index={index} name={all[index]}");
					Assert.AreEqual(all[index], all[GetIndex(
						type.Assembly.GetName().Name + " " + type.FullName)]);
				}
			}
		}
	}
}
