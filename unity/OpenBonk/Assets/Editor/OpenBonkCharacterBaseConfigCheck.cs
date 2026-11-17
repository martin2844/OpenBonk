#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class OpenBonkCharacterBaseConfigCheck
{
    [Serializable] private sealed class JsonCharacter
    {
        public OpenBonkCharacterBaseConfig.StatSpec[] baseStats;
        public OpenBonkCharacterBaseConfig.ColliderSpec collider;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(string json)
    {
        try {
            var parsed = JsonConvert.DeserializeObject<JsonCharacter>(json);
            OpenBonkCharacterBaseConfig.Validate(parsed.baseStats, parsed.collider);
        } catch (InvalidDataException) { return; }
        catch (JsonException) { return; }
        throw new Exception("Invalid configuration accepted: " + json);
    }

    public static void Run()
    {
        var fallback = ScriptableObject.CreateInstance<CharacterData>();
        var target = ScriptableObject.CreateInstance<CharacterData>();
        try
        {
            fallback.colliderHeight = 2; fallback.colliderWidth = .5f;
            fallback.statModifiers = new List<StatModifier> { new StatModifier { stat = EStat.Luck, modifyType = EStatModifyType.Flat, modification = .15f } };
            var absent = JsonConvert.DeserializeObject<JsonCharacter>("{}");
            OpenBonkCharacterBaseConfig.Apply(target, fallback, OpenBonkCharacterBaseConfig.Validate(absent.baseStats, absent.collider));
            Require(target.colliderHeight == 2 && target.colliderWidth == .5f && target.statModifiers.Count == 1 && !ReferenceEquals(target.statModifiers, fallback.statModifiers), "absent fields preserve defaults");
            var configured = JsonConvert.DeserializeObject<JsonCharacter>("{\"baseStats\":[{\"stat\":\"MaxHealth\",\"mode\":\"Multiplication\",\"amount\":0.65},{\"stat\":\"Luck\",\"mode\":\"Flat\",\"amount\":4},{\"stat\":\"Luck\",\"mode\":\"Addition\",\"amount\":0.1}],\"collider\":{\"height\":3.75,\"width\":0.7}}");
            OpenBonkCharacterBaseConfig.Apply(target, fallback, OpenBonkCharacterBaseConfig.Validate(configured.baseStats, configured.collider));
            Require(target.statModifiers.Count == 3 && target.statModifiers[0].stat == EStat.MaxHealth && target.statModifiers[0].modifyType == EStatModifyType.Multiplication && Math.Abs(target.statModifiers[0].modification - .65f) < .0001f && target.colliderHeight == 3.75f && target.colliderWidth == .7f, "custom stats and collider applied");
            var empty = JsonConvert.DeserializeObject<JsonCharacter>("{\"baseStats\":[],\"collider\":null}");
            OpenBonkCharacterBaseConfig.Apply(target, fallback, OpenBonkCharacterBaseConfig.Validate(empty.baseStats, empty.collider));
            Require(target.statModifiers.Count == 0 && target.colliderHeight == 2 && target.colliderWidth == .5f, "empty stats replace defaults and null collider falls back");
            foreach (string bad in new[] {
                "{\"baseStats\":\"bad\"}", "{\"baseStats\":[null]}",
                "{\"baseStats\":[{\"stat\":\"0\",\"mode\":\"Flat\",\"amount\":1}]}",
                "{\"baseStats\":[{\"stat\":\"Unused0\",\"mode\":\"Flat\",\"amount\":1}]}",
                "{\"baseStats\":[{\"stat\":\"Luck\",\"mode\":\"0\",\"amount\":1}]}",
                "{\"baseStats\":[{\"stat\":\"Luck\",\"mode\":\"Flat\"}]}",
                "{\"baseStats\":[{\"stat\":\"Luck\",\"mode\":\"Flat\",\"amount\":10001}]}",
                "{\"baseStats\":[{\"stat\":\"Luck\",\"mode\":\"Flat\",\"amount\":\"NaN\"}]}",
                "{\"baseStats\":[{\"stat\":\"Luck\",\"mode\":\"Flat\",\"amount\":\"Infinity\"}]}",
                "{\"baseStats\":[{\"stat\":\"Luck\",\"mode\":\"Multiplication\",\"amount\":-0.1}]}",
                "{\"baseStats\":[{\"stat\":\"Luck\",\"mode\":\"Flat\",\"amount\":1},{\"stat\":\"Luck\",\"mode\":\"Flat\",\"amount\":2}]}",
                "{\"collider\":{\"height\":1,\"width\":1}}", "{\"collider\":{\"height\":2}}", "{\"collider\":\"bad\"}"
            }) Reject(bad);
            Debug.Log("OpenBonkCharacterBaseConfigCheck PASS");
        }
        finally { UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(fallback); }
    }
}
#endif
