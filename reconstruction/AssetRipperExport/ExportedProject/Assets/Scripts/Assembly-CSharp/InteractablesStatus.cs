using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;

public static class InteractablesStatus
{
	public class InteractableStatusContainer
	{
		public int numTotal;

		public int numUsed;

		public string debugName;

		public InteractableStatusContainer(string debugName)
		{
			this.debugName = debugName;
		}

		public bool DisplayInDebug()
		{
			return true;
		}

		public bool IsDone()
		{
			return numUsed >= numTotal;
		}
	}

	public static Dictionary<string, InteractableStatusContainer> interactablesByName = new Dictionary<string, InteractableStatusContainer>();

	public static Action<string> A_InteractableUsed;

	public static Action<string> A_InteractableSpawned;

	public static void Init()
	{
		Cleanup();
		MapGenerationController.A_PreGeneration += PreMapGeneration;
		BaseInteractable.A_DebugSpawn += OnInteractableSpawn;
		BaseInteractable.A_DebugDisable += OnInteractableDisable;
		DetectInteractables.A_Interacted += OnInteractableUse;
		ChargeShrine.A_Charged += OnChargeShrineCharged;
		InteractableShadyGuy.A_ShadyGuyDone += OnShadyGuyUsed;
		InteractableMicrowave.A_Exploded += OnMicrowaveExplode;
		GameManager.A_DungeonEnded += OnDungeonEnded;
	}

	public static void Cleanup()
	{
		MapGenerationController.A_PreGeneration -= PreMapGeneration;
		BaseInteractable.A_DebugSpawn -= OnInteractableSpawn;
		BaseInteractable.A_DebugDisable -= OnInteractableDisable;
		DetectInteractables.A_Interacted -= OnInteractableUse;
		ChargeShrine.A_Charged -= OnChargeShrineCharged;
		InteractableShadyGuy.A_ShadyGuyDone -= OnShadyGuyUsed;
		InteractableMicrowave.A_Exploded -= OnMicrowaveExplode;
		GameManager.A_DungeonEnded -= OnDungeonEnded;
	}

	private static void PreMapGeneration()
	{
		interactablesByName.Clear();
	}

	private static void OnInteractableSpawn(string debugName)
	{
		if (string.IsNullOrEmpty(debugName)) return;
		if (!interactablesByName.TryGetValue(debugName, out var entry)) interactablesByName.Add(debugName, entry = new InteractableStatusContainer(debugName));
		entry.numTotal++;
		A_InteractableSpawned?.Invoke(debugName);
	}

	private static void OnInteractableDisable(string debugName)
	{
		if (debugName != null && interactablesByName.TryGetValue(debugName, out var entry)) entry.numTotal--;
	}

	private static void OnInteractableUse(BaseInteractable interactable, bool success)
	{
		if (!success || interactable == null || interactable.GetType() == typeof(InteractableMicrowave)) return;
		MarkUsed(interactable.GetDebugName());
	}

	private static void OnChargeShrineCharged(bool whatever)
	{
		MarkUsed(ChargeShrine.debugName);
	}

	private static void OnShadyGuyUsed(InteractableShadyGuy shadyGuy)
	{
		MarkUsed(InteractableShadyGuy.debugName);
	}

	private static void OnMicrowaveExplode()
	{
		MarkUsed(InteractableMicrowave.debugName);
	}

	private static void OnDungeonEnded()
	{
		interactablesByName.Remove(InteractableChest.debugNameCrypt);
		interactablesByName.Remove(InteractablePot.debugNameCrypt);
	}

	private static void MarkUsed(string name)
	{
		if (name == null || !interactablesByName.TryGetValue(name, out var entry)) return;
		entry.numUsed++;
		A_InteractableUsed?.Invoke(name);
	}

	public static void PrintAll()
	{
	}
}
