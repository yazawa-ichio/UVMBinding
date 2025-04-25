using UnityEngine;
using UVMBinding;

namespace AppTests
{
	public class GlobalState : INotifyDirtyEvent
	{
		[RuntimeInitializeOnLoadMethod]
		static void Init()
		{
			I.Value1 = "INIT";
		}

		public static readonly GlobalState I = new GlobalState();

		public NotifyDirtyEventHandler DitryHandler { get; set; }

		string m_Value1 = "INIT";
		public string Value1
		{
			get { return m_Value1; }
			set
			{
				if (m_Value1 != value)
				{
					m_Value1 = value;
					DitryHandler.SetDitry(nameof(Value1));
				}
			}
		}

		public string Value2 { get; set; } = "INIT2";
	}
}