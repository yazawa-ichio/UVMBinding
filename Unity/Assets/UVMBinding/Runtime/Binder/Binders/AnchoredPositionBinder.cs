using UnityEngine;

namespace UVMBinding.Binders
{
	//RectTransform 向けの座標バインド。
	//PositionBinder は localPosition に書くが、RectTransform の localPosition は
	//アンカーと親の解決済み矩形から導かれるため、レイアウトが確定する前に書くと
	//結果が安定しない。anchoredPosition は親の大きさに依存しないため影響を受けない。
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