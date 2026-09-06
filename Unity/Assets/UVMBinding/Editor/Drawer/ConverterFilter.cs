using System;
using System.Collections.Generic;
using UVMBinding.Converters;
using UVMBinding.Core;

namespace UVMBinding.Drawer
{
	public enum ConverterGroup
	{
		Direct,
		NeedsCast,
		Mismatch,
	}

	public static class ConverterFilter
	{
		static readonly Dictionary<Type, IConverter> s_Instances = new Dictionary<Type, IConverter>();

		public static Type GetBinderValueType(Type binderType)
		{
			for (var type = binderType; type != null; type = type.BaseType)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Binder<>))
				{
					return type.GetGenericArguments()[0];
				}
			}
			return null;
		}

		public static int Count => ConverterList.GetNames().Length - 1;

		public static Type GetCandidateType(int index)
		{
			return ConverterList.GetType(index);
		}

		public static ConverterGroup Classify(Type converterType, Type inputType, Type requiredType)
		{
			var instance = converterType != null ? GetInstance(converterType) : null;
			if (instance == null)
			{
				return ConverterGroup.Direct;
			}
			var entry = Rank(inputType, instance.InputType);
			var exit = Rank(instance.OutputType, requiredType);
			return entry > exit ? entry : exit;
		}

		public static void GetGroups(Type inputType, Type requiredType,
			List<int> direct, List<int> needsCast, List<int> mismatch)
		{
			var names = ConverterList.GetNames();
			for (int i = 1; i < names.Length; i++)
			{
				switch (Classify(ConverterList.GetType(i), inputType, requiredType))
				{
					case ConverterGroup.Direct:
						direct.Add(i);
						break;
					case ConverterGroup.NeedsCast:
						needsCast.Add(i);
						break;
					default:
						mismatch.Add(i);
						break;
				}
			}
		}

		static ConverterGroup Rank(Type from, Type to)
		{
			if (from == null || to == null)
			{
				return ConverterGroup.Direct;
			}
			if (to.IsAssignableFrom(from) || CastRegistry.IsImplicit(from, to))
			{
				return ConverterGroup.Direct;
			}
			return CastRegistry.IsRegistered(from, to) ? ConverterGroup.NeedsCast : ConverterGroup.Mismatch;
		}

		static IConverter GetInstance(Type converterType)
		{
			if (s_Instances.TryGetValue(converterType, out var instance))
			{
				return instance;
			}
			try
			{
				instance = Activator.CreateInstance(converterType) as IConverter;
			}
			catch (Exception)
			{
				instance = null;
			}
			return s_Instances[converterType] = instance;
		}
	}
}
