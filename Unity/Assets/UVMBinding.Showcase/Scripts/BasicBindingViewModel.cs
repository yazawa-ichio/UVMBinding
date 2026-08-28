using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	//画面 1「基本バインド」。表示系 Binder の確認用。
	public class BasicBindingViewModel : ViewModel
	{
		[Bind]
		public string Message { get; set; } = "Hello UVMBinding";

		[Bind]
		public string CounterText { get; set; } = "Count : 0";

		//ActiveBinder / TargetActiveBinder 用
		[Bind]
		public bool ShowPanel { get; set; } = true;

		//EnabledBinder / InteractableBinder 用
		[Bind]
		public bool IsEnabled { get; set; } = true;

		//ImageSpriteBinder 用
		[Bind]
		public Sprite Icon { get; set; }

		//RawImageTextureBinder 用
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
