using UnityEngine;

namespace UVMBinding.Binders
{
	public class OnOffActiveBinder : Binder<bool, GameObject>
	{
		[SerializeField]
		GameObject m_On;
		[SerializeField]
		GameObject m_Off;

		protected override void UpdateValue(bool value)
		{
			if (m_On)
			{
				m_On.SetActive(value);
			}
			if (m_Off)
			{
				m_Off.SetActive(!value);
			}
		}
	}
}