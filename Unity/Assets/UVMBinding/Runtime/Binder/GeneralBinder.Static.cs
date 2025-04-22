using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UVMBinding
{
	public partial class GeneralBinder
	{
		static Dictionary<(Type type, string target), IValueSetter> s_ValueSetters = new();

		public static void Register<TComponent, TValue>(string target, Action<TComponent, TValue> setter)
		{
			s_ValueSetters[(typeof(TComponent), target)] = new ActionValueSetter<TComponent, TValue>(setter);
		}

		static IValueSetter Get(Type type, string target)
		{
			var key = (type, target);
			if (!s_ValueSetters.TryGetValue(key, out var setter))
			{
				s_ValueSetters[key] = setter = new ReflectionValueSetter(type, target);
			}
			return setter;
		}

		internal interface IValueSetter
		{
			void SetValue(Component component, object value);
		}

		class ActionValueSetter<TComponent, TValue> : IValueSetter
		{
			Action<TComponent, TValue> m_Setter;

			public ActionValueSetter(Action<TComponent, TValue> setter)
			{
				m_Setter = setter;
			}

			public void SetValue(Component component, object value)
			{
				m_Setter((TComponent)(object)component, (TValue)value);
			}
		}

		class ReflectionValueSetter : IValueSetter
		{
			readonly string m_Target;
			readonly FieldInfo m_Field;
			readonly PropertyInfo m_Property;
			readonly MethodInfo m_Method;

			public ReflectionValueSetter(Type type, string target)
			{
				const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

				m_Target = target;
				while (type != null)
				{
					m_Field = type.GetField(m_Target, flags);
					if (m_Field != null)
					{
						return;
					}
					m_Property = type.GetProperty(m_Target, flags);
					if (m_Property != null)
					{
						return;
					}
					m_Method = type.GetMethod(m_Target, flags);
					if (m_Method != null)
					{
						return;
					}
					type = type.BaseType;
				}
			}

			public void SetValue(Component component, object value)
			{
				m_Field?.SetValue(component, value);
				m_Property?.SetValue(component, value);
			}

		}
	}
}