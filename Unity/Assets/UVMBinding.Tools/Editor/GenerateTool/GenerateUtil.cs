using System;
using System.Collections.Generic;

namespace UVMBinding.Tools
{
	public static class GenerateUtil
	{
		public static Dictionary<System.Type, string> Names = new Dictionary<System.Type, string>
		{
			{ typeof(byte),"Byte" },
			{ typeof(ushort),"UShort" },
			{ typeof(uint),"UInt" },
			{ typeof(ulong),"ULong" },
			{ typeof(sbyte),"SByte" },
			{ typeof(short),"Short" },
			{ typeof(int),"Int" },
			{ typeof(long),"Long" },
			{ typeof(float),"Float" },
			{ typeof(double),"Double" },
		};

		public static bool CanCast(Type x, Type y)
		{
			var value = (IConvertible)System.Activator.CreateInstance(x);
			try
			{
				var method = typeof(IConvertible).GetMethod("To" + y.Name);
				UnityEngine.Debug.Log("To" + y.Name);
				UnityEngine.Debug.Log("To" + method);
				method.Invoke(value, new object[] { null });
			}
			catch (InvalidCastException)
			{
				return false;
			}
			return true;
		}

	}

}