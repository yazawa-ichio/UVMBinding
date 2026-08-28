using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class VariantScreen : MonoBehaviour
	{
		[SerializeField]
		View m_ReflectionView;

		void Start()
		{
			int n = 0;
			GetComponent<View>().Attach<VariantViewModel>(vm =>
			{
				vm.Tracked.Value = "Property<string>";
				vm.OnChangeTracked = () => { n++; vm.Tracked.Value = $"Property<string> #{n}"; };
				vm.OnSetDirty = () => vm.SetDirty(nameof(VariantViewModel.DirtyTarget));
			});

			//素の POCO を ReflectionViewModel 経由でバインドする
			if (m_ReflectionView != null)
			{
				m_ReflectionView.Bind(new ReflectionSource());
			}
		}
	}
}
