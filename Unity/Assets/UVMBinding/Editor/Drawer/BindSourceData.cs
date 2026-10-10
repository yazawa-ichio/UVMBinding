using System;
using System.Collections.Generic;
using System.Reflection;
using UVMBinding.Core;

namespace UVMBinding.Drawer
{
	public static class BindSourceData
	{
		static readonly Dictionary<string, Type> s_TypeCache = new Dictionary<string, Type>();

		public static Type FindType(string fullName)
		{
			if (string.IsNullOrEmpty(fullName))
			{
				return null;
			}
			var type = ViewModelList.GetType(fullName);
			if (type != null)
			{
				return type;
			}
			if (s_TypeCache.TryGetValue(fullName, out var cached))
			{
				return cached;
			}
			return s_TypeCache[fullName] = FindAny(fullName);
		}

		static Type FindAny(string fullName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = assembly.GetType(fullName, false);
				if (type != null)
				{
					return type;
				}
			}
			return null;
		}

		public static List<string> GetBindPaths(Type type)
		{
			var paths = new List<string>();
			if (type == null)
			{
				return paths;
			}
			if (typeof(IViewModel).IsAssignableFrom(type))
			{
				foreach (var entry in ViewModelData.Get(type).Properties)
				{
					paths.Add(entry.Path);
				}
				return paths;
			}
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			for (var current = type; current != null && current != typeof(object); current = current.BaseType)
			{
				foreach (var field in current.GetFields(flags))
				{
					if (field.Name.IndexOf('<') >= 0)
					{
						continue;
					}
					Add(paths, field.Name);
				}
				foreach (var property in current.GetProperties(flags))
				{
					if (property.CanRead && property.GetIndexParameters().Length == 0)
					{
						Add(paths, property.Name);
					}
				}
			}
			return paths;
		}

		public static List<string> GetEventPaths(Type type)
		{
			var paths = new List<string>();
			if (type == null)
			{
				return paths;
			}
			if (typeof(IViewModel).IsAssignableFrom(type))
			{
				foreach (var entry in ViewModelData.Get(type).Events)
				{
					paths.Add(entry.Path);
				}
				return paths;
			}
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			for (var current = type; current != null && current != typeof(object); current = current.BaseType)
			{
				foreach (var method in current.GetMethods(flags))
				{
					if (method.IsSpecialName)
					{
						continue;
					}
					if (method.GetParameters().Length <= 1)
					{
						Add(paths, method.Name);
					}
				}
			}
			return paths;
		}

		static void Add(List<string> paths, string name)
		{
			if (!paths.Contains(name))
			{
				paths.Add(name);
			}
		}
	}
}
