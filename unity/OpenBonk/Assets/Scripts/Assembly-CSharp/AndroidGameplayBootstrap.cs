using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Game.MapGeneration;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;

/// <summary>
/// Temporary gameplay reconstruction for the Android player. The exported
/// IL2CPP project has no managed bodies for its map and player startup code, so
/// this supplies a small playable arena until those systems are recovered.
/// </summary>
public sealed class AndroidGameplayBootstrap : MonoBehaviour
{
    private static AndroidGameplayBootstrap activeBootstrap;
    public static bool IsRunning => activeBootstrap != null;
    private readonly List<Enemy> enemies = new List<Enemy>();
    private static GameObject selectedTreePrefab;
    private static GameObject selectedEnemyPrefab;
    private static GameObject selectedGroundTilePrefab;
    private static StageData selectedStageData;
    private Transform player;
    private CharacterController controller;
    private Camera gameplayCamera;
    private Material grassMaterial;
    private Material playerMaterial;
    private Material enemyMaterial;
    private Vector2 moveTouchStart;
    private int moveTouchId = -1;
    private Vector2 moveInput;
    private float nextShotAt;
    private float nextSpawnAt;
    private float health = 100f;
    private int level = 1;
    private int kills;
    private int xp;
    private bool dead;
    private int mapColumns = 40;
    private int mapRows = 20;
    private float tileSize = 2f;
    private MeshCollider generatedGround;
    private bool restoredTerrain;

    private void Awake() { activeBootstrap = this; }

    private void OnDestroy() { if (activeBootstrap == this) activeBootstrap = null; }

    public static void Begin(GameObject treePrefab, GameObject enemyPrefab, GameObject groundTilePrefab, StageData stageData)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (FindObjectOfType<AndroidGameplayBootstrap>() != null) return;
        selectedTreePrefab = treePrefab;
        selectedEnemyPrefab = enemyPrefab;
        selectedGroundTilePrefab = groundTilePrefab;
        selectedStageData = stageData;
        var root = new GameObject("Android Gameplay Reconstruction");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidGameplayBootstrap>();
#endif
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = selectedStageData != null ? selectedStageData.ambienceColor : new Color(0.35f, 0.38f, 0.32f);
        var sun = new GameObject("Android Arena Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = selectedStageData != null ? selectedStageData.lightIntensity : 1.25f;
        if (selectedStageData != null) sun.color = selectedStageData.lightColor;
        sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        if (selectedStageData != null)
        {
            if (selectedStageData.skybox != null) RenderSettings.skybox = selectedStageData.skybox;
            RenderSettings.fog = selectedStageData.fogIntensity > 0f;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = selectedStageData.fogColor;
            RenderSettings.fogDensity = selectedStageData.fogIntensity;
            if (selectedStageData.mapParameters != null)
            {
                mapColumns = Mathf.Clamp(selectedStageData.mapParameters.scaledTileWidth, 12, 64);
                mapRows = Mathf.Clamp(selectedStageData.mapParameters.scaledTileHeight, 8, 40);
                tileSize = Mathf.Max(1f, selectedStageData.mapParameters.tileWidth);
            }
        }
        DisableExportedPostProcessing();
        DisableSceneCanvases();
        grassMaterial = MakeMaterial(new Color(0.16f, 0.32f, 0.13f));
        playerMaterial = MakeMaterial(new Color(0.18f, 0.82f, 0.92f));
        enemyMaterial = MakeMaterial(new Color(0.82f, 0.19f, 0.16f));
        BuildPlayer();
        ConfigureCamera();
        StartCoroutine(InitializeArena());
    }

