using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class OptionalScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<OptionalViewModel>(vm =>
			{
				string tmp = "未導入";
				string loc = "未導入";
#if UVMB_TMP
				tmp = "有効 (UVMB_TMP)";
#endif
#if UVMB_LOCALIZATION
				loc = "有効 (UVMB_LOCALIZATION)";
#endif
				vm.Availability = $"TMP : {tmp}   /   Localization : {loc}";
			});
		}
	}
}
