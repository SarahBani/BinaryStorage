using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BinStorage.Common;
using BinStorage.Data;

namespace BinStorage
{
    public class BinaryStorage : IBinaryStorage
    {

        #region Properties

        private readonly StorageConfiguration _configuration;

        private readonly string _storageFilePath;

        private Lazy<MemoryManager> _memoryManager = new Lazy<MemoryManager>(() => new MemoryManager());

        private int _fileCounter = 0;

        private int _writtenFilesCounter = 0;

        private int _readFilesCounter = 0;

        #endregion /Properties

        #region Constructors

        public BinaryStorage(StorageConfiguration configuration)
        {
            /// Theses values must be set in TestApp project, 
            /// but I didn't want to change it because it was mentioned not to change the test
            configuration.MaxIndexFile = int.Parse(Utility.GetAppSetting(Constant.AppSetting_IndexSizeLimitInKB)) * 1024;
            configuration.MaxStorageFile = int.Parse(Utility.GetAppSetting(Constant.AppSetting_StorageFileSizeLimitInKB)) * 1024;

            _configuration = configuration;
            _storageFilePath = GetStorageFileName(_configuration.WorkingFolder);
            if (!StorageIndex.IsInit) // it is the first execution
            {
                Init();
            }
        }

        #endregion /Constructors

        #region Methods

        public void Add(string key, Stream data, StreamInfo parameters)
        {
            try
            {
                int fileNo = ++_fileCounter;
                Utility.ConsoleWriteLine($"Start adding file no: {fileNo}");
                data.Dispose(); // Because this stream disposes quickly, I don't want it!

                if (key is null)
                {
                    throw new CustomException(ExceptionKey.NullKey);
                }
                if (data is null)
                {
                    throw new CustomException(ExceptionKey.NullData);
                }
                if (parameters is null)
                {
                    throw new CustomException(ExceptionKey.NullParameters);
                }
                if (StorageIndex.Contains(key))
                {
                    throw new CustomException(ExceptionKey.DuplicateKey);
                }
                if (parameters.Length.HasValue && parameters.Length != data.Length)
                {
                    throw new CustomException(ExceptionKey.StreamLengthNotMatched);
                }
                StorageIndex.Add(key);

                SetupWrite(key, fileNo, parameters).ConfigureAwait(false);
            }
            catch (CustomException ex)
            {
                switch (ex.ExceptionKey)
                {
                    case ExceptionKey.NullKey:
                    case ExceptionKey.NullData:
                    case ExceptionKey.NullParameters:
                        ///  key is null, data is null, parameters is null
                        throw new ArgumentNullException();
                    case ExceptionKey.StreamLengthNotMatched:
                    case ExceptionKey.DuplicateKey:
                        /// An element with the same key already exists or
                        /// provided hash or length does not match the data.
                        throw new ArgumentException();
                    default:
                        throw new Exception();
                }
            }
            catch (Exception)
            {
                //throw new CustomException(ExceptionKey.Unknown);
                throw;
            }
        }

        public Stream Get(string key)
        {
            int fileNo = ++_fileCounter;
            Utility.ConsoleWriteLine($"Start getting file no: {fileNo}");
            _memoryManager.Value.OptimizeMemoryConsumption();
            try
            {
                return SetupReadAsync(key, fileNo).Result;
            }
            catch (Exception)
            {
                //throw new CustomException(ExceptionKey.Unknown);
                throw;
            }
            finally
            { // call it before & after reading had better results in my tests
                _memoryManager.Value.OptimizeMemoryConsumption();
            }
        }

        public bool Contains(string key)
        {
            return StorageIndex.Contains(key);
        }

        public void Dispose()
        {
            _memoryManager.Value.FlushMemory();
        }

        /// <summary>
        /// initialize at first time
        /// </summary>
        private void Init()
        {
            StorageIndex.SetMaxIndexSize(_configuration.MaxIndexFile);
            // if the file has remained from the previous execution, it should be deleted
            // because its length may be larger then the current data
            if (File.Exists(_storageFilePath))
            {
                File.Delete(_storageFilePath);
            }
            File.Create(_storageFilePath);
            _memoryManager.Value.FlushMemory(); // to prevent occasionally file is being used by another process error
        }

        private string GetStorageFileName(string workingFolder)
        {
            string storageFileName = Utility.GetAppSetting(Constant.AppSetting_StorageFileName);
            return Path.Combine(workingFolder, storageFileName);
        }

