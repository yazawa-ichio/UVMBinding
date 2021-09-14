using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Binders
{

	public abstract class EnumPopupBinderBase<T> : Binder<T> where T : Enum
	{
		static readonly T[] s_Values = (T[])Enum.GetValues(typeof(T));

		Dropdown m_Dropdown = null;

		[SerializeField]
		T[] m_Values = s_Values;

		void TryInit()
		{
			if (m_Dropdown != null) return;
			m_Dropdown = GetComponent<Dropdown>();
			m_Dropdown.ClearOptions();
			m_Dropdown.AddOptions(m_Values.Select(x => x.ToString()).ToList());
			m_Dropdown.onValueChanged.AddListener(x => Set(m_Values[x]));
		}

		protected virtual void Awake()
		{
			TryInit();
		}

		protected override void OnBind()
		{
			TryInit();
		}

		protected override void UpdateValue(T value)
		{
			TryInit();
			m_Dropdown.value = Array.IndexOf(m_Values, value);
		}

	}

}