using System;
using UVMBinding.Logger;
using UVMBinding.Core;

namespace UVMBinding.Converters
{
	[Serializable]
	public abstract class ConverterBase<TInput, TOutput> : IConverter
	{
		BindingProperty m_Output;
		PropertySubscriber<TOutput> m_OutputSubscriber;
		IBindingProperty m_Input;
		int m_Hash;
		bool m_ForceUpdate;
		bool m_OnUpdateLock;
		bool m_AllowCast;
		IUpdatableConverter m_Updatable;

		bool IConverter.NeedsUpdate => m_ForceUpdate || m_Updatable != null;

		public abstract TOutput Convert(TInput input);

		public virtual bool TryInverseConvert(TOutput value, ref TInput ret)
		{
			return false;
		}

		Type IConverter.InputType => typeof(TInput);

		Type IConverter.OutputType => typeof(TOutput);

		bool IConverter.TryConvert(IBindingProperty property, bool allowCast, ref IBindingProperty output)
		{
			m_AllowCast = allowCast;
			if (!property.CanReceive<TInput>(allowCast))
			{
				Log.Debug("{0} Fail TryConvert {1} => {2}", this, typeof(TInput), typeof(TOutput));
				return false;
			}
			Log.Trace("{0} Convert Success {1} => {2}", this, typeof(TInput), typeof(TOutput));
			m_ForceUpdate = true;
			m_Updatable = this as IUpdatableConverter;
			if (m_Output == null)
			{
				{
					m_Output = new BindingProperty(property.Path, new ValueHolder<TOutput>());
				}
				m_OutputSubscriber = new PropertySubscriber<TOutput> { Action = OnChanged, Strict = true };
				m_Output.AddSubscriber(m_OutputSubscriber);
			}
			m_Input = property;
			TryUpdate();
			output = m_Output;
			return true;
		}

		public virtual void Unbind()
		{
			m_Input = null;
		}

		public void SetDirty()
		{
			m_ForceUpdate = true;
		}

		public void TryUpdate()
		{
			if (m_Updatable != null)
			{
				if (m_OnUpdateLock)
				{
					return;
				}
				m_OnUpdateLock = true;
				try
				{
					m_Updatable.OnUpdate();
				}
				finally
				{
					m_OnUpdateLock = false;
				}
			}
			if (m_Output == null || m_Input == null) return;
			if (!m_ForceUpdate && m_Input.Hash == m_Hash)
			{
				return;
			}
			m_ForceUpdate = false;
			m_Hash = m_Input.Hash;
			if (TryReadInput(out var input))
			{
				m_Output.TrySetValue(Convert(input));
			}
			else
			{
				m_Output.TrySetValue(Convert((TInput)m_Input.GetObject()));
			}
			m_Output.SetDirty();
		}

		protected void SetImmediate(TOutput output)
		{
			if (m_Output == null) return;
			m_Output.TrySetValue(output);
			m_Output.SetDirty();
		}

		bool TryReadInput(out TInput value)
		{
			if (m_AllowCast)
			{
				return m_Input.TryGetValueCast(out value);
			}
			return m_Input.TryGetValue(out value);
		}

		bool TryWriteInput(TInput value)
		{
			if (m_AllowCast)
			{
				return m_Input.TrySetValueCast(value);
			}
			return m_Input.TrySetValue(value);
		}

		void OnChanged(TOutput output)
		{
			TInput input = default;
			if (TryInverseConvert(output, ref input))
			{
				if (m_Input == null || !TryWriteInput(input))
				{
					m_Input?.SetObject(input);
				}
			}
		}

	}
}