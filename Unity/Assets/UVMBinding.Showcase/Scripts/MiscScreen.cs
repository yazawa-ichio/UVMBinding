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
					vm.AnchoredPosition = right ? new Vector2(240f, 0f) : new Vector2(40f, 0f);
				};
			});
		}
	}
}
