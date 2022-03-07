namespace UVMBinding.Tests
{
	[DispName("BinderTest")]
	public class BinderTestViewModel : ViewModel
	{
		[Bind]
		public string Text1
		{
			get => GetImpl<string>(nameof(Text1));
			set => SetImpl<string>(nameof(Text1), value);
		}

		[Bind]
		public float Text2
		{
			get => GetImpl<float>(nameof(Text2));
			set => SetImpl<float>(nameof(Text2), value);
		}

		[Event, IgnoreCodeGen]
		public event System.Action OnButton1
		{
			add => Event.Subscribe(nameof(OnButton1), value);
			remove => Event.Unsubscribe(nameof(OnButton1), value);
		}

		[Event, IgnoreCodeGen]
		public event System.Action<string> OnButton2
		{
			add => Event.Subscribe(nameof(OnButton2), value);
			remove => Event.Unsubscribe(nameof(OnButton2), value);
		}

		[Bind]
		public NestViewModel Nest { get; set; } = new NestViewModel();

		public class NestViewModel : ViewModel
		{
			[Bind]
			public string NestText { get; set; }
			[Event]
			public System.Action NestAction { get; set; }
		}

	}


}