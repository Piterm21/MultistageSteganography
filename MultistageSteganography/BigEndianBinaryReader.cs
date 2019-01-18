using System;
using System.IO;

namespace MultistageSteganography
{
    class MemoryStreamWithTypedReads : MemoryStream
    {
        byte? currentByte = null;
        byte indexOfLastReturnedBitInCurrentByte = 0;
        byte[] tempBuffer = new byte[8];
        bool isLittleEndian = true;

        public MemoryStreamWithTypedReads (byte[] buffer) : base(buffer) { }

        private Int64 tempBufferToValue (int numberOfBytes)
        {
            Int64 result = 0;

            int shift = 56;

            if (isLittleEndian) {
                Array.Reverse(tempBuffer);
            }

            for (int i = 0; i < 8; i++) {
                result |= ((Int64)tempBuffer[i] << shift);
                shift -= 8;
            }

            if (!isLittleEndian) {
                result = result >> (64 - (numberOfBytes * 8));
            }

            return result;
        }

        public void toggleEndianess()
        {
            isLittleEndian = !isLittleEndian;
            Array.Reverse(tempBuffer);
        }

        public byte readByte ()
        {
            byte result = 0;

            Read(tempBuffer, 0, 1);
            result = (byte)tempBufferToValue(1);

            return result;
        }

        public UInt16 readUInt16 ()
        {
            UInt16 result = 0;

            Read(tempBuffer, 0, 2);
            result = (UInt16)tempBufferToValue(2);

            return result;
        }

        public UInt32 readUInt32 ()
        {
            UInt32 result = 0;

            Read(tempBuffer, 0, 4);
            result = (UInt32)tempBufferToValue(4);

            return result;
        }

        public UInt64 readUInt64 ()
        {
            UInt64 result = 0;

            Read(tempBuffer, 0, 8);
            result = (UInt64)tempBufferToValue(8);

            return result;
        }

        public byte lastByte ()
        {
            byte result = 0;

            result = (byte)tempBufferToValue(1);

            return result;
        }

        public UInt16 lastUInt16 ()
        {
            UInt16 result = 0;

            result = (UInt16)tempBufferToValue(2);

            return result;
        }

        public UInt32 lastUInt32 ()
        {
            UInt32 result = 0;

            result = (UInt32)tempBufferToValue(4);

            return result;
        }

        public UInt64 lastUInt64 ()
        {
            UInt64 result = 0;

            result = (UInt64)tempBufferToValue(8);

            return result;
        }

        public byte[] readBytes (int n)
        {
            byte[] result = new byte[n];

            Read(result, 0, n);

            if (isLittleEndian) {
                Array.Reverse(result);
            }

            return result;
        }

        public byte[] readBitsCorrectForNullMarker (byte amount)
        {
            byte[] result;

            if (amount > 0) {
                result = new byte[amount];
                byte bitsRead = 0;

                while (bitsRead < amount) {
                    if (currentByte == null) {
                        if (!this.peekForEndOfImageMarker()) {
                            currentByte = readByte();

                            if (currentByte == 0xFF) {
                                byte temp = readByte();

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

        public bool checkForEndOfImageMarker ()
        {
            bool result = false;
            byte[] temp = new byte[2];
            Read(temp, 0, 2);
            Seek(-2, SeekOrigin.Current);

            if ((UInt16)(temp[0] << 8 | temp[1]) == 0xFFD9) {
                this.Read(temp, 0, 2);
                result = true;
            }

            return result;
        }

        public bool peekForEndOfImageMarker ()
        {
            bool result = false;
            byte[] temp = new byte[2];
            Read(temp, 0, 2);
            Seek(-2, SeekOrigin.Current);

            if ((UInt16)(temp[0] << 8 | temp[1]) == 0xFFD9) {
                result = true;
            }

            return result;
        }
    }
}
