using System;
using UVMBinding.Core;

namespace UVMBinding
{

	public sealed class ReflectionViewModel : ViewModel
	{
		public static void Register<TDataSource, TValue>(string path, Action<TDataSource, TValue> setter, Func<TDataSource, TValue> getter)
		{
			ReflectionPropertyValueResolver.Register(path, setter, getter);
		}

		public static void Register<TDataSource, TValue>(string path, Func<TDataSource, TValue> getter)
		{
			ReflectionPropertyValueResolver.Register(path, default, getter);
		}

		Type m_Type;
		object m_DataSource;
		INotifyDirtyEvent m_DitryEvent;

		public ReflectionViewModel(object dataSource)
		{
			if (dataSource == null)
			{
				throw new ArgumentNullException(nameof(dataSource));
			}
			m_DataSource = dataSource;
			m_Type = dataSource.GetType();
			m_DitryEvent = m_DataSource as INotifyDirtyEvent;
			Property.ShouldCreateProperty = true;
		}

		protected override void OnBind()
		{
			Property.OnNewProperty += OnNewProperty;
			if (m_DitryEvent != null)
			{
				m_DitryEvent.DitryHandler += OnDitryEvent;
			}
			Event.OnPublishEvent += OnPublishEvent;
			foreach (var prop in Property.GetAll())
			{
				OnNewProperty(prop);
			}
		}

		protected override void OnUnbind()
		{
			if (m_DitryEvent != null)
			{
				m_DitryEvent.DitryHandler -= OnDitryEvent;
			}
			Property.OnNewProperty -= OnNewProperty;
			Event.OnPublishEvent -= OnPublishEvent;
			foreach (var prop in Property.GetAll())
			{
				prop.OnChangedObject -= OnChangedObject;
			}
		}

		void OnPublishEvent(PublishEvent publishEvent)
		{
			var resolver = ReflectionEventMethodResolver.Get(m_DataSource.GetType(), publishEvent.Name, publishEvent.Type);
			if (resolver != null && resolver.CanInvoke)
			{
				resolver.Invoke(m_DataSource, publishEvent.Args);
			}
		}


		private void OnDitryEvent(NotifyDirtyEvent e)
		{
			if (string.IsNullOrEmpty(e.Path))
			{
				using (ListPool<IBindingProperty>.Use(out var list))
				{
					foreach (var property in Property.GetAllImpl(list))
					{
						SetPropertyValue(property);
					}
				}
			}
			else
			{
				var property = Property.Get<object>(e.Path);
				SetPropertyValue(property);
			}
		}

		void SetPropertyValue(IBindingProperty property)
		{
			var resolver = ReflectionPropertyValueResolver.Get(m_Type, property.Path);
			if (resolver != null && resolver.CanRead)
			{
				var value = ReflectionPropertyValueResolver.Get(m_Type, property.Path).GetValue(m_DataSource);
				property.SetObject(value);
			}
		}

		void OnNewProperty(IBindingProperty property)
		{
			property.OnChangedObject += OnChangedObject;
			var resolver = ReflectionPropertyValueResolver.Get(m_DataSource.GetType(), property.Path);
			if (resolver != null && resolver.CanRead)
			{
				property.SetObject(resolver.GetValue(m_DataSource));
			}
		}

		void OnChangedObject(string path, object value)
		{
			var resolver = ReflectionPropertyValueResolver.Get(m_DataSource.GetType(), path);
			if (resolver != null && resolver.CanRead)
			{
				resolver.SetValue(m_DataSource, value);
			}
		}

	}
}