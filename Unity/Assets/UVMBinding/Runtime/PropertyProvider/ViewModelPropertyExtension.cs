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
			var handle = m_ViewModel.Property.SubscribeAssignable<T>(name, action, true);
			m_Disposables.Add(handle);
		}

		public void Set<T>(string name, T value)
		{
			var property = m_PropertyContainer.Get<T>(name);
			if (property == null)
			{
				//型不一致。Get<T> が理由を記録済み
				return;
			}
			property.Value = value;
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