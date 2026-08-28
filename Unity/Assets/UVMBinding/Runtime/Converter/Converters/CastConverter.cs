using System;
using System.Collections.Generic;
using UVMBinding.Core;
using UVMBinding.Logger;

namespace UVMBinding.Converters
{
	//数値同士の変換。実装は CastRegistry から入出力の型で取得する。
	//シリアライズされる状態を持たないため、型ごとのクラスを用意する必要がない。
	[Serializable]
	[DispName("Core/Cast")]
	public class CastConverter : IConverter
	{
		IConverter m_Impl;
		Type m_InputType;
		Type m_OutputType;

		public Type GetInputType() => m_InputType;

		public Type GetOutputType() => m_OutputType;

		bool IConverter.TryConvert(IBindingProperty input, Type outputType, ref IBindingProperty output)
		{
			var inputType = input.GetBindType();
			if (outputType == null)
			{
				Log.Warning("{0} Fail TryConvert OutputType Unknown Input:{1}", this, inputType);
				return false;
			}
			if (m_Impl == null || m_InputType != inputType || m_OutputType != outputType)
			{
				m_Impl = CastRegistry.Create(inputType, outputType);
				m_InputType = inputType;
				m_OutputType = outputType;
			}
			if (m_Impl == null)
			{
				Log.Warning("{0} Not Registered Cast {1} => {2}", this, inputType, outputType);
				return false;
			}
			return m_Impl.TryConvert(input, outputType, ref output);
		}

		public void TryUpdate()
		{
			m_Impl?.TryUpdate();
		}

		public void Unbind()
		{
			m_Impl?.Unbind();
		}
	}

	//生成コード（Cast.Generated.cs）の RegisterGenerated から登録される。
	//Register の呼び出しがジェネリック実体化への静的参照になるため AOT で削除されない。
	internal static partial class CastRegistry
	{
		//実体はバインドごとに状態を持つため、都度生成する
		delegate IConverter Factory();

		static Dictionary<(Type, Type), Factory> s_Map;

		static void Register<TInput, TOutput>(Func<TInput, TOutput> convert)
		{
			s_Map[(typeof(TInput), typeof(TOutput))] = () => new CastImpl<TInput, TOutput>(convert);
		}

		public static IConverter Create(Type inputType, Type outputType)
		{
			if (s_Map == null)
			{
				s_Map = new Dictionary<(Type, Type), Factory>();
				RegisterGenerated();
			}
			if (s_Map.TryGetValue((inputType, outputType), out var factory))
			{
				return factory();
			}
			return null;
		}

		internal static bool IsRegistered(Type inputType, Type outputType)
		{
			if (s_Map == null)
			{
				s_Map = new Dictionary<(Type, Type), Factory>();
				RegisterGenerated();
			}
			return s_Map.ContainsKey((inputType, outputType));
		}
	}

	internal class CastImpl<TInput, TOutput> : ConverterBase<TInput, TOutput>
	{
		readonly Func<TInput, TOutput> m_Convert;

		public CastImpl(Func<TInput, TOutput> convert)
		{
			m_Convert = convert;
		}

		public override TOutput Convert(TInput input)
		{
			return m_Convert(input);
		}
	}

}
