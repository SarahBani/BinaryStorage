using System;
using BinStorage.Common;

namespace BinStorage.Data
{
    /// <summary>
    /// This class is used for this Index reference of the binary storage data
    /// </summary>
    [Serializable] // made this serializable in order to be able to estemiate the size
    public class IndexReference
    {

        #region Properties

        /// <summary>
        /// a pair (?) of byte offset
        /// </summary>
        public byte[] Offset { get; private set; }

        /// <summary>
        /// size of data also in bytes
        /// </summary>
        public byte[] Size { get; private set; }

        #endregion /Properties

        #region Constructors

        public IndexReference()
        {

        }

        #endregion /Constructors

        #region Methods

        public void Setup(int offset, int dataLength)
        {
            var convertor = new Convertor();
            Offset = convertor.ConvertIntToByteArray(offset);
            Size = convertor.ConvertIntToByteArray(dataLength);
        }

        #endregion /Methods

    }
}