    private IEnumerator InitializeArena()
    {
        yield return null;
        MapData map = Assets.Scripts.Managers.MapController.currentMap;
        ProceduralTileGeneration generator = FindObjectOfType<ProceduralTileGeneration>();
        if (map != null && map.mapType == EMapType.Tiles && selectedStageData != null &&
            selectedStageData.mapParameters != null && selectedStageData.mapEdgeFillType == MapEdgeFillType.None &&
            selectedStageData.mapParameters.heightGenerationStrategy == EHeightGenerationStrategy.Me &&
            generator != null && generator.flatTile != null && generator.slopeTile != null && generator.wallFlat != null)
        {
            MapParameters parameters = selectedStageData.mapParameters;
            Vector3 spawn = generator.Generate(out Vector3 spawnDirection, selectedStageData, parameters);
            generatedGround = generator.collider.GetComponent<MeshCollider>();
            restoredTerrain = true;
            mapColumns = parameters.size;
            mapRows = parameters.size;
            tileSize = parameters.scaledTileWidth;
            controller.enabled = false;
            player.position = spawn + Vector3.up * 1.1f;
            if (spawnDirection != Vector3.zero) player.forward = spawnDirection;
            controller.enabled = true;
            gameplayCamera.transform.position = player.position + new Vector3(0f, 22f, -22f);
            Debug.Log("Android restored tile map: stage=" + selectedStageData.name + ", seed=" + generator.debugSeed +
                ", tiles=" + generator.tiles.Count + ", fills=" + generator.fillTiles.Count + ", spawn=" + spawn);
        }
        yield return BuildArena();
        for (int i = 0; i < 8; i++) SpawnEnemy();
        nextSpawnAt = Time.time + 4f;
    }

