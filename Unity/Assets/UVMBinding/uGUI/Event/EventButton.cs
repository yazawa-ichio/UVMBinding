using UnityEngine;
using UnityEngine.UI;
using UVMBinding.Arguments;
using UVMBinding.Core;

namespace UVMBinding
{
	public class EventButton : ViewEventBase, IHasEventArgument
	{
		[SerializeReference]
		EventArgument m_Aargument = default;

		protected virtual void Awake()
		{
			if (TryGetComponent(out Button button))
			{
				button.onClick.AddListener(OnClick);
			}
		}

		protected virtual void OnClick()
		{
			if (m_Aargument != null)
			{
				m_Aargument.Do(this);
			}
			else
			{
				Dispatch();
			}
		}

		public override System.Type EventType()
		{
			return m_Aargument?.GetEventType() ?? null;
		}

		EventArgument IHasEventArgument.GetArgument()
		{
			return m_Aargument;
		}
	}

}