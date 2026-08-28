using System;
using UnityEngine;
using UVMBinding.Core;
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

		public bool TryConvert(IBindingProperty input, Type outputType, ref IBindingProperty output)
		{
			if (m_Converters.Length == 0)
			{
				Log.Warning("Converters.Length Zero");
				return false;
			}
			for (int i = 0; i < m_Converters.Length; i++)
			{
				var conv = m_Converters[i];
				//最終段だけが Binder の型に合わせる必要がある。中間段は次段の入力型に従う
				var stageOutputType = (i == m_Converters.Length - 1) ? outputType : conv?.GetOutputType();
				if (conv == null || !conv.TryConvert(input, stageOutputType, ref input))
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