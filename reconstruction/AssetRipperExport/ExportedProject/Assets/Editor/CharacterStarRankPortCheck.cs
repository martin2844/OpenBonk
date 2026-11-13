using System;
using Assets.Scripts.Saves___Serialization.Progression;
using Inventory__Items__Pickups;
using UnityEngine;

public static class CharacterStarRankPortCheck
{
    public static void Run()
    {
        var previous = XpUtility.xpForLevelsTable;
        try
        {
            XpUtility.Init();
            var progress = new CharacterProgression();
            int threshold = XpUtility.LevelToXp(99);
            progress.xp = threshold - 1;
            if (progress.GetRank() != 99 || progress.HasStar()) throw new Exception("rank 99 must not show a star");
            progress.xp = threshold;
            if (progress.GetRank() != 100 || !progress.HasStar()) throw new Exception("rank 100 must show a star");
            progress.xp = XpUtility.LevelToXp(100);
            if (progress.GetRank() != 101 || !progress.HasStar()) throw new Exception("rank 101 must retain its star");
            Debug.Log("CharacterStarRankPortCheck passed: star begins at rank 100 at exact XP threshold");
        }
        finally { XpUtility.xpForLevelsTable = previous; }
    }
}
