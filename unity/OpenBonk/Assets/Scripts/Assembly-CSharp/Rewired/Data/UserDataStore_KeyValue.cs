#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Rewired.Utils.Attributes;
using Rewired.Utils.Libraries.TinyJson;
using UnityEngine;

namespace Rewired.Data
{
	public abstract class UserDataStore_KeyValue : UserDataStore
	{
		private class ControllerAssignmentSaveInfo
		{
			public class PlayerInfo
			{
				public int id;

				public bool hasKeyboard;

				public bool hasMouse;

				public JoystickInfo[] joysticks;

				public int joystickCount => 0;

				public int IndexOfJoystick(int joystickId)
				{
					return 0;
				}

				public bool ContainsJoystick(int joystickId)
				{
					return false;
				}
			}

			public class JoystickInfo
			{
				public Guid instanceGuid;

				public string hardwareIdentifier;

				public int id;
			}

			public PlayerInfo[] players;

			public int playerCount => 0;

			public ControllerAssignmentSaveInfo()
			{
			}

			public ControllerAssignmentSaveInfo(int playerCount)
			{
			}

			public int IndexOfPlayer(int playerId)
			{
				return 0;
			}

			public bool ContainsPlayer(int playerId)
			{
				return false;
			}
		}

		private class JoystickAssignmentHistoryInfo
		{
			public readonly Joystick joystick;

			public readonly int oldJoystickId;

			public JoystickAssignmentHistoryInfo(Joystick joystick, int oldJoystickId)
			{
			}
		}

		protected interface IDataStore
		{
			bool Save();

			bool Load();

			bool Clear();

			bool TryGetValue(string key, out object result);

			bool SetValue(string key, object value);
		}

		[Serializable]
		protected class ControllerElementByRoleMap
		{
			[Serializable]
			public struct Entry
			{
				public int actionId;

				public ControllerElementType elementType;

				public AxisRange axisRange;

				public bool invert;

				public Pole axisContribution;

				public bool TryGetElementAssignment(ControllerType controllerType, Controller.Element targetElement, out ElementAssignment assignment)
				{
					assignment = default(ElementAssignment);
					return false;
				}

				public override string ToString()
				{
					return null;
				}
			}

			[DoNotSerialize]
			public string role;

			public List<Entry> data;

			[Preserve]
			public ControllerElementByRoleMap()
			{
			}

			public void Add(ActionElementMap elementMap)
			{
			}

			public override string ToString()
			{
				return null;
			}

			public string ToJson()
			{
				return null;
			}

			public static ControllerElementByRoleMap FromJson(string role, string json)
			{
				return null;
			}
		}

		public enum ActionMappingSaveMode
		{
			ByController = 0,
			ByControllerElementRole = 1
		}

		[CompilerGenerated]
		private sealed class _003CLoadJoystickAssignmentsDeferred_003Ed__81 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public UserDataStore_KeyValue _003C_003E4__this;

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
			public _003CLoadJoystickAssignmentsDeferred_003Ed__81(int _003C_003E1__state)
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

		private static readonly string thisScriptName;

		private const string logPrefix = "Rewired: ";

		private const string key_controllerAssignments = "ControllerAssignments";

		private const int controllerMapKeyVersion = 0;

		private const int controllerElementByRoleMapKeyVersion = 0;

		[Tooltip("Should this script be used? If disabled, nothing will be saved or loaded.")]
		[SerializeField]
		private bool _isEnabled;

		[Tooltip("Should saved data be loaded on start?")]
		[SerializeField]
		private bool _loadDataOnStart;

		[Tooltip("Should Player Joystick assignments be saved and loaded? This is not totally reliable for all Joysticks on all platforms. Some platforms/input sources do not provide enough information to reliably save assignments from session to session and reboot to reboot.")]
		[SerializeField]
		private bool _loadJoystickAssignments;

		[Tooltip("Should Player Keyboard assignments be saved and loaded?")]
		[SerializeField]
		private bool _loadKeyboardAssignments;

		[Tooltip("Should Player Mouse assignments be saved and loaded?")]
		[SerializeField]
		private bool _loadMouseAssignments;

		[Tooltip("How should Action mapping data be saved?\n\nBy Controller: Data is stored per-controller. Action mappings apply only to the specific controller for which it was saved.\n\nBy Controller Element Role: Data is stored per-element on the controller if the controller element has a known role. Action mappings are mirrored on controller elements with the same role on all other controllers for the Player. Example: When saving Action mappings for a gamepad, element on all gamepads that have the same roles will inherit the mappings. This allows you to remap once for all compatible gamepads simultaneously, for example. This can extend beyond just gamepads, however. For example: On a console platform, a racing wheel with A, B, X, Y, D-Pad etc. elements will also reflect the same Action mappings if the gamepad is remapped. Action mappings for any controller elements that do not have known roles will be saved per-controller. Warning: Do not use this mode if you need to allow a Player to save different mappings for multiple controllers of the same type such as gamepads. (This option currently works best for gamepads and only miminally for other controller types.)")]
		[SerializeField]
		private ActionMappingSaveMode _actionMappingSaveMode;

