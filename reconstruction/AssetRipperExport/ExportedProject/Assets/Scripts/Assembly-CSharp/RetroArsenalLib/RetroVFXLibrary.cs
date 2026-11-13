using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RetroArsenalLib
{
	public class RetroVFXLibrary : MonoBehaviour
	{
		public static RetroVFXLibrary GlobalAccess;

		public int TotalEffects;

		public int CurrentParticleEffectIndex;

		public int CurrentParticleEffectNum;

		public Vector3[] ParticleEffectSpawnOffsets;

		public float[] ParticleEffectLifetimes;

		public GameObject[] ParticleEffectPrefabs;

		private List<Transform> currentActivePEList;

		private StringBuilder effectNameBuilder;

		private void Awake()
		{
		}

		public string GetCurrentPENameString()
		{
			return null;
		}

		public void PreviousParticleEffect()
		{
		}

		public void NextParticleEffect()
		{
		}

		private void DestroyLoopingParticleEffects()
		{
		}

		private void UpdateEffectNameString()
		{
		}

		public void SpawnParticleEffect(Vector3 positionInWorldToSpawn)
		{
		}
	}
}
