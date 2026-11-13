using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.AI.Navigation;
using UnityEngine;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;

public class RsgController : MonoBehaviour
{
	public enum EDungeonType
	{
		Normal = 0,
		BossDungeon = 1
	}

	[CompilerGenerated]
	private sealed class _003CGenerateMap_003Ed__41 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public RsgController _003C_003E4__this;

		private Stopwatch _003Ctimer_003E5__2;

		private float _003ClowestCordsHeight_003E5__3;

		private int _003CmaxPieces_003E5__4;

		private int _003CnumPieces_003E5__5;

		private int _003Ccollisions_003E5__6;

		private int _003CmaxCollisions_003E5__7;

		private int _003ClookAhead_003E5__8;

		private RsgPiece _003Cprevious_003E5__9;

		private RsgPiece _003CpieceBeforeLookahead_003E5__10;

		private List<RsgPiece> _003ClookaheadPieces_003E5__11;

		private int _003Cj_003E5__12;

		private int _003Ck_003E5__13;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CGenerateMap_003Ed__41(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public bool combineColliderMesh;

	public NavMeshSurface navmeshSurface;

	public GameObject[] prefabs;

	public GameObject roomStart;

	public GameObject roomEnd;

	public GraveyardBossRoom roomBoss;

	private float generationDelay;

	private static ConsistentRandom random;

	public static int seed;

	public static Action<float> A_GenerationFinished;

	private static int customSeed;

	private float extraTime = 1.2f;

	private float extraTimeBoss = 1f;

	public static RsgController Instance;

	public int testSeed;

	private EDungeonType dungeonType;

	public static bool isCurrentMapRandomSeed;

	private float totalTraversalTime = 60f;

	private RsgPiece previousPiece;

	private List<RsgPiece> allPieces;

	private List<Bounds> bounds;

	private int mapLength;

	public int minPieces = 3;

	public int maxPieces = 5;

	private Vector3 startPosition = Vector3.one * 2000f;

	public GameObject combinedColliderMesh;

	private bool HasCustomSeed => customSeed != 0;

	public RsgStart rsgStart { get; private set; }

	public InteractableCryptLeave rsgEnd { get; private set; }

	private void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
	}

	public static void SetCustomSeed(int seed)
	{
		customSeed = seed;
	}

	private bool CanUseCustomSeed()
	{
		return !ChallengesTracker.HasChallengeModifier("crypt");
	}

	public void Generate(int newSeed, EDungeonType dungeonType, out float traversalTime)
	{
		isCurrentMapRandomSeed = !(HasCustomSeed && CanUseCustomSeed());
		seed = isCurrentMapRandomSeed ? newSeed : customSeed;
		this.dungeonType = dungeonType;
		random = new ConsistentRandom(seed);
		StartCoroutine(GenerateMap());
		traversalTime = totalTraversalTime;
	}

	private int FindMapLength()
	{
		return random.Next(minPieces, maxPieces);
	}

	public void ClearMap()
	{
		if (allPieces != null)
			foreach (RsgPiece piece in allPieces)
				Destroy(piece.gameObject);
		allPieces = new List<RsgPiece>();
		if (combinedColliderMesh != null) Destroy(combinedColliderMesh);
	}

