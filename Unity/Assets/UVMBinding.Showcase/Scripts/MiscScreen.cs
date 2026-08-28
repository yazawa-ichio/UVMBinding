using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class MiscScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<MiscViewModel>(vm =>
			{
				bool right = false;
				vm.OnMove = () =>
				{
					right = !right;
					vm.Position = right ? new Vector3(160f, 0f, 0f) : Vector3.zero;
				};
			});
		}
	}
}
