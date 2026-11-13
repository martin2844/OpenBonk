using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Utility;
using UnityEngine;

public static class PlayerSpawnBiasPortCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .0001f;
    public static void Run()
    {
        var go = new GameObject("spawn bias test"); var body = go.AddComponent<Rigidbody>();
        var movement = go.AddComponent<PlayerMovement>(); typeof(PlayerMovement).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(movement, body);
        var player = go.AddComponent<MyPlayer>(); player.playerMovement = movement;
        body.velocity = new Vector3(4f, 0f, 0f); player.UpdateSpawnDirectionBias();
        Check(Near(player.GetAverageMovingDirection().x, .04f) && Near(player.GetAverageMovingDirectionTarget().x, .14f), "speed threshold accumulates both original direction vectors");
        for (int i = 0; i < 199; i++) player.UpdateSpawnDirectionBias();
        Check(Near(player.GetAverageMovingDirection().magnitude, 4f) && Near(player.GetAverageMovingDirectionTarget().magnitude, 3f), "direction vectors have separate size limits");
        Check(Near(player.GetSpawnDirectionBias(), .4f), "moving direction produces capped spawn bias");
        var managerObject = new GameObject("spawn manager"); var manager = managerObject.AddComponent<GameManager>(); GameManager.Instance = manager; manager.whatIsGroundAndObjects = 1;
        MyPlayer.Instance = player; PlayerMovement.Instance = movement; var map = ScriptableObject.CreateInstance<MapData>(); map.mapType = EMapType.ProceduralMesh; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
        MapInfo.mapBoundsLower = new Vector3(-500f, -100f, -500f); MapInfo.mapBoundsUpper = new Vector3(500f, 100f, 500f); MyTime.time = 1f;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.position = new Vector3(0f, -1f, 0f); ground.transform.localScale = new Vector3(1000f, 2f, 1000f); Physics.SyncTransforms();
        body.velocity = Vector3.zero; var oldRandom = UnityEngine.Random.state; int unbiased = 0, biased = 0;
        UnityEngine.Random.InitState(718); for (int i = 0; i < 200; i++) { var point = SpawnPositions.GetEnemySpawnPosition(null, 1, false); Check(point != SpawnPositions.INVALID_POS, "unbiased spawn must hit ground"); if (point.x > 0f) unbiased++; }
        UnityEngine.Random.InitState(718); for (int i = 0; i < 200; i++) { var point = SpawnPositions.GetEnemySpawnPosition(null, 1, true); Check(point != SpawnPositions.INVALID_POS, "biased spawn must hit ground"); if (point.x > 0f) biased++; }
        Check(biased > unbiased + 30, "moving direction biases generated enemy positions forward: " + unbiased + " vs " + biased);
        UnityEngine.Random.InitState(420);
        for (int i = 0; i < 30; i++)
        {
            Vector3 point = SpawnPositions.GetPositionAroundPlayer(8f);
            Check(point != SpawnPositions.INVALID_POS && Mathf.Abs(new Vector2(point.x, point.z).magnitude - 8f) < .01f && Mathf.Abs(point.y) < .01f, "around-player spawn respects limited radius and floor");
        }
        MapInfo.mapBoundsLower = new Vector3(-100f, -100f, -100f); MapInfo.mapBoundsUpper = new Vector3(100f, 100f, 100f);
        body.position = new Vector3(95f, 0f, 0f); player.transform.position = body.position; MyTime.time = 2f; Physics.SyncTransforms();
        UnityEngine.Random.InitState(881); int atBoundary = 0;
        for (int i = 0; i < 60; i++)
        {
            Vector3 point = SpawnPositions.GetEnemySpawnPosition(null, 1, true);
            Check(point != SpawnPositions.INVALID_POS && point.x >= -98.001f && point.x <= 98.001f && point.z >= -98.001f && point.z <= 98.001f, "enemy spawn stays within bounds near map edge");
            if (Mathf.Abs(point.x - 98f) < .01f) atBoundary++;
        }
        Check(atBoundary > 0, "outward samples clamp to the upper map boundary");
        ground.SetActive(false); Physics.SyncTransforms();
        Check(SpawnPositions.GetPositionAroundPlayer(8f) == SpawnPositions.INVALID_POS, "around-player spawn returns invalid position when no surface is available");
        ground.SetActive(true); Physics.SyncTransforms();
        UnityEngine.Random.state = oldRandom; MyPlayer.Instance = null; PlayerMovement.Instance = null; GameManager.Instance = null; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { null });
        UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(managerObject); UnityEngine.Object.DestroyImmediate(map);
        body.velocity = Vector3.zero; player.UpdateSpawnDirectionBias();
        Check(Near(player.GetAverageMovingDirection().x, 3.92f) && Near(player.GetAverageMovingDirectionTarget().x, 2.82f), "stopping decays vectors at original rates");
        for (int i = 0; i < 1000; i++) player.UpdateSpawnDirectionBias();
        Check(player.GetSpawnDirectionBias() == 0f && player.GetAverageMovingDirection().magnitude < .001f, "stationary direction eventually loses spawn bias");
        body.velocity = new Vector3(3f, 4f, 0f); player.UpdateSpawnDirectionBias();
        Check(Near(player.GetAverageMovingDirection().x, .024f) && player.GetAverageMovingDirection().y == 0f, "speed includes vertical velocity but direction is horizontal");
        UnityEngine.Object.DestroyImmediate(go); Debug.Log("PlayerSpawnBiasPortCheck passed: speed threshold, direction accumulation, caps, decay and vertical movement");
    }
}
