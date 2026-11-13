using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Actors.Player;
using UnityEngine;

public class ZapBar : MonoBehaviour
{
	public GameObject zapIcon;

	public GameObject zapBar;

	public Transform barFill;

	private PassiveAbilityZooma zapAbility;

	private void Start()
	{
		if (MyPlayer.Instance == null || MyPlayer.Instance.inventory == null ||
			!MyPlayer.Instance.inventory.HasPassive(EPassive.Zap)) return;
		if (zapIcon != null) zapIcon.SetActive(true);
		if (zapBar != null) zapBar.SetActive(true);
	}

	private void FixedUpdate()
	{
		if (zapAbility == null)
		{
			if (MyPlayer.Instance != null && MyPlayer.Instance.inventory != null)
				zapAbility = MyPlayer.Instance.inventory.passiveAbility as PassiveAbilityZooma;
			return;
		}
		if (barFill != null) barFill.localScale = new Vector3(zapAbility.GetProgress(), 1f, 1f);
	}
}
