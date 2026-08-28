using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class EffectScreen : MonoBehaviour
	{
		void Start()
		{
			GetComponent<View>().Attach<EffectViewModel>(vm =>
			{
				var rng = new System.Random(1);
				vm.OnToggleVisible = () => vm.Visible = !vm.Visible;
				vm.OnRandomize = () =>
				{
					vm.Alpha = 0.3f + (float)rng.NextDouble() * 0.7f;
					vm.Fill = (float)rng.NextDouble();
					vm.Tint = new Color((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble(), 1f);
					vm.Offset = new Vector3((float)(rng.NextDouble() - 0.5) * 120f, 0f, 0f);
					vm.Scale = Vector3.one * (0.6f + (float)rng.NextDouble() * 0.8f);
				};
			});
		}
	}
}