		[NonSerialized]
		private bool _allowImpreciseJoystickAssignmentMatching;

		[NonSerialized]
		private bool _deferredJoystickAssignmentLoadPending;

		[NonSerialized]
		private bool _wasJoystickEverDetected;

		[NonSerialized]
		private List<int> __allActionIds;

		[NonSerialized]
		private string __allActionIdsString;

		[NonSerialized]
		private readonly StringBuilder _sb;

		[NonSerialized]
		private Dictionary<string, ControllerElementByRoleMap> _tempElementByRoleMaps;

		[NonSerialized]
		private Dictionary<string, bool> _tempElementByRoleMapsEnabled;

		public bool isEnabled
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool loadDataOnStart
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool loadJoystickAssignments
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool loadKeyboardAssignments
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool loadMouseAssignments
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public ActionMappingSaveMode actionMappingSaveMode
		{
			get
			{
				return default(ActionMappingSaveMode);
			}
			set
			{
			}
		}

		protected abstract IDataStore dataStore { get; }

		private bool loadControllerAssignments => false;

		private List<int> allActionIds => null;

		private string allActionIdsString => null;

		public override void Save()
		{
		}

		public override void SaveControllerData(int playerId, ControllerType controllerType, int controllerId)
		{
		}

		public override void SaveControllerData(ControllerType controllerType, int controllerId)
		{
		}

		public override void SavePlayerData(int playerId)
		{
		}

		public override void SaveInputBehavior(int playerId, int behaviorId)
		{
		}

		public override void Load()
		{
		}

		public override void LoadControllerData(int playerId, ControllerType controllerType, int controllerId)
		{
		}

		public override void LoadControllerData(ControllerType controllerType, int controllerId)
		{
		}

		public override void LoadPlayerData(int playerId)
		{
		}

		public override void LoadInputBehavior(int playerId, int behaviorId)
		{
		}

		protected override void OnInitialize()
		{
		}

		protected override void OnControllerConnected(ControllerStatusChangedEventArgs args)
		{
		}

		protected override void OnControllerPreDisconnect(ControllerStatusChangedEventArgs args)
		{
		}

		protected override void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
		{
		}

		public override void SaveControllerMap(int playerId, ControllerMap controllerMap)
		{
		}

		public override ControllerMap LoadControllerMap(int playerId, ControllerIdentifier controllerIdentifier, int categoryId, int layoutId)
		{
			return null;
		}

		public virtual void ClearSaveData()
		{
		}

		private int LoadAll()
		{
			return 0;
		}

		private int LoadPlayerDataNow(int playerId)
		{
			return 0;
		}

		private int LoadPlayerDataNow(Player player)
		{
			return 0;
		}

		private int LoadAllJoystickCalibrationData()
		{
			return 0;
		}

		private int LoadJoystickCalibrationData(Joystick joystick)
		{
			return 0;
		}

		private int LoadJoystickCalibrationData(int joystickId)
		{
			return 0;
		}

		private int LoadJoystickData(int joystickId)
		{
			return 0;
		}

		private int LoadControllerDataNow(int playerId, ControllerType controllerType, int controllerId)
		{
			return 0;
		}

		private int LoadControllerDataNow(ControllerType controllerType, int controllerId)
		{
			return 0;
		}

		private int LoadControllerMaps(int playerId, ControllerType controllerType, int controllerId)
		{
			return 0;
		}

		private ControllerMap LoadControllerMap(Player player, ControllerIdentifier controllerIdentifier, int categoryId, int layoutId)
		{
			return null;
		}

		private bool LoadControllerElementMapByRole(Player player, Controller controller, string role, int mapCategoryId, int layoutId, Dictionary<string, ControllerElementByRoleMap> elementByRoleMaps)
		{
			return false;
		}

		private int LoadInputBehaviors(int playerId)
		{
			return 0;
		}

		private int LoadInputBehaviorNow(int playerId, int behaviorId)
		{
			return 0;
		}

		private int LoadInputBehaviorNow(Player player, InputBehavior inputBehavior)
		{
			return 0;
		}

		private bool LoadControllerAssignmentsNow()
		{
			return false;
		}

		private bool LoadKeyboardAndMouseAssignmentsNow(ControllerAssignmentSaveInfo data)
		{
			return false;
		}

