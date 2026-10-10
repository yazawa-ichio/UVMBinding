using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class ProviderScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<ProviderViewModel>(vm =>
			{
				vm.OnToggleA = () => vm.FlagA = !vm.FlagA;
				vm.OnToggleB = () => vm.FlagB = !vm.FlagB;
			});
		}
	}
}
