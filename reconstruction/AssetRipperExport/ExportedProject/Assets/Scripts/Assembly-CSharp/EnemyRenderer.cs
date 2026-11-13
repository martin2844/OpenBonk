using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Utility;
using UnityEngine;

public class EnemyRenderer : MonoBehaviour
{
	public Enemy enemy;

	public Renderer enemyRenderer;

	private MaterialPropertyBlock propertyBlock;

	private Color freezeColorSpecular = MyColorUtility.GetHexToColor("#009A9A");

	private Color freezeColorAlbedo = MyColorUtility.GetHexToColor("#A8FEFF");

	private Color stunColorSpecular = MyColorUtility.GetHexToColor("#918200");

	private Color stunColorAlbedo = MyColorUtility.GetHexToColor("#FFFF88");

	private Color echoColorSpecular = MyColorUtility.GetHexToColor("#00D182");

	private Color echoColorAlbedo = MyColorUtility.GetHexToColor("#94FFCD");

	private Color charmColorAlbedo = MyColorUtility.GetHexToColor("#FFD5E9");

	private Color charmColorSpecular = MyColorUtility.GetHexToColor("#C11B85");

	private Color lastSetSpecularColor;

	private string specularColorKey = "_SpecularColor";

	private string albedoColorKey = "_AlbedoColor";

	private string rimColorKey = "_RimColor";

	private void Awake()
	{
		if (enemy == null) return;
		enemy.A_DebuffAdded += OnDebuffAdded;
		enemy.A_DebuffRemoved += OnDebuffRemoved;
	}

	private void OnDestroy()
	{
		if (enemy == null) return;
		enemy.A_DebuffAdded -= OnDebuffAdded;
		enemy.A_DebuffRemoved -= OnDebuffRemoved;
	}

	public void Reset()
	{
	}

	public void Set(EnemyData enemyData)
	{
		if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
		enemyRenderer.sharedMaterial = enemyData.material;
		enemyRenderer.GetPropertyBlock(propertyBlock);
		propertyBlock.Clear();
		enemyRenderer.SetPropertyBlock(propertyBlock);
	}

	private void OnDebuffAdded(EDebuff debuff)
	{
		RefreshColor(debuff);
	}

	private void GetDebuffColor(out Color specular, out Color albedo)
	{
		specular = new Color(0f, 0f, 0f, 1f);
		albedo = Color.white;
		if (enemy.debuffs.ContainsKey(EDebuff.Echo))
		{
			specular = echoColorSpecular;
			albedo = echoColorAlbedo;
		}
		foreach (var entry in enemy.debuffs)
		{
			if (entry.Key == EDebuff.Freeze) { specular = freezeColorSpecular; albedo = freezeColorAlbedo; }
			else if (entry.Key == EDebuff.Stun) { specular = stunColorSpecular; albedo = stunColorAlbedo; }
			break;
		}
	}

	private void OnDebuffRemoved(EDebuff debuff)
	{
		RefreshColor(debuff);
	}

	public void SetInvulnerable(bool invulnerable)
	{
		enemyRenderer.GetPropertyBlock(propertyBlock);
		propertyBlock.SetColor(rimColorKey, new Color(0f, 1f, 1f, invulnerable ? 1f : 0f));
		enemyRenderer.SetPropertyBlock(propertyBlock);
	}

	private void RefreshColor(EDebuff debuff)
	{
		GetDebuffColor(out Color specular, out Color albedo);
		if (lastSetSpecularColor == specular) return;
		lastSetSpecularColor = specular;
		enemyRenderer.GetPropertyBlock(propertyBlock);
		propertyBlock.SetColor(specularColorKey, specular);
		propertyBlock.SetColor(albedoColorKey, albedo);
		enemyRenderer.SetPropertyBlock(propertyBlock);
	}
}
