#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.Player.Movement;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Utility;
using UnityEngine;

// Read-only terrain sampling; a connected grid is not proof of player reachability.
internal sealed class AndroidBananaTerrainRouteProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_banana_terrain_route_probe", false)) return;
        var go = new GameObject("banana terrain route probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidBananaTerrainRouteProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        InteractableBananarang banana = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted)
                banana = FindObjectOfType<InteractableBananarang>();
            if (banana != null) break;
            yield return null;
        }
        if (banana == null || MyPlayer.Instance == null) { Debug.Log("Banana terrain route INCONCLUSIVE: player or banana unavailable"); yield break; }
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool walk = intent.Call<bool>("getBooleanExtra", "megabonk_banana_walk_route_probe", false);
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        bool previousWaves = manager != null && manager.enabledWaves;
        var invincibleField = typeof(MyPlayer).GetField("isInvincible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var invincibleUntil = typeof(MyPlayer).GetField("damageCooldownOverAtTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        bool previousInvincible = (bool)invincibleField.GetValue(player);
        float previousUntil = (float)invincibleUntil.GetValue(player);
        if (walk) { if (manager != null) manager.enabledWaves = false; invincibleField.SetValue(player, true); invincibleUntil.SetValue(player, float.MaxValue); }
        Vector3 start = MyPlayer.Instance.transform.position, end = banana.transform.position;
        const float step = 4f, margin = 100f, minNormal = .7f;
        float x0 = Mathf.Min(start.x, end.x) - margin, z0 = Mathf.Min(start.z, end.z) - margin;
        int width = Mathf.CeilToInt((Mathf.Abs(start.x-end.x) + margin*2f)/step)+1;
        int height = Mathf.CeilToInt((Mathf.Abs(start.z-end.z) + margin*2f)/step)+1;
        if (width * height > 40000) { Debug.Log("Banana terrain route INCONCLUSIVE: grid exceeds safety limit"); if (walk) { if (manager != null) manager.enabledWaves = previousWaves; invincibleField.SetValue(player, previousInvincible); invincibleUntil.SetValue(player, previousUntil); } yield break; }
        var elevations = new float[width * height];
        var cellNodes = new List<int>[width * height];
        var nodeHeights = new List<float>();
        var nodeCells = new List<int>();
        int valid = 0, mask = GameManager.Instance.whatIsGround;
        float rayTop = Mathf.Max(start.y, end.y) + 180f;
        for (int z=0; z<height; z++)
        {
            for (int x=0; x<width; x++)
            {
                int index = z*width+x; elevations[index] = float.NaN;
                var hits = Physics.RaycastAll(new Vector3(x0+x*step, rayTop, z0+z*step), Vector3.down, 360f, mask, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider == null || hit.collider.transform.IsChildOf(banana.transform) || hit.normal.y < minNormal) continue;
                    if (float.IsNaN(elevations[index])) { elevations[index] = hit.point.y; valid++; }
                    if (cellNodes[index] == null) cellNodes[index] = new List<int>();
                    bool duplicate = false;
                    foreach (int previous in cellNodes[index]) if (Mathf.Abs(nodeHeights[previous] - hit.point.y) < .1f) { duplicate = true; break; }
                    if (duplicate) continue;
                    cellNodes[index].Add(nodeHeights.Count); nodeHeights.Add(hit.point.y); nodeCells.Add(index);
                }
            }
            if ((z & 3) == 3) yield return null;
        }
        int Nearest(Vector3 target)
        {
            int best = -1; float bestDistance = float.PositiveInfinity;
            for (int z=0; z<height; z++) for (int x=0; x<width; x++)
            {
                int i=z*width+x; if (float.IsNaN(elevations[i])) continue;
                float dx=x0+x*step-target.x, dz=z0+z*step-target.z;
                float dy=elevations[i]-target.y;
                if (Mathf.Abs(dy)>5f) continue;
                float distance=dx*dx+dz*dz;
                if (distance < bestDistance) { best=i; bestDistance=distance; }
            }
            return bestDistance <= step*step*2f ? best : -1;
        }
        int from=Nearest(start), to=Nearest(end);
        var directions = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        foreach (float maxRise in new[] { 1.5f, 3f, 6f, 12f, 25f })
        {
            int visited=0; bool connected=false;
            var queue=new Queue<int>(); var seen=new bool[elevations.Length]; var parents=new int[elevations.Length];
            if (from>=0 && to>=0) { queue.Enqueue(from); seen[from]=true; parents[from]=-1; }
            while (queue.Count>0)
            {
                int current=queue.Dequeue(); visited++; if (current==to) { connected=true; break; }
                int cx=current%width, cz=current/width;
                foreach (var delta in directions)
                {
                    int nx=cx+delta.x, nz=cz+delta.y; if (nx<0 || nx>=width || nz<0 || nz>=height) continue;
                    int next=nz*width+nx; if (seen[next] || float.IsNaN(elevations[next]) || Mathf.Abs(elevations[next]-elevations[current])>maxRise) continue;
                    seen[next]=true; parents[next]=current; queue.Enqueue(next);
                }
            }
            int pathLength=0;
            if (connected)
            {
                var path=new List<Vector3>();
                for (int cell=to; cell>=0; cell=parents[cell]) path.Add(new Vector3(x0+(cell%width)*step,elevations[cell],z0+(cell/width)*step));
                path.Reverse(); pathLength=path.Count;
                var lines=new List<string>();
                foreach (var point in path) lines.Add($"{point.x:R},{point.y:R},{point.z:R}");
                System.IO.File.WriteAllLines(Application.persistentDataPath + "/banana-terrain-route-" + maxRise.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture) + ".csv", lines);
            }
            Debug.Log($"Banana terrain route {(connected ? "CONNECTED" : "INCONCLUSIVE")}: start={start} banana={end} grid={width}x{height} step={step} margin={margin} valid={valid} visited={visited} startCell={from} goalCell={to} rise={maxRise} minNormal={minNormal} pathCells={pathLength}; highest sampled ground only, obstacles and player motion untested");
        }
        int NearestNode(Vector3 target)
        {
            int best = -1; float bestScore = float.PositiveInfinity;
            for (int node=0; node<nodeHeights.Count; node++)
            {
                int cell=nodeCells[node]; float dx=x0+(cell%width)*step-target.x, dz=z0+(cell/width)*step-target.z;
                float dy=nodeHeights[node]-target.y, horizontal=dx*dx+dz*dz;
                if (horizontal > step*step*2f || Mathf.Abs(dy)>5f) continue;
                float score=horizontal+dy*dy;
                if (score<bestScore) { best=node; bestScore=score; }
            }
            return best;
        }
        int startNode=NearestNode(start), goalNode=NearestNode(end);
        List<Vector3> walkPath=null;
        foreach (float maxRise in new[] { 1.5f, 3f, 6f, 12f, 25f })
        {
            var queue=new Queue<int>(); var seen=new bool[nodeHeights.Count]; var parents=new int[nodeHeights.Count];
            int visited=0; bool connected=false;
            if (startNode>=0 && goalNode>=0) { queue.Enqueue(startNode); seen[startNode]=true; parents[startNode]=-1; }
            while (queue.Count>0)
            {
                int current=queue.Dequeue(); visited++; if (current==goalNode) { connected=true; break; }
                int cell=nodeCells[current], cx=cell%width, cz=cell/width;
                foreach (var delta in directions)
                {
                    int nx=cx+delta.x, nz=cz+delta.y; if (nx<0 || nx>=width || nz<0 || nz>=height) continue;
                    var candidates=cellNodes[nz*width+nx]; if (candidates==null) continue;
                    foreach (int next in candidates)
                    {
                        if (seen[next] || Mathf.Abs(nodeHeights[next]-nodeHeights[current])>maxRise) continue;
                        seen[next]=true; parents[next]=current; queue.Enqueue(next);
                    }
                }
            }
            int pathLength=0;
            if (connected)
            {
                var path=new List<Vector3>();
                for (int node=goalNode; node>=0; node=parents[node]) { int cell=nodeCells[node]; path.Add(new Vector3(x0+(cell%width)*step,nodeHeights[node],z0+(cell/width)*step)); }
                path.Reverse(); pathLength=path.Count; var lines=new List<string>();
                if (walkPath == null && maxRise <= 3f) walkPath = path;
                foreach (var point in path) lines.Add($"{point.x:R},{point.y:R},{point.z:R}");
                System.IO.File.WriteAllLines(Application.persistentDataPath + "/banana-multisurface-route-" + maxRise.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture) + ".csv", lines);
            }
            Debug.Log($"Banana multisurface route {(connected ? "CONNECTED" : "INCONCLUSIVE")}: nodes={nodeHeights.Count} visited={visited} startNode={startNode} goalNode={goalNode} rise={maxRise} pathCells={pathLength}; sampled surfaces only, clearance, obstacles and actual player motion untested");
        }
        if (walk)
        {
            if (walkPath == null) Debug.Log("Banana physical route INCONCLUSIVE: no sampled route at 3-unit rise");
            else yield return WalkRoute(player, banana, walkPath);
            if (manager != null) manager.enabledWaves = previousWaves;
            if (player != null) { invincibleField.SetValue(player, previousInvincible); invincibleUntil.SetValue(player, previousUntil); }
        }
    }

    static IEnumerator WalkRoute(MyPlayer player, InteractableBananarang banana, List<Vector3> path)
    {
        if (player.playerInput == null || player.playerMovement == null) { Debug.Log("Banana physical route INCONCLUSIVE: player movement unavailable"); yield break; }
        var input = player.playerInput; bool oldEnabled = input.enabled; input.enabled = false;
        var movement = player.playerMovement; var body = movement.GetRb();
        int waypoint = 0; float deadline=Time.realtimeSinceStartup+180f, lastProgress=Time.realtimeSinceStartup, nextJump=0f;
        Debug.Log($"Banana physical route START: waypoints={path.Count} player={body.position} banana={banana.transform.position}");
        while (player != null && banana != null && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForFixedUpdate();
            if (MyTime.paused || !GameManager.Instance.IsPlaying()) continue;
            while (waypoint < path.Count && Vector2.Distance(new Vector2(body.position.x,body.position.z),new Vector2(path[waypoint].x,path[waypoint].z)) < 1.75f)
            { waypoint++; lastProgress=Time.realtimeSinceStartup; }
            float remaining=Vector3.Distance(body.position,banana.transform.position);
            if (remaining < 3f || waypoint >= path.Count) break;
            if (Time.realtimeSinceStartup-lastProgress > 10f) { Debug.Log($"Banana physical route STALLED: waypoint={waypoint}/{path.Count} position={body.position} next={path[waypoint]} remaining={remaining:F1}"); break; }
            Vector3 delta=path[waypoint]-body.position; delta.y=0f; delta.Normalize();
            bool jump=movement.grounded && Time.realtimeSinceStartup>=nextJump && (path[waypoint].y-body.position.y>.4f || Time.realtimeSinceStartup-lastProgress>2f);
            if (jump) nextJump=Time.realtimeSinceStartup+1f;
            movement.SetBetterInput(new InputState(delta.x,delta.z,0f,0f,jump,false,jump,false));
            movement.MovementTick();
        }
        if (player != null && banana != null)
        {
            float remaining=Vector3.Distance(body.position,banana.transform.position);
            Debug.Log($"Banana physical route {(remaining < 3f ? "REACHED" : "INCONCLUSIVE")}: waypoint={waypoint}/{path.Count} position={body.position} banana={banana.transform.position} distance={remaining:F1}; scripted inputs, protected player and disabled waves; interaction untested");
            movement.SetBetterInput(default(InputState));
            if (remaining < 3f)
            {
                var detector = FindObjectOfType<DetectInteractables>();
                var currentField = typeof(DetectInteractables).GetField("currentInteractable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var inventory = player.inventory.weaponInventory;
                bool initiallyAbsent = inventory.GetWeaponLevel(EWeapon.Bananarang) == 0;
                bool initiallyLocked = !MyAchievements.IsAchievementDone("a_bananarang");
                float detectDeadline=Time.realtimeSinceStartup+5f; bool detected=false;
                while (detector != null && banana != null && Time.realtimeSinceStartup < detectDeadline)
                {
                    yield return new WaitForFixedUpdate();
                    movement.SetBetterInput(default(InputState)); movement.MovementTick();
                    if (currentField.GetValue(detector) != banana) continue;
                    detected=true; detector.TryInteract(); break;
                }
                bool unlocked=MyAchievements.IsAchievementDone("a_bananarang");
                int level=inventory.GetWeaponLevel(EWeapon.Bananarang);
                Debug.Log($"Banana physical interaction {(initiallyAbsent && initiallyLocked && detected && unlocked && level==1 ? "PASS" : "INCONCLUSIVE")}: actualDetectorSelected={detected} initiallyAbsent={initiallyAbsent} initiallyLocked={initiallyLocked} achievement={unlocked} weaponLevel={level}; TryInteract invoked by probe, no physical button");
            }
        }
        if (input != null) input.enabled=oldEnabled;
    }
}
#endif
