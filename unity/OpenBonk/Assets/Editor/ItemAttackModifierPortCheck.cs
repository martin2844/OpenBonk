using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;

public static class ItemAttackModifierPortCheck
{
    static void Equal(float actual, float expected, string name)
    {
        if (Mathf.Abs(actual - expected) > 0.0001f)
            throw new Exception(name + ": expected " + expected + ", got " + actual);
    }

    public static void Run()
    {
        var modifier = new ItemAttackModifier();
        Equal(modifier.multiplicativeValues, 1f, "initial multiplicative stat");
        Equal(modifier.damageMultiplier, 1f, "initial damage multiplier");
        modifier.AddMultiplier(2f);
        modifier.AddMultiplier(1.5f);
        var damage = new DamageContainer(0f, "test") { damage = 7f };
        modifier.Apply(damage);
        Equal(damage.damage, 21f, "combined damage multiplier");
        modifier.flatValues = 3f;
        modifier.additiveValues = 4f;
        modifier.multiplicativeValues = 5f;
        modifier.Recycle();
        Equal(modifier.flatValues, 0f, "recycled flat stat");
        Equal(modifier.additiveValues, 0f, "recycled additive stat");
        Equal(modifier.multiplicativeValues, 1f, "recycled multiplicative stat");
        Equal(modifier.damageMultiplier, 3f, "original recycle preserves damage multiplier");
        Debug.Log("ItemAttackModifierPortCheck passed");
    }
}
