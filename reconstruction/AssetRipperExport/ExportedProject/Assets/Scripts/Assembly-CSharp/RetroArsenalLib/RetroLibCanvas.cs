using UnityEngine;
using UnityEngine.UI;

namespace RetroArsenalLib
{
	public class RetroLibCanvas : MonoBehaviour
	{
		public static RetroLibCanvas GlobalAccess;

		public bool MouseOverButton;

		public Text PENameText;

		public Text ToolTipText;

		private RaycastHit rayHit;

		private void Awake()
		{
		}

		private void Start()
		{
		}

		private void Update()
		{
		}

		public void UpdateToolTip(string toolTipText)
		{
		}

		public void ClearToolTip()
		{
		}

		private void SelectPreviousPE()
		{
		}

		private void SelectNextPE()
		{
		}

		private void SpawnCurrentParticleEffect()
		{
		}

		public void UIButtonClick(string buttonTypeClicked)
		{
		}
	}
}
