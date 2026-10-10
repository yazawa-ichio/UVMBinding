using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Showcase
{
	public class ShowcaseTabs : MonoBehaviour
	{
		[SerializeField]
		List<GameObject> m_Screens = new List<GameObject>();
		[SerializeField]
		List<UnityEngine.UI.Button> m_Tabs = new List<UnityEngine.UI.Button>();
		[SerializeField]
		Color m_SelectedColor = new Color(0.30f, 0.55f, 0.90f, 1f);
		[SerializeField]
		Color m_NormalColor = new Color(0.22f, 0.24f, 0.29f, 1f);

		void Start()
		{
			for (int i = 0; i < m_Tabs.Count; i++)
			{
				int index = i;
				m_Tabs[i].onClick.AddListener(() => Select(index));
			}
			Select(0);
		}

		public int ScreenCount => m_Screens.Count;

		public GameObject GetScreen(int index) => (index >= 0 && index < m_Screens.Count) ? m_Screens[index] : null;

		public void Select(int index)
		{
			for (int i = 0; i < m_Screens.Count; i++)
			{
				if (m_Screens[i] != null)
				{
					m_Screens[i].SetActive(i == index);
				}
			}
			for (int i = 0; i < m_Tabs.Count; i++)
			{
				var image = m_Tabs[i].GetComponent<UnityEngine.UI.Image>();
				if (image != null)
				{
					image.color = (i == index) ? m_SelectedColor : m_NormalColor;
				}
			}
		}
	}
}
