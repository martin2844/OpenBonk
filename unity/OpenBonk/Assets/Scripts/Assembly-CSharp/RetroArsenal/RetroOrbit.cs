using UnityEngine;

namespace RetroArsenal
{
	public class RetroOrbit : MonoBehaviour
	{
		public Transform target;

		public Vector3 cameraOffset;

		public float defaultDistance;

		private float _currentDistance;

		public float xSpeed;

		public float ySpeed;

		public float yMinLimit;

		public float yMaxLimit;

		public float distanceMin;

		public float distanceMax;

		public float zoomLerpSpeed;

		public float smoothingFactor;

		public float collisionOffset;

		private float rotationYAxis;

		private float rotationXAxis;

		private float velocityX;

		private float velocityY;

		private Vector3 originalTargetPosition;

		private void Start()
		{
		}

		private void LateUpdate()
		{
		}
	}
}
