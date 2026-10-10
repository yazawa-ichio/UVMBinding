using UnityEngine;
using UVMBinding.Converters;
using UVMBinding.Core;
using UVMBinding.Logger;

namespace UVMBinding
{

	public abstract class Binder<T> : MonoBehaviour, IBinder, IBinderInfo
	{
		[SerializeField, PropertyPath]
		string m_Path = null;
		[SerializeReference]
		IConverter[] m_Converters = System.Array.Empty<IConverter>();
		[SerializeField]
		bool m_AllowCast = false;

		public string Path => m_Path;

		bool IBinder.IsRebind => m_ChangePath && m_Property != null && m_Path != m_Property.Path;

		bool IBinder.IsActive => this != null;

		bool IBinder.NeedsUpdate
		{
			get
			{
				for (int i = 0; i < m_Converters.Length; i++)
				{
					if (m_Converters[i] != null && m_Converters[i].NeedsUpdate)
					{
						return true;
					}
				}
				return false;
			}
		}

		IBindingProperty m_Property;
		BindKind m_Kind;
		int m_Hash;
		bool m_ForceUpdate;
		bool m_ChangePath;

		public void ChangePath(string path)
		{
			m_Path = path;
			m_ChangePath = true;
			BinderPathVersion.Value++;
			Debug.AssertFormat(!string.IsNullOrEmpty(m_Path), this, "{0} Path Empty", this);
		}

		bool TryRead(out T value)
		{
			if (m_AllowCast)
			{
				return m_Property.TryGetValueCast(out value);
			}
			return m_Property.TryGetValue(out value);
		}

		bool TryWrite(T value)
		{
			if (m_AllowCast)
			{
				return m_Property.TrySetValueCast(value);
			}
			return m_Property.TrySetValue(value);
		}

		protected void Set(T val)
		{
			if (m_Property != null)
			{
				Log.Trace(this, "{0} {1} SetValue {2}", this, m_Path, val);
				if (!TryWrite(val) && !m_Property.TrySetObject(val))
				{
					Log.Warning(this, "{0} {1} SetValue rejected. {2} => {3}",
						this, m_Path, typeof(T), m_Property.GetBindType());
				}
			}
			else
			{
				Log.Warning(this, "Not Found BindingProperty {0}", m_Path);
			}
		}

		protected T Get()
		{
			if (m_Property != null)
			{
				if (TryRead(out var value))
				{
					return value;
				}
				return (T)m_Property.GetObject();
			}
			Log.Warning(this, "Not Found BindingProperty {0}", m_Path);
			return default;
		}

		string IBinderInfo.Path => m_Path;

		System.Type IBinderInfo.BindType => typeof(T);

		object IBinderInfo.GetBoundValue() => m_Property?.GetObject();

		BindKind IBinderInfo.Kind => m_Kind;

		bool IBinderInfo.AllowCast => m_AllowCast;

		bool IBinderInfo.HasConverter => m_Converters.Length > 0;

		System.Type IBinderInfo.ConverterInputType
		{
			get
			{
				if (m_Converters.Length == 0)
				{
					return typeof(T);
				}
				return m_Converters[0]?.InputType;
			}
		}

		void IBinderInfo.GetConverterStages(System.Collections.Generic.List<ConverterStage> list)
		{
			for (int i = 0; i < m_Converters.Length; i++)
			{
				var conv = m_Converters[i];
				if (conv == null)
				{
					list.Add(ConverterStage.Empty);
					continue;
				}
				list.Add(new ConverterStage(conv.InputType, conv.OutputType));
			}
		}

		string IBinderInfo.GetDisplayText() => GetDisplayText();

		protected virtual string GetDisplayText() => null;

		bool IViewElement.CanUse(IView view)
		{
			if (BindSourceProvider.IsOwnedByProvider(this))
			{
				return false;
			}
			return CanUse(view);
		}
		protected virtual bool CanUse(IView view) => true;

		void IBinder.Bind(IBindingProperty prop)
		{
			Log.Debug(this, "{0} Bind Property Path:{1} {2}", this, m_Path, prop);
			m_Property = null;
			m_Kind = BindKind.None;
			var current = prop;
			for (int i = 0; i < m_Converters.Length; i++)
			{
				var conv = m_Converters[i];
				if (conv == null || !conv.TryConvert(current, m_AllowCast, ref current))
				{
					Log.Debug(this, "Converter stage {0} failed. {1}", i, m_Path);
					return;
				}
			}
			m_Kind = current.GetBindKind<T>(m_AllowCast);
			if (m_Kind == BindKind.None)
			{
				Log.Debug(this, "Type mismatch {0} {1} => {2}", m_Path, current.GetBindType(), typeof(T));
				return;
			}
			m_Property = current;
			InvokeBind();
		}

		void InvokeBind()
		{
			Log.Trace(this, "{0} Bind Property Path:{1}", this, m_Path);
			m_ChangePath = false;
			m_ForceUpdate = true;
			OnBind();
		}

		protected virtual void OnBind() { }

		void IBinder.Unbind()
		{
			for (int i = 0; i < m_Converters.Length; i++)
			{
				m_Converters[i]?.Unbind();
			}
			m_Property = null;
			m_Kind = BindKind.None;
			OnUnbind();
		}

		protected virtual void OnUnbind() { }

		void IBinder.TryUpdate()
		{
			TryUpdate();
		}

		protected void TryUpdate()
		{
			for (int i = 0; i < m_Converters.Length; i++)
			{
				m_Converters[i]?.TryUpdate();
			}
			if (m_Property == null || (!m_ForceUpdate && m_Property.Hash == m_Hash))
			{
				return;
			}
			Log.Trace(this, "{0} Update Property Path:{1} Value:{2}", this, m_Path, m_Property);
			m_ForceUpdate = false;
			m_Hash = m_Property.Hash;
			if (TryRead(out var value))
			{
				UpdateValue(value);
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

		protected bool TryGetTarget(out TComponent target)
		{
			return TryGetComponent(out target);
		}

		protected virtual void OnInit(TComponent target) { }

	}

}