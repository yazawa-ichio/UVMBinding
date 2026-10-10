using System;
using UnityEngine;

namespace UVMBinding.Showcase
{
	[RequireComponent(typeof(View))]
	public class CollectionScreen : MonoBehaviour
	{
		int m_Serial;

		void Start()
		{
			GetComponent<View>().Attach<CollectionViewModel>(vm =>
			{
				void Sync() => vm.Summary = $"{vm.Rows.Count} 件";

				void Add()
				{
					m_Serial++;
					var row = new CollectionRowViewModel { Label = $"Item {m_Serial:00}" };
					row.OnPick = index => vm.Summary = $"{vm.Rows.Count} 件 / 選択 {index}";
					vm.Rows.Add(row);
					Sync();
				}

				vm.OnAdd = Add;
				vm.OnRemove = () =>
				{
					if (vm.Rows.Count > 0)
					{
						vm.Rows.RemoveAt(vm.Rows.Count - 1);
						Sync();
					}
				};
				vm.OnClear = () => { vm.Rows.Clear(); Sync(); };

				for (int i = 0; i < 3; i++) Add();
			});
		}
	}
}
