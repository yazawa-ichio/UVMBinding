using NUnit.Framework;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class ValueHolderTests
	{
		class Animal { }
		class Dog : Animal { }
		class Cat : Animal { }

		[Test]
		public void 完全一致で読み書きできる()
		{
			IValueHolder holder = new ValueHolder<int> { Value = 1 };

			Assert.IsTrue(holder.TrySetValue(2, out var changed));
			Assert.IsTrue(changed);
			Assert.IsTrue(holder.TryGetValue<int>(out var value));
			Assert.AreEqual(2, value);
		}

		[Test]
		public void 同じ値を書いても変化と見なさない()
		{
			IValueHolder holder = new ValueHolder<int> { Value = 1 };

			Assert.IsTrue(holder.TrySetValue(1, out var changed));
			Assert.IsFalse(changed);
		}

		[Test]
		public void 代入できる向きの書き込みは通る()
		{
			IValueHolder holder = new ValueHolder<Animal>();

			Assert.IsTrue(holder.TrySetValue(new Dog(), out var changed));
			Assert.IsTrue(changed);
			Assert.IsInstanceOf<Dog>(holder.GetObject());
		}

		[Test]
		public void 代入できない向きの書き込みは通らない()
		{
			IValueHolder holder = new ValueHolder<Dog>();

			Assert.IsFalse(holder.TrySetValue(new Animal(), out var changed));
			Assert.IsFalse(changed);
			Assert.IsNull(holder.GetObject());
		}

		[Test]
		public void 無関係な型の書き込みは通らない()
		{
			IValueHolder holder = new ValueHolder<int> { Value = 1 };

			Assert.IsFalse(holder.TrySetValue("Text", out _));
			Assert.AreEqual(1, holder.GetObject());
		}

		[Test]
		public void 代入できる向きの読み取りは通る()
		{
			IValueHolder holder = new ValueHolder<Dog> { Value = new Dog() };

			Assert.IsTrue(holder.TryGetValue<Animal>(out var value));
			Assert.IsInstanceOf<Dog>(value);
		}

		[Test]
		public void 代入できない向きの読み取りは通らない()
		{
			IValueHolder holder = new ValueHolder<Animal> { Value = new Dog() };

			Assert.IsFalse(holder.TryGetValue<Dog>(out _));
		}

		[Test]
		public void 実行時の値が一致すればダウンキャストで取り出せる()
		{
			IValueHolder holder = new ValueHolder<Animal> { Value = new Dog() };

			Assert.IsTrue(holder.TryGetValueAs<Dog>(out var dog));
			Assert.IsNotNull(dog);
		}

		[Test]
		public void 実行時の値が違えばダウンキャストできない()
		{
			IValueHolder holder = new ValueHolder<Animal> { Value = new Cat() };

			Assert.IsFalse(holder.TryGetValueAs<Dog>(out _));
		}

		[Test]
		public void Object格納には任意の型を書ける()
		{
			IValueHolder holder = new ValueHolder<object>();

			Assert.IsTrue(holder.TrySetValue(10, out _));
			Assert.AreEqual(10, holder.GetObject());

			Assert.IsTrue(holder.TrySetValue("Text", out _));
			Assert.AreEqual("Text", holder.GetObject());

			Assert.IsTrue(holder.TryGetValueAs<string>(out var text));
			Assert.AreEqual("Text", text);
		}
	}
}