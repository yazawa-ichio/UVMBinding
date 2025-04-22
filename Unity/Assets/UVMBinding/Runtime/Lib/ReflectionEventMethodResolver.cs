using System;
using System.Collections.Generic;
using System.Reflection;

namespace UVMBinding.Core
{
	internal class ReflectionEventMethodResolver
	{

		static Dictionary<(Type type, string target, Type param), ReflectionEventMethodResolver> s_PropertyValueCache = new();

		public static ReflectionEventMethodResolver Get(Type type, string target, Type param)
		{
			var key = (type, target, param);
			if (!s_PropertyValueCache.TryGetValue(key, out var property))
			{
				s_PropertyValueCache[key] = property = new ReflectionEventMethodResolver(type, target, param);
			}
			return property;
		}

		readonly string m_Target;
		readonly MethodInfo m_Method;
		readonly Type m_Param;

		public bool CanInvoke => m_Method != null;

		public ReflectionEventMethodResolver(Type type, string target, Type param)
		{
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

			m_Target = target;
			m_Param = param;
			while (type != null)
			{
				var methods = type.GetMethods(flags);
				foreach (var method in methods)
				{
					if (method.Name == m_Target)
					{
						var prms = method.GetParameters();
						if (prms.Length == 0 && param == null)
						{
							m_Method = method;
							break;
						}
						else if (prms.Length == 1 && prms[0].ParameterType == param)
						{
							m_Method = method;
							break;
						}
						else if (prms.Length == 1 && prms[0].ParameterType.IsAssignableFrom(param))
						{
							m_Method = method;
							break;
						}
					}
				}
				type = type.BaseType;
			}
		}

		public void Invoke(object obj, object param)
		{
			if (m_Method != null)
			{
				if (m_Param == null)
				{
					m_Method.Invoke(obj, Array.Empty<object>());
				}
				else
				{
					m_Method.Invoke(obj, new[] { param });
				}
			}
		}

	}
}