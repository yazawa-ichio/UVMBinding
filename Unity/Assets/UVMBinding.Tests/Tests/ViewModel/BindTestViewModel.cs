using System;
using System.Collections.Generic;

namespace UVMBinding.Tests
{
	public class BindTestViewModel : ViewModel
	{
		[Bind]
		public int IntValue { get; set; }

		[Bind]
		public int IntValue2 { get; set; } = 20;

		[Bind(nameof(IntValue2))]
		public Property<int> IntValue2Property { get; private set; }

		[Bind]
		public float FloatValue { get; set; }

		[Bind]
		public float FloatValue2 { get; private set; } = 0.5f;

		[Bind]
		public string StringValue { get; set; }

		[Bind]
		public string StringValue2 { get; set; } = "Test";

		[Bind]
		public ValueCollection<int> Collection { get; private set; }

		[Event(nameof(Func))]
		public Action PropertyAction { get; set; }

		[Event]
		public Action<int> PropertyValueAction { get; set; }

		[Event(nameof(Func))]
		public event Action EventAction;

		[Event]
		public event Action<float> EventValueAction;

		public int FuncCount;

		[Event]
		public void Func()
		{
			FuncCount++;
		}

		public List<string> FuncValueList = new List<string>();

		[Event]
		public void FuncValue(string value)
		{
			FuncValueList.Add(value);
		}
	}
}