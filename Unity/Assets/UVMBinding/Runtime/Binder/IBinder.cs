namespace UVMBinding.Core
{
	internal interface IBinder : IViewElement
	{
		string Path { get; }
		bool IsRebind { get; }
		bool IsActive { get; }
		bool NeedsUpdate { get; }
		void TryUpdate();
		void Bind(IBindingProperty prop);
		void Unbind();
	}

	internal static class BinderPathVersion
	{
		public static int Value;
	}

	public enum BindKind
	{
		None,
		Exact,
		Assignable,
		Converted,
		Cast,
	}

	public readonly struct ConverterStage
	{
		public readonly bool IsEmpty;
		public readonly System.Type InputType;
		public readonly System.Type OutputType;

		public static ConverterStage Empty => new ConverterStage(null, null, true);

		public ConverterStage(System.Type inputType, System.Type outputType)
			: this(inputType, outputType, false)
		{
		}

		ConverterStage(System.Type inputType, System.Type outputType, bool empty)
		{
			InputType = inputType;
			OutputType = outputType;
			IsEmpty = empty;
		}
	}

	public interface IBinderInfo
	{
		string Path { get; }
		System.Type BindType { get; }

		object GetBoundValue();

		bool HasConverter { get; }

		System.Type ConverterInputType { get; }

		void GetConverterStages(System.Collections.Generic.List<ConverterStage> list);

		BindKind Kind { get; }

		bool AllowCast { get; }

		string GetDisplayText();
	}
}