    private static Material MakeMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        var material = new Material(shader);
        material.color = color;
        return material;
    }

    private IEnumerator BuildArena()
    {
        float worldWidth = mapColumns * tileSize;
        float worldDepth = mapRows * tileSize;
        if (restoredTerrain)
        {
            // The recovered generator already created the ground and its collider.
        }
        else if (selectedGroundTilePrefab != null)
        {
            Material[] topMaterials = selectedStageData != null ? selectedStageData.flatMaterials : null;
            int generatedTiles = 0;
            for (int z = 0; z < mapRows; z++)
            for (int x = 0; x < mapColumns; x++)
            {
                Vector3 tilePosition = new Vector3((x - (mapColumns - 1) * 0.5f) * tileSize, 0f,
                    (z - (mapRows - 1) * 0.5f) * tileSize);
                GameObject tile = Instantiate(selectedGroundTilePrefab, tilePosition, Quaternion.identity);
                tile.name = "Forest Ground Tile";
                Renderer tileRenderer = tile.GetComponent<Renderer>();
                if (tileRenderer != null && topMaterials != null && topMaterials.Length > 0)
                {
                    Material material = topMaterials[Random.Range(0, topMaterials.Length)];
                    if (material != null) tileRenderer.sharedMaterial = material;
                }
                if (++generatedTiles % 24 == 0) yield return null;
            }
        }
        else
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Forest Arena Floor";
            floor.transform.position = new Vector3(0f, -0.65f, 0f);
            floor.transform.localScale = new Vector3(worldWidth, 1f, worldDepth);
            floor.GetComponent<Renderer>().material = grassMaterial;
        }

        if (selectedStageData != null && selectedStageData.randomMapObjects != null)
        {
            int generatedObjects = 0;
            foreach (RandomMapObject group in selectedStageData.randomMapObjects)
            {
                if (group == null || group.prefabs == null || group.prefabs.Length == 0) continue;
                int count = Mathf.Clamp(Mathf.RoundToInt(group.amount * 0.15f), 1, 48);
                for (int i = 0; i < count; i++)
                {
                    GameObject prefab = group.prefabs[Random.Range(0, group.prefabs.Length)];
                    if (prefab == null) continue;
                    Vector3 position = new Vector3(Random.Range(-worldWidth * 0.46f, worldWidth * 0.46f), group.upOffset,
                        Random.Range(-worldDepth * 0.46f, worldDepth * 0.46f));
                    if (new Vector2(position.x - player.position.x, position.z - player.position.z).sqrMagnitude < 625f) continue;
                    float groundY = 0f;
                    if (restoredTerrain && !TryGroundHeight(position, out groundY)) continue;
                    if (restoredTerrain) position.y += groundY;
                    GameObject scenery = Instantiate(prefab, position, prefab.transform.rotation);
                    scenery.name = "Forest Map Object";
                    scenery.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * scenery.transform.rotation;
                    scenery.transform.localScale *= Random.Range(group.scaleMin, Mathf.Max(group.scaleMin, group.scaleMax));
                    if (++generatedObjects % 8 == 0) yield return null;
                }
            }
        }
        else for (int i = 0; i < 30; i++)
        {
            float angle = i * Mathf.PI * 2f / 30f + Random.Range(-0.08f, 0.08f);
            float radius = Random.Range(worldDepth * 0.32f, worldDepth * 0.48f);
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            if (selectedTreePrefab != null)
            {
                var tree = Instantiate(selectedTreePrefab, position, selectedTreePrefab.transform.rotation);
                tree.name = "Forest Tree";
                tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * tree.transform.rotation;
                tree.transform.localScale *= Random.Range(0.8f, 1.1f);
                continue;
            }
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Forest Tree Trunk";
            trunk.transform.position = position + Vector3.up * 3.2f;
            trunk.transform.localScale = new Vector3(1.5f, 3.2f, 1.5f);
            trunk.GetComponent<Renderer>().material = grassMaterial;
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Forest Canopy";
            crown.transform.position = position + Vector3.up * 7.3f;
            crown.transform.localScale = Vector3.one * Random.Range(8f, 12f);
            crown.GetComponent<Renderer>().material = grassMaterial;
            Destroy(crown.GetComponent<Collider>());
        }
    }

    private void BuildPlayer()
    {
        var body = new GameObject("Player");
        body.layer = LayerMask.NameToLayer("Player");
        body.transform.position = new Vector3(0f, 0.95f, 0f);
        controller = body.AddComponent<CharacterController>();
        controller.height = 2.2f;
        controller.radius = 0.57f;
        player = body.transform;

        CharacterData selectedData = null;
        CharacterData[] availableCharacters = Resources.LoadAll<CharacterData>("AndroidCharacters");
        for (int i = 0; i < availableCharacters.Length; i++)
        {
            if (availableCharacters[i] != null && availableCharacters[i].eCharacter == CharacterMenu.selectedCharacter)
            {
                selectedData = availableCharacters[i];
                break;
            }
        }
        GameObject characterPrefab = selectedData != null ? selectedData.prefab : Resources.Load<GameObject>("AndroidMegachad");
        Debug.Log("Android gameplay character: selected=" + CharacterMenu.selectedCharacter + ", prefab=" + (characterPrefab != null ? characterPrefab.name : "none"));
        if (characterPrefab != null)
        {
            GameObject character = Instantiate(characterPrefab, player, false);
            character.name = "Megachad Character";
            character.transform.localPosition = Vector3.zero;
            character.transform.localRotation = Quaternion.identity;
            character.transform.localScale = Vector3.one;
        }
        else
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "Player Fallback Visual";
            fallback.transform.SetParent(player, false);
            fallback.transform.localScale = new Vector3(1.15f, 1.1f, 1.15f);
            fallback.GetComponent<Renderer>().material = playerMaterial;
            Destroy(fallback.GetComponent<Collider>());
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Player Head";
            head.transform.SetParent(player, false);
            head.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            head.transform.localScale = Vector3.one * 0.64f;
            head.GetComponent<Renderer>().material = MakeMaterial(new Color(0.94f, 0.73f, 0.48f));
            Destroy(head.GetComponent<Collider>());
        }
    }

    private void ConfigureCamera()
    {
        gameplayCamera = Camera.main;
        if (gameplayCamera == null)
        {
            var cameraObject = new GameObject("Android Gameplay Camera");
            gameplayCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }
        foreach (var camera in FindObjectsOfType<Camera>())
            if (camera != gameplayCamera) camera.enabled = false;
        gameplayCamera.enabled = true;
        gameplayCamera.cullingMask = ~0;
        gameplayCamera.useOcclusionCulling = false;
        gameplayCamera.targetTexture = null;
        gameplayCamera.rect = new Rect(0f, 0f, 1f, 1f);
        gameplayCamera.clearFlags = selectedStageData != null && selectedStageData.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        gameplayCamera.backgroundColor = new Color(0.28f, 0.48f, 0.63f);
        gameplayCamera.fieldOfView = 55f;
        gameplayCamera.nearClipPlane = 0.1f;
        gameplayCamera.farClipPlane = selectedStageData != null && selectedStageData.farClipPlane > 0f ? selectedStageData.farClipPlane : 250f;
        gameplayCamera.transform.position = player.position + new Vector3(0f, 22f, -22f);
        gameplayCamera.transform.rotation = Quaternion.LookRotation(player.position + Vector3.up - gameplayCamera.transform.position);
    }

    private static void DisableSceneCanvases()
    {
        foreach (Canvas canvas in FindObjectsOfType<Canvas>(true))
            canvas.enabled = false;
    }

    private static void DisableExportedPostProcessing()
    {
        foreach (Behaviour behaviour in FindObjectsOfType<Behaviour>(true))
        {
            string typeName = behaviour.GetType().Name;
            if (typeName == "PostProcessLayer" || typeName == "PostProcessVolume")
                behaviour.enabled = false;
        }
    }

    private void Update()
    {
        ReadMovement();
        if (!dead)
        {
            Vector3 forward = gameplayCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = gameplayCamera.transform.right;
            right.y = 0f;
            right.Normalize();
            Vector3 motion = (right * moveInput.x + forward * moveInput.y);
            if (motion.sqrMagnitude > 1f) motion.Normalize();
            controller.Move((motion * 8f + Vector3.down * 4f) * Time.deltaTime);
            Vector3 groundedPosition = player.position;
            if (!restoredTerrain && groundedPosition.y < 1f) groundedPosition.y = 1f;
            groundedPosition.x = Mathf.Clamp(groundedPosition.x, -mapColumns * tileSize * 0.48f, mapColumns * tileSize * 0.48f);
            groundedPosition.z = Mathf.Clamp(groundedPosition.z, -mapRows * tileSize * 0.48f, mapRows * tileSize * 0.48f);
            if ((groundedPosition - player.position).sqrMagnitude > 0.0001f)
                player.position = groundedPosition;
            if (motion.sqrMagnitude > 0.02f) player.forward = motion;
            UpdateCamera();
            UpdateEnemies();
            AutoAttack();
            if (Time.time >= nextSpawnAt && enemies.Count < 18)
            {
                SpawnEnemy();
                nextSpawnAt = Time.time + Mathf.Max(1.3f, 4.5f - level * 0.12f);
            }
        }
    }

    private void ReadMovement()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.touchCount == 0) moveTouchId = -1;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && moveTouchId < 0 && touch.position.x < Screen.width * 0.55f)
            {
                moveTouchId = touch.fingerId;
                moveTouchStart = touch.position;
            }
            if (touch.fingerId == moveTouchId)
            {
                Vector2 delta = (touch.position - moveTouchStart) / Mathf.Max(1f, Screen.height * 0.13f);
                moveInput = Vector2.ClampMagnitude(delta, 1f);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) moveTouchId = -1;
            }
        }
    }

    private void UpdateCamera()
    {
        Vector3 target = player.position + new Vector3(0f, 22f, -22f);
        gameplayCamera.transform.position = Vector3.Lerp(gameplayCamera.transform.position, target, 8f * Time.deltaTime);
        gameplayCamera.transform.rotation = Quaternion.Slerp(gameplayCamera.transform.rotation,
            Quaternion.LookRotation(player.position + Vector3.up - gameplayCamera.transform.position), 8f * Time.deltaTime);
    }

    private void SpawnEnemy()
    {
        float angle = Random.value * Mathf.PI * 2f;
        Vector3 position = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Random.Range(18f, 29f);
        if (restoredTerrain && TryGroundHeight(position, out float groundY)) position.y = groundY;
        GameObject body;
        if (selectedEnemyPrefab != null)
        {
            body = Instantiate(selectedEnemyPrefab, position + Vector3.up, selectedEnemyPrefab.transform.rotation);
            foreach (Behaviour behaviour in body.GetComponentsInChildren<Behaviour>(true))
            {
                string typeName = behaviour.GetType().Name;
                if (typeName == "Enemy" || typeName == "EnemyMovement") behaviour.enabled = false;
            }
            var rigidbody = body.GetComponent<Rigidbody>();
            if (rigidbody != null) { rigidbody.isKinematic = true; rigidbody.useGravity = false; }
        }
        else
        {
            body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.position = position + Vector3.up;
            body.transform.localScale = Vector3.one * 1.6f;
            body.GetComponent<Renderer>().material = enemyMaterial;
        }
        body.name = "Forest Enemy";
        enemies.Add(new Enemy { body = body.transform, hp = 2 + level / 3 });
    }

    private void UpdateEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            if (enemy.body == null) { enemies.RemoveAt(i); continue; }
            Vector3 toward = player.position - enemy.body.position;
            toward.y = 0f;
            if (toward.magnitude > 1.7f)
                enemy.body.position += toward.normalized * (2.1f + level * 0.04f) * Time.deltaTime;
            if (restoredTerrain && TryGroundHeight(enemy.body.position, out float groundY))
            {
                Vector3 grounded = enemy.body.position;
                grounded.y = groundY + 1f;
                enemy.body.position = grounded;
            }
            if (toward.magnitude < 1.6f && Time.time >= enemy.nextHitAt)
            {
                health -= 8f;
                enemy.nextHitAt = Time.time + 0.8f;
                if (health <= 0f) dead = true;
            }
        }
    }

    private bool TryGroundHeight(Vector3 position, out float height)
    {
        if (generatedGround != null && generatedGround.Raycast(
            new Ray(new Vector3(position.x, generatedGround.bounds.max.y + 10f, position.z), Vector3.down),
            out RaycastHit hit, generatedGround.bounds.size.y + 20f))
        {
            height = hit.point.y;
            return true;
        }
        height = 0f;
        return false;
    }

    private void AutoAttack()
    {
        if (Time.time < nextShotAt || enemies.Count == 0) return;
        Enemy target = null;
        float nearest = 22f * 22f;
        foreach (Enemy enemy in enemies)
        {
            if (enemy.body == null) continue;
            float distance = (enemy.body.position - player.position).sqrMagnitude;
            if (distance < nearest) { nearest = distance; target = enemy; }
        }
        if (target == null) return;
        nextShotAt = Time.time + Mathf.Max(0.22f, 0.75f - level * 0.025f);
        var bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bolt.name = "Auto Attack";
        bolt.transform.position = player.position + Vector3.up * 0.6f;
        bolt.transform.localScale = Vector3.one * 0.36f;
        bolt.GetComponent<Renderer>().material = playerMaterial;
        Destroy(bolt.GetComponent<Collider>());
        StartCoroutine(FlyBolt(bolt.transform, target));
    }

    private System.Collections.IEnumerator FlyBolt(Transform bolt, Enemy target)
    {
        float time = 0f;
        while (bolt != null && target != null && target.body != null && time < 1.4f)
        {
            time += Time.deltaTime;
            bolt.position = Vector3.MoveTowards(bolt.position, target.body.position, 32f * Time.deltaTime);
            if ((bolt.position - target.body.position).sqrMagnitude < 1f)
            {
                target.hp--;
                Destroy(bolt.gameObject);
                if (target.hp <= 0)
                {
                    enemies.Remove(target);
                    Destroy(target.body.gameObject);
                    kills++;
                    xp += 10;
                    if (xp >= level * 50) { xp = 0; level++; health = Mathf.Min(100f, health + 15f); }
                }
                yield break;
            }
            yield return null;
        }
        if (bolt != null) Destroy(bolt.gameObject);
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUI.skin.label.fontSize = Mathf.Max(20, Screen.height / 35);
        GUI.Label(new Rect(24, 110, 500, 48), "HEALTH  " + Mathf.CeilToInt(health) + " / 100");
        GUI.Label(new Rect(24, 155, 500, 48), "LEVEL  " + level + "     KILLS  " + kills);
        GUI.Label(new Rect(24, Screen.height - 76, Screen.width - 48, 55), "Touch and drag the left side to move");
        if (dead)
        {
            GUI.color = new Color(1f, 0.3f, 0.24f);
            GUI.Label(new Rect(Screen.width * 0.34f, Screen.height * 0.45f, Screen.width * 0.5f, 90), "RUN ENDED  •  KILLS " + kills);
        }
    }

    private sealed class Enemy
    {
        public Transform body;
        public int hp;
        public float nextHitAt;
    }
}
