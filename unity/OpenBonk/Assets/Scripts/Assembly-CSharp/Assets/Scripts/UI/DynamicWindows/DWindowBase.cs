using UnityEngine;
using Assets.Scripts.Managers;

namespace Assets.Scripts.UI.DynamicWindows
{
	public class DWindowBase : MonoBehaviour
	{
		private int rebuildAfterFrames;

		protected void QueueRebuild()
		{
			rebuildAfterFrames = 3;
		}

		private void LateUpdate()
		{
			if (rebuildAfterFrames < 0) return;
			if (--rebuildAfterFrames == 0)
			{
				UiUtility.RebuildUi(transform);
				ButtonManager.Refresh();
			}
		}
	}
}
