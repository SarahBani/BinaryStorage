namespace BinStorage.Common
{
    public static class Constant
    {

        #region AppSetting

        public const string AppSetting_StorageFileName = "StorageFileName";
        public const string AppSetting_CompressionThresholdInKB = "CompressionThresholdInKB";
        public const string AppSetting_IndexSizeLimitInKB = "IndexSizeLimitInKB";
        public const string AppSetting_StorageFileSizeLimitInKB = "StorageFileSizeLimitInKB";
        public const string AppSetting_MemoryFlushSizeLimitInKB = "MemoryFlushSizeLimitInKB";
        public const string AppSetting_MemoryCriticalSizeLimitInKB = "MemoryCriticalSizeLimitInKB";
        public const string AppSetting_CacheExpirationMinutes = "CacheExpirationMinutes";
        public const string AppSetting_CacheFetchCount = "CacheFetchCount";

        #endregion /AppSetting

        #region Exceptions

        public const string Exception_HasError = "An error has occured!";
        public const string Exception_NullKey = "Key is null!";
        public const string Exception_NullData = "Data is null!";
        public const string Exception_NullParameters = "Parameters is null!";
        public const string Exception_HashNotMatched = "The hash does not match!";
        public const string Exception_StreamLengthNotMatched = "The length does not match with the length of the stream data!";
        public const string Exception_NoDataRelatedToKey = "There is no data related to the specific key!";
        public const string Exception_DuplicateKey = "the key is duplicated!";
        public const string Exception_DuplicateData = "The data is duplicated!";
        public const string Exception_CorruptedData = "The data on storage file is corrupted!";
        public const string Exception_IndexSizeExceeded = "The index memory size is excedeed!";
        public const string Exception_StorageFileSizeExceeded = "The storage file size is excedeed!";
        public const string Exception_DiskSpaceExceeded = "The disk space is excedeed!";
        public const string Exception_OutOfMemory = "The system runs out of memory!";

        #endregion /Exceptions

    }
}
