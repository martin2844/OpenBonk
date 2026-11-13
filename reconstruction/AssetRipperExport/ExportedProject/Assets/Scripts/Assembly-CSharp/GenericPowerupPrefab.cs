using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Utility;
using UnityEngine;

public class GenericPowerupPrefab : MonoBehaviour
{
	public Pickup pickup;

	public ParticleSystem ps;

	public MeshRenderer minimapRenderer;

	public MeshRenderer iconRenderer;

	public Material hpMaterial;

	public Material nukeMaterial;

	public Material timeFreezeMaterial;

	public Material shieldMaterial;

	public Material rageMaterial;

	public Material hasteMaterial;

	public Material stonksMaterial;

	public Material magnetMaterial;

	private MaterialPropertyBlock propertyBlock;

	private void TryInit()
	{
		if (propertyBlock != null) return;
		propertyBlock = new MaterialPropertyBlock();
		if (iconRenderer != null) iconRenderer.GetPropertyBlock(propertyBlock);
	}

	public void Set(EPickup ePickup)
	{
		TryInit();
		pickup.ePickup = ePickup;
		if (ps != null) { var main = ps.main; main.startColor = MyColorUtility.PickupToColor(ePickup); }
		Material material = GetMinimapMaterial(ePickup);
		if (material != null && minimapRenderer != null) minimapRenderer.material = material;
		if (material != null && iconRenderer != null)
		{
			propertyBlock.SetTexture("_MainTex", material.mainTexture);
			iconRenderer.SetPropertyBlock(propertyBlock);
		}
	}

	private Material GetMinimapMaterial(EPickup ePickup)
	{
		switch (ePickup)
		{
			case EPickup.Health: return hpMaterial;
			case EPickup.Nuke: return nukeMaterial;
			case EPickup.Time: return timeFreezeMaterial;
			case EPickup.Shield: return shieldMaterial;
			case EPickup.Rage: return rageMaterial;
			case EPickup.Haste: return hasteMaterial;
			case EPickup.Stonks: return stonksMaterial;
			case EPickup.Magnet: return magnetMaterial;
			default: return null;
		}
	}
}
