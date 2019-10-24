using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using BinStorage.Common;

namespace BinStorage.Data
{
    /// <summary>
    /// This static class is used for storing the persistant data structure & operations related to it
    /// </summary>
    public static class StorageIndex
    {

        #region Properties

        /// <summary>
        /// used for ensure than Init method run only once
        /// </summary>
        private static bool _isInit;
        public static bool IsInit
        {
            get
            {
                if (!_isInit)
                {
                    lock (_locker)
                    {
                        if (!_isInit)
                        {
                            _isInit = true;
                            return false;
                        }
                    }
                }
                return _isInit;
            }
        }

        /// <summary>
        /// A thread-safe dictionary which is used as a persistant data structure
        /// </summary>
        private static readonly ConcurrentDictionary<string, (IndexInformation Information, IndexReference Reference)> _index = new ConcurrentDictionary<string, (IndexInformation, IndexReference)>();

        /// <summary>
        /// The last offset of the data which has added to the file
        /// </summary>
        private static int _lastOffset;

        private static readonly object _locker = new object();

        private static long _maxIndexSize = 0;

        #endregion /Properties

        #region Methods

        public static void SetMaxIndexSize(long size)
        {
            _maxIndexSize = size;
        }

        public static bool Contains(string key) => _index.ContainsKey(key);

        private static bool ContainsHash(byte[] hashedData) => _index.Values
            .Any(q => q.Information.HashedData != null && q.Information.HashedData.IsEqual(hashedData));

        public static void Add(string key) => _index.TryAdd(key, (new IndexInformation(), new IndexReference()));

        public static int SetContent(string key, int dataLength, byte[] hashedData, byte[] crc, bool isCompressed)
        {
            int offset = 0;
            int indexSize = 0;
            var content = Get(key);

            /// I want to set the current position for each Item on file,
            /// so I lock it to prevent the specific file bytes are assigned to more than one item & prevent overlaping 
            lock (_locker)
            {
                if (ContainsHash(hashedData))
                {
                    throw new CustomException(ExceptionKey.DuplicateData);
                }
                content.Information.Setup(hashedData, crc, isCompressed);
                offset = _lastOffset;
                _lastOffset += dataLength;
                content.Reference.Setup(offset, dataLength);
                indexSize = GetSize();
            }
            if (indexSize > _maxIndexSize)
            {
                throw new CustomException(ExceptionKey.IndexSizeExceeded);
            }
            return offset;
        }

        public static (IndexInformation Information, IndexReference Reference) Get(string key)
        {
            (IndexInformation, IndexReference) content = (null, null);
            if (Contains(key))
            {
                content = _index[key];
            }
            return content;
        }

        public static void Remove(string key)
        {
            if (Contains(key))
            {
                Get(key).Information.Remove();
                _index.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Get the size of the Index to check its size is not exceeded
        /// </summary>
        /// <returns></returns>
        private static int GetSize()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, _index);
                return (int)stream.Length;
            }
        }

        public static void CompleteStoring(string key)
        {
            var content = Get(key);
            content.Information.CompleteStoring();
        }

        #endregion /Methods

    }
}
