using System;
using UnityEngine;
using UnityEngine.Events;
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
		[Bind]
		public TestEnum Popup { get; set; } = TestEnum.Value3;
		[Event]
		public UnityEvent Dialog { get; set; } = new UnityEvent();
		[Event]
		public Action Input { get; set; }
		[Event]
		public Action InputClear { get; set; }
		[Bind]
		public bool Toggle { get; set; } = true;
		[Bind]
		public ReflectionTestModel ReflectionTest { get; set; } = new ReflectionTestModel();
	}

	public class ReflectionTestModel : INotifyDirtyEvent
	{
		public string Text = "BBB";

		public int Value = 10;

		public bool Toggle = true;

		public bool Select;

		public RectTransform RectTransform
		{
			get
			{
				return null;
			}
			set
			{
				Debug.Log("Set RectTransform:" + value);
			}
		}

		public NotifyDirtyEventHandler DitryHandler { get; set; }

		void EventTest(object tmp)
		{
			Debug.Log("TMP:" + tmp);
			Text = "AAA";
			Value++;
			GlobalState.I.Value1 = "AAA";
			DitryHandler.SetAllDitry();
		}
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

		[Event]
		public Action<string> OnSubmit { get; set; }

		[Event]
		void Submit()
		{
			OnSubmit?.Invoke(Input);
		}
	}

	public class InputDialog
	{
		public string Message { get; set; }

		public string Input { get; set; }

		void Submit()
		{

		}
	}

	public class WarningViewModel
	{
		[Event]
		public string Value { get; set; }
		[Event]
		public Action<bool> Safe { get; set; }
		[Event]
		public Action<bool, bool> Warning { get; set; }
	}

}