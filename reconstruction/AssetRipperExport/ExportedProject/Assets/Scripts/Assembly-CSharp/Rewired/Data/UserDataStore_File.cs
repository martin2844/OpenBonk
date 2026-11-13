#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Rewired.Utils.Libraries.CLZF2;
using UnityEngine;

namespace Rewired.Data
{
	public class UserDataStore_File : UserDataStore_KeyValue
	{
		private sealed class DataStore : IDataStore
		{
			private Dictionary<string, object> _data;

			private readonly string _absFilePath;

			private IDataHandler _dataHandler;

			public DataStore(string fileName, string absDirectory, IDataHandler dataHandler)
			{
			}

			public bool TryGetValue(string key, out object value)
			{
				value = null;
				return false;
			}

			public bool SetValue(string key, object value)
			{
				return false;
			}

			public bool Save()
			{
				return false;
			}

			public bool Load()
			{
				return false;
			}

			public bool Clear()
			{
				return false;
			}
		}

		private sealed class LocalFileDataHandler : IDataHandler
		{
			private readonly Func<DataFormat> _dataFormatDelegate;

			private readonly Codec _codec;

			public LocalFileDataHandler(Func<DataFormat> dataFormatDelegate, Codec codec)
			{
			}

			public bool Load(string absoluteFilePath, out string data)
			{
				data = null;
				return false;
			}

			public bool Save(string absoluteFilePath, string data)
			{
				return false;
			}

			public bool Clear(string absoluteFilePath)
			{
				return false;
			}
		}

		private abstract class Codec
		{
			public abstract byte[] Encode(string @string);

			public abstract string Decode(byte[] data);
		}

		private sealed class UTF8Text : Codec
		{
			public override byte[] Encode(string @string)
			{
				return null;
			}

			public override string Decode(byte[] data)
			{
				return null;
			}
		}

		private sealed class CLZF2 : Codec
		{
			private readonly Rewired.Utils.Libraries.CLZF2.CLZF2 _cLZF2;

			public override byte[] Encode(string @string)
			{
				return null;
			}

			public override string Decode(byte[] data)
			{
				return null;
			}
		}

		public interface IDataHandler
		{
			bool Load(string absoluteFilePath, out string data);

			bool Save(string absoluteFilePath, string data);

			bool Clear(string absoluteFilePath);
		}

		public enum DataFormat
		{
			Text = 0,
			Binary = 1
		}

		private static readonly string thisScriptName;

		private const string logPrefix = "Rewired: ";

		private const string defaultExtensionText = ".json";

		private const string defaultExtensionBinary = ".bin";

		private const string defaultFileName = "RewiredSaveData.json";

		[Tooltip("The data file name. Changing this will make saved data already stored with the old file name no longer accessible.")]
		[SerializeField]
		private string _fileName;

		[Tooltip("Determines if the file should be stored as binary or text. Changing this will make saved data already stored no longer accessible.")]
		[SerializeField]
		private DataFormat _dataFormat;

		[NonSerialized]
		private string __directory;

		[NonSerialized]
		private DataStore _dataStore;

		[NonSerialized]
		private IDataHandler __dataHandler;

		[NonSerialized]
		private bool _initialized;

		public string directory
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public string fileName
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public DataFormat dataFormat
		{
			get
			{
				return default(DataFormat);
			}
			set
			{
			}
		}

		protected IDataHandler dataHandler
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected override IDataStore dataStore => null;

		protected virtual void SetInitialValues()
		{
		}

		protected override void OnInitialize()
		{
		}

		private void OnDataSourceChanged()
		{
		}
	}
}

#endif
