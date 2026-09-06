using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class BasicBindingScreen : MonoBehaviour
	{
		[SerializeField]
		Sprite m_Icon;
		[SerializeField]
		Texture m_Preview;

		int m_Count;

		void Start()
		{
			var view = GetComponent<View>();
			view.Attach<BasicBindingViewModel>(vm =>
			{
				vm.AvatarIcon = m_Icon;
				vm.Preview = m_Preview;

				vm.OnIncrement = () =>
				{
					m_Count++;
					vm.CounterText = $"Count : {m_Count}";
				};
				vm.OnTogglePanel = () => vm.ShowPanel = !vm.ShowPanel;
				vm.OnToggleEnabled = () => vm.IsEnabled = !vm.IsEnabled;
			});
		}
	}
}
