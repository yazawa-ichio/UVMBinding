using UnityEngine;
using UVMBinding.Binders;

namespace UVMBinding.Arguments
{
	public class CollectionIndexArgument : EventArgument<int>, ICollectionIndex
	{
		[SerializeField]
		int m_Index;

		public int Index { get => m_Index; set => m_Index = value; }

		public override int GetValue()
		{
			return Index;
		}
	}
}