using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class ConverterScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<ConverterViewModel>(vm =>
			{
				vm.OnIncrement = () =>
				{
					vm.IntValue++;
					vm.FloatValue = vm.IntValue;
				};
				vm.OnToggle = () =>
				{
					vm.BoolValue = !vm.BoolValue;
					vm.ByteValue = (byte)(vm.BoolValue ? 1 : 0);
				};
			});
		}
	}
}
