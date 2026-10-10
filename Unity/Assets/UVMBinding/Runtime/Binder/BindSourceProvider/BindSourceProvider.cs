using UVMBinding.Core;
using UnityEngine;

namespace UVMBinding
{

	public abstract class BindSourceProvider : MonoBehaviour
	{
		[SerializeField]
		BindSourceTargetData m_Target = new BindSourceTargetData();

		public virtual bool IsTarget(Component component)
		{
			return m_Target.IsTarget(component);
		}

		public static bool IsOwnedByProvider(Component element)
		{
			using (ListPool<BindSourceProvider>.Use(out var providers))
			{
				element.GetComponents(providers);
				foreach (var provider in providers)
				{
					if (provider.IsTarget(element))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
