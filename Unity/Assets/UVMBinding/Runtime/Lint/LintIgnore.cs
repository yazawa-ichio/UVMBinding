using UnityEngine;

namespace UVMBinding.Lint
{
	[DisallowMultipleComponent]
	public class LintIgnore : MonoBehaviour
	{
		[SerializeField, TextArea]
		string m_Reason = default;

		public string Reason => m_Reason;

		public bool IsValid => !string.IsNullOrWhiteSpace(m_Reason);
	}
}