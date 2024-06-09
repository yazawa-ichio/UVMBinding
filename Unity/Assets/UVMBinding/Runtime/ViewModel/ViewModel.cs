using UVMBinding.Core;
using UVMBinding.Logger;

namespace UVMBinding
{
	[UnityEngine.Scripting.Preserve]
	public abstract class ViewModel : IViewModel
	{
		public EventBroker Event { get; private set; } = new EventBroker();

		PropertyContainer m_Properties = new();

		PropertyContainer IViewModel.Property => m_Properties;

		protected PropertyContainer Property => m_Properties;

		T IViewModel.Get<T>(string path) => GetImpl<T>(path);

		protected T GetImpl<T>(string path)
		{
			return m_Properties.Get<T>(path).Value;
		}

		void IViewModel.Set<T>(string path, T val) => SetImpl<T>(path, val);

		protected void SetImpl<T>(string path, T val)
		{
			Log.Trace("{0} SetImpl<{1}> Path:{2} Value {3}", this, typeof(T), path, val);
			m_Properties.Get<T>(path).Value = val;
		}

		public void SetDirty(string path)
		{
			Log.Debug("{0} SetDirty Path:{1}", this, path);
			m_Properties.SetDirty(path);
		}

		public void SetAllDirty()
		{
			Log.Debug("{0} SetAllDirty", this);
			m_Properties.SetAllDirty();
		}

	}

}