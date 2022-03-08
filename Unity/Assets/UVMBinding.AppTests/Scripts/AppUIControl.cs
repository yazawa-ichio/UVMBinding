using ILib.UI;
using System.Threading.Tasks;
using UnityEngine;
using UVMBinding;

namespace AppTests
{
	public class AppUIControl : UIControl<AppViewModel>, IExecuteBack
	{
		[SerializeField]
		bool m_Root;

		protected override Task OnCreated(AppViewModel prm)
		{
			GetComponent<View>().Attach(prm);
			prm.Event.Subscribe("Close", Close);
			return base.OnCreated(prm);
		}

		public bool TryBack()
		{
			if (!m_Root)
			{
				Close();
			}
			return true;
		}

	}
}