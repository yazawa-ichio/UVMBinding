namespace UVMBinding
{

	[UnityEngine.Scripting.Preserve]
	internal class EventBase
	{
		public string Name { get; private set; }

		public EventBase Next;

		public EventBase(string name)
		{
			Name = name;
		}
	}

	internal class Event : EventBase
	{
		public event System.Action OnEvent;

		public Event(string name) : base(name) { }

		public void Invoke()
		{
			OnEvent?.Invoke();
		}

		public override string ToString()
		{
			if (OnEvent == null)
			{
				return "Empty";
			}
			return $"{OnEvent.Method}:({OnEvent.Target})";
		}
	}

	internal class Event<T> : EventBase
	{
		public event System.Action<T> OnEvent;

		public Event(string name) : base(name) { }

		public void Invoke(T val)
		{
			OnEvent?.Invoke(val);
		}

		public override string ToString()
		{
			if (OnEvent == null)
			{
				return "Empty";
			}
			return $"{OnEvent.Method}:({OnEvent.Target})";
		}
	}

}