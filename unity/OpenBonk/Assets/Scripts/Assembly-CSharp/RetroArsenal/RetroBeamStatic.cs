using UnityEngine;

namespace RetroArsenal
{
	public class RetroBeamStatic : MonoBehaviour
	{
		[Header("Prefabs")]
		public GameObject beamLineRendererPrefab;

		public GameObject beamStartPrefab;

		public GameObject beamEndPrefab;

		private GameObject beamStart;

		private GameObject beamEnd;

		private GameObject beam;

		private LineRenderer line;

		[Header("Beam Options")]
		public bool beamCollides;

		public float beamLength;

		public float beamEndOffset;

		public float textureScrollSpeed;

		public float textureLengthScale;

		[Header("Width Pulse Options")]
		public float widthMultiplier;

		private float customWidth;

		private float originalWidth;

		private float lerpValue;

		public float pulseSpeed;

		private bool pulseExpanding;

		private void Start()
		{
		}

		private void FixedUpdate()
		{
		}

		public void SpawnBeam()
		{
		}
	}
}
