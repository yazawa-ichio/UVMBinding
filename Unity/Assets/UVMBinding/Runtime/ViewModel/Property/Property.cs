using UVMBinding.Core;

namespace UVMBinding
{

	public class Property<T>
	{
		public readonly string Path;
		BindingProperty m_Property;
		SubscribeHandle<T> m_Handle;

		public T Value
		{
			get
			{
				m_Property.TryGetValue<T>(out var value);
				return value;
			}
			set { m_Property.TrySetValue(value); }
		}

		public event System.Action<T> OnChanged;

		public Property(string path, IViewModel vm)
		{
			Path = path;
			m_Property = vm.Property.GetOrCreate<T>(Path);
			m_Handle = vm.Property.SubscribeStrict<T>(Path, OnNotifyChanged, notifyCurrent: false);
		}

		public Property(string path, IViewModel vm, T val) : this(path, vm)
		{
			Value = val;
		}

		void OnNotifyChanged(T val)
		{
			OnChanged?.Invoke(val);
		}

		public void SetDirty()
		{
			m_Property.SetDirty();
		}

		public static implicit operator T(Property<T> property)
		{
			return property.Value;
		}

	}

}