using UnityEngine;
using UVMBinding.Core;
using UVMBinding.Logger;

namespace UVMBinding
{
	public abstract class Binder<T> : MonoBehaviour, IBinder
	{
		static bool s_IsValueType = typeof(T).IsValueType;

		[SerializeField]
		string m_Path = null;
		[SerializeReference]
		IConverter m_Converter;

		public string Path => m_Path;

		bool IBinder.IsActive => this != null;

		IBindingProperty m_Property;
		int m_Hash;
		bool m_ForceUpdate;

		protected void Set(T val)
		{
			if (m_Property != null)
			{
				Log.Trace(this, "{0} {1} SetValue {2}", this, m_Path, val);
				if (m_Property is IBindingProperty<T> prop)
				{
					prop.Value = val;
				}
				else
				{
					m_Property.SetObject(val);
				}
			}
			else
			{
				Log.Warning(this, "Not Found BindingProperty {0}", m_Path);
			}
		}

		bool IViewElement.CanUse(IView view) => CanUse(view);

		protected virtual bool CanUse(IView view) => true;

		void IBinder.Bind(IBindingProperty prop)
		{
			if (m_Converter != null && m_Converter.TryConvert(prop, ref m_Property))
			{
				InvokeBind();
				return;
			}
			if (s_IsValueType)
			{
				if (prop is IBindingProperty<T> ret)
				{
					m_Property = ret;
					InvokeBind();
				}
			}
			else
			{
				if (prop.IsAssignable<T>())
				{
					m_Property = prop;
					InvokeBind();
				}
			}
		}

		void InvokeBind()
		{
			Log.Trace(this, "{0} Bind Property Path:{1}", this, m_Path);
			m_ForceUpdate = true;
			OnBind();
		}

		protected virtual void OnBind() { }

		void IBinder.Unbind()
		{
			if (m_Converter != null)
			{
				m_Converter.Unbind();
			}
			m_Property = null;
			OnUnbind();
		}

		protected virtual void OnUnbind() { }

		void IBinder.TryUpdate()
		{
			TryUpdate();
		}

		protected void TryUpdate()
		{
			if (m_Converter != null)
			{
				m_Converter.TryUpdate();
			}
			if (m_Property == null || (!m_ForceUpdate && m_Property.Hash == m_Hash))
			{
				return;
			}
			Log.Trace(this, "{0} Update Property Path:{1} Value:{2}", this, m_Path, m_Property);
			m_ForceUpdate = false;
			m_Hash = m_Property.Hash;
			if (m_Property is IBindingProperty<T> prop)
			{
				UpdateValue(prop.Value);
			}
			else
			{
				UpdateValue((T)m_Property.GetObject());
			}
		}

		protected abstract void UpdateValue(T value);

	}

	public abstract class Binder<TValue, TComponent> : Binder<TValue>
	{
		TComponent m_Target;

		protected TComponent Target
		{
			get
			{
				TryInit();
				return m_Target;
			}
		}

		protected void TryInit()
		{
			if (m_Target == null && TryGetComponent(out m_Target))
			{
				OnInit(m_Target);
			}
		}

		protected virtual void OnInit(TComponent target) { }

	}


}