        public async Task SetupWrite(string key, int fileNo, StreamInfo parameters)
        {
            Utility.ConsoleWriteLine($"Start SetupWrite file: {fileNo}");
            var cancellationTokenSource = new CancellationTokenSource();
            try
            {
                var convertor = new Convertor();
               
                var streamCompress = new FileStream(key, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                int inputSize = (int)streamCompress.Length;
                bool isCompressionNeeded = IsCompressionNeeded(parameters.IsCompressed, inputSize);
                byte[] storageData = null;
                byte[] crc = null;
                int dataLength = 0;
                Utility.ConsoleWriteLine($"Start Compressing file: {fileNo}");
                var taskCompress = (isCompressionNeeded ? convertor.CompressAsync(streamCompress) : convertor.ConvertStreamToByteArrayAsync(streamCompress))
                     .ContinueWith(action =>
                     {
                         streamCompress.Dispose();
                         if (action.IsFaulted)
                         {
                             foreach (var ex in (action.Exception as AggregateException).InnerExceptions)
                             {
                                 throw ex;
                             }
                         }
                         storageData = action.Result;
                         Utility.ConsoleWriteLine($"End Compressing file: {fileNo}");
                         crc = new Crc16().ComputeChecksumBytes(storageData);
                         dataLength = storageData.Length;
                     });

                byte[] hashedData = null;
                // I used 2 different streams to prevent conflict in opertaions
                var streamMD5 = new FileStream(key, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                Utility.ConsoleWriteLine($"Start Md5 file: {fileNo}");
                var taskMd5 = convertor.GetMd5HashAsync(streamMD5)
                    .ContinueWith(action =>
                    {
                        streamMD5.Dispose();
                        if (action.IsFaulted)
                        {
                            foreach (var ex in (action.Exception as AggregateException).InnerExceptions)
                            {
                                throw ex;
                            }
                        }
                        hashedData = action.Result;
                        Utility.ConsoleWriteLine($"End Md5 file: {fileNo}");
                        if (parameters.Hash != null && !parameters.Hash.IsEqual(hashedData))
                        {
                            throw new CustomException(ExceptionKey.HashNotMatched);
                        }
                    });

                await Task.WhenAll(taskCompress, taskMd5);

                int offset = StorageIndex.SetContent(key, dataLength, hashedData, crc, isCompressionNeeded);
                Utility.ConsoleWriteLine($"Start WriteFileAsync file: {fileNo}");
                var _ = WriteFileAsync(storageData, offset, cancellationTokenSource.Token)
                .ContinueWith(action =>
                {
                    if (action.IsFaulted)
                    {
                        foreach (var ex in (action.Exception as AggregateException).InnerExceptions)
                        {
                            throw ex;
                        }
                    }
                    StorageIndex.CompleteStoring(key);
                    Utility.ConsoleWriteLine($"End WriteFileAsync file: {fileNo} - Writen Files Count: {++_writtenFilesCounter}");
                })
                .ConfigureAwait(false); // I do not want it to wait

                if (_configuration.MaxStorageFile > 0 && offset + dataLength > _configuration.MaxStorageFile) // if the file will exceed the max size
                {
                    throw new CustomException(ExceptionKey.StorageFileSizeExceeded);
                }
            }
            catch (CustomException ex)
            {
                StorageIndex.Remove(key);
                cancellationTokenSource.Cancel();
                switch (ex.ExceptionKey)
                {
                    case ExceptionKey.HashNotMatched:
                    case ExceptionKey.DuplicateData:
                        /// An element with the same key already exists or
                        /// provided hash or length does not match the data.
                        throw new ArgumentException();
                    case ExceptionKey.IndexSizeExceeded:
                    case ExceptionKey.StorageFileSizeExceeded:
                        ///  I/O exception occurred during persisting data
                        throw new IOException();
                    case ExceptionKey.Unknown:
                    default:
                        throw new Exception();
                }
            }
            catch (Exception ex)
            {
                StorageIndex.Remove(key);
                cancellationTokenSource.Cancel();
                //throw new CustomException(ExceptionKey.Unknown);
                throw;
            }
            finally
            {
                _memoryManager.Value.OptimizeMemoryConsumption();
            }
        }

        private bool IsCompressionNeeded(bool hasAlreadyCompressed, int streamDataLength)
        {
            return (!hasAlreadyCompressed &&
                 streamDataLength > int.Parse(Utility.GetAppSetting(Constant.AppSetting_CompressionThresholdInKB)));
        }

        private async Task WriteFileAsync(byte[] data, int offset, CancellationToken token)
        {
            using (FileStream fileStream = new FileStream(_storageFilePath, FileMode.Open, FileAccess.Write,
                FileShare.ReadWrite, bufferSize: 4096, useAsync: true))
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                await fileStream.WriteAsync(data, 0, data.Length, token)
                    .ConfigureAwait(false);
            }
        }

        private async Task<Stream> SetupReadAsync(string key, int fileNo)
        {
            try
            {
                Utility.ConsoleWriteLine($"Start SetupReadAsync file: {fileNo}");

                if (!StorageIndex.Contains(key))
                {
                    throw new CustomException(ExceptionKey.NoDataRelatedToKey);
                }
                byte[] byteArrayData = null;
                var (indexInformation, indexReference) = StorageIndex.Get(key);
                var convertor = new Convertor();

                if (indexInformation.Data != null) // Use the cached data
                {
                    byteArrayData = indexInformation.Data;
                }
                else
                {
                    if (!indexInformation.IsStoringCompleted) // Check if index has not setup or data has already written on the file 
                    { /// If data has not already written, wait for it
                        var cancellationTokenSource = new CancellationTokenSource();
                        var taskWriteCompleted = IsWriteCompleted(indexInformation, cancellationTokenSource.Token);
                        var taskIsRemoved = IsRemoved(indexInformation, cancellationTokenSource.Token);
                        await Task.WhenAny(taskWriteCompleted, taskIsRemoved).ContinueWith(action=> {
                            if (action.IsFaulted)
                            {
                                foreach (var ex in (action.Exception as AggregateException).InnerExceptions)
                                {
                                    throw ex;
                                }
                            }
                        });
                        cancellationTokenSource.Cancel();
                        if (taskWriteCompleted.IsCanceled)
                        { /// Item is removed, so can't read anything
                            return null;
                        }
                        (indexInformation, indexReference) = StorageIndex.Get(key); // It may not filled already
                    }

                    int offset = convertor.ConvertByteArrayToInt(indexReference.Offset);
                    int size = convertor.ConvertByteArrayToInt(indexReference.Size);
                    Utility.ConsoleWriteLine($"Start ReadFileAsync file: {fileNo}");
                    byteArrayData = await ReadFileAsync(offset, size);
                    Utility.ConsoleWriteLine($"End ReadFileAsync file: {fileNo}");
                    var crc = new Crc16().ComputeChecksumBytes(byteArrayData);
                    if (!crc.IsEqual(indexInformation.CRC))
                    {
                        throw new CustomException(ExceptionKey.CorruptedData);
                    }
                    CheckCaching(indexInformation, byteArrayData);
                }
                var streamData = (indexInformation.IsCompressed ? convertor.DecompressAsync(byteArrayData).Result : convertor.ConvertByteArrayToStream(byteArrayData));
                Utility.ConsoleWriteLine($"End reading file: {fileNo} - Read Files Count: {++_readFilesCounter}");
                return streamData;
            }
            catch (CustomException ex)
            {
                switch (ex.ExceptionKey)
                {
                    case ExceptionKey.NullKey:
                        ///  key is null.
                        throw new ArgumentNullException();
                    case ExceptionKey.NoDataRelatedToKey:
                        ///  key does not exist
                        throw new KeyNotFoundException();
                    case ExceptionKey.CorruptedData:
                    case ExceptionKey.Unknown:
                    default:
                        throw new Exception();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<byte[]> ReadFileAsync(int offset, int size)
        {
            byte[] byteArrayData = new byte[size];
            using (FileStream fileStream = new FileStream(_storageFilePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite, bufferSize: 4096, useAsync: true))
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                await fileStream.ReadAsync(byteArrayData, 0, size);
            }
            return byteArrayData;
        }

        private void CheckCaching(IndexInformation indexInformation, byte[] data)
        {
            byte.TryParse(Utility.GetAppSetting(Constant.AppSetting_CacheFetchCount), out byte cacheFetchCount);
            if (cacheFetchCount > 0 && indexInformation.FetchTimes >= cacheFetchCount)
            {
                Utility.ConsoleWriteLine($"StartCaching FetchTimes: {indexInformation.FetchTimes}");
                indexInformation.Cache(data);
                Utility.ConsoleWriteLine($"EndCaching FetchTimes: {indexInformation.FetchTimes}");
            }
            else
            {
                indexInformation.IncreaseFetchTimes();
            }
        }

        /// <summary>
        /// When there is a request for reading an item which hasn't written yet
        /// it should wait until the process of writing finishes
        /// </summary>
        /// <param name="indexContent"></param>
        /// <returns></returns>
        private async Task<bool> IsWriteCompleted(IndexInformation indexInformation, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler handler = null;
            indexInformation.CompleteStoringHandler += handler = (sender, e) =>
            {
                indexInformation.CompleteStoringHandler -= handler; // unsubscribe event   
                tcs.SetResult(true);
            };
            token.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    indexInformation.CompleteStoringHandler -= handler; // unsubscribe event  
                    tcs.SetCanceled();
                }
            });
            return await tcs.Task;
        }

        /// <summary>
        /// When there is a request for reading an item which hasn't written yet,
        /// but the item has removed because of an exception
        /// the thread should not wait
        /// </summary>
        /// <param name="indexContent"></param>
        /// <returns></returns>
        private async Task<bool> IsRemoved(IndexInformation indexInformation, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler handler = null;
            indexInformation.RemoveHandler += handler = (sender, e) =>
            {
                indexInformation.RemoveHandler -= handler; // unsubscribe event   
                tcs.SetResult(true);
            };
            token.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    indexInformation.RemoveHandler -= handler; // unsubscribe event  
                    tcs.SetCanceled();
                }
            });
            return await tcs.Task;
        }

        #endregion /Methods

    }
}
