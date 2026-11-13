using UnityEngine;

namespace Inventory__Items__Pickups.Pickups
{
	public class XpVisuals : MonoBehaviour
	{
		private enum XpTier
		{
			Low = 0,
			Medium = 10,
			High = 50
		}

		public ParticleSystem ps;

		private ParticleSystem.MainModule psMain;

		public Pickup pickup;

		public Color c_low;

		public Color c_mid;

		public Color c_high;

		public Color c_echo;

		private XpTier currentXpTier;

		private void OnEnable()
		{
			// A pooled Echo Shard may have the same tier as the next ordinary XP.
			// Force the next pickup value notification to restore its tier color.
			currentXpTier = (XpTier)(-1);
		}

		private void Awake()
		{
			if (pickup != null) pickup.A_ValueUpdated += OnValueUpdated;
			if (ps != null) psMain = ps.main;
		}

		private void OnDestroy()
		{
			if (pickup != null) pickup.A_ValueUpdated -= OnValueUpdated;
		}

		private void Update()
		{
		}

		public void SetEchoXp()
		{
			if (ps != null) psMain.startColor = c_echo;
		}

		private void OnValueUpdated(int newValue)
		{
			XpTier tier = GetXpTier(newValue);
			if (tier == currentXpTier) return;
			currentXpTier = tier;
			if (ps != null) psMain.startColor = tier == XpTier.High ? c_high : tier == XpTier.Medium ? c_mid : c_low;
			transform.localScale = Vector3.one * (tier == XpTier.High ? 1.5f : tier == XpTier.Medium ? 1.25f : 1f);
		}

		private XpTier GetXpTier(int value)
		{
			return value >= 50 ? XpTier.High : value >= 10 ? XpTier.Medium : XpTier.Low;
		}
	}
}
