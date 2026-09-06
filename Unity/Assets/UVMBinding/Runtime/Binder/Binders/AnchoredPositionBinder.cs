using UnityEngine;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.None)]
	public class AnchoredPositionBinder : Binder<Vector2, RectTransform>
	{
		[SerializeField]
		Vector2 m_Offset;

		protected override void UpdateValue(Vector2 value)
		{
			Target.anchoredPosition = m_Offset + value;
		}
	}
}