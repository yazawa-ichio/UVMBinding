using System;
using System.Collections.Generic;
using System.Reflection;

namespace UVMBinding.Core
{
	internal class ReflectionPropertyValueResolver
	{
		private IPropertyValue m_PropertyValue;

		public bool CanRead => m_PropertyValue.CanRead;

		public bool CanWrite => m_PropertyValue.CanWrite;

		private ReflectionPropertyValueResolver(IPropertyValue propertyValue)
		{
			m_PropertyValue = propertyValue;
		}

		public void SetValue(object dataSource, object value)
		{
			m_PropertyValue.SetValue(dataSource, value);
		}

		public object GetValue(object dataSource)
		{
			return m_PropertyValue.GetValue(dataSource);
		}

		static Dictionary<(Type type, string path), ReflectionPropertyValueResolver> s_PropertyValueCache = new();

		public static void Register<TDataSource, TValue>(string path, Action<TDataSource, TValue> setter, Func<TDataSource, TValue> getter)
		{
			s_PropertyValueCache[(typeof(TDataSource), path)] = new(new ActionValueSetter<TDataSource, TValue>(setter, getter));
		}

		public static ReflectionPropertyValueResolver Get(Type type, string path)
		{
			var key = (type, path);
			if (!s_PropertyValueCache.TryGetValue(key, out var property))
			{
				s_PropertyValueCache[key] = property = new(new ReflectionValueSetter(type, path));
			}
			return property;
		}

		interface IPropertyValue
		{
			bool CanWrite { get; }
			bool CanRead { get; }
			void SetValue(object component, object value);
			object GetValue(object component);
		}

		interface IPropertyValue<TValue>
		{
			void Set(object component, TValue value);
			TValue Get(object component);
		}

		class ActionValueSetter<TDataSource, TValue> : IPropertyValue, IPropertyValue<TValue>
		{
			Action<TDataSource, TValue> m_Setter;
			Func<TDataSource, TValue> m_Getter;

			public bool CanWrite => m_Setter != null;

			public bool CanRead => m_Getter != null;

			public ActionValueSetter(Action<TDataSource, TValue> setter, Func<TDataSource, TValue> getter)
			{
				m_Setter = setter;
				m_Getter = getter;
			}

			public void SetValue(object data, object value)
			{
				m_Setter((TDataSource)(object)data, (TValue)value);
			}

			public object GetValue(object data)
			{
				return m_Getter((TDataSource)data);
			}

			public void Set(object data, TValue value)
			{
				m_Setter((TDataSource)data, value);
			}

			public TValue Get(object component)
			{
				return m_Getter((TDataSource)component);
			}

		}

		class ReflectionValueSetter : IPropertyValue
		{
			readonly string m_Path;
			readonly FieldInfo m_Field;
			readonly PropertyInfo m_Property;

			public bool CanWrite => m_Field != null || (m_Property != null && m_Property.CanWrite);

			public bool CanRead => m_Field != null || (m_Property != null && m_Property.CanRead);

			public ReflectionValueSetter(Type type, string path)
			{
				const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

				m_Path = path;
				while (type != null)
				{
					m_Field = type.GetField(m_Path, flags);
					if (m_Field != null)
					{
						return;
					}
					m_Property = type.GetProperty(m_Path, flags);
					if (m_Property != null)
					{
						return;
					}
					type = type.BaseType;
				}
			}

			public void SetValue(object data, object value)
			{
				m_Field?.SetValue(data, value);
				m_Property?.SetValue(data, value);
			}

			public object GetValue(object data)
			{
				if (m_Field != null)
					return m_Field.GetValue(data);
				if (m_Property != null)
					return m_Property.GetValue(data);
				return null;
			}

		}

	}
}