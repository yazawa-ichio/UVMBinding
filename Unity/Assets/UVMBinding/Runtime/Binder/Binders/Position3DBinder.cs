using UnityEngine;

namespace UVMBinding.Binders
{
	[Lint.LintIncompatibleTarget(typeof(RectTransform),
		"localPosition of a RectTransform is derived from the parent rect, so the result is unstable. Use AnchoredPositionBinder")]
	[SemanticRole(SemanticRole.None)]
	public class Position3DBinder : Binder<Vector3>
	{
		[SerializeField]
		bool m_Global;
		[SerializeField]
		Vector3 m_Offset;

		protected override void UpdateValue(Vector3 value)
		{
			if (m_Global)
			{
				transform.position = m_Offset + value;
			}
			else
			{
				transform.localPosition = m_Offset + value;
			}
		}
	}
}