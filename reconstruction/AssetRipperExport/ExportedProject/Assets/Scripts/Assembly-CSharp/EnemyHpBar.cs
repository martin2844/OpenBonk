using Assets.Scripts.Actors.Enemies;
using UnityEngine;

public class EnemyHpBar : MonoBehaviour
{
	public Transform hpBar;

	private Enemy enemy;

	public void Set(Enemy enemy)
	{
		this.enemy = enemy;
	}

	private void Update()
	{
		if (enemy == null || enemy.IsDeadOrDyingNextFrame()) { Destroy(gameObject); return; }
		transform.position = enemy.GetHeadPosition() + Vector3.up * 1.2f;
		hpBar.localScale = new Vector3(enemy.hp / enemy.maxHp, 1f, 1f);
	}
}
