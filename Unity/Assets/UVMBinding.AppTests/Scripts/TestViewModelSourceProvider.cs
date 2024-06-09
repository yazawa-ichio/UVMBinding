using UnityEngine;
using UVMBinding;
using UVMBinding.Core;

namespace AppTests
{
	class TestViewModelSourceProvider : ViewModelSourceProvider
	{
		class TestViewModel : ViewModel
		{
			[Bind]
			public string TestString { get; set; }
		}

		[SerializeField]
		string m_Test = "Test";

		TestViewModel m_TestViewModel;

		public override IViewModel GetViewModel()
		{
			return m_TestViewModel = new TestViewModel()
			{
				TestString = m_Test
			};
		}

		private void OnValidate()
		{
			if (m_TestViewModel != null)
			{
				m_TestViewModel.TestString = m_Test;
			}
		}
	}
}