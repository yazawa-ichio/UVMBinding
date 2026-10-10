using System;
using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.PropertyProvider
{

	internal interface IPropertyProvider
	{
		IEnumerable<ExtensionPropertyInfo> GetInfos();
		void Init(IViewModelPropertyExtension propertyExtension);
		void Reset();
	}

	[Serializable]
	public abstract class PropertyProvider : IPropertyProvider
	{
		public abstract IEnumerable<ExtensionPropertyInfo> GetInfos();

		public abstract void Init(IViewModelPropertyExtension propertyExtension);

		public virtual void Reset() { }
	}


	[Serializable]
	public abstract class PropertyProvider<T1, TResult> : PropertyProvider
	{
		[SerializeField] private string m_ValueKey;
		[SerializeField, PropertyPath] private string m_SourceKey1;

		public override IEnumerable<ExtensionPropertyInfo> GetInfos()
		{
			yield return new ExtensionPropertyInfo(m_ValueKey, typeof(TResult), m_SourceKey1);
		}

		public override void Init(IViewModelPropertyExtension propertyExtension)
		{
			propertyExtension.Create()
				.Add<T1>(m_SourceKey1)
				.Handle((value1) =>
				{
					TResult result = Provide(value1);
					propertyExtension.Set(m_ValueKey, result);
				});
		}

		public abstract TResult Provide(T1 value1);
	}

	[Serializable]
	public abstract class PropertyProvider<T1, T2, TResult> : PropertyProvider
	{
		[SerializeField] private string m_ValueKey;
		[SerializeField, PropertyPath] private string m_SourceKey1;
		[SerializeField, PropertyPath] private string m_SourceKey2;

		public override IEnumerable<ExtensionPropertyInfo> GetInfos()
		{
			yield return new ExtensionPropertyInfo(m_ValueKey, typeof(TResult), m_SourceKey1, m_SourceKey2);
		}

		public override void Init(IViewModelPropertyExtension propertyExtension)
		{
			propertyExtension.Create()
				.Add<T1>(m_SourceKey1)
				.Add<T2>(m_SourceKey2)
				.Handle((value1, value2) =>
				{
					TResult result = Combine(value1, value2);
					propertyExtension.Set(m_ValueKey, result);
				});
		}

		public abstract TResult Combine(T1 value1, T2 value2);
	}

	[Serializable]
	public abstract class PropertyProvider<T1, T2, T3, TResult> : PropertyProvider
	{
		[SerializeField] private string m_ValueKey;
		[SerializeField, PropertyPath] private string m_SourceKey1;
		[SerializeField, PropertyPath] private string m_SourceKey2;
		[SerializeField, PropertyPath] private string m_SourceKey3;

		public override IEnumerable<ExtensionPropertyInfo> GetInfos()
		{
			yield return new ExtensionPropertyInfo(m_ValueKey, typeof(TResult), m_SourceKey1, m_SourceKey2, m_SourceKey3);
		}

		public override void Init(IViewModelPropertyExtension propertyExtension)
		{
			propertyExtension.Create()
				.Add<T1>(m_SourceKey1)
				.Add<T2>(m_SourceKey2)
				.Add<T3>(m_SourceKey3)
				.Handle((value1, value2, value3) =>
				{
					TResult result = Combine(value1, value2, value3);
					propertyExtension.Set(m_ValueKey, result);
				});
		}

		public abstract TResult Combine(T1 value1, T2 value2, T3 value3);
	}

	[Serializable]
	public abstract class PropertyProvider<T1, T2, T3, T4, TResult> : PropertyProvider
	{
		[SerializeField] private string m_ValueKey;
		[SerializeField, PropertyPath] private string m_SourceKey1;
		[SerializeField, PropertyPath] private string m_SourceKey2;
		[SerializeField, PropertyPath] private string m_SourceKey3;
		[SerializeField, PropertyPath] private string m_SourceKey4;

		public override IEnumerable<ExtensionPropertyInfo> GetInfos()
		{
			yield return new ExtensionPropertyInfo(m_ValueKey, typeof(TResult), m_SourceKey1, m_SourceKey2, m_SourceKey3, m_SourceKey4);
		}

		public override void Init(IViewModelPropertyExtension propertyExtension)
		{
			propertyExtension.Create()
				.Add<T1>(m_SourceKey1)
				.Add<T2>(m_SourceKey2)
				.Add<T3>(m_SourceKey3)
				.Add<T4>(m_SourceKey4)
				.Handle((value1, value2, value3, value4) =>
				{
					TResult result = Combine(value1, value2, value3, value4);
					propertyExtension.Set(m_ValueKey, result);
				});
		}

		public abstract TResult Combine(T1 value1, T2 value2, T3 value3, T4 value4);
	}

	[Serializable]
	public abstract class PropertyProvider<T1, T2, T3, T4, T5, TResult> : PropertyProvider
	{
		[SerializeField] private string m_ValueKey;
		[SerializeField, PropertyPath] private string m_SourceKey1;
		[SerializeField, PropertyPath] private string m_SourceKey2;
		[SerializeField, PropertyPath] private string m_SourceKey3;
		[SerializeField, PropertyPath] private string m_SourceKey4;
		[SerializeField, PropertyPath] private string m_SourceKey5;

		public override IEnumerable<ExtensionPropertyInfo> GetInfos()
		{
			yield return new ExtensionPropertyInfo(m_ValueKey, typeof(TResult), m_SourceKey1, m_SourceKey2, m_SourceKey3, m_SourceKey4, m_SourceKey5);
		}

		public override void Init(IViewModelPropertyExtension propertyExtension)
		{
			propertyExtension.Create()
				.Add<T1>(m_SourceKey1)
				.Add<T2>(m_SourceKey2)
				.Add<T3>(m_SourceKey3)
				.Add<T4>(m_SourceKey4)
				.Add<T5>(m_SourceKey5)
				.Handle((value1, value2, value3, value4, value5) =>
				{
					TResult result = Combine(value1, value2, value3, value4, value5);
					propertyExtension.Set(m_ValueKey, result);
				});
		}

		public abstract TResult Combine(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5);
	}
}