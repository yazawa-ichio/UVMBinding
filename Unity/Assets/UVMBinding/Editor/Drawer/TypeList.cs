using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UVMBinding.Arguments;

namespace UVMBinding.Drawer
{
	internal class ConverterList : TypeList<IConverter> { }

	public class EventArgumentList : TypeList<EventArgument> { }

	public class TypeList<T>
	{
		class Entry
		{
			public readonly Type Type;
			public readonly string Name;
			public readonly string DispName;

			public Entry(Type type)
			{
				Type = type;
				Name = type.Assembly.GetName().Name + " " + type.FullName;
				DispName = type.Name;
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

		static TypeList()
		{
			s_Entries = TypeCache.GetTypesDerivedFrom<T>()
			   .Where(x => !x.IsAbstract && !x.IsInterface && !x.IsGenericType)
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



}