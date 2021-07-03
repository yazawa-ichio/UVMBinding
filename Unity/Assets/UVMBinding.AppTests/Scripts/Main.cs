using UnityEngine;

namespace AppTests
{
	public class Main : MonoBehaviour
	{
		[SerializeField]
		AppUIStack m_UIStack = null;

		RootViewModel m_Root;

		void Start()
		{
			m_UIStack.Push<RootViewModel>("Root", (vm) =>
			{
				m_Root = vm;
				vm.Dialog = OnDialog;
				vm.Input = OnInpu;
				vm.InputClear = () => vm.InputMessage = "";
			});
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				m_UIStack.ExecuteBack();
			}
		}

		void OnDialog()
		{
			m_UIStack.Push<DialogViewModel>("Dialog", (vm) =>
			{
				vm.Message = "Push Dialog?";
				vm.Notification = false;
				vm.OkText = "YES";
				vm.Ok = OnDialog;
				vm.No = vm.PublishClose;
			});
		}

		void OnInpu()
		{
			m_UIStack.Push<InputDialogViewModel>("InputDialog", (vm) =>
			{
				vm.Message = "Input Message";
				vm.Input.Value = m_Root.InputMessage;
				vm.OnSubmit += (x) =>
				{
					if (m_Root.InputMessage == x)
					{
						vm.PublishClose();
					}
					else
					{
						OnSubmit(x);
					}
				};
			});
		}

		void OnSubmit(string message)
		{
			m_UIStack.Switch<DialogViewModel>("Dialog", (vm) =>
			{
				vm.Message = $"Change Message\n{message}";
				vm.Notification = true;
				vm.Ok = () =>
				{
					m_Root.InputMessage = message;
					vm.PublishClose();
				};
			});
		}

	}
}