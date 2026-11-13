using UnityEngine;

namespace Assets.Scripts.Camera
{
	public class MinimapArrow : MonoBehaviour
	{
		public Transform target;

		public MeshRenderer arrowRenderer;

		private Material material;

		public void Set(Transform target, Color color)
		{
			this.target = target;
			material = arrowRenderer.material;
			material.SetColor("_Color", color);
		}

		private void OnDestroy()
		{
			if (material != null) Destroy(material);
		}
	}
}
