using UnityEngine.UI;
using UVMBinding.Core;

namespace UVMBinding.Events
{
	public class InputFieldSubmitEvent : ViewEventBase
	{
		protected virtual void Awake()
		{
			if (TryGetComponent(out InputField input))
			{
				input.onEndEdit.AddListener(OnEdit);
			}
		}

		void OnEdit(string arg)
		{
			Dispatch(arg);
		}

		public override System.Type EventType()
		{
			return typeof(string);
		}
	}

}