using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class InputScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<InputViewModel>(vm =>
			{
				vm.OnToggleInteractable = () => vm.IsInteractable = !vm.IsInteractable;
				//双方向バインドの結果を別プロパティへ写して可視化する
				var p = ((Core.IViewModel)vm).Property;
				p.Subscribe<string>(nameof(InputViewModel.TextValue), x => vm.TextEcho = string.IsNullOrEmpty(x) ? "(未入力)" : x);
				p.Subscribe<float>(nameof(InputViewModel.SliderValue), x => vm.SliderEcho = x.ToString("0.00"));
				p.Subscribe<bool>(nameof(InputViewModel.ToggleValue), x => vm.ToggleEcho = x ? "ON" : "OFF");
				p.Subscribe<int>(nameof(InputViewModel.DropdownIndex), x => vm.DropdownEcho = x.ToString());
			});
		}
	}
}
