using UnityEngine;
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

		int m_BindingCount;

		T IViewModel.Get<T>(string path) => GetImpl<T>(path);

		protected T GetImpl<T>(string path)
		{
			if (m_Properties.TryGetValue<T>(path, out var value))
			{
				return value;
			}
			throw m_Properties.CreateMismatchException<T>(path);
		}

		void IViewModel.Set<T>(string path, T val) => SetImpl<T>(path, val);

		bool IViewModel.TryGetAs<T>(string path, out T value) => TryGetAsImpl(path, out value);

		protected bool TryGetAsImpl<T>(string path, out T value)
		{
			return m_Properties.TryGetValueAs(path, out value);
		}

		bool IViewModel.TrySet<T>(string path, T val) => TrySetImpl(path, val);

		protected bool TrySetImpl<T>(string path, T val)
		{
			return m_Properties.TrySetValue(path, val);
		}

		protected void SetImpl<T>(string path, T val)
		{
			Log.Trace("{0} SetImpl<{1}> Path:{2} Value {3}", this, typeof(T), path, val);
			if (!m_Properties.TrySetValue(path, val))
			{
				throw m_Properties.CreateMismatchException<T>(path);
			}
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

		void IViewModel.OnBind()
		{
			m_BindingCount++;
			Log.Debug("{0} OnBind BindingCount:{1}", this, m_BindingCount);
			if (m_BindingCount == 1)
			{
				OnBind();
			}
		}

		void IViewModel.OnUnbind()
		{
			m_BindingCount--;
			Log.Debug("{0} OnUnbind BindingCount:{1}", this, m_BindingCount);
			Debug.Assert(m_BindingCount >= 0, "Unbind called more than bind");
			if (m_BindingCount == 0)
			{
				OnUnbind();
			}
		}

		protected virtual void OnBind() { }

		protected virtual void OnUnbind() { }

	}

}