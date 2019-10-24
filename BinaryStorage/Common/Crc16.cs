using System;

namespace BinStorage.Common
{
    public class Crc16
    {

        #region Properties

        private const ushort _polynomial = 0xA001;

        private ushort[] _table = new ushort[256];

        #endregion /Properties

        #region Constructors

        public Crc16()
        {
            _table = new ushort[256];
            ushort value;
            ushort temp;
            for (ushort i = 0; i < _table.Length; ++i)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ _polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                _table[i] = value;
            }
        }

        #endregion /Constructors

        #region Methods

        private ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ _table[index]);
            }
            return crc;
        }

        public byte[] ComputeChecksumBytes(byte[] bytes)
        {
            ushort crc = ComputeChecksum(bytes);
            return BitConverter.GetBytes(crc);
        }

        #endregion /Methods

    }
}
