using System;
using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.PropertyProvider
{
	public interface IViewModelPropertyExtension
	{
		void Set<T>(string name, T value);
		void Subscribe<T>(string name, Action<T> action);

		CombineParameterQuery Create()
		{
			return new CombineParameterQuery
			{
				PropertyExtension = this
			};
		}
	}

	public class ViewModelPropertyExtension : MonoBehaviour, IViewModelPropertyExtension
	{
		[SerializeReference]
		private IPropertyProvider[] m_Properties = Array.Empty<IPropertyProvider>();

		private Binding m_Binding;
		private List<IDisposable> m_Disposables;
		private PropertyContainer m_PropertyContainer;
		private IViewModel m_ViewModel;
		private bool m_IsInitialized;

		public IEnumerable<ExtensionPropertyInfo> GetPropertyInfos()
		{
			foreach (var property in m_Properties)
			{
				foreach (var info in property.GetInfos())
				{
					yield return info;
				}
			}
		}

		private void Awake()
		{
			m_Disposables = new List<IDisposable>();
			m_PropertyContainer = new PropertyContainer();
			m_PropertyContainer.OnNewProperty += BindToBinder;
		}

		private void OnDestroy()
		{
			m_PropertyContainer.OnNewProperty -= BindToBinder;
			ResetBind();
		}

		public void Subscribe<T>(string name, Action<T> action)
		{
			bool newProperty = false;
			if (!m_ViewModel.Property.TryGet(name, out var property))
			{
				property = m_ViewModel.Property.Get<object>(name);
				newProperty = true;
			}
			if (property is IBindingProperty<T> bindingProperty)
			{
				SubscribeHandle<T> handle = new SubscribeHandle<T>(bindingProperty, action);
				m_Disposables.Add(handle);
				action(bindingProperty.Value);
			}
			else
			{
				Action<string, object> castAction = (_, value) =>
				{
					action((T)value);
				};
				property.OnChangedObject += castAction;
				SubscribeHandle<T> handle = new SubscribeHandle<T>(property, castAction);
				m_Disposables.Add(handle);
				if (!newProperty)
				{
					action((T)property.GetObject());
				}
			}
		}

		public void Set<T>(string name, T value)
		{
			m_PropertyContainer.Get<T>(name).Value = value;
		}

		internal void Init(IViewModel viewModel, Binding binding)
		{
			ResetBind();
			m_ViewModel = viewModel;
			m_Binding = binding;
			m_IsInitialized = true;
			foreach (var combiner in m_Properties)
			{
				combiner.Init(this);
			}
		}

		private void BindToBinder(IBindingProperty prop)
		{
			m_Binding.BindToBinder(prop);
		}

		void ResetBind()
		{
			if (!m_IsInitialized) return;
			IDisposable[] disposables = m_Disposables.ToArray();
			m_Disposables.Clear();
			foreach (IDisposable disposable in disposables)
			{
				disposable.Dispose();
			}
			foreach (var combiner in m_Properties)
			{
				combiner.Reset();
			}

		}
	}
}