using UVMBinding.Core;

namespace UVMBinding
{

	public class Property<T>
	{
		public readonly string Path;
		IBindingProperty<T> m_Property;

		//型不一致で m_Property が取れないことがある。落とさず既定値を返す
		public T Value
		{
			get { return m_Property != null ? m_Property.Value : default; }
			set { if (m_Property != null) { m_Property.Value = value; } }
		}

		public event System.Action<T> OnChanged;

		public Property(string path, IViewModel vm)
		{
			Path = path;
			m_Property = vm.Property.Get<T>(Path);
			if (m_Property != null)
			{
				m_Property.OnChanged += OnNotifyChanged;
			}
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
			m_Property?.SetDirty();
		}

		public static implicit operator T(Property<T> property)
		{
			return property.Value;
		}

	}

}