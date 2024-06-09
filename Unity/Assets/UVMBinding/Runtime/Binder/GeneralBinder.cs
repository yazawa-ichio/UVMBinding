using UnityEngine;

namespace UVMBinding
{
	internal class GeneralBinderPropertyAttribute : PropertyAttribute { }

	public partial class GeneralBinder : Binder<object>
	{
		[SerializeField, SelfComponentTarget]
		Component m_Target;

		[SerializeField, GeneralBinderProperty]
		string m_Property;

		IValueSetter m_ValueSetter;

		public Component GetTarget() => m_Target;

		protected override void UpdateValue(object value)
		{
			if (m_Target == null)
			{
				return;
			}
			if (m_ValueSetter == null)
			{
				m_ValueSetter = Get(m_Target.GetType(), m_Property);
			}
			m_ValueSetter.SetValue(m_Target, value);
		}
	}
}