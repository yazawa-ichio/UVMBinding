using System.Collections.Generic;
using UVMBinding.Core;
using UVMBinding.Drawer;
using UVMBinding.PropertyProvider;

namespace UVMBinding.Lint
{
	public interface IBindSource
	{
		bool HasBind(string path);
		bool HasEvent(string name, System.Type argType);
		System.Type GetDeclaredType(string path);
		bool IsDynamic { get; }
	}

	public class ViewModelBindSource : IBindSource
	{
		public bool IsDynamic { get; }

		readonly HashSet<string> m_BindPaths = new HashSet<string>();
		readonly HashSet<string> m_EventPaths = new HashSet<string>();
		readonly Dictionary<string, System.Type> m_DeclaredTypes = new Dictionary<string, System.Type>();

		readonly Dictionary<string, string[]> m_DependsOn = new Dictionary<string, string[]>();

		public IReadOnlyDictionary<string, string[]> DependsOn => m_DependsOn;

		public ViewModelBindSource(System.Type type)
		{
			IsDynamic = type.GetCustomAttributes(typeof(DynamicPathAttribute), true).Length > 0;
			var data = ViewModelData.Get(type);
			foreach (var entry in data.Properties)
			{
				m_BindPaths.Add(entry.Path);
				m_DeclaredTypes[entry.Path] = Unwrap(entry.Info.PropertyType);
				if (entry.SourcePaths.Length > 0)
				{
					m_DependsOn[entry.Path] = entry.SourcePaths;
				}
			}
			foreach (var entry in data.Events)
			{
				m_EventPaths.Add(entry.Path);
			}
		}

		public bool HasBind(string path) => m_BindPaths.Contains(path);

		public bool HasEvent(string name, System.Type argType) => m_EventPaths.Contains(name);

		public System.Type GetDeclaredType(string path)
		{
			return m_DeclaredTypes.TryGetValue(path, out var type) ? type : null;
		}

		static System.Type Unwrap(System.Type declared)
		{
			if (declared == null || !declared.IsGenericType)
			{
				return declared;
			}
			var definition = declared.GetGenericTypeDefinition();
			var argument = declared.GetGenericArguments()[0];
			if (definition == typeof(Property<>))
			{
				return argument;
			}
			if (definition == typeof(Collection<>))
			{
				return typeof(List<IViewModel>);
			}
			if (definition == typeof(ValueCollection<>))
			{
				return typeof(List<>).MakeGenericType(argument);
			}
			return declared;
		}
	}

	public class ExtendedBindSource : IBindSource
	{
		readonly IBindSource m_Inner;
		readonly Dictionary<string, System.Type> m_Extensions = new Dictionary<string, System.Type>();

		public bool IsDynamic => m_Inner.IsDynamic;

		public ExtendedBindSource(IBindSource inner)
		{
			m_Inner = inner;
		}

		public void Add(ExtensionPropertyInfo info)
		{
			m_Extensions[info.Path] = info.Type;
		}

		public bool HasBind(string path)
		{
			return m_Extensions.ContainsKey(path) || m_Inner.HasBind(path);
		}

		public bool HasEvent(string name, System.Type argType) => m_Inner.HasEvent(name, argType);

		public System.Type GetDeclaredType(string path)
		{
			return m_Extensions.TryGetValue(path, out var type) ? type : m_Inner.GetDeclaredType(path);
		}
	}

	public class ReflectionBindSource : IBindSource
	{
		readonly System.Type m_Type;

		public bool IsDynamic => false;

		public ReflectionBindSource(System.Type type)
		{
			m_Type = type;
		}

		public bool HasBind(string path)
		{
			var resolver = ReflectionPropertyValueResolver.Get(m_Type, path);
			return resolver != null && resolver.CanRead;
		}

		public bool HasEvent(string name, System.Type argType)
		{
			var resolver = ReflectionEventMethodResolver.Get(m_Type, name, argType);
			return resolver != null && resolver.CanInvoke;
		}

		public System.Type GetDeclaredType(string path)
		{
			var resolver = ReflectionPropertyValueResolver.Get(m_Type, path);
			return resolver != null && resolver.CanRead ? resolver.ValueType : null;
		}
	}
}
