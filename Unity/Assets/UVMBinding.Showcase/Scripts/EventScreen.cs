using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class EventScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<EventViewModel>(vm =>
			{
				vm.OnPlain = () => vm.Log = "Action（引数なし）";
				vm.OnWithText = x => vm.Log = $"Action<string> : {x}";
				vm.OnWithColor = c => vm.Log = $"Action<Color> : {ColorUtility.ToHtmlStringRGB(c)}";
			});
		}
	}
}
