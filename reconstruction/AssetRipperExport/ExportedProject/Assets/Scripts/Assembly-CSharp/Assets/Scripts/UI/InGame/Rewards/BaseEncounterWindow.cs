using UnityEngine;

namespace Assets.Scripts.UI.InGame.Rewards
{
	public abstract class BaseEncounterWindow : MonoBehaviour
	{
		public abstract void Open(EEncounter encounterType);

		public abstract void OnClose();

		public abstract void ChooseOffer(int index);
	}
}
