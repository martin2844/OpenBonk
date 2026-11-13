#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace Rewired.Glyphs.UnityUI
{
	[AddComponentMenu("Rewired/Glyphs/Unity UI/Unity UI Text Mesh Pro Glyph Helper")]
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class UnityUITextMeshProGlyphHelper : MonoBehaviour
	{
		private delegate bool ParseTagAttributesHandler(string text, int startIndex, int count, out string replacement);

		private abstract class Tag
		{
			public enum TagType
			{
				ControllerElement = 0,
				Action = 1,
				Player = 2
			}

			public abstract class Pool
			{
				public abstract bool Return(Tag obj);
			}

			public sealed class Pool<T> : Pool where T : Tag, new()
			{
				private readonly List<T> _list;

				public T Get()
				{
					return null;
				}

				public override bool Return(Tag obj)
				{
					return false;
				}
			}

			public readonly TagType tagType;

			private Pool _pool;

			protected Pool pool
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			protected Tag(TagType tagType)
			{
			}

			public void ReturnToPool()
			{
			}

			protected abstract void Clear();

			public static void Clear(List<Tag> list)
			{
			}
		}

		private sealed class ControllerElementTag : Tag
		{
			public DisplayType type;

			public int playerId;

			public int actionId;

			public AxisRange actionRange;

			private readonly List<GlyphOrText> _glyphsOrText;

			public ControllerElementTag()
				: base(TagType.ControllerElement)
			{
				_glyphsOrText = new List<GlyphOrText>();
			}

			public List<GlyphOrText> glyphsOrText => null;

			public override string ToString()
			{
				return null;
			}

			protected override void Clear()
			{
			}

			public static bool TryParseString(string text, int startIndex, int count, StringBuilder sb1, StringBuilder sb2, Dictionary<string, string> workDictionary, Pool<ControllerElementTag> pool, out ControllerElementTag result)
			{
				result = null;
				return false;
			}
		}

		private sealed class ActionTag : Tag
		{
			public int actionId;

			public AxisRange actionRange;

			private string _displayName;

			public ActionTag()
				: base(TagType.Action)
			{
			}

			public string displayName
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public override string ToString()
			{
				return null;
			}

			protected override void Clear()
			{
			}

			public static bool TryParseString(string text, int startIndex, int count, StringBuilder sb1, StringBuilder sb2, Dictionary<string, string> workDictionary, Pool<ActionTag> pool, out ActionTag result)
			{
				result = null;
				return false;
			}
		}

		private sealed class PlayerTag : Tag
		{
			public int playerId;

			private string _displayName;

			public PlayerTag()
				: base(TagType.Player)
			{
			}

			public string displayName
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public override string ToString()
			{
				return null;
			}

			protected override void Clear()
			{
			}

			public static bool TryParseString(string text, int startIndex, int count, StringBuilder sb1, StringBuilder sb2, Dictionary<string, string> workDictionary, Pool<PlayerTag> pool, out PlayerTag result)
			{
				result = null;
				return false;
			}
		}

		private struct GlyphOrText : IEquatable<GlyphOrText>
		{
			public string glyphKey;

			public Sprite sprite;

			public string name;

			public override bool Equals(object obj)
			{
				return false;
			}

			public override int GetHashCode()
			{
				return 0;
			}

			public bool Equals(GlyphOrText other)
			{
				return false;
			}

			public static bool operator ==(GlyphOrText a, GlyphOrText b)
			{
				return false;
			}

			public static bool operator !=(GlyphOrText a, GlyphOrText b)
			{
				return false;
			}
		}

		private class Asset
		{
			public readonly uint id;

			private ITMProSpriteAsset _spriteAsset;

			private Material _material;

			private static uint s_idCounter;

			private static Shader __tmProShader;

			public ITMProSpriteAsset spriteAsset => null;

			public Material material => null;

			private static Shader tmProShader => null;

			public Asset(Material baseMaterial)
			{
			}

			public static Material CreateMaterial(Material baseMaterial, uint id)
			{
				return null;
			}

			public void Destroy()
			{
			}
		}

		[Serializable]
		public struct TMProSpriteOptions : IEquatable<TMProSpriteOptions>
		{
			[Tooltip("Scale.")]
			[SerializeField]
			private float _scale;

			[Tooltip("This value will be multiplied by the Sprite width and height and applied to offset.")]
			[SerializeField]
			private Vector2 _offsetSizeMultiplier;

			[Tooltip("An extra offset that is cumulative with Offset Size Multiplier.")]
			[SerializeField]
			private Vector2 _extraOffset;

			[Tooltip("This value will be multiplied by the Sprite width applied to X Advance.")]
			[SerializeField]
			private float _xAdvanceWidthMultiplier;

			[Tooltip("An extra offset that is cumulative with X Advance Width Multiplier.")]
			[SerializeField]
			private float _extraXAdvance;

			public float scale
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public Vector2 offsetSizeMultiplier
			{
				get
				{
					return default(Vector2);
				}
				set
				{
				}
			}

			public Vector2 extraOffset
			{
				get
				{
					return default(Vector2);
				}
				set
				{
				}
			}

			public float xAdvanceWidthMultiplier
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public float extraXAdvance
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public static TMProSpriteOptions Default => default(TMProSpriteOptions);

			public override bool Equals(object obj)
			{
				return false;
			}

			public override int GetHashCode()
			{
				return 0;
			}

			public bool Equals(TMProSpriteOptions other)
			{
				return false;
			}

			public static bool operator ==(TMProSpriteOptions a, TMProSpriteOptions b)
			{
				return false;
			}

			public static bool operator !=(TMProSpriteOptions a, TMProSpriteOptions b)
			{
				return false;
			}
		}

		[Serializable]
		public struct SpriteMaterialProperties
		{
			[Tooltip("Sprite material color.")]
			[SerializeField]
			private Color _color;

			public Color color
			{
				get
				{
					return default(Color);
				}
				set
				{
				}
			}

			public static SpriteMaterialProperties Default => default(SpriteMaterialProperties);
		}

		private interface ITMProSprite
		{
			uint id { get; set; }

			float width { get; set; }

			float height { get; set; }

			float xOffset { get; set; }

			float yOffset { get; set; }

			float xAdvance { get; set; }

			Vector2 position { get; set; }

			Vector2 pivot { get; set; }

			float scale { get; set; }

			string name { get; set; }

			uint unicode { get; set; }

			int hashCode { get; set; }

			Sprite sprite { get; set; }
		}

		private interface ITMProSpriteAsset
		{
			int spriteCount { get; }

			Texture spriteSheet { get; set; }

			TMP_SpriteAsset GetSpriteAsset();

			ITMProSprite GetSprite(int index);

			void AddSprite(ITMProSprite sprite);

			bool Contains(string spriteName);

			void Clear();

			void UpdateLookupTables();

			void Destroy();
		}

		private static class TMProAssetVersionHelper
		{
			private static bool _isVersionSupportedChecked;

			private static bool CheckVersionSupported()
			{
				return false;
			}

			public static ITMProSprite CreateSprite()
			{
				return null;
			}

			public static ITMProSpriteAsset CreateSpriteAsset()
			{
				return null;
			}
		}

		private class TMProSprite_AssetV1_0_0 : ITMProSprite
		{
			public class TMPro_SpriteAsset : ITMProSpriteAsset
			{
				private TMP_SpriteAsset _spriteAsset;

				private readonly List<TMProSprite_AssetV1_0_0> _sprites;

				public int spriteCount => 0;

				public Texture spriteSheet
				{
					get
					{
						return null;
					}
					set
					{
					}
				}

				public TMP_SpriteAsset GetSpriteAsset()
				{
					return null;
				}

				public ITMProSprite GetSprite(int index)
				{
					return null;
				}

				public void AddSprite(ITMProSprite sprite)
				{
				}

				public void Clear()
				{
				}

				public bool Contains(string spriteName)
				{
					return false;
				}

				public void UpdateLookupTables()
				{
				}

				public void Destroy()
				{
				}
			}

			public TMP_Sprite spriteInfo;

			public uint id
			{
				get
				{
					return 0u;
				}
				set
				{
				}
			}

			public float width
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public float height
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public float xOffset
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public float yOffset
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public float xAdvance
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public Vector2 position
			{
				get
				{
					return default(Vector2);
				}
				set
				{
				}
			}

			public Vector2 pivot
			{
				get
				{
					return default(Vector2);
				}
				set
				{
				}
			}

			public float scale
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public string name
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public uint unicode
			{
				get
				{
					return 0u;
				}
				set
				{
				}
			}

			public int hashCode
			{
				get
				{
					return 0;
				}
				set
				{
				}
			}

			public Sprite sprite
			{
				get
				{
					return null;
				}
				set
				{
				}
			}
		}

		private class TMProSprite_AssetV1_1_0 : ITMProSprite
		{
			public class TMPro_SpriteCharacter
			{
				private const string typeFullName = "TMPro.TMP_SpriteCharacter";

				private readonly object _source;

				private readonly PropertyInfo _glyph;

				private readonly PropertyInfo _unicode;

				private readonly PropertyInfo _name;

				private readonly PropertyInfo _scale;

				private readonly PropertyInfo _glyphIndex;

				private static Type s_type;

				public object source => null;

				public Glyph glyph
				{
					get
					{
						return null;
					}
					set
					{
					}
				}

				public uint unicode
				{
					get
					{
						return 0u;
					}
					set
					{
					}
				}

				public string name
				{
					get
					{
						return null;
					}
					set
					{
					}
				}

				public float scale
				{
					get
					{
						return 0f;
					}
					set
					{
					}
				}

				public uint glyphIndex
				{
					get
					{
						return 0u;
					}
					set
					{
					}
				}

				private static Type GetReflectedType()
				{
					return null;
				}
			}

			public class TMPro_SpriteGlyph
			{
				private const string typeFullName = "TMPro.TMP_SpriteGlyph";

				private readonly Glyph _source;

				private readonly FieldInfo _sprite;

				private static Type s_type;

				public Glyph source => null;

				public Sprite sprite
				{
					get
					{
						return null;
					}
					set
					{
					}
				}

				private static Type GetReflectedType()
				{
					return null;
				}

				private static void Initialize(Glyph glyph)
				{
				}
			}

			public class TMPro_SpriteAsset : ITMProSpriteAsset
			{
				private readonly PropertyInfo _spriteCharacterTable;

				private readonly PropertyInfo _spriteGlyphTable;

				private readonly IList _spriteCharacterTableList;

				private readonly IList _spriteGlyphTableList;

				private readonly List<TMProSprite_AssetV1_1_0> _sprites;

				private TMP_SpriteAsset _spriteAsset;

				public int spriteCount => 0;

				public Texture spriteSheet
				{
					get
					{
						return null;
					}
					set
					{
					}
				}

				public TMP_SpriteAsset GetSpriteAsset()
				{
					return null;
				}

				public ITMProSprite GetSprite(int index)
				{
					return null;
				}

				public void AddSprite(ITMProSprite sprite)
				{
				}

				public void Clear()
				{
				}

				public bool Contains(string spriteName)
				{
					return false;
				}

				public void UpdateLookupTables()
				{
				}

				public void Destroy()
				{
				}
			}

			private readonly TMPro_SpriteGlyph _spriteGlyph;

			private readonly TMPro_SpriteCharacter _spriteCharacter;

			private static bool? s_isVersionSupported;

			public TMPro_SpriteGlyph spriteGlyph => null;

			public TMPro_SpriteCharacter spriteCharacter => null;

			public uint id
			{
				get
				{
					return 0u;
				}
				set
				{
				}
			}

			public float width
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public float height
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public float xOffset
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public float yOffset
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public float xAdvance
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public Vector2 position
			{
				get
				{
					return default(Vector2);
				}
				set
				{
				}
			}

			public Vector2 pivot
			{
				get
				{
					return default(Vector2);
				}
				set
				{
				}
			}

			public float scale
			{
				get
				{
					return 0f;
				}
				set
				{
				}
			}

			public string name
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public uint unicode
			{
				get
				{
					return 0u;
				}
				set
				{
				}
			}

			public int hashCode
			{
				get
				{
					return 0;
				}
				set
				{
				}
			}

			public Sprite sprite
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public static bool CheckVersionSupported()
			{
				return false;
			}
		}

		private enum DisplayType
		{
			Glyph = 0,
			Text = 1,
			GlyphOrText = 2
		}

		[Tooltip("Enter text into this field and not in the TMPro Text field directly. Text will be parsed for special tags, and the final result will be passed on to the Text Mesh Pro Text component. See the documentation for special tag format.")]
		[SerializeField]
		[TextArea(3, 10)]
		private string _text;

		[Tooltip("Optional reference to an object that defines options. If blank, the global default options will be used.")]
		[SerializeField]
		private ControllerElementGlyphSelectorOptionsSOBase _options;

		[Tooltip("Options that control how Text Mesh Pro displays Sprites.")]
		[SerializeField]
		private TMProSpriteOptions _spriteOptions;

		[Tooltip("Optional material for Sprites. If blank, the default material will be used.\nMaterial is instantiated for each Sprite Asset, so making changes to values in the base material later will not affect Sprites. Changing the base material at runtime will copy only certain properties from the new material to Sprite materials.")]
		[SerializeField]
		private Material _baseSpriteMaterial;

		[Tooltip("If enabled, local values such as Sprite color will be used instead of the value on the base material.")]
		[SerializeField]
		private bool _overrideSpriteMaterialProperties;

		[Tooltip("These properties will override the properties on the Sprite material if Override Sprite Material Properties is enabled.")]
		[SerializeField]
		private SpriteMaterialProperties _spriteMaterialProperties;

		[NonSerialized]
		private TextMeshProUGUI _tmProText;

		[NonSerialized]
		private string _textPrev;

		[NonSerialized]
		private readonly StringBuilder _processTagSb;

		[NonSerialized]
		private readonly StringBuilder _tempSb;

		[NonSerialized]
		private readonly StringBuilder _tempSb2;

		[NonSerialized]
		private Asset _primaryAsset;

		[NonSerialized]
		private readonly List<Asset> _assignedAssets;

		[NonSerialized]
		private readonly List<Asset> _assetsPool;

		[NonSerialized]
		private readonly List<ActionElementMap> _tempAems;

		[NonSerialized]
		private readonly List<Sprite> _tempGlyphs;

		[NonSerialized]
		private readonly List<Asset> _dirtyAssets;

		[NonSerialized]
		private readonly List<string> _tempKeys;

		[NonSerialized]
		private readonly List<GlyphOrText> _glyphsOrTextTemp;

		[NonSerialized]
		private readonly List<Asset> _currentlyUsedAssets;

		[NonSerialized]
		private readonly List<Tag> _currentTags;

		[NonSerialized]
		private Dictionary<string, string> _tempStringDictionary;

		[NonSerialized]
		private bool _initialized;

		[NonSerialized]
		private bool _rebuildRequired;

		[NonSerialized]
		private Texture2D _stubTexture;

		private Tag.Pool<ControllerElementTag> __controllerElementTagPool;

		private Tag.Pool<ActionTag> __actionTagPool;

		private Tag.Pool<PlayerTag> __playerTagPool;

		[NonSerialized]
		private Dictionary<string, ParseTagAttributesHandler> __tagHandlers;

		private static string[] __s_displayTypeNames;

		private static DisplayType[] __s_displayTypeValues;

		private static string[] __s_axisRangeNames;

		private static AxisRange[] __s_axisRangeValues;

		private Tag.Pool<ControllerElementTag> controllerElementTagPool => null;

		private Tag.Pool<ActionTag> actionTagPool => null;

		private Tag.Pool<PlayerTag> playerTagPool => null;

		private Dictionary<string, ParseTagAttributesHandler> tagHandlers => null;

		public virtual string text
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual ControllerElementGlyphSelectorOptionsSOBase options
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual TMProSpriteOptions spriteOptions
		{
			get
			{
				return default(TMProSpriteOptions);
			}
			set
			{
			}
		}

		public virtual Material baseSpriteMaterial
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual bool overrideSpriteMaterialProperties
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public virtual SpriteMaterialProperties spriteMaterialProperties
		{
			get
			{
				return default(SpriteMaterialProperties);
			}
			set
			{
			}
		}

		private static int shaderPropertyId_color => 0;

		private static string[] s_displayTypeNames => null;

		private static DisplayType[] s_displayTypeValues => null;

		private static string[] s_axisRangeNames => null;

		private static AxisRange[] s_axisRangeValues => null;

		protected virtual void OnEnable()
		{
		}

		protected virtual void Start()
		{
		}

		protected virtual void Update()
		{
		}

		protected virtual void OnDestroy()
		{
		}

		public virtual void ForceUpdate()
		{
		}

		protected virtual ControllerElementGlyphSelectorOptions GetOptionsOrDefault()
		{
			return null;
		}

		private bool Initialize()
		{
			return false;
		}

		private void MainUpdate()
		{
		}

		private bool ParseText(string text, out string newText)
		{
			newText = null;
			return false;
		}

		private bool ProcessNextTag(ref string text, StringBuilder sb)
		{
			return false;
		}

		private bool ProcessTag_ControllerElement(string text, int startIndex, int count, out string replacement)
		{
			replacement = null;
			return false;
		}

		private bool ProcessTag_Action(string text, int startIndex, int count, out string replacement)
		{
			replacement = null;
			return false;
		}

		private bool ProcessTag_Player(string text, int startIndex, int count, out string replacement)
		{
			replacement = null;
			return false;
		}

		private bool TryCreateTMProString(List<GlyphOrText> glyphs, out string result)
		{
			result = null;
			return false;
		}

		private bool TryGetControllerElementGlyphsOrText(ControllerElementTag tag, List<GlyphOrText> results)
		{
			return false;
		}

		private bool TryGetActionDisplayName(ActionTag tag, out string result)
		{
			result = null;
			return false;
		}

		private bool TryGetPlayerDisplayName(PlayerTag tag, out string result)
		{
			result = null;
			return false;
		}

		private bool TryAssignSprite(Sprite sprite, string key)
		{
			return false;
		}

		private void RequireRebuild()
		{
		}

		private void CreatePrimaryAsset()
		{
		}

		private Asset GetOrCreateAsset(Sprite sprite)
		{
			return null;
		}

		private Asset CreateAsset()
		{
			return null;
		}

		private void RemoveUnusedAssets()
		{
		}

		private void SetDirty(Asset asset)
		{
		}

		private void ForEachAsset(Action<Asset> callback)
		{
		}

		private static void ParseAttributes(string text, int startIndex, int count, StringBuilder sbKey, StringBuilder sbValue, Dictionary<string, string> results)
		{
		}

		private static bool IsValidKeyChar(char c)
		{
			return false;
		}

		private static bool IsValidTagNameChar(char c)
		{
			return false;
		}

		private static bool IsValidNonQuotedValueChar(char c)
		{
			return false;
		}

		private static bool IsEqual(List<GlyphOrText> a, List<GlyphOrText> b)
		{
			return false;
		}

		private static void WriteSpriteKey(StringBuilder sb, string key)
		{
		}

		private static bool TryGetGlyphsOrText(ActionElementMap aem, DisplayType displayType, List<Sprite> glyphs, List<string> keys, List<GlyphOrText> results)
		{
			return false;
		}

		private static bool IsGlyphAllowed(DisplayType displayType)
		{
			return false;
		}

		private static bool IsTextAllowed(DisplayType displayType)
		{
			return false;
		}

		private static void CopyMaterialProperties(Material source, Material destination)
		{
		}

		private static void CopySpriteMaterialPropertiesToMaterial(SpriteMaterialProperties properties, Material material)
		{
		}
	}
}

#endif
