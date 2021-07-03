using UVMBinding.Logger;

namespace UVMBinding.Core
{
	public static class Assignable<TSrc, TDst>
	{
		public static readonly bool Is = typeof(TDst).IsAssignableFrom(typeof(TSrc));
		static Assignable()
		{
			Log.Trace("Assignable.Is:{0} Src:{1} > Dst:{2}", Is, typeof(TSrc), typeof(TDst));
		}
	}
}