using System;
using Assets.Scripts.Game.Spawning;
using UnityEngine;

public static class EnemyWavePortCheck
{
    static void Require(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Run()
    {
        var wave=new EnemyWave();
        Require(wave.waveTime==60 && wave.minNumEnemies==50 && wave.spawnInterval==5f,"original constructor defaults");
        Require(Mathf.Abs(wave.EnemiesPerSecond()-10f)<.0001f,"default spawn rate");
        wave.waveTime=17; wave.minNumEnemies=7; wave.spawnInterval=2.5f;
        Require(Mathf.Abs(wave.EnemiesPerSecond()-2.8f)<.0001f,"fractional spawn rate");
        Require(wave.ToString()==string.Format("{0} | waveTime: 17 | minNumEnemies: 7",wave.eWaveType),"original diagnostic text");
        wave.spawnInterval=0f; Require(float.IsPositiveInfinity(wave.EnemiesPerSecond()),"nonzero count and zero interval");
        wave.minNumEnemies=0; Require(float.IsNaN(wave.EnemiesPerSecond()),"zero count and zero interval");
        Debug.Log("EnemyWavePortCheck passed: original defaults, rate, text and zero-interval behavior");
    }
}
