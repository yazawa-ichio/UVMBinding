using UnityEngine;
using UnityEngine.UI;
using UVMBinding.Arguments;
using UVMBinding.Core;

namespace UVMBinding.Events
{
	public class EventButton : ViewEventBase, IHasEventArgument
	{
		[SerializeReference, UnityEngine.Serialization.FormerlySerializedAs("m_Aargument")]
		EventArgument m_Argument = default;

		protected virtual void Awake()
		{
			if (TryGetComponent(out Button button))
			{
				button.onClick.AddListener(OnClick);
			}
		}

		protected virtual void OnClick()
		{
			if (m_Argument != null)
			{
				m_Argument.Do(this);
			}
			else
			{
				Dispatch();
			}
		}

		public override System.Type EventType()
		{
			return m_Argument?.GetEventType() ?? null;
		}

		EventArgument IHasEventArgument.GetArgument()
		{
			return m_Argument;
		}
	}

}