		private bool LoadJoystickAssignmentsNow(ControllerAssignmentSaveInfo data)
		{
			return false;
		}

		private ControllerAssignmentSaveInfo LoadControllerAssignmentData()
		{
			return null;
		}

		[IteratorStateMachine(typeof(_003CLoadJoystickAssignmentsDeferred_003Ed__81))]
		private IEnumerator LoadJoystickAssignmentsDeferred()
		{
			return null;
		}

		private void SaveAll()
		{
		}

		private void SavePlayerDataNow(int playerId)
		{
		}

		private void SavePlayerDataNow(Player player)
		{
		}

		private void SaveAllJoystickCalibrationData()
		{
		}

		private void SaveJoystickCalibrationData(int joystickId)
		{
		}

		private void SaveJoystickCalibrationData(Joystick joystick)
		{
		}

		private void SaveJoystickData(int joystickId)
		{
		}

		private void SaveControllerDataNow(int playerId, ControllerType controllerType, int controllerId)
		{
		}

		private void SaveControllerDataNow(ControllerType controllerType, int controllerId)
		{
		}

		private void SaveControllerMaps(Player player, PlayerSaveData playerSaveData)
		{
		}

		private void SaveControllerMaps(int playerId, ControllerType controllerType, int controllerId)
		{
		}

		private void SaveControllerMap(Player player, ControllerMap controllerMap)
		{
		}

		private void SaveControllerMapByController(Player player, ControllerMap controllerMap)
		{
		}

		private void SaveControllerMapByControllerElementRole(Player player, Controller controller, ControllerMap controllerMap)
		{
		}

		private bool AddControllerElementByRoleMapEntry(Player player, Controller controller, ActionElementMap elementMap, ref Dictionary<string, ControllerElementByRoleMap> maps)
		{
			return false;
		}

		private void SaveInputBehaviors(Player player, PlayerSaveData playerSaveData)
		{
		}

		private void SaveInputBehaviorNow(int playerId, int behaviorId)
		{
		}

		private void SaveInputBehaviorNow(Player player, InputBehavior inputBehavior)
		{
		}

		private bool SaveControllerAssignments()
		{
			return false;
		}

		private static void AppendPlayerKey(StringBuilder sb, Player player)
		{
		}

		private string GetControllerMapKey(Player player, ControllerIdentifier controllerIdentifier, int categoryId, int layoutId, int ppKeyVersion)
		{
			return null;
		}

		private string GetControllerMapKnownActionIdsKey(Player player, ControllerIdentifier controllerIdentifier, int categoryId, int layoutId, int ppKeyVersion)
		{
			return null;
		}

		private static void AppendControllerMapKeyCommonSuffix(StringBuilder sb, Player player, ControllerIdentifier controllerIdentifier, int categoryId, int layoutId, int keyVersion)
		{
		}

		private static void AppendControllerElementByRoleMapKey(StringBuilder sb, string elementRole, int categoryId, int layoutId, int keyVersion)
		{
		}

		private string GetJoystickCalibrationMapKey(Joystick joystick)
		{
			return null;
		}

		private string GetInputBehaviorKey(Player player, int inputBehaviorId)
		{
			return null;
		}

		private string GetControllerMapJson(Player player, ControllerIdentifier controllerIdentifier, int categoryId, int layoutId)
		{
			return null;
		}

		private List<int> GetControllerMapKnownActionIds(Player player, ControllerIdentifier controllerIdentifier, int categoryId, int layoutId)
		{
			return null;
		}

		private string GetJoystickCalibrationMapJson(Joystick joystick)
		{
			return null;
		}

		private string GetInputBehaviorJson(Player player, int id)
		{
			return null;
		}

		private void AddDefaultMappingsForNewActions(ControllerIdentifier controllerIdentifier, ControllerMap controllerMap, List<int> knownActionIds)
		{
		}

		private Joystick FindJoystickPrecise(ControllerAssignmentSaveInfo.JoystickInfo joystickInfo)
		{
			return null;
		}

		private bool TryFindJoysticksImprecise(ControllerAssignmentSaveInfo.JoystickInfo joystickInfo, out List<Joystick> matches)
		{
			matches = null;
			return false;
		}

		private void RefreshLayoutManager(int playerId)
		{
		}

		private void OnControllerMapsSaved(Player player)
		{
		}

		private static int GetDuplicateIndex(Player player, ControllerIdentifier controllerIdentifier)
		{
			return 0;
		}

		private static bool TryGetString(IDataStore store, string key, out string result)
		{
			result = null;
			return false;
		}

		private static int SortOldestToNewest(ControllerMapSaveData a, ControllerMapSaveData b)
		{
			return 0;
		}
	}
}

#endif
