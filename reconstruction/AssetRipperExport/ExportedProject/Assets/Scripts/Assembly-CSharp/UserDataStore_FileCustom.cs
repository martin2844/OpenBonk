#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using Rewired.Data;

public class UserDataStore_FileCustom : UserDataStore_File
{
	protected override void SetInitialValues()
	{
	}

	public void UpdatePath()
	{
	}
}
#endif
