using System;
using UVMBinding.Logger;

namespace UVMBinding.Converters
{
	[Serializable]
	public abstract class ConverterBase<TInput, TOutput> : IConverter
	{
		static readonly bool s_InputIsValueType = typeof(TInput).IsValueType;

		public virtual bool IsActive => this != null;
		BindingProperty<TOutput> m_Output;
		IBindingProperty m_Input;
		int m_Hash;
		bool m_ForceUpdate;

		public abstract TOutput Convert(TInput input);

		public virtual bool TryInverseConvert(TOutput value, ref TInput ret)
		{
			return false;
		}

		public Type GetInputType()
		{
			return typeof(TInput);
		}

		public Type GetOutputType()
		{
			return typeof(TOutput);
		}

		bool IConverter.TryConvert(IBindingProperty property, ref IBindingProperty output)
		{
			if (s_InputIsValueType)
			{
				if (!(property is IBindingProperty<TInput>))
				{
					Log.Debug("{0} Fail TryConvert {1} => {2}", this, typeof(TInput), typeof(TOutput));
					return false;
				}
			}
			else
			{
				if (!property.IsAssignable<TInput>())
				{
					Log.Debug("{0} Fail TryConvert {1} => {2}", this, typeof(TInput), typeof(TOutput));
					return false;
				}
			}
			Log.Trace("{0} Convert Success {1} => {2}", this, typeof(TInput), typeof(TOutput));
			m_ForceUpdate = true;
			if (m_Output == null)
			{
				m_Output = new BindingProperty<TOutput>(property.Path);
				m_Output.OnChanged += OnChanged;
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

		public void TryUpdate()
		{
			if (m_Output == null || m_Input == null) return;
			if (!m_ForceUpdate && m_Input.Hash == m_Hash)
			{
				return;
			}
			m_ForceUpdate = false;
			m_Hash = m_Input.Hash;
			if (m_Input is IBindingProperty<TInput> input)
			{
				m_Output.Value = Convert(input.Value);
			}
			else
			{
				m_Output.Value = Convert((TInput)m_Input.GetObject());
			}
			m_Output.SetDirty();
		}

		void OnChanged(TOutput output)
		{
			TInput input = default;
			if (TryInverseConvert(output, ref input))
			{
				if (m_Input is IBindingProperty<TInput> prop)
				{
					prop.Value = input;
				}
				else
				{
					m_Input?.SetObject(input);
				}
			}
		}

	}
}