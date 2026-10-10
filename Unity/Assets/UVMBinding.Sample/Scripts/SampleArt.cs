using UnityEngine;

namespace UVMBinding.Sample
{
	[System.Serializable]
	public class SampleArt
	{
		[SerializeField] Sprite m_Crossroads = default;
		[SerializeField] Sprite m_Shop = default;
		[SerializeField] Sprite m_Tavern = default;
		[SerializeField] Sprite m_Pass = default;
		[SerializeField] Sprite m_Cave = default;
		[SerializeField] Sprite m_River = default;
		[SerializeField] Sprite m_Gate = default;
		[SerializeField] Sprite m_Grave = default;

		[SerializeField] Sprite m_Dog = default;
		[SerializeField] Sprite m_Bandit = default;
		[SerializeField] Sprite m_Eater = default;

		public Sprite Crossroads => m_Crossroads;
		public Sprite Shop => m_Shop;
		public Sprite Tavern => m_Tavern;
		public Sprite Gate => m_Gate;
		public Sprite Grave => m_Grave;

		public Sprite Scenery(RouteKind kind)
		{
			switch (kind)
			{
				case RouteKind.Pass: return m_Pass;
				case RouteKind.Cave: return m_Cave;
				case RouteKind.River: return m_River;
				default: return m_Crossroads;
			}
		}

		public Sprite Portrait(Enemy enemy)
		{
			if (enemy == null) return null;
			if (enemy.Name == "山賊") return m_Bandit;
			if (enemy.Name == "岩喰い") return m_Eater;
			return m_Dog;
		}
	}
}
