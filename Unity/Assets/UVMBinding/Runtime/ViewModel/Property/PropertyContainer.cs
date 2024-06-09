using System;
using System.Collections.Generic;
using UVMBinding.Logger;

namespace UVMBinding.Core
{

	[UnityEngine.Scripting.Preserve]
	public class PropertyContainer
	{

		Dictionary<string, BindingProperty> m_Properties = new();

		internal event Action<IBindingProperty> OnNewProperty;

		public SubscribeHandle<T> Subscribe<T>(string path, Action<T> notify)
		{
			Log.Debug("Property Subscribe<{0}> Path:{1}", typeof(T), path);
			var porp = Get<T>(path);
			porp.OnChanged += notify;
			return new SubscribeHandle<T>(porp, notify);
		}

		public void Unsubscribe<T>(string path, Action<T> notify)
		{
			Log.Debug("Property Unsubscribe<{0}> Path:{1}", typeof(T), path);
			Get<T>(path).OnChanged -= notify;
		}

		internal IBindingProperty<T> Get<T>(string path)
		{
			if (m_Properties.TryGetValue(path, out BindingProperty property))
			{
				var last = property;
				while (property != null)
				{
					if (property is BindingProperty<T> ret)
					{
						//データがすでにある
						return ret;
					}
					last = property;
					property = property.Next;
				}
				{
					var ret = NewProperty<T>(path);
					last.Next = ret;
					return ret;
				}
			}
			else
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
			property = null;
			return false;
		}

		BindingProperty<T> NewProperty<T>(string path)
		{
			Log.Trace("Register New Property<{0}> Path:{1}", typeof(T), path);
			var ret = new BindingProperty<T>(path);
			OnNewProperty?.Invoke(ret);
			return ret;
		}

		public void SetDirty(string path)
		{
			Log.Trace("SetDirty {0}", path);
			if (m_Properties.TryGetValue(path, out BindingProperty property))
			{
				while (property != null)
				{
					property.SetDirty();
					property = property.Next;
				}
			}
		}

		public void SetAllDirty()
		{
			Log.Trace("SetAllDirty");
			foreach (var _property in m_Properties.Values)
			{
				var property = _property;
				while (property != null)
				{
					property.SetDirty();
					property = property.Next;
				}
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
			foreach (var _property in m_Properties.Values)
			{
				var property = _property;
				while (property != null)
				{
					ret.Add(property);
					property = property.Next;
				}
			}
			return ret;
		}

	}

}