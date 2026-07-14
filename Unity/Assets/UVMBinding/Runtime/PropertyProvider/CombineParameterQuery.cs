using System;

namespace UVMBinding.PropertyProvider
{
	public struct CombineParameterQuery
	{
		public IViewModelPropertyExtension PropertyExtension;

		public CombineParameterBuilder<T1> Add<T1>(string key)
		{
			return new CombineParameterBuilder<T1>
			{
				SourceKey = key
			};
		}
	}

	public struct CombineParameterBuilder<T1>
	{
		public IViewModelPropertyExtension PropertyExtension;
		public string SourceKey;

		public void Convert<TResult>(string key, Func<T1, TResult> func)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			Handle(x =>
			{
				propertyExtension.Set(key, func(x));
			});
		}

		public void Handle(Action<T1> action)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			propertyExtension.Subscribe<T1>(SourceKey, action);
		}


		public CombineParameterBuilder<T1, T2> Add<T2>(string key2)
		{
			return new CombineParameterBuilder<T1, T2>
			{
				PropertyExtension = PropertyExtension, SourceKey1 = SourceKey, SourceKey2 = key2
			};
		}
	}

	public struct CombineParameterBuilder<T1, T2>
	{
		public IViewModelPropertyExtension PropertyExtension;
		public string SourceKey1;
		public string SourceKey2;

		public void Combine<TResult>(string key, Func<T1, T2, TResult> func)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			Handle((v1, v2) =>
			{
				propertyExtension.Set(key, func(v1, v2));
			});
		}

		public void Handle(Action<T1, T2> action)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			bool key1 = false;
			bool key2 = false;
			T1 value1 = default;
			T2 value2 = default;
			propertyExtension.Subscribe<T1>(SourceKey1, v =>
			{
				value1 = v;
				key1 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T2>(SourceKey2, v =>
			{
				value2 = v;
				key2 = true;
				TryInvoke();
			});

			void TryInvoke()
			{
				if (key1 && key2)
				{
					action(value1, value2);
				}
			}
		}


		public CombineParameterBuilder<T1, T2, T3> Add<T3>(string key)
		{
			return new CombineParameterBuilder<T1, T2, T3>
			{
				PropertyExtension = PropertyExtension, SourceKey1 = SourceKey1, SourceKey2 = SourceKey2, SourceKey3 = key
			};
		}
	}

	public struct CombineParameterBuilder<T1, T2, T3>
	{
		public IViewModelPropertyExtension PropertyExtension;
		public string SourceKey1;
		public string SourceKey2;
		public string SourceKey3;

		public void Combine<TResult>(string key, Func<T1, T2, T3, TResult> func)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			Handle((v1, v2, v3) =>
			{
				propertyExtension.Set(key, func(v1, v2, v3));
			});
		}

		public void Handle(Action<T1, T2, T3> action)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			bool key1 = false;
			bool key2 = false;
			bool key3 = false;
			T1 value1 = default;
			T2 value2 = default;
			T3 value3 = default;
			propertyExtension.Subscribe<T1>(SourceKey1, v =>
			{
				value1 = v;
				key1 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T2>(SourceKey2, v =>
			{
				value2 = v;
				key2 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T3>(SourceKey3, v =>
			{
				value3 = v;
				key3 = true;
				TryInvoke();
			});

			void TryInvoke()
			{
				if (key1 && key2 && key3)
				{
					action(value1, value2, value3);
				}
			}
		}

		public CombineParameterBuilder<T1, T2, T3, T4> Add<T4>(string key)
		{
			return new CombineParameterBuilder<T1, T2, T3, T4>
			{
				PropertyExtension = PropertyExtension,
				SourceKey1 = SourceKey1,
				SourceKey2 = SourceKey2,
				SourceKey3 = SourceKey3,
				SourceKey4 = key
			};
		}
	}

	public struct CombineParameterBuilder<T1, T2, T3, T4>
	{
		public IViewModelPropertyExtension PropertyExtension;
		public string SourceKey1;
		public string SourceKey2;
		public string SourceKey3;
		public string SourceKey4;

		public void Combine<TResult>(string key, Func<T1, T2, T3, T4, TResult> func)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			Handle((v1, v2, v3, v4) =>
			{
				propertyExtension.Set(key, func(v1, v2, v3, v4));
			});
		}

		public void Handle(Action<T1, T2, T3, T4> action)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			bool key1 = false;
			bool key2 = false;
			bool key3 = false;
			bool key4 = false;
			T1 value1 = default;
			T2 value2 = default;
			T3 value3 = default;
			T4 value4 = default;
			propertyExtension.Subscribe<T1>(SourceKey1, v =>
			{
				value1 = v;
				key1 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T2>(SourceKey2, v =>
			{
				value2 = v;
				key2 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T3>(SourceKey3, v =>
			{
				value3 = v;
				key3 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T4>(SourceKey4, v =>
			{
				value4 = v;
				key4 = true;
				TryInvoke();
			});

			void TryInvoke()
			{
				if (key1 && key2 && key3 && key4)
				{
					action(value1, value2, value3, value4);
				}
			}
		}

		public CombineParameterBuilder<T1, T2, T3, T4, T5> Add<T5>(string key)
		{
			return new CombineParameterBuilder<T1, T2, T3, T4, T5>
			{
				PropertyExtension = PropertyExtension,
				SourceKey1 = SourceKey1,
				SourceKey2 = SourceKey2,
				SourceKey3 = SourceKey3,
				SourceKey4 = SourceKey4,
				SourceKey5 = key
			};
		}
	}

	public struct CombineParameterBuilder<T1, T2, T3, T4, T5>
	{
		public IViewModelPropertyExtension PropertyExtension;
		public string SourceKey1;
		public string SourceKey2;
		public string SourceKey3;
		public string SourceKey4;
		public string SourceKey5;

		public void Combine<TResult>(string key, Func<T1, T2, T3, T4, T5, TResult> func)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			Handle((v1, v2, v3, v4, v5) =>
			{
				propertyExtension.Set(key, func(v1, v2, v3, v4, v5));
			});
		}

		public void Handle(Action<T1, T2, T3, T4, T5> action)
		{
			IViewModelPropertyExtension propertyExtension = PropertyExtension;
			bool key1 = false;
			bool key2 = false;
			bool key3 = false;
			bool key4 = false;
			bool key5 = false;
			T1 value1 = default;
			T2 value2 = default;
			T3 value3 = default;
			T4 value4 = default;
			T5 value5 = default;
			propertyExtension.Subscribe<T1>(SourceKey1, v =>
			{
				value1 = v;
				key1 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T2>(SourceKey2, v =>
			{
				value2 = v;
				key2 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T3>(SourceKey3, v =>
			{
				value3 = v;
				key3 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T4>(SourceKey4, v =>
			{
				value4 = v;
				key4 = true;
				TryInvoke();
			});
			propertyExtension.Subscribe<T5>(SourceKey5, v =>
			{
				value5 = v;
				key5 = true;
				TryInvoke();
			});


			void TryInvoke()
			{
				if (key1 && key2 && key3 && key4 && key5)
				{
					action(value1, value2, value3, value4, value5);
				}
			}
		}
	}
}