using System;
using System.Collections.Generic;

namespace UVMBinding.Core
{
	internal static class ListPool<T>
	{
		public struct Scope : IDisposable
		{
			public List<T> Value;

			public Scope(List<T> value)
			{
				Value = value;
			}

			public void Dispose()
			{
				Push(Value);
				Value = null;
			}
		}

		static Stack<List<T>> s_Pool = new();

		public static List<T> Pop()
		{
			if (s_Pool.Count > 0)
			{
				return s_Pool.Pop();
			}
			return new List<T>();
		}

		public static void Push(List<T> list)
		{
			if (list != null)
			{
				list.Clear();
				s_Pool.Push(list);
			}
		}

		public static Scope Use(out List<T> list)
		{
			list = Pop();
			return new Scope(list);
		}

	}

}