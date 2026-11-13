using Actors.Enemies;
using UnityEngine;

public class EnemyDisplay : MonoBehaviour
{
	public AnimatedMesh animatedMesh;

	public MeshRenderer meshRenderer;

	public Camera camera;

	public LayerMask layerMask;

	private float cameraDistance = 1f;

	private void Awake()
	{
		MyButtonLog.A_EnemySelected += SetEnemy;
	}

	private void OnDestroy()
	{
		MyButtonLog.A_EnemySelected -= SetEnemy;
	}

	public void SetEnemy(EEnemy eEnemy)
	{
		EnemyData data = DataManager.Instance.GetEnemyData(eEnemy);
		animatedMesh.gameObject.SetActive(data != null);
		if (data != null)
		{
			animatedMesh.SetAnimation(data.animation);
			meshRenderer.sharedMaterial = data.material;
		}
		EncapsulateEnemyRenderer();
		animatedMesh.gameObject.layer = LayerMask.NameToLayer("UiCamera");
	}

	private void EncapsulateEnemyRenderer()
	{
		Bounds bounds = meshRenderer.bounds;
		Vector3 size = bounds.size;
		float largest = Mathf.Max(size.x, size.y, size.z);
		float distance = cameraDistance * largest / (2f * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f)) + largest * 0.5f;
		camera.transform.position = bounds.center - camera.transform.forward * distance;
	}
}
