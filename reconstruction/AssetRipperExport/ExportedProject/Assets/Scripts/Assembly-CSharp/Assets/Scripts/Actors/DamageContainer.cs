using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat;
using UnityEngine;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Actors
{
	public class DamageContainer
	{
		public const string unknownDamageSource = "Unkown";

		public Vector3 direction;

		public float damage;

		public bool crit;

		public bool isExecute;

		public float knockback;

		public Enemy enemy;

		public EDamageEffect damageEffect;

		public EElement element;

		public float procCoefficient;

		public string damageSource;

		public int damageBlockedByArmor;

		public DcFlags flags;

		public bool canProcJoe;

		public DamageContainer(float procCoefficient, string damageSource)
		{
			Reuse(procCoefficient, damageSource);
		}

		public void Reuse(float procCoefficient, string damageSource)
		{
			this.procCoefficient = procCoefficient;
			this.damageSource = damageSource;
			direction = Vector3.zero;
			damage = knockback = 0f;
			crit = isExecute = canProcJoe = false;
			enemy = null;
			damageEffect = default;
			element = default;
			damageBlockedByArmor = 0;
			flags = DcFlags.None;
		}

		public void Copy(DamageContainer dcOther)
		{
			direction = dcOther.direction;
			damage = dcOther.damage;
			crit = dcOther.crit;
			isExecute = dcOther.isExecute;
			knockback = dcOther.knockback;
			enemy = dcOther.enemy;
			damageEffect = dcOther.damageEffect;
			element = dcOther.element;
			procCoefficient = dcOther.procCoefficient;
			damageSource = dcOther.damageSource;
			damageBlockedByArmor = dcOther.damageBlockedByArmor;
			flags = dcOther.flags;
			canProcJoe = dcOther.canProcJoe;
		}

		public Color GetColor()
		{
			if (damageEffect != EDamageEffect.None) return MyColorUtility.GetDamageEffectColor(damageEffect);
			return crit ? new Color(1f, 235f / 255f, 4f / 255f, 1f) : Color.white;
		}
	}
}
