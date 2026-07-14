using System;
using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.PropertyProvider
{
	[Serializable]
	public class CombineStringProvider : PropertyProvider
	{
		[SerializeField]
		string m_Format = "";

		[SerializeField]
		string m_ValueKey;

		[SerializeField, PropertyPath]
		string[] m_SourceKeys = Array.Empty<string>();

		public override IEnumerable<ExtensionPropertyInfo> GetInfos()
		{
			yield return new ExtensionPropertyInfo(m_ValueKey, typeof(string));
		}

		public override void Init(IViewModelPropertyExtension propertyExtension)
		{
			object[] values = new object[m_SourceKeys.Length];
			for (int i = 0; i < m_SourceKeys.Length; i++)
			{
				int index = i;
				propertyExtension.Subscribe<object>(m_SourceKeys[i], v =>
				{
					Debug.Log(v);
					values[index] = v;
					foreach (var value in values)
					{
						if (value == null)
						{
							return;
						}
					}
					string ret = string.Format(m_Format, values);
					Debug.Log(ret);
					propertyExtension.Set(m_ValueKey, ret);
				});
			}
		}
	}
}