using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding
{
	public class SelectViewBind : MonoBehaviour, IBindSourceProvider
	{
		[SerializeField]
		View m_View;

		void Awake()
		{
			if (m_View == null) return;
			using (ListPool<IViewElement>.Use(out var elements))
			{
				GetComponents(elements);
				foreach (var elm in elements)
				{
					m_View.Add(elm);
				}
			}
		}

		void OnDestroy()
		{
			if (m_View == null) return;
			using (ListPool<IViewElement>.Use(out var elements))
			{
				GetComponents(elements);
				foreach (var elm in elements)
				{
					m_View.Remove(elm);
				}
			}
		}

	}
}