using Coffee.UIExtensions;
using UnityEngine;

public class MenuParticles : MonoBehaviour
{
	public GameObject coinEffect;

	public GameObject cointEffectParent;

	public UIParticleAttractor coinAttractor;

	public static MenuParticles Instance;

	private void Awake()
	{
		Instance = this;
	}

	public void CoinEffect(Vector3 position)
	{
		if (coinEffect == null || cointEffectParent == null || coinAttractor == null) return;
		GameObject instance = Instantiate(coinEffect, cointEffectParent.transform);
		instance.transform.position = position;
		if (instance.transform.childCount == 0) return;
		ParticleSystem particles = instance.transform.GetChild(0).GetComponent<ParticleSystem>();
		if (particles == null) return;
		coinAttractor.AddParticleSystem(particles);
		DestroyObject destroy = instance.GetComponent<DestroyObject>();
		if (destroy != null) destroy.OnDestroy += () => { if (coinAttractor != null) coinAttractor.RemoveParticleSystem(particles); };
	}

	public void OnCoinCollected()
	{
	}
}
