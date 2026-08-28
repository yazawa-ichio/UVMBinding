using UnityEngine;

namespace UVMBinding.Showcase
{
	//画面 1 の駆動役。ViewModel を組み立てて View にアタッチする。
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
				vm.Icon = m_Icon;
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
