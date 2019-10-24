using System;

namespace BinStorage.Common
{

    public enum ExceptionKey
    {
        Unknown,
        NullKey,
        NullData,
        NullParameters,
        HashNotMatched,
        StreamLengthNotMatched,
        NoDataRelatedToKey,
        DuplicateKey,
        DuplicateData,
        CorruptedData,
        IndexSizeExceeded,
        StorageFileSizeExceeded,
        DiskSpaceExceeded,
        OutOfMemory,
    }

    /// <summary>
    /// This class is used for handling custom exceptions & displaying appropriate messages
    /// </summary>
    public class CustomException : Exception
    {

        #region Properties

        public ExceptionKey ExceptionKey { get; private set; }

        public string CustomMessage { get; private set; }

        #endregion /Properties

        #region Constructors

        public CustomException(ExceptionKey exceptionKey, params object[] args)
        {
            ExceptionKey = exceptionKey;
            CustomMessage = string.Format(GetMessage(), args);
        }

        public CustomException(string message)
        {
            CustomMessage = message;
        }

        #endregion /Constructors

        #region Methods

        private string GetMessage()
        {
            switch (ExceptionKey)
            {
                case ExceptionKey.NullKey:
                    return Constant.Exception_NullKey;
                case ExceptionKey.NullData:
                    return Constant.Exception_NullData;
                case ExceptionKey.NullParameters:
                    return Constant.Exception_NullParameters;
                case ExceptionKey.HashNotMatched:
                    return Constant.Exception_HashNotMatched;
                case ExceptionKey.StreamLengthNotMatched:
                    return Constant.Exception_StreamLengthNotMatched;
                case ExceptionKey.NoDataRelatedToKey:
                    return Constant.Exception_NoDataRelatedToKey;
                case ExceptionKey.DuplicateKey:
                    return Constant.Exception_DuplicateKey;
                case ExceptionKey.DuplicateData:
                    return Constant.Exception_DuplicateData;
                case ExceptionKey.CorruptedData:
                    return Constant.Exception_CorruptedData;
                case ExceptionKey.IndexSizeExceeded:
                    return Constant.Exception_IndexSizeExceeded;
                case ExceptionKey.StorageFileSizeExceeded:
                    return Constant.Exception_StorageFileSizeExceeded;
                case ExceptionKey.DiskSpaceExceeded:
                    return Constant.Exception_DiskSpaceExceeded;
                case ExceptionKey.OutOfMemory:
                    return Constant.Exception_OutOfMemory;
                case ExceptionKey.Unknown:
                default:
                    return Constant.Exception_HasError;
            }
        }

        #endregion /Methods

    }
}
