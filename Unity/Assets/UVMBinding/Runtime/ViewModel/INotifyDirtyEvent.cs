namespace UVMBinding
{
	public struct NotifyDirtyEvent
	{
		public string Path;

		public NotifyDirtyEvent(string path)
		{
			Path = path;
		}
	}

	public delegate void NotifyDirtyEventHandler(NotifyDirtyEvent e);

	public interface INotifyDirtyEvent
	{
		NotifyDirtyEventHandler DitryHandler { get; set; }
	}

	public static class NotifyDirtyEventExtensions
	{
		public static void SetDitry(this INotifyDirtyEvent self, string path)
		{
			self?.DitryHandler?.Invoke(new NotifyDirtyEvent(path));
		}

		public static void SetAllDitry(this INotifyDirtyEvent self)
		{
			self?.DitryHandler?.Invoke(new NotifyDirtyEvent(string.Empty));
		}

		public static void SetDitry(this NotifyDirtyEventHandler self, string path)
		{
			self?.Invoke(new NotifyDirtyEvent(path));
		}

		public static void SetAllDitry(this NotifyDirtyEventHandler self)
		{
			self?.Invoke(new NotifyDirtyEvent(string.Empty));
		}
	}

}