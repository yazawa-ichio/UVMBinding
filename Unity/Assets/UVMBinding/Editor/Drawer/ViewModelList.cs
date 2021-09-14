using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UVMBinding.Core;

namespace UVMBinding.Drawer
{
	public static class ViewModelList
	{
		class Entry
		{
			public readonly Type Type;
			public readonly string Name;
			public readonly string DispName;

			public Entry(Type type)
			{
				Type = type;
				Name = type.FullName;
				DispName = Name.Replace(".", "/");
				var disp = type.GetCustomAttributes(typeof(DispNameAttribute), false).FirstOrDefault();
				if (disp != null)
				{
					DispName = (disp as DispNameAttribute).Name;
				}
			}
		}

		static Dictionary<string, Entry> s_Entries;
		static string[] s_Names;
		static string[] s_DispNames;

		static ViewModelList()
		{
			s_Entries = TypeCache.GetTypesDerivedFrom<IViewModel>()
			   .Where(x => !x.IsAbstract && !x.IsInterface)
			   .Select(x => new Entry(x)).ToDictionary(x => x.Name);
			s_Names = new string[] { "None" }.Concat(s_Entries.Select(x => x.Key)).ToArray();
			s_DispNames = new string[] { "None" }.Concat(s_Entries.Select(x => x.Value.DispName)).ToArray();
		}

		public static string[] GetDispNames()
		{
			return s_DispNames;
		}

		public static string[] GetNames()
		{
			return s_Names;
		}

		public static string GetDispName(string name)
		{
			var index = Array.IndexOf(s_Names, name);
			if (index < 0)
			{
				index = 0;
			}
			return s_DispNames[index];
		}

		public static int GetIndex(string name)
		{
			var index = Array.IndexOf(s_Names, name);
			if (index < 0)
			{
				index = 0;
			}
			return index;
		}

		public static Type GetType(string name)
		{
			if (s_Entries.TryGetValue(name, out var entry))
			{
				return entry.Type;
			}
			return null;
		}

		public static Type GetType(int index)
		{
			return GetType(s_Names[index]);
		}
	}

	public class ViewModelData
	{
		static Dictionary<Type, ViewModelData> s_Cache = new Dictionary<Type, ViewModelData>();

		public static ViewModelData Get(Type type)
		{
			if (s_Cache.TryGetValue(type, out var value))
			{
				return value;
			}
			return s_Cache[type] = new ViewModelData(type);
		}

		public class PropertyEntry
		{
			public readonly string Path;
			public readonly PropertyInfo Info;

			public PropertyEntry(string path, PropertyInfo property)
			{
				Path = path;
				Info = property;
			}
		}

		public class EventEntry
		{
			public readonly string Path;
			public readonly MethodInfo Info;
			public readonly PropertyInfo Property;

			public EventEntry(string path, MethodInfo method)
			{
				Path = path;
				Info = method;
			}

			public EventEntry(string path, PropertyInfo property)
			{
				Path = path;
				Property = property;
			}
		}

		Type m_Type;
		public PropertyEntry[] Properties;
		public EventEntry[] Events;

		public ViewModelData(Type type)
		{
			m_Type = type;
			var properties = new List<PropertyEntry>();
			var events = new List<EventEntry>();

			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				foreach (var attr in property.GetCustomAttributes<BindAttribute>())
				{
					var path = string.IsNullOrEmpty(attr.Path) ? property.Name : attr.Path;
					properties.Add(new PropertyEntry(path, property));
				}
				foreach (var attr in property.GetCustomAttributes<EventAttribute>())
				{
					var path = string.IsNullOrEmpty(attr.Path) ? property.Name : attr.Path;
					events.Add(new EventEntry(path, property));
				}
			}
			foreach (var _event in type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				foreach (var attr in _event.GetCustomAttributes<EventAttribute>())
				{
					var path = string.IsNullOrEmpty(attr.Path) ? _event.Name : attr.Path;
					events.Add(new EventEntry(path, _event.GetRaiseMethod()));
				}
			}
			foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				foreach (var attr in method.GetCustomAttributes<EventAttribute>())
				{
					var path = string.IsNullOrEmpty(attr.Path) ? method.Name : attr.Path;
					events.Add(new EventEntry(path, method));
				}
			}
			Properties = properties.ToArray();
			Events = events.ToArray();
		}

	}

}