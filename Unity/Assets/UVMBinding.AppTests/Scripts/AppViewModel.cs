using System;
using UnityEngine;
using UVMBinding;

namespace AppTests
{
	public class AppViewModel : ViewModel
	{
		public void PublishClose()
		{
			Event.Publish("Close");
		}
	}


	public class RootViewModel : AppViewModel
	{
		[Bind]
		public string InputMessage { get; set; } = "";
		[Event]
		public Action Dialog { get; set; }
		[Event]
		public Action Input { get; set; }
		[Event]
		public Action InputClear { get; set; }
	}

	public class DialogViewModel : AppViewModel
	{
		[Bind]
		public bool Notification { get; set; } = true;
		[Bind]
		public string Message { get; set; }
		[Bind]
		public string OkText { get; set; } = "OK";
		[Bind]
		public string NoText { get; set; } = "NO";
		[Event]
		public Action Ok { get; set; }
		[Event]
		public Action No { get; set; }
	}

	public class InputDialogViewModel : AppViewModel
	{
		[Bind]
		public string Message { get; set; }
		[Bind]
		public Property<string> Input { get; private set; }

		public Action<string> OnSubmit { get; set; }

		[Event]
		void Submit()
		{
			OnSubmit?.Invoke(Input);
		}
	}

}