using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Binders
{

	public abstract class EnumPopupBinderBase<T> : Binder<T, Dropdown> where T : Enum
	{
		static readonly T[] s_Values = (T[])Enum.GetValues(typeof(T));

		[SerializeField]
		T[] m_Values = s_Values;

		protected virtual void Awake()
		{
			TryInit();
		}

		protected override void OnInit(Dropdown target)
		{
			target.ClearOptions();
			target.AddOptions(m_Values.Select(x => x.ToString()).ToList());
			target.onValueChanged.AddListener(x => Set(m_Values[x]));
		}

		protected override void UpdateValue(T value)
		{
			Target.value = Array.IndexOf(m_Values, value);
		}

	}

}