	private IEnumerator GenerateMap()
	{
		foreach (GameObject prefab in prefabs) prefab.SetActive(true);
		roomStart.SetActive(true);
		roomEnd.SetActive(true);
		Stopwatch timer = Stopwatch.StartNew();
		mapLength = FindMapLength();
		bounds = new List<Bounds>();
		allPieces = new List<RsgPiece>();
		float lowestHeight = float.MaxValue;
		int maxPiecesInMap = mapLength + 2;
		int numPieces = 0;
		int collisions = 0;
		const int maxCollisions = 5;
		const int lookAhead = 2;
		totalTraversalTime = 0f;
		RsgPiece previous = null;
		while (numPieces < maxPiecesInMap)
		{
			List<RsgPiece> lookaheadPieces = new List<RsgPiece>();
			RsgPiece pieceBeforeLookahead = previous;
			bool collided = false;
			for (int j = 0; j < lookAhead; j++)
			{
				if (generationDelay > 0f) yield return new WaitForSeconds(generationDelay);
				RsgPiece piece;
				if (numPieces == 0)
				{
					piece = Instantiate(roomStart).GetComponent<RsgPiece>();
					rsgStart = piece.GetComponent<RsgStart>();
					j = lookAhead;
				}
				else if (numPieces >= maxPiecesInMap - 1)
				{
					piece = Instantiate(roomEnd).GetComponent<RsgPiece>();
					rsgEnd = piece.GetComponentInChildren<InteractableCryptLeave>();
					rsgEnd.SetType(dungeonType);
					rsgEnd.gameObject.SetActive(true);
					j = lookAhead;
				}
				else piece = Instantiate(prefabs[random.Next(0, prefabs.Length)]).GetComponent<RsgPiece>();
				if (piece.mirror && random.NextDouble() >= 0.5) MirrorPiece(piece);
				if (piece.reverse && random.NextDouble() >= 0.5) ReversePiece(piece);
				piece.SetCollider();
				lookaheadPieces.Add(piece);
				piece.name = numPieces.ToString();
				GetNextTransform(previous, out Vector3 pos, out Quaternion rotation);
				piece.transform.rotation = rotation;
				piece.transform.position = pos;
				if (previous != null)
				{
					float angle = Vector3.SignedAngle(previous.end.forward, piece.start.forward, Vector3.up);
					piece.transform.Rotate(Vector3.up, -Mathf.RoundToInt(angle));
				}
				piece.transform.position = pos + piece.transform.position - piece.start.position;
				if (BoundsOverlap(piece)) { collided = true; break; }
				previous = piece;
			}
			if (collided)
			{
				for (int k = lookaheadPieces.Count - 1; k >= 0; k--)
				{
					if (generationDelay > 0f) yield return new WaitForSeconds(generationDelay);
					Destroy(lookaheadPieces[k].gameObject);
				}
				collisions++;
				if (collisions >= maxCollisions)
				{
					foreach (RsgPiece accepted in allPieces) Destroy(accepted.gameObject);
					StartCoroutine(GenerateMap());
					yield break;
				}
				previous = pieceBeforeLookahead;
				continue;
			}
			if (lookaheadPieces.Count > 0)
			{
				RsgPiece accepted = lookaheadPieces[0];
				lowestHeight = Mathf.Min(lowestHeight, accepted.GetLowestCordY());
				allPieces.Add(accepted);
				BoundsAdd(accepted);
				for (int k = 1; k < lookaheadPieces.Count; k++) Destroy(lookaheadPieces[k].gameObject);
				previous = accepted;
				numPieces++;
			}
		}
		foreach (GameObject prefab in prefabs) prefab.SetActive(false);
		roomStart.SetActive(false);
		roomEnd.SetActive(false);
		totalTraversalTime = 0f;
		foreach (RsgPiece piece in allPieces) totalTraversalTime += piece.traverseTime;
		totalTraversalTime *= dungeonType == EDungeonType.Normal ? extraTime : extraTimeBoss;
		if (combineColliderMesh) CombineMeshes(allPieces);
		timer.Stop();
		if (navmeshSurface != null) navmeshSurface.BuildNavMesh();
		A_GenerationFinished?.Invoke(lowestHeight);
	}

	private void MirrorPiece(RsgPiece piece)
	{
		piece.Mirror();
	}

	private void ReversePiece(RsgPiece piece)
	{
		Transform oldStart = piece.start;
		piece.start = piece.end;
		piece.end = oldStart;
		piece.end.Rotate(Vector3.up, 180f);
		piece.start.Rotate(Vector3.up, 180f);
		piece.start.name = "start";
		piece.end.name = "end";
	}

	private bool BoundsOverlap(RsgPiece piece)
	{
		Bounds candidate = piece.GetBounds();
		foreach (Bounds existing in bounds)
			if (candidate.Intersects(existing))
				return true;
		return false;
	}

	private void BoundsAdd(RsgPiece piece)
	{
		bounds.Add(piece.GetBounds());
	}

	private void GetNextTransform(RsgPiece piece, out Vector3 pos, out Quaternion rotation)
	{
		pos = startPosition;
		rotation = Quaternion.identity;
		if (piece != null)
		{
			pos = piece.end.position;
			rotation = piece.end.rotation;
		}
	}

	public void CombineMeshes(List<RsgPiece> allPieces)
	{
		List<MeshCollider> colliders = new List<MeshCollider>();
		foreach (RsgPiece piece in allPieces) colliders.Add(piece.newCollider);
		List<CombineInstance> combined = new List<CombineInstance>();
		foreach (MeshCollider collider in colliders)
		{
			for (int i = 0; i < collider.sharedMesh.subMeshCount; i++)
			{
				combined.Add(new CombineInstance
				{
					mesh = collider.sharedMesh,
					subMeshIndex = i,
					transform = collider.transform.localToWorldMatrix
				});
			}
		}
		combinedColliderMesh = new GameObject("MapMesh");
		MeshFilter filter = combinedColliderMesh.AddComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mesh.CombineMeshes(combined.ToArray(), true);
		AutoWeld(mesh, 0.25f);
		filter.mesh = mesh;
		combinedColliderMesh.AddComponent<MeshCollider>().sharedMesh = mesh;
		combinedColliderMesh.SetActive(true);
		combinedColliderMesh.layer = LayerMask.NameToLayer("Ground");
		foreach (MeshCollider collider in colliders) collider.enabled = false;
	}

	public static void AutoWeld(Mesh mesh, float threshold)
	{
		Vector3[] vertices = mesh.vertices;
		Vector3[] unique = new Vector3[vertices.Length];
		int[] remap = new int[vertices.Length];
		int count = 0;
		for (int i = 0; i < vertices.Length; i++)
		{
			int j = 0;
			for (; j < count; j++)
				if ((unique[j] - vertices[i]).sqrMagnitude < threshold) break;
			if (j == count) unique[count++] = vertices[i];
			remap[i] = j;
		}
		int[] triangles = mesh.triangles;
		for (int i = 0; i < triangles.Length; i++) triangles[i] = remap[triangles[i]];
		Vector3[] compact = new Vector3[count];
		Array.Copy(unique, compact, count);
		mesh.Clear();
		mesh.vertices = compact;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		mesh.Optimize();
	}

	private void OnDrawGizmos()
	{
	}
}
