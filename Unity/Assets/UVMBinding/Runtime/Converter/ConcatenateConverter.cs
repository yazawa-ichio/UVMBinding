using System;
using UnityEngine;
using UVMBinding.Logger;

namespace UVMBinding
{
	internal class ConcatenateConverter : IConverter
	{
		[SerializeReference]
		IConverter[] m_Converters = Array.Empty<IConverter>();

		public Type GetInputType()
		{
			if (m_Converters.Length == 0)
			{
				return null;
			}
			return m_Converters[0].GetInputType();
		}

		public Type GetOutputType()
		{
			if (m_Converters.Length == 0)
			{
				return null;
			}
			return m_Converters[m_Converters.Length - 1].GetOutputType();
		}

		public bool TryConvert(IBindingProperty input, ref IBindingProperty output)
		{
			if (m_Converters.Length == 0)
			{
				Log.Warning("Converters.Length Zero");
				return false;
			}
			foreach (var conv in m_Converters)
			{
				if (conv == null || !conv.TryConvert(input, ref input))
				{
					Unbind();
					return false;
				}
			}
			Log.Trace("{0} Convert Success", this);
			output = input;
			return true;
		}

		public void Unbind()
		{
			Log.Trace("{0} Unbind", this);
			foreach (var conv in m_Converters)
			{
				conv.Unbind();
			}
		}

		public void TryUpdate()
		{
			foreach (var conv in m_Converters)
			{
				conv.TryUpdate();
			}
		}

	}
}