using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Showcase
{
	public class ShowcaseViewModelSourceProvider : ViewModelSourceProvider<ShowcaseViewModelSourceProvider.SourceViewModel>
	{
		public class SourceViewModel : ViewModel
		{
			[Bind] public string SourceText { get; set; }
		}

		[SerializeField]
		string m_Text = "Provider が差し込んだ値";

		SourceViewModel m_ViewModel;

		protected override SourceViewModel GetSource()
		{
			return m_ViewModel = new SourceViewModel { SourceText = m_Text };
		}
	}
}
