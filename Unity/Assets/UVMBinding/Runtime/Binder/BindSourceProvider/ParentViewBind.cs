using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding
{
	public class ParentViewBind : MonoBehaviour, IBindSourceProvider
	{
		[SerializeField]
		private int m_Depth = 1;

		IView m_View;

		void Awake()
		{
			using (ListPool<IView>.Use(out var views))
			{
				GetComponentsInParent(true, views);
				if (views.Count > m_Depth)
				{
					m_View = views[m_Depth];
				}
			}
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
			m_View = null;
		}

	}
}