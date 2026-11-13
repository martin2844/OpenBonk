using System;

public class CharacterPrefabUI : SelectionButton
{
	public static Action<CharacterData> A_CharacterSelected;

	public static Action<CharacterPrefabUI> A_CharacterClicked;

	public CharacterData characterData;

	public void SetCharacter(CharacterData data)
	{
	}

	protected void OnCharacterSelected(CharacterData data)
	{
	}

	protected override void Init()
	{
	}

	protected override void Cleanup()
	{
	}

	protected override void OnClicked()
	{
	}

	protected override void OnSelectedCharacter()
	{
	}
}
