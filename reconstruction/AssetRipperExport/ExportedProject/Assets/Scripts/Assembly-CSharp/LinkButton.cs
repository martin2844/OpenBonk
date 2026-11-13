using Assets.Scripts.Utility;
using UnityEngine;

public class LinkButton : MonoBehaviour
{
    public void Discord()
    {
        Application.OpenURL(Links.discord);
    }

    public void Youtube()
    {
        Application.OpenURL(Links.youtube);
    }
}
