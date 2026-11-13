using Assets.Scripts.Actors.Player;
using UnityEngine;

public class FootstepFx : MonoBehaviour
{
    public RandomSfx sfx;
    public ParticleSystem ps;
    private bool inited;

    private void Awake()
    {
        if (inited) return;
        inited = true;
        if (sfx == null || DataManager.Instance == null || MyPlayer.Instance == null) return;
        CharacterData character = DataManager.Instance.GetCharacterData(MyPlayer.Instance.character);
        if (character != null) sfx.sounds = character.audioFootsteps;
    }

    private void OnEnable()
    {
        if (sfx != null) sfx.Play();
        if (ps != null) ps.Play();
    }
}
