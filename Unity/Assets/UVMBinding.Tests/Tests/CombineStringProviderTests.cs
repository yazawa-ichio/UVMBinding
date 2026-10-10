using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using UVMBinding.Core;
using UVMBinding.PropertyProvider;
using AllocIs = UnityEngine.TestTools.Constraints.Is;

namespace UVMBinding.Tests
{
	public class CombineStringProviderTests
	{
		class Extension : IViewModelPropertyExtension
		{
			public readonly PropertyContainer Source = new PropertyContainer();
			public readonly List<string> Outputs = new List<string>();
			public int SetCount;

			public void Set<T>(string name, T value)
			{
				SetCount++;
				Outputs.Add(value as string);
			}

			public void Subscribe<T>(string name, Action<T> action)
			{
				Source.Subscribe<T>(name, action, true);
			}
		}

		static CombineStringProvider Create(string format, string valueKey, params string[] sources)
		{
			var provider = new CombineStringProvider();
			SetField(provider, "m_Format", format);
			SetField(provider, "m_ValueKey", valueKey);
			SetField(provider, "m_SourceKeys", sources);
			return provider;
		}

		static void SetField(object target, string name, object value)
		{
			var field = target.GetType().GetField(name,
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.IsNotNull(field, $"フィールドが見つからない: {name}");
			field.SetValue(target, value);
		}

		Extension Setup(out CombineStringProvider provider)
		{
			var extension = new Extension();
			provider = Create("{0} / Lv.{1}", "Caption", "Name", "Level");
			extension.Source.GetOrCreate<string>("Name");
			extension.Source.GetOrCreate<int>("Level");
			provider.Init(extension);
			extension.Source.TrySetValue("Name", "ゆん");
			extension.Source.TrySetValue("Level", 1);
			return extension;
		}

		[Test]
		public void 依存元が揃うと文字列が出る()
		{
			var extension = Setup(out _);

			Assert.AreEqual("ゆん / Lv.1", extension.Outputs[extension.Outputs.Count - 1]);
		}

		[Test]
		public void 参照型の依存元は揃うまで出力しない()
		{
			var extension = new Extension();
			var provider = Create("{0} / {1}", "Caption", "Name", "Title");
			extension.Source.GetOrCreate<string>("Name");
			extension.Source.GetOrCreate<string>("Title");
			provider.Init(extension);

			extension.Source.TrySetValue("Name", "ゆん");

			Assert.AreEqual(0, extension.SetCount, "片方だけでは出さない");
		}

		[Test]
		public void 値型の依存元は既定値でも揃ったとみなす()
		{
			var extension = new Extension();
			var provider = Create("{0} / Lv.{1}", "Caption", "Name", "Level");
			extension.Source.GetOrCreate<string>("Name");
			extension.Source.GetOrCreate<int>("Level");
			provider.Init(extension);

			extension.Source.TrySetValue("Name", "ゆん");

			Assert.AreEqual(1, extension.SetCount, "Level 未設定でも出力される");
			Assert.AreEqual("ゆん / Lv.0", extension.Outputs[0], "既定値がそのまま出る");
		}

		[Test]
		public void 同じ値を入れ直しても再評価しない()
		{
			var extension = Setup(out _);
			var before = extension.SetCount;

			extension.Source.TrySetValue("Level", 1);

			Assert.AreEqual(before, extension.SetCount, "値が変わらなければ何もしない");
		}

		[Test]
		public void 値型の依存元はボックス化する()
		{
			var extension = Setup(out _);
			int next = 1;
			void Change() { extension.Source.TrySetValue("Level", ++next); }
			Change();

			Assert.That(Change, AllocIs.AllocatingGCMemory(),
				"現状はアロケートする。TASK-021 の計測結果");
		}

		[Test]
		public void 文字列が変わらなくても書式化は走る()
		{
			var extension = new Extension();
			var provider = Create("固定", "Caption", "Level");
			extension.Source.GetOrCreate<int>("Level");
			provider.Init(extension);
			extension.Source.TrySetValue("Level", 1);
			var before = extension.SetCount;

			extension.Source.TrySetValue("Level", 2);

			Assert.AreEqual(before + 1, extension.SetCount, "同じ文字列でも呼ばれる");
		}
	}
}
