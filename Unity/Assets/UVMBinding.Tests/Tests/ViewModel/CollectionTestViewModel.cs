namespace UVMBinding.Tests
{
	public class CollectionTestViewModel : ViewModel
	{
		[Bind]
		public Collection<CollectionItemViewModel> List { get; private set; }
	}

	public class CollectionItemViewModel : ViewModel
	{
		public System.Action<int, CollectionItemViewModel> OnClick;

		[Bind]
		public int No { get; set; }

		[Event]
		void Click(int index)
		{
			OnClick?.Invoke(index, this);
		}
	}

}