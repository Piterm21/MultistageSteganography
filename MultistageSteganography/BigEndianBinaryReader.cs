using System;
using System.IO;


namespace MultistageSteganography
{
    class BinaryBitReader : BinaryReader
    {
        public BinaryBitReader (System.IO.Stream stream) : base(stream) { }
        byte? currentByte = null;
        byte indexOfLastReturnedBitInCurrentByte = 0;

        public byte[] readBitsCorrectForNullMarker(byte amount)
        {
            byte[] result;

            if (amount > 0) {
                result = new byte[amount];
                byte bitsRead = 0;

                while (bitsRead < amount) {
                    if (currentByte == null) {
                        if (!this.peekForEndOfImageMarker()) {
                            currentByte = this.ReadByte();

                            if (currentByte == 0xFF) {
                                byte temp = this.ReadByte();

                                if (temp != 0x00) {
                                    throw new Exception("unexpected marker in scan data");
                                }
                            }
                        } else {
                            amount = bitsRead;
                        }
                    } else {
                        while (indexOfLastReturnedBitInCurrentByte < 8 && (bitsRead < amount)) {
                            result[bitsRead] = (byte)((((currentByte << indexOfLastReturnedBitInCurrentByte) & 0x80) > 0) ? 1 : 0);
                            bitsRead++;
                            indexOfLastReturnedBitInCurrentByte++;
                        }

                        if (indexOfLastReturnedBitInCurrentByte == 8) {
                            indexOfLastReturnedBitInCurrentByte = 0;
                            currentByte = null;
                        }
                    }
                }
            } else {
                result = new byte[0];
            }

            return result;
        }

        public bool checkForEndOfImageMarker()
        {
            bool result = false;
            byte[] temp = new byte[2];
            this.BaseStream.Read(temp, 0, 2);
            this.BaseStream.Seek(-2, SeekOrigin.Current);

            if ((UInt16)(temp[0] << 8 | temp[1]) == 0xFFD9) {
                this.ReadBytes(2);
                result = true;
            }

            return result;
        }

        public bool peekForEndOfImageMarker()
        {
            bool result = false;
            byte[] temp = new byte[2];
            this.BaseStream.Read(temp, 0, 2);
            this.BaseStream.Seek(-2, SeekOrigin.Current);

            if ((UInt16)(temp[0] << 8 | temp[1]) == 0xFFD9) {
                result = true;
            }

            return result;
        }
    }

    class BigEndianBinaryReader : BinaryBitReader
    {
        public BigEndianBinaryReader (System.IO.Stream stream) : base(stream) { }

        public override ulong ReadUInt64()
        {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        public override uint ReadUInt32()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public override ushort ReadUInt16()
        {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }
    }
}
