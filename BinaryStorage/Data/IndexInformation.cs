using System;

namespace BinStorage.Data
{
    /// <summary>
    /// This class is used for other information & properties of the Index 
    /// </summary>
    [Serializable] // made this serializable in order to be able to estemiate the size
    public class IndexInformation
    {

        #region Properties

        public byte[] HashedData { get; private set; }

        public byte[] CRC { get; private set; }

        public bool IsCompressed { get; private set; }

        /// <summary>
        /// Indicates whether or not writing on disk is completed
        /// used for waiting for writing completion before reading the data
        /// </summary>
        public bool IsStoringCompleted { get; private set; }

        /// <summary>
        /// Restoring the times of fetching in order to use for caching frequently used data
        /// </summary>
        public byte FetchTimes { get; private set; }

        /// <summary>
        /// Cached frequently used data
        /// </summary>
        public byte[] Data { get; private set; }

        #endregion /Properties

        #region Events

        /// used for announcing the data writing has completed & can be read
        [field: NonSerialized] // enable us to unsubscribe assigned EventHandler
        public event EventHandler CompleteStoringHandler;

        /// used for announcing the data has removed before writing has completed because of an exception
        /// so if a thread is waiting for the completion of writing, cancel the thread
        [field: NonSerialized] // enable us to unsubscribe assigned EventHandler
        public event EventHandler RemoveHandler;

        #endregion /Events

        #region Constructors

        public IndexInformation()
        {
        }

        #endregion /Constructors

        #region Methods     

        public void Setup(byte[] hashedData, byte[] crc, bool isCompressed)
        {
            HashedData = hashedData;
            CRC = crc;
            IsCompressed = isCompressed;
        }

        /// <summary>
        /// Set IsCompleted true &
        /// raise the CompeteHandler event if it has already asigned
        /// it is useful when reading this data is requested before the end of writing
        /// it announces that data has written & can be read
        /// </summary>
        public void CompleteStoring()
        {
            IsStoringCompleted = true;
            CompleteStoringHandler?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// raise the RemoveHandler event if it has already asigned
        /// it is useful when reading this data is requested before the end of writing,
        /// but because of an exception, the data is removed
        /// if a thread waits for completion of writing, cancel the thread
        /// </summary>
        public void Remove()
        {
            RemoveHandler?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Increase the number of fetches in order to use for caching frequently used data
        /// </summary>
        public void IncreaseFetchTimes() => FetchTimes += 1;

        /// <summary>
        /// Cache frequently used data
        /// </summary>
        /// <param name="data"></param>
        public void Cache(byte[] data)
        {
            Data = data;
        }

        #endregion /Methods

    }
}
