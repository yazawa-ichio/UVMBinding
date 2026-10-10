using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace UVMBinding.PropertyProvider
{
	[Serializable]
	public class CombineBoolProvider : PropertyProvider
	{
		enum Operation
		{
			All,
			Any
		}

		[SerializeField]
		string m_ValueKey;

		[SerializeField, PropertyPath]
		string[] m_SourceKeys = Array.Empty<string>();

		[SerializeField]
		bool m_DefaultValue;

		[SerializeField]
		Operation m_Operation;

		[SerializeField]
		bool m_Reverse;

		public override IEnumerable<ExtensionPropertyInfo> GetInfos()
		{
			yield return new ExtensionPropertyInfo(m_ValueKey, typeof(bool), m_SourceKeys,
				m_Operation == Operation.All ? PropertyRelation.All : PropertyRelation.Any, m_Reverse);
		}

		public override void Init(IViewModelPropertyExtension propertyExtension)
		{
			bool?[] values = new bool?[m_SourceKeys.Length];
			for (int i = 0; i < m_SourceKeys.Length; i++)
			{
				int index = i;
				propertyExtension.Subscribe<bool>(m_SourceKeys[i], v =>
				{
					values[index] = v;
					bool ret = GetCurrent();
					if (m_Reverse)
					{
						ret = !ret;
					}

					propertyExtension.Set(m_ValueKey, ret);
				});
			}

			bool GetCurrent()
			{
				foreach (var value in values)
				{
					if (value == null)
					{
						return m_DefaultValue;
					}
				}

				switch (m_Operation)
				{
					case Operation.All:
						foreach (var value in values)
						{
							if (!value.GetValueOrDefault())
							{
								return false;
							}
						}
						return true;
					case Operation.Any:
						foreach (var value in values)
						{
							if (value.GetValueOrDefault())
							{
								return true;
							}
						}
						return false;
				}

				return m_DefaultValue;
			}
		}
	}

}