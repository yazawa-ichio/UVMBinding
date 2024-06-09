using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Arguments;
using UVMBinding.Core;

namespace UVMBinding.Binders
{
	public interface ICollectionIndex
	{
		int Index { get; set; }
	}

	public class CollectionBinder : Binder<IList<IViewModel>>
	{
		[SerializeField]
		GameObject m_Preafb = null;
		[SerializeField]
		bool m_DisableAutoActive = false;

		List<GameObject> m_Instance = new();

		protected override void UpdateValue(IList<IViewModel> value)
		{
			// 破棄済みを削除
			for (int i = m_Instance.Count - 1; i >= 0; i--)
			{
				if (m_Instance[i] == null)
				{
					m_Instance.RemoveAt(i);
				}
			}
			// 多い分を削除
			for (int i = m_Instance.Count - 1; i >= value.Count; i--)
			{
				GameObject.Destroy(m_Instance[i]);
				m_Instance.RemoveAt(i);
			}
			for (int i = 0; i < value.Count; i++)
			{
				if (m_Instance.Count == i)
				{
					m_Instance.Add(Instantiate(m_Preafb, transform));
				}
				Bind(i, m_Instance[i], value[i]);
			}
		}

		protected virtual void Bind(int index, GameObject instance, IViewModel vm)
		{
			var view = instance.GetComponent<IView>();
			view.Prepare();
			view.ViewModel = vm;
			if (!instance.activeSelf && !m_DisableAutoActive)
			{
				instance.SetActive(true);
			}
			using (ListPool<ICollectionIndex>.Use(out var list))
			{
				instance.GetComponentsInChildren(true, list);
				foreach (var item in list)
				{
					item.Index = index;
				}
			}
			using (ListPool<IViewElement>.Use(out var list))
			{
				view.GetElements(list);
				foreach (var item in list)
				{
					if (item is IHasEventArgument e && e.GetArgument() is ICollectionIndex i)
					{
						i.Index = index;
					}
				}
			}
			view.TryUpdate();
		}
	}
}