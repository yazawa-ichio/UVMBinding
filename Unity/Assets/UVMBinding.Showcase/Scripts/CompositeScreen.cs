using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class CompositeScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<CompositeViewModel>(vm =>
			{
				bool a = true;
				vm.Child = new ChildViewModel { ChildText = "子 A" };
				vm.OnSwap = () =>
				{
					a = !a;
					vm.Child = new ChildViewModel { ChildText = a ? "子 A" : "子 B" };
				};
			});
		}
	}
}
