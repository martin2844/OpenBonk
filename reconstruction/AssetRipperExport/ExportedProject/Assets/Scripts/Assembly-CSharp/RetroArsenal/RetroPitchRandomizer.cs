using UnityEngine;

namespace RetroArsenal
{
	public class RetroPitchRandomizer : MonoBehaviour
	{
		public float randomPercent = 10f;

		private void Start()
		{
			var source = transform.GetComponent<AudioSource>();
			if (source != null)
				source.pitch *= 1f + Random.Range(randomPercent / -100f, randomPercent / 100f);
		}
	}
}
