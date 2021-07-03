using System;
using UVMBinding.Core;

namespace UVMBinding.Arguments
{
	[Serializable]
	public abstract class EventArgument
	{
		public abstract Type GetEventType();

		public abstract void Do(ViewEventBase viewEvent);
	}

	public abstract class EventArgument<T> : EventArgument
	{
		public abstract T GetValue();

		public override Type GetEventType() => typeof(T);

		public override void Do(ViewEventBase viewEvent)
		{
			viewEvent.Dispatch(GetValue());
		}
	}
}