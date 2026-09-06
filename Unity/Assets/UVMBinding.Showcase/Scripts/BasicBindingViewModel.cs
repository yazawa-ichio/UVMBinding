using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	public class BasicBindingViewModel : ViewModel
	{
		[Bind]
		public string Message { get; set; } = "Hello UVMBinding";

		[Bind]
		public string CounterText { get; set; } = "Count : 0";

		[Bind]
		public bool ShowPanel { get; set; } = true;

		[Bind]
		public bool IsEnabled { get; set; } = true;

		[Bind]
		public Sprite AvatarIcon { get; set; }

		[Bind]
		public Texture Preview { get; set; }

		[Event]
		public Action OnIncrement { get; set; }

		[Event]
		public Action OnTogglePanel { get; set; }

		[Event]
		public Action OnToggleEnabled { get; set; }
	}
}
