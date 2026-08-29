using System;
using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Logger;

namespace UVMBinding.Core
{

	[UnityEngine.Scripting.Preserve]
	public class PropertyContainer
	{

		Dictionary<string, BindingProperty> m_Properties = new();

		internal event Action<IBindingProperty> OnNewProperty;

		internal event Action<string, IValueHolder> OnChangedValue;

		internal bool ShouldCreateProperty;

		HashSet<string> m_SubscribePaths;

		Action<string, IValueHolder> m_InvokeChangedValueCache;
		Action<string, IValueHolder> InvokeChangedValueCache
		{
			get
			{
				if (m_InvokeChangedValueCache == null)
				{
					m_InvokeChangedValueCache = InvokeChangedValue;
				}
				return m_InvokeChangedValueCache;
			}
		}


		void InvokeChangedValue(string path, IValueHolder holder)
		{
			OnChangedValue?.Invoke(path, holder);
		}

		public SubscribeHandle<T> Subscribe<T>(string path, Action<T> notify)
		{
			Log.Debug("Property Subscribe<{0}> Path:{1}", typeof(T), path);
			var porp = Get<T>(path);
			if (porp == null)
			{
				//型不一致。Get<T> が理由を記録済みなので何もしない
				return default;
			}
			porp.OnChanged += notify;
			return new SubscribeHandle<T>(porp, notify);
		}

		public SubscribeAssignableHandle<T> SubscribeAssignable<T>(string path, Action<T> notify, bool notifyCurrent = true)
		{
			Log.Debug("Property SubscribeAssignable<{0}> Path:{1}", typeof(T), path);
			if (m_SubscribePaths == null)
			{
				m_SubscribePaths = new HashSet<string>();
			}
			bool isFirst = m_SubscribePaths.Add(path);
			T value = default;
			if (isFirst || notifyCurrent)
			{
				if (m_Properties.TryGetValue(path, out BindingProperty property))
				{
					if (isFirst)
					{
						property.OnChangedValue += InvokeChangedValueCache;
					}
					if (property is BindingProperty<T> cast)
					{
						value = cast.Value;
					}
					else if (property.IsAssignable<T>())
					{
						value = (T)property.GetObject();
					}
				}
			}
			if (notifyCurrent)
			{
				notify(value);
			}
			return new SubscribeAssignableHandle<T>(this, path, notify);
		}

		public void Unsubscribe<T>(string path, Action<T> notify)
		{
			Log.Debug("Property Unsubscribe<{0}> Path:{1}", typeof(T), path);
			var property = Get<T>(path);
			if (property != null)
			{
				property.OnChanged -= notify;
			}
		}

		//1 path = 1 型。型は最初の登録で確定する。
		//型が合わない場合は例外を投げず null を返す。出荷後に落とさないため。
		//不一致は静的検査（TASK-007）で防ぐ方針。
		internal IBindingProperty<T> Get<T>(string path)
		{
			if (m_Properties.TryGetValue(path, out BindingProperty property))
			{
				if (property is BindingProperty<T> ret)
				{
					return ret;
				}
				Log.Error("Property Type Mismatch Path:{0} Registered:{1} Requested:{2}", path, property.GetBindType(), typeof(T));
				return null;
			}
			{
				var ret = NewProperty<T>(path);
				m_Properties[path] = ret;
				return ret;
			}
		}

		internal bool TryGet(string path, out IBindingProperty property)
		{
			if (m_Properties.TryGetValue(path, out BindingProperty prop))
			{
				property = prop;
				return true;
			}
			if (ShouldCreateProperty)
			{
				var ret = NewProperty<object>(path);
				property = m_Properties[path] = ret;
				return true;
			}
			property = null;
			return false;
		}

		BindingProperty<T> NewProperty<T>(string path)
		{
			Log.Trace("Register New Property<{0}> Path:{1}", typeof(T), path);
			if (typeof(T) == typeof(object))
			{
				var ret = new ObjectBindingProperty(path);
				OnNewProperty?.Invoke(ret);
				if (m_SubscribePaths != null && m_SubscribePaths.Contains(path))
				{
					ret.OnChangedValue += InvokeChangedValueCache;
				}
				return (BindingProperty<T>)(object)ret;
			}
			else
			{
				var ret = new BindingProperty<T>(path);
				OnNewProperty?.Invoke(ret);
				if (m_SubscribePaths != null && m_SubscribePaths.Contains(path))
				{
					ret.OnChangedValue += InvokeChangedValueCache;
				}
				return ret;
			}
		}

		public void SetDirty(string path)
		{
			Log.Trace("SetDirty {0}", path);
			if (m_Properties.TryGetValue(path, out BindingProperty property))
			{
				property.SetDirty();
			}
		}

		public void SetAllDirty()
		{
			Log.Trace("SetAllDirty");
			foreach (var property in m_Properties.Values)
			{
				property.SetDirty();
			}
		}

		internal IBindingProperty[] GetAll()
		{
			using (ListPool<IBindingProperty>.Use(out var list))
			{
				return GetAllImpl(list).ToArray();
			}
		}

		internal List<IBindingProperty> GetAllImpl(List<IBindingProperty> ret)
		{
			foreach (var property in m_Properties.Values)
			{
				ret.Add(property);
			}
			return ret;
		}

	}

}