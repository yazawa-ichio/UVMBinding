namespace UVMBinding.Core
{
	internal interface IBindingProperty
	{
		string Path { get; }
		int Hash { get; }
		object GetObject();
		void SetObject(object val);
		bool TrySetObject(object val);
		System.Type GetBindType();
		bool IsAssignable<T>();
		bool CanReceive<T>(bool allowCast);
		BindKind GetBindKind<T>(bool allowCast);
		void SetDirty();
		event System.Action<string, IValueHolder> OnChangedValue;

		bool TryGetValue<T>(out T value);
		bool TrySetValue<T>(T value);
		bool TryGetValueCast<T>(out T value);
		bool TrySetValueCast<T>(T value);
		bool TryGetValueAs<T>(out T value);
	}

}