using System;
using UnityEngine;
using UnityEngine.UI;

public class MyButtonSetting : MyButton
{
	public BetterSetting betterSetting;

	public MaskableGraphic background;

	public Color defaultColor;

	public Color hoverColor;

	public Action A_StartHover;

	public Action A_StopHover;

	private float nextUpdateTime;

	private int holdingDir;

	private float cooldownTimer;

	private float multiplier;

	private float canUseHorizontalTime;

	public override void StartHover()
	{
		if (background != null) background.color = hoverColor;
		isHovering = true;
		ResetCooldown();
		ResetCanUseHorizontalTime();
		A_StartHover?.Invoke();
	}

	private void ResetCanUseHorizontalTime()
	{
		canUseHorizontalTime = Time.time + 0.2f;
	}

	public override void StopHover()
	{
		if (background != null) background.color = defaultColor;
		isHovering = false;
		A_StopHover?.Invoke();
	}

	protected override void OnClick()
	{
	}

	private new void Update()
	{
		base.Update();
		if (betterSetting == null || AndroidControllerBindings.IsListening) return;
		if (betterSetting.IsDisabled()) { holdingDir = 0; return; }
		if (!isHovering || Time.time < canUseHorizontalTime) return;
		ProcessHorizontal(MyInputManager.GetAxis(MyInputManager.UIHorizontal), Time.time);
	}

	private void ProcessHorizontal(float axis, float now)
	{
		int dir = axis > 0.5f ? 1 : axis < -0.5f ? -1 : 0;
		if (dir != holdingDir)
		{
			cooldownTimer = 0.3f;
			multiplier = 1f;
			nextUpdateTime = now;
		}
		holdingDir = dir;
		if (now < nextUpdateTime) return;
		nextUpdateTime = now + cooldownTimer;
		cooldownTimer = Mathf.Clamp(cooldownTimer * 0.6f, 0.01f, 0.35f);
		multiplier *= 1.015f;
		if (holdingDir == 0) return;
		betterSetting.ControllerInputDir(holdingDir, multiplier);
		if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSelect();
	}

	private void ResetCooldown()
	{
		cooldownTimer = 0.3f;
		multiplier = 1f;
		nextUpdateTime = Time.time;
	}
}
