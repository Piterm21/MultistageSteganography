using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace MultistageSteganography
{
    public struct BmpHeader
    {
        public UInt16 signature;
        public UInt32 fileSize;
        public UInt32 reserved;
        public UInt32 dataOffset;
    }

    public struct InfoHeader
    {
        public UInt32 size;
        public UInt32 width;
        public UInt32 height;
        public UInt16 planes;
        public UInt16 bitsPerPixel;
        public UInt32 compresion;
        public UInt32 imageSize;
        public UInt32 xPixelsPerM;
        public UInt32 yPixelsPerM;
        public UInt32 colorsUsed;
        public UInt32 importantColors;
    }

    public struct BmpFileHeaders
    {
        public BmpHeader header;
        public InfoHeader infoHeader;
    }

    public struct JpgComponent
    {
        public byte componentSelector;

        public byte repeatCount;
        public byte DCtableSelector;
        public UInt16 ACtableSelector;
    }

    public struct JpgHeaders
    {
        public bool fileOk;
        public int imageDataStartOffsetInBytes;
        public bool isLittleEndian;

        public Dictionary<byte, HuffmanCodesBinaryTree> huffmanCodesBinaryTrees;
        public Dictionary<string, Int16> codeToSignedValueDictionary;
        public Dictionary<Int16, string> signedValueToCodeDictionary;

        public JpgComponent[] jpgComponents;
    }

    public struct JpgMarkers
    {
        public const UInt16 startOfImage = 0xFFD8;
        public const UInt16 startOfFirstHeader = 0xFFE0;
        public const UInt16 startOfLastHeader = 0xFFED;

        //Image data start headers
        public const UInt16 startOfFrameBaselineDCT = 0xFFC0;
        public const UInt16 startOfFrameProgressiveDCT = 0xFFC2;
        public const UInt16 startOfHuffmanTables = 0xFFC4;

        public const UInt16 startOfFirstRestart = 0xFFD0;
        public const UInt16 startOfLastRestart = 0xFFD7;

        public const UInt16 startOfScan = 0xFFDA;
        public const UInt16 startOfQuantizationTables = 0xFFDB;
        public const UInt16 startOfRestartInterval = 0xFFDD;
        //End of image data starting headers

        public const UInt16 endOfImage = 0xFFD9;
    }

    public class HuffmanCodesBinaryTree
    {
        public byte maxCodeLength;
        public UInt32 inFileLength;
        public Dictionary<byte, Stack<bool>> valueToHuffmanCodeDictionary;
        public HackBinaryTree binaryTree;

        public HuffmanCodesBinaryTree()
        {
            valueToHuffmanCodeDictionary = new Dictionary<byte, Stack<bool>>();
            binaryTree = new HackBinaryTree();
        }
    }

    public struct CodepointValueAndOverflowLength
    {
        public byte value;
        public UInt16 overflowLength;
    }

    public class HackBinaryTree
    {
        public HackBinaryTreeElement root;
        public Queue<HackBinaryTreeElement> freeNodesAtCurrentDepth;

        public HackBinaryTree()
        {
            freeNodesAtCurrentDepth = new Queue<HackBinaryTreeElement>();

            root = new HackBinaryTreeElement();
            root.parent = null;

            root.left = new HackBinaryTreeElement();
            root.left.isLeft = true;
            root.left.parent = root;

            root.right = new HackBinaryTreeElement();
            root.right.isLeft = false;
            root.right.parent = root;

            freeNodesAtCurrentDepth.Enqueue(root.left);
            freeNodesAtCurrentDepth.Enqueue(root.right);
        }

        public void addValue (byte value)
        {
            freeNodesAtCurrentDepth.Dequeue().value = value;
        }

        public void addDepth ()
        {
            Queue<HackBinaryTreeElement> tempFreeNodesAtCurrentDepth = new Queue<HackBinaryTreeElement>();

            while (freeNodesAtCurrentDepth.Count != 0) {
                HackBinaryTreeElement currentNode = freeNodesAtCurrentDepth.Dequeue();

                currentNode.left = new HackBinaryTreeElement();
                currentNode.left.isLeft = true;
                currentNode.left.parent = currentNode;

                currentNode.right = new HackBinaryTreeElement();
                currentNode.right.isLeft = false;
                currentNode.right.parent = currentNode;

                tempFreeNodesAtCurrentDepth.Enqueue(currentNode.left);
                tempFreeNodesAtCurrentDepth.Enqueue(currentNode.right);
            }

            freeNodesAtCurrentDepth = tempFreeNodesAtCurrentDepth;
        }

        public CodepointValueAndOverflowLength findFirstMatchingCodepoint(byte[] codepoint)
        {
            CodepointValueAndOverflowLength result;
            HackBinaryTreeElement currentElement = root;
            int codepointIndex = 0;

            while (currentElement.left != null && currentElement.right != null) {
                if (codepoint[codepointIndex] == 0) {
                    currentElement = currentElement.left;
                } else {
                    currentElement = currentElement.right;
                }

                codepointIndex++;
            }

            result.value = currentElement.value;
            result.overflowLength = (UInt16)(codepoint.Length - codepointIndex);

            return result;
        }
    }

    public class HackBinaryTreeElement
    {
        public byte value;
        public bool isLeft;
        public HackBinaryTreeElement parent;
        public HackBinaryTreeElement left;
        public HackBinaryTreeElement right;

        public Stack<bool> unwind()
        {
            Stack<bool> result = new Stack<bool>();

            HackBinaryTreeElement currentElement = this;

            while (currentElement.parent != null) {
                result.Push(!currentElement.isLeft);
                currentElement = currentElement.parent;
            }

            return result;
        }
    }

    public struct DCTtable
    {
        public byte DCidentifier;
        public byte ACidentifier;

        public DCTvalue[] table;
    }

    public struct DCTvalue
    {
        public UInt16 originalCodedLength;
        public Int16 value;
    }

    public struct JpgFileStatistics
    {
        public List<DCTtable> decodedDCTtables;
        public long startOfScanOffset;
        public long endOfScanOffset;
        public JpgHeaders jpgFileHeaders;
    }

    enum FileType { NONE, JPEG, BMP };

    class FileFormatHelpers
    {
        public static BmpFileHeaders readBmpFileHeaders (byte[] fileBytes)
        {
            BmpFileHeaders bmpFileHeaders = new BmpFileHeaders();
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(fileBytes));

            bmpFileHeaders.header.signature = binaryReader.ReadUInt16();
            bmpFileHeaders.header.fileSize = binaryReader.ReadUInt32();
            bmpFileHeaders.header.reserved = binaryReader.ReadUInt32();
            bmpFileHeaders.header.dataOffset = binaryReader.ReadUInt32();

            bmpFileHeaders.infoHeader.size = binaryReader.ReadUInt32();
            bmpFileHeaders.infoHeader.width = binaryReader.ReadUInt32();
            bmpFileHeaders.infoHeader.height = binaryReader.ReadUInt32();
            bmpFileHeaders.infoHeader.planes = binaryReader.ReadUInt16();
            bmpFileHeaders.infoHeader.bitsPerPixel = binaryReader.ReadUInt16();
            bmpFileHeaders.infoHeader.compresion = binaryReader.ReadUInt32();
            bmpFileHeaders.infoHeader.imageSize = binaryReader.ReadUInt32();
            bmpFileHeaders.infoHeader.xPixelsPerM = binaryReader.ReadUInt32();
            bmpFileHeaders.infoHeader.yPixelsPerM = binaryReader.ReadUInt32();
            bmpFileHeaders.infoHeader.colorsUsed = binaryReader.ReadUInt32();
            bmpFileHeaders.infoHeader.importantColors = binaryReader.ReadUInt32();

            return bmpFileHeaders;
        }

        public static string binaryToString(Int16 value, byte expectedLength)
        {
            string valueInString = "";

            for (int currentLength = 0; currentLength < expectedLength; currentLength++) {
                if (((value >> currentLength) & 1) == 1) {
                    valueInString = "1" + valueInString;
                } else {
                    valueInString = "0" + valueInString;
                }
            }

            return valueInString;
        }

        public static bool areFileBytesJpeg(ref byte[] fileBytes)
        {
            bool result = false;

            BinaryBitReader binaryReader = new BinaryBitReader(new MemoryStream(fileBytes));
            UInt16 readMarker = binaryReader.ReadUInt16();
            bool firstPass = true;

            if (readMarker != JpgMarkers.startOfImage && firstPass) {
                firstPass = false;
                binaryReader = new BigEndianBinaryReader(new MemoryStream(fileBytes));
                readMarker = binaryReader.ReadUInt16();

                if (readMarker == JpgMarkers.startOfImage) {
                    result = true;
                }
            } else {
                result = true;
            }

            return result;
        }

        public static JpgHeaders readJpgFileHeader (ref byte[] fileBytes)
        {
            JpgHeaders jpgFileHeaders = new JpgHeaders();
            jpgFileHeaders.fileOk = true;
            jpgFileHeaders.imageDataStartOffsetInBytes = 2;
            jpgFileHeaders.isLittleEndian = true;

            jpgFileHeaders.codeToSignedValueDictionary = new Dictionary<string, Int16>();
            jpgFileHeaders.codeToSignedValueDictionary.Add("", 0);
            jpgFileHeaders.signedValueToCodeDictionary = new Dictionary<Int16, string>();
            jpgFileHeaders.signedValueToCodeDictionary.Add(0, "");

            jpgFileHeaders.huffmanCodesBinaryTrees = new Dictionary<byte, HuffmanCodesBinaryTree>();

            Int16 rangeMin = -2047;
            Int16 rangeMax = 2047;

            Int16 currentStartingMinValue = -1;
            Int16 currentMinValue = 0;
            Int16 currentMaxValue = 0;
            byte codeLength = 1;

            while (currentMinValue > rangeMin && currentMaxValue < rangeMax) {
                Int16 numberOfValuesInCurrentHalfIteration = (Int16)(1 << (codeLength - 1));
                Int16 currentValueIteration = 0;

                while (currentValueIteration < numberOfValuesInCurrentHalfIteration) {
                    currentMinValue = (Int16)(currentStartingMinValue + currentValueIteration);
                    jpgFileHeaders.codeToSignedValueDictionary.Add(binaryToString(currentValueIteration, codeLength), currentMinValue);
                    jpgFileHeaders.signedValueToCodeDictionary.Add(currentMinValue, binaryToString(currentValueIteration, codeLength));
                    currentValueIteration++;
                }

                currentValueIteration = 0;

                while (currentValueIteration < numberOfValuesInCurrentHalfIteration) {
                    currentMaxValue = (Int16)(numberOfValuesInCurrentHalfIteration + currentValueIteration);
                    jpgFileHeaders.codeToSignedValueDictionary.Add(binaryToString((Int16)(numberOfValuesInCurrentHalfIteration + currentValueIteration), codeLength), currentMaxValue);
                    jpgFileHeaders.signedValueToCodeDictionary.Add(currentMaxValue, binaryToString((Int16)(numberOfValuesInCurrentHalfIteration + currentValueIteration), codeLength));
                    currentValueIteration++;
                }

                currentStartingMinValue -= (Int16)(1 << (codeLength));
                codeLength++;
            }

            BinaryBitReader binaryReader = new BinaryBitReader(new MemoryStream(fileBytes));
            UInt16 readMarker;
            bool firstPass = true;
            bool dataNotStarted = true;

            while (((readMarker = binaryReader.ReadUInt16()) != JpgMarkers.endOfImage) && jpgFileHeaders.fileOk && dataNotStarted) {
                dataNotStarted = readMarker != JpgMarkers.startOfScan;

                if (dataNotStarted) {
                    switch (readMarker) {
                        case (JpgMarkers.startOfHuffmanTables): {
                            UInt16 expectedTablesLength = binaryReader.ReadUInt16();
                            jpgFileHeaders.imageDataStartOffsetInBytes += 2 + expectedTablesLength;

                            for (UInt64 readBytes = 2; readBytes < expectedTablesLength; readBytes++) {
                                UInt32 currentTableLength = 0;
                                byte classAndIndetifierByte = binaryReader.ReadByte();
                                currentTableLength += 8;
                                readBytes++;
                                HuffmanCodesBinaryTree huffmanCodesBinaryTree = new HuffmanCodesBinaryTree();
                                ref HackBinaryTree currentBinaryTree = ref huffmanCodesBinaryTree.binaryTree;
                                byte maxCodeLength = 0;

                                byte[] extractedHuffmanCodeCounts = new byte[16];

                                for (int lengthOfCode = 1; lengthOfCode <= 16; lengthOfCode++) {
                                    extractedHuffmanCodeCounts[lengthOfCode - 1] = binaryReader.ReadByte();
                                    currentTableLength += 8;
                                    readBytes++;
                                }

                                for (int lengthOfCode = 1; lengthOfCode <= 16; lengthOfCode++) {
                                    for (int codeIndex = 0; codeIndex < extractedHuffmanCodeCounts[lengthOfCode - 1]; codeIndex++) {
                                        maxCodeLength = (byte)lengthOfCode;

                                        HackBinaryTreeElement nextElement = currentBinaryTree.freeNodesAtCurrentDepth.Peek();
                                        byte value = binaryReader.ReadByte();

                                        huffmanCodesBinaryTree.valueToHuffmanCodeDictionary.Add(value, nextElement.unwind());
                                        currentBinaryTree.addValue(value);

                                        currentTableLength += 8;
                                        readBytes++;
                                    }

                                    currentBinaryTree.addDepth();
                                }


                                huffmanCodesBinaryTree.maxCodeLength = maxCodeLength;
                                huffmanCodesBinaryTree.inFileLength = currentTableLength;

                                jpgFileHeaders.huffmanCodesBinaryTrees.Add(classAndIndetifierByte, huffmanCodesBinaryTree);
                            }
                        } break;

                        case (JpgMarkers.startOfFrameBaselineDCT): {
                            UInt16 expectedLength = binaryReader.ReadUInt16();
                            binaryReader.ReadBytes(5);

                            byte amountOfComponents = binaryReader.ReadByte();
                            jpgFileHeaders.jpgComponents = new JpgComponent[amountOfComponents];

                            for (int componentIndex = 0; componentIndex < amountOfComponents; componentIndex++) {
                                jpgFileHeaders.jpgComponents[componentIndex].componentSelector = binaryReader.ReadByte();
                                byte horizontalAndVerticalRepeatCount = binaryReader.ReadByte();
                                jpgFileHeaders.jpgComponents[componentIndex].repeatCount = (byte)(((horizontalAndVerticalRepeatCount >> 4) & 0x0F) * (horizontalAndVerticalRepeatCount & 0x0F));
                                binaryReader.ReadByte();
                            }

                            jpgFileHeaders.imageDataStartOffsetInBytes += 2 + expectedLength;
                        } break;

                        default: {
                            if (readMarker != JpgMarkers.startOfImage && firstPass) {
                                firstPass = false;
                                binaryReader = new BigEndianBinaryReader(new MemoryStream(fileBytes));
                                readMarker = binaryReader.ReadUInt16();

                                if (readMarker != JpgMarkers.startOfImage) {
                                    jpgFileHeaders.fileOk = false;
                                } else {
                                    jpgFileHeaders.isLittleEndian = false;
                                }
                            } else {
                                UInt16 length = binaryReader.ReadUInt16();
                                int dataLength = length - 2;
                                binaryReader.ReadBytes(dataLength);
                                jpgFileHeaders.imageDataStartOffsetInBytes += 2 + length;
                            }
                        } break;
                    }
                }
            }

            return jpgFileHeaders;
        }

        public static JpgFileStatistics getJpgFileStatistics (ref byte[] fileBytes)
        {
            JpgFileStatistics result = new JpgFileStatistics();

            result.jpgFileHeaders = readJpgFileHeader(ref fileBytes);
            BinaryBitReader binaryReader;

            if (result.jpgFileHeaders.isLittleEndian) {
                binaryReader = new BinaryBitReader(new MemoryStream(fileBytes));
            } else {
                binaryReader = new BigEndianBinaryReader(new MemoryStream(fileBytes));
            }

            binaryReader.ReadBytes(result.jpgFileHeaders.imageDataStartOffsetInBytes);
            UInt16 startScanMarker = binaryReader.ReadUInt16();

            if (startScanMarker == JpgMarkers.startOfScan) {
                byte numberOfComponents = extractScanHeader(ref binaryReader, ref result.jpgFileHeaders);
                result.startOfScanOffset = binaryReader.BaseStream.Position;
                result.decodedDCTtables = extractDCTtables(numberOfComponents, ref binaryReader, ref result.jpgFileHeaders);
                result.endOfScanOffset = binaryReader.BaseStream.Position - 3;
            }

            return result;
        }

        public static byte[] encodeDataIntoLayersFileBytes(ref byte[] messageAsByteList, LayerInformation layerInformation)
        {
            string[] fileExtensionSplit = layerInformation.filePath.Split('.');
            string fileExtension = fileExtensionSplit[fileExtensionSplit.Length - 1].ToLower();
            byte[] fileBytes = convertLayerToByteArray(layerInformation, false);

            switch (fileExtension) {
                case "bmp": {
                    BmpFileHeaders bmpFileHeaders = readBmpFileHeaders(fileBytes);
                    encodeDataIntoFileBytesBmp(ref bmpFileHeaders, ref messageAsByteList, ref fileBytes);
                } break;

                case "jpg":
                case "jpeg": {
                    JpgFileStatistics jpgFileStatistics = getJpgFileStatistics(ref fileBytes);
                    encodeDataIntoFileBytesJpg(ref jpgFileStatistics, ref messageAsByteList, ref fileBytes);
                } break;
            }

            attachLengthInFrontOfArray(ref fileBytes);

            return fileBytes;
        }

        public static FileType checkFileType (ref byte[] fileBytes)
        {
            FileType result = FileType.NONE;

            if (fileBytes.Length >= 54) {
                BmpFileHeaders bmpFileHeaders = readBmpFileHeaders(fileBytes);

                if (bmpFileHeaders.header.signature == ((UInt16)('B' << 8) | (UInt16)'M')) {
                    result = FileType.BMP;
                }
            }

            if (fileBytes.Length >= 2 && areFileBytesJpeg(ref fileBytes)) {
                result = FileType.JPEG;
            }

            return result;
        }

        public static void encodeDataIntoFileBytesBmp(ref BmpFileHeaders bmpFileHeaders, ref byte[] messageAsByteArray, ref byte[] fileBytes)
        {
            int currentMessageBit = 0;
            for (int y = 0; y < bmpFileHeaders.infoHeader.height && (currentMessageBit < messageAsByteArray.Length * 8); y++) {
                for (int x = 0; x < bmpFileHeaders.infoHeader.width * (bmpFileHeaders.infoHeader.bitsPerPixel / 8) && (currentMessageBit < messageAsByteArray.Length * 8); x++) {
                    int currentMessageByte = currentMessageBit / 8;
                    int bitPosition = currentMessageBit - currentMessageByte * 8;
                    byte valueToSet = (byte)((messageAsByteArray[currentMessageByte] & (byte)(1 << bitPosition)) >> bitPosition);
                    long currentBytesOffset = (x + y * bmpFileHeaders.infoHeader.width * 3) + bmpFileHeaders.header.dataOffset;
                    fileBytes[currentBytesOffset] = (byte)((((byte)(fileBytes[currentBytesOffset] >> 1)) << 1) | valueToSet);
                    currentMessageBit++;
                }
            }
        }

        private static byte[] extractNBytesOfDataFromFileBytes(UInt64 n, ref byte[] fileBytes, ref int currentOffset)
        {
            List<byte> resultBytesList = new List<byte>();
            byte[] result = null;
            byte currentByte = 0;
            int currentBit = 0;

            if ((n * 8) < (UInt64)(fileBytes.Length - currentOffset)) {
                for (UInt64 i = 0; i < (n * 8); i++) {
                    byte pixel = fileBytes[currentOffset];
                    currentByte |= (byte)((pixel & 0x01) << currentBit);

                    if (currentBit == 7) {
                        resultBytesList.Add(currentByte);
                        currentByte = 0;
                        currentBit = -1;
                    }

                    currentBit++;
                    currentOffset++;
                }

                result = resultBytesList.ToArray();
            }

            return result;
        }

        public static byte[] decodeDataFromFileBytesBmp(ref byte[] fileBytes)
        {
            BmpFileHeaders bmpFileHeaders = readBmpFileHeaders(fileBytes);

            List<byte> resultByteList = new List<byte>();
            byte[] result = null;

            int currentOffsetInFile = (int)bmpFileHeaders.header.dataOffset;

            byte[] dataLengthBytes = extractNBytesOfDataFromFileBytes(8, ref fileBytes, ref currentOffsetInFile);
            UInt64 expectedLength = convertByteArrayToUInt64(dataLengthBytes);
            result = extractNBytesOfDataFromFileBytes(expectedLength, ref fileBytes, ref currentOffsetInFile);

            return result;
        }

        public static void encodeDataIntoFileBytesJpg(ref JpgFileStatistics jpgFileStatistics, ref byte[] messageAsByteArray, ref byte[] fileBytes)
        {
            long valuesUsed = 0;

            UInt64 currentMessageBit = 0;
            for (int tableIndex = 0; tableIndex < jpgFileStatistics.decodedDCTtables.Count && (currentMessageBit < (UInt64)messageAsByteArray.Length * 8); tableIndex++) {
                DCTvalue[] currentDCTtable = jpgFileStatistics.decodedDCTtables[tableIndex].table;

                for (int valueInTableIndex = 0; valueInTableIndex < currentDCTtable.Length && (currentMessageBit < (UInt64)messageAsByteArray.Length * 8); valueInTableIndex++) {
                    ref DCTvalue currentValue = ref currentDCTtable[valueInTableIndex];

                    if (currentValue.value != 0) {
                        int currentMessageByte = (int)(currentMessageBit / 8);
                        byte bitPosition = (byte)(currentMessageBit - (UInt64)currentMessageByte * 8);
                        Int16 valueToSet = (Int16)(((messageAsByteArray[currentMessageByte] & (byte)(1 << bitPosition)) != 0) ? 1 : 0);

                        if (currentValue.value != 1) {
                            currentValue.value = (Int16)((Int16)((currentValue.value >> 1) << 1) | valueToSet);
                            currentMessageBit++;
                        }
                    }

                    valuesUsed++;
                }
            }

            List<bool> newBitstreamToOverride = new List<bool>();
            long oldBitstreamLength = 0;
            long newBitstreamLength = 0;
            long overwrittenValuesEncoded = 0;

            //TODO: fix encoding bug can't be noticed with small amount of changes but noticable when amount of changes is large 
            for (int tableIndex = 0; tableIndex < jpgFileStatistics.decodedDCTtables.Count && overwrittenValuesEncoded < valuesUsed; tableIndex++) {
                DCTvalue[] currentDCTtable = new DCTvalue[64];
                currentDCTtable = jpgFileStatistics.decodedDCTtables[tableIndex].table;

                bool first = true;
                for (int valueInTableIndex = 0; valueInTableIndex < currentDCTtable.Length && overwrittenValuesEncoded < valuesUsed; valueInTableIndex++) {
                    DCTvalue currentValue = currentDCTtable[valueInTableIndex];

                    if (currentValue.value != 0 || first) {
                        oldBitstreamLength += currentValue.originalCodedLength;
                        byte huffmanTableIdentifier = first ? jpgFileStatistics.decodedDCTtables[tableIndex].DCidentifier : jpgFileStatistics.decodedDCTtables[tableIndex].ACidentifier;
                        newBitstreamLength += encodeAndAddCodepointToList(currentValue.value, huffmanTableIdentifier, ref jpgFileStatistics.jpgFileHeaders, ref newBitstreamToOverride);

                        first = false;
                    } else {
                        //NOTICE: encoding of zeros is done by combining next value and amount of zeroes into single encoded codepoint
                        // Z zeroes amount encoding C codepoint encoding ZZZZCCCC
                        // to handle it create inherited value thats gonna handleded and consumed in next iteration 
                        // only exception to this rule are endoing of 16 zeros in a row wich gives value F0 which is directly encoded without need to inherit to next iteration
                        // other excpetion is encoding of remaining values being zero which equals to value 00 which encoded directly as above
                        byte numberOfZeros = 1;

                        while ((valueInTableIndex < (currentDCTtable.Length - 1)) && (currentDCTtable[valueInTableIndex + 1].value == 0)) {
                            numberOfZeros++;
                            valueInTableIndex++;
                            currentValue = currentDCTtable[valueInTableIndex];
                            overwrittenValuesEncoded++;
                        }

                        if (valueInTableIndex == currentDCTtable.Length - 1) {
                            numberOfZeros = 0;
                        }

                        if (numberOfZeros != 0) {
                            while (numberOfZeros >= 16) {
                                // 11110000
                                short valueToEncode = 0xF0;
                                int encodedLength = encodeAndAddCodepointToList(valueToEncode, jpgFileStatistics.decodedDCTtables[tableIndex].ACidentifier, ref jpgFileStatistics.jpgFileHeaders, ref newBitstreamToOverride);
                                newBitstreamLength += encodedLength;
                                oldBitstreamLength += encodedLength;

                                numberOfZeros -= 16;
                            }

                            if (numberOfZeros != 0) {
                                valueInTableIndex++;
                                currentValue = currentDCTtable[valueInTableIndex];

                                int encodedLength = encodeAndAddCodepointToList(currentValue.value, numberOfZeros, jpgFileStatistics.decodedDCTtables[tableIndex].ACidentifier, ref jpgFileStatistics.jpgFileHeaders, ref newBitstreamToOverride);
                                newBitstreamLength += encodedLength;
                                oldBitstreamLength += currentValue.originalCodedLength;
                            }
                        } else {
                            // 00000000
                            short valueToEncode = 0x00;
                            int encodedLength = encodeAndAddCodepointToList(valueToEncode, jpgFileStatistics.decodedDCTtables[tableIndex].ACidentifier, ref jpgFileStatistics.jpgFileHeaders, ref newBitstreamToOverride);
                            newBitstreamLength += encodedLength;
                            oldBitstreamLength += encodedLength;
                        }
                    }

                    overwrittenValuesEncoded++;
                }
            }

            MemoryStream memoryStream = new MemoryStream();
            long notOverridenDataStart = jpgFileStatistics.startOfScanOffset + (oldBitstreamLength / 8);

            memoryStream.Write(fileBytes, 0, (int)jpgFileStatistics.startOfScanOffset);

            byte value = 0;
            int currentBit = 0;
            int bytesWriten = 0;
            int currentIndex = 0;

            while (newBitstreamToOverride.Count > currentIndex) {
                value |= (byte)((newBitstreamToOverride[currentIndex] ? 1 : 0) << (7 - currentBit));
                currentBit++;

                if (currentBit == 8) {
                    currentBit = 0;
                    writeEncodedValueToStream(value, ref memoryStream);
                    value = 0;
                    bytesWriten++;
                }
                currentIndex++;
            }

            long currentNotOverridenByte = notOverridenDataStart;
            int offsetCorrection = (8 - (Math.Abs((int)(newBitstreamLength - oldBitstreamLength)) % 8));

            if (currentBit != 0) {
                UInt16 buffer = (UInt16)(value << 8);

                UInt16 valueToOr = (UInt16)((byte)(fileBytes[currentNotOverridenByte] << (8 - currentBit)));
                valueToOr = (UInt16)(valueToOr << (offsetCorrection - (8 - currentBit)));
                buffer |= valueToOr;
                currentNotOverridenByte++;
                buffer |= (UInt16)(fileBytes[currentNotOverridenByte] << (offsetCorrection - 8));
                writeEncodedValueToStream((byte)(buffer >> 8), ref memoryStream);
                buffer = (UInt16)(buffer << 8);
                currentNotOverridenByte++;
                //NOTICE: only fix marker if it's byte aligned?
                while (currentNotOverridenByte <= jpgFileStatistics.endOfScanOffset) {
                    if (fileBytes[currentNotOverridenByte] == 0 && fileBytes[currentNotOverridenByte - 1] == 0xFF && (offsetCorrection != 8 || offsetCorrection != 16)) {
                        currentNotOverridenByte++;
                    }

                    buffer |= (UInt16)(fileBytes[currentNotOverridenByte] << (offsetCorrection - 8));

                    writeEncodedValueToStream((byte)(buffer >> 8), ref memoryStream);

                    buffer = (UInt16)(buffer << 8);
                    currentNotOverridenByte++;
                }

                byte finalValue = (byte)(buffer >> 8);

                for (int currentPadIndex = 7 - (currentBit); currentPadIndex >= 0; currentPadIndex--) {
                    finalValue |= (byte)(1 << currentPadIndex);
                }

                if (finalValue != 0xFF) {
                    writeEncodedValueToStream(finalValue, ref memoryStream);
                }
            } else {
                memoryStream.Write(fileBytes, (int)currentNotOverridenByte, (int)(jpgFileStatistics.endOfScanOffset - currentNotOverridenByte));
            }

            memoryStream.Write(fileBytes, (int)(jpgFileStatistics.endOfScanOffset + 1), (int)(fileBytes.Length - (jpgFileStatistics.endOfScanOffset + 1)));

            fileBytes = memoryStream.ToArray();
        }

        private static byte[] extractNBytesOfDataFromDCTtables (UInt64 n, ref List<DCTtable> decodedDCTtables, ref int currentTableIndex, ref int currentValueInTableIndex)
        {
            List<byte> resultBytesList = new List<byte>();
            byte[] result = null;
            byte currentByte = 0;
            int currentBit = 0;

            if ((n * 8) < ((UInt64)(decodedDCTtables.Count) * 64)) {
                while (currentTableIndex < decodedDCTtables.Count && (UInt64)resultBytesList.Count < n) {
                    while (currentValueInTableIndex < 64 && (UInt64)resultBytesList.Count < n) {
                        DCTvalue currentDCTvalue = decodedDCTtables[currentTableIndex].table[currentValueInTableIndex];

                        if (currentDCTvalue.value != 0 && currentDCTvalue.value != 1) {
                            currentByte |= (byte)((currentDCTvalue.value & 0x01) << currentBit);

                            if (currentBit == 7) {
                                resultBytesList.Add(currentByte);
                                currentByte = 0;
                                currentBit = -1;
                            }

                            currentBit++;
                        }

                        currentValueInTableIndex++;
                    }

                    if ((UInt64)resultBytesList.Count < n) {
                        currentValueInTableIndex = 0;
                        currentTableIndex++;
                    }
                }

                result = resultBytesList.ToArray();
            }

            return result;
        }

        public static byte[] decodeDataFromFileBytesJpg (ref byte[] fileBytes)
        {
            JpgHeaders jpgFileHeaders = FileFormatHelpers.readJpgFileHeader(ref fileBytes);
            BinaryBitReader binaryReader;
            byte[] result = null;

            if (jpgFileHeaders.isLittleEndian) {
                binaryReader = new BinaryBitReader(new MemoryStream(fileBytes));
            } else {
                binaryReader = new BigEndianBinaryReader(new MemoryStream(fileBytes));
            }

            binaryReader.ReadBytes(jpgFileHeaders.imageDataStartOffsetInBytes);
            UInt16 startScanMarker = binaryReader.ReadUInt16();

            if (startScanMarker == JpgMarkers.startOfScan) {
                byte numberOfComponents = FileFormatHelpers.extractScanHeader(ref binaryReader, ref jpgFileHeaders);
                List<DCTtable> decodedDCTtables = FileFormatHelpers.extractDCTtables(numberOfComponents, ref binaryReader, ref jpgFileHeaders);
                int currentTableIndex = 0;
                int currentValueInTableIndex = 0;
                byte[] dataLengthBytes = extractNBytesOfDataFromDCTtables(8, ref decodedDCTtables, ref currentTableIndex, ref currentValueInTableIndex);
                UInt64 expectedLength = convertByteArrayToUInt64(dataLengthBytes);

                result = extractNBytesOfDataFromDCTtables(expectedLength, ref decodedDCTtables, ref currentTableIndex, ref currentValueInTableIndex);
            }

            return result;
        }

        public static byte[] readBitsFromBinaryReader(ref HuffmanCodesBinaryTree huffmanCodesBinaryTree, ref byte[] overReadBitsFromPreviousRead, ref BinaryBitReader binaryReader)
        {
            int additionAmountOfBitsToRead = 0;

            if (huffmanCodesBinaryTree.maxCodeLength > overReadBitsFromPreviousRead.Length) {
                additionAmountOfBitsToRead = huffmanCodesBinaryTree.maxCodeLength - overReadBitsFromPreviousRead.Length;
            }

            byte[] readAdditonalBits = new byte[additionAmountOfBitsToRead];
            readAdditonalBits = binaryReader.readBitsCorrectForNullMarker((byte)additionAmountOfBitsToRead);
            byte[] readBits = new byte[huffmanCodesBinaryTree.maxCodeLength > overReadBitsFromPreviousRead.Length ? huffmanCodesBinaryTree.maxCodeLength : overReadBitsFromPreviousRead.Length];
            Array.Copy(overReadBitsFromPreviousRead, 0, readBits, 0, overReadBitsFromPreviousRead.Length);
            Array.Copy(readAdditonalBits, 0, readBits, overReadBitsFromPreviousRead.Length, readAdditonalBits.Length);

            return readBits;
        }

        public static DCTvalue extractValueFromBits(ref byte[] readBits, ref BinaryBitReader binaryReader, ref JpgHeaders jpgFileHeaders, byte signedCodepointLength, int overflowLength)
        {
            byte[] signedCodepoint = new byte[signedCodepointLength];

            Array.Copy(readBits, (readBits.Length - overflowLength), signedCodepoint, 0, 
                signedCodepointLength < overflowLength ? signedCodepointLength : overflowLength
            );

            byte missingBitsToRead = (byte)(signedCodepointLength > overflowLength ? (signedCodepointLength - overflowLength) : 0);
            byte[] reminderCodepointBits = binaryReader.readBitsCorrectForNullMarker(missingBitsToRead);

            if (reminderCodepointBits.Length != 0) {
                Array.Copy(reminderCodepointBits, 0, signedCodepoint, overflowLength, signedCodepointLength - overflowLength);
            }

            string stringSignedCodepoint = "";

            for (int signedCodepointIndex = 0; signedCodepointIndex < signedCodepoint.Length; signedCodepointIndex++) {
                stringSignedCodepoint += signedCodepoint[signedCodepointIndex] == 0 ? "0" : "1";
            }

            DCTvalue result = new DCTvalue();
            result.value = jpgFileHeaders.codeToSignedValueDictionary[stringSignedCodepoint];
            result.originalCodedLength = (UInt16)signedCodepointLength;

            return result;
        }

        public static void copyOverflowBits(ref CodepointValueAndOverflowLength codepointValueAndOverflowLength, ref HuffmanCodesBinaryTree huffmanCodesBinaryTree, ref byte[] overReadBits, ref byte[] readBits, int bitsUsedAfterHuffmanCodepoint)
        {
            int finalAmountOfUsedBits = bitsUsedAfterHuffmanCodepoint + readBits.Length - codepointValueAndOverflowLength.overflowLength;
            int finalOverflowAmount = 0;

            if (finalAmountOfUsedBits < readBits.Length) {
                finalOverflowAmount = readBits.Length - finalAmountOfUsedBits;
            }

            overReadBits = new byte[finalOverflowAmount];

            if (finalOverflowAmount != 0) {
                Array.Copy(
                    readBits, finalAmountOfUsedBits,
                    overReadBits, 0,
                    finalOverflowAmount
                );
            }
        }

        public static void writeEncodedValueToStream(byte value, ref MemoryStream memoryStream)
        {
            memoryStream.Write(new byte[] { value }, 0, 1);

            if (value == 0xFF) {
                memoryStream.Write(new byte[] { (byte)0x00 }, 0, 1);
            }
        }

        public static int encodeAndAddCodepointToList (short valueToEncode, byte additionalLengthPart, byte huffmanTableIdentifier, ref JpgHeaders jpgFileHeaders, ref List<bool> valuesList)
        {
            int encodedLength = 0;

            string newCodepoint = jpgFileHeaders.signedValueToCodeDictionary[valueToEncode];
            encodedLength += newCodepoint.Length;
            byte codepointLengthToEncode = (byte)((additionalLengthPart << 4) | (byte)newCodepoint.Length);
            Stack<bool> huffmanCodedCodepointLength = jpgFileHeaders.huffmanCodesBinaryTrees[huffmanTableIdentifier].valueToHuffmanCodeDictionary[codepointLengthToEncode];
            encodedLength += huffmanCodedCodepointLength.Count;

            foreach (bool bit in huffmanCodedCodepointLength) {
                valuesList.Add(bit);
            }

            foreach (char bit in newCodepoint) {
                valuesList.Add(bit == '1');
            }

            return encodedLength;
        }

        public static int encodeAndAddCodepointToList(short valueToEncode, byte huffmanTableIdentifier, ref JpgHeaders jpgFileHeaders, ref List<bool> valuesList)
        {
            int encodedLength = 0;

            string newCodepoint = jpgFileHeaders.signedValueToCodeDictionary[valueToEncode];
            encodedLength += newCodepoint.Length;
            Stack<bool> huffmanCodedCodepointLength = jpgFileHeaders.huffmanCodesBinaryTrees[huffmanTableIdentifier].valueToHuffmanCodeDictionary[(byte)newCodepoint.Length];
            encodedLength += huffmanCodedCodepointLength.Count;

            foreach (bool bit in huffmanCodedCodepointLength) {
                valuesList.Add(bit);
            }

            foreach (char bit in newCodepoint) {
                valuesList.Add(bit == '1');
            }

            return encodedLength;
        }

        public static byte extractScanHeader(ref BinaryBitReader binaryReader, ref JpgHeaders jpgFileHeaders)
        {
            UInt16 scanHeaderLength = binaryReader.ReadUInt16();
            byte numberOfComponents = binaryReader.ReadByte();

            for (int componentIndex = 0; componentIndex < numberOfComponents; componentIndex++) {
                binaryReader.ReadByte();
                byte DCandACtableSelector = binaryReader.ReadByte();

                jpgFileHeaders.jpgComponents[componentIndex].DCtableSelector = (byte)((DCandACtableSelector >> 4) & 0x0F);
                jpgFileHeaders.jpgComponents[componentIndex].ACtableSelector = (UInt16)(DCandACtableSelector & 0x0F);
            }

            binaryReader.ReadBytes(3);

            return numberOfComponents;
        }

        public static List<DCTtable> extractDCTtables(byte numberOfComponents, ref BinaryBitReader binaryReader, ref JpgHeaders jpgFileHeaders)
        {
            byte[] overReadBits = new byte[0];
            List<DCTtable> decodedDCTtables = new List<DCTtable>();

            while (!binaryReader.checkForEndOfImageMarker()) {
                for (int componentIndex = 0; componentIndex < numberOfComponents; componentIndex++) {
                    for (int componentRepetIndex = 0; componentRepetIndex < jpgFileHeaders.jpgComponents[componentIndex].repeatCount; componentRepetIndex++) {
                        DCTtable currentDCTtable = new DCTtable();
                        currentDCTtable.DCidentifier = (byte)(jpgFileHeaders.jpgComponents[componentIndex].DCtableSelector);
                        currentDCTtable.ACidentifier = (byte)(0x10 | (jpgFileHeaders.jpgComponents[componentIndex].ACtableSelector));
                        currentDCTtable.table = new DCTvalue[64];

                        HuffmanCodesBinaryTree huffmanCodesBinaryTree = jpgFileHeaders.huffmanCodesBinaryTrees[currentDCTtable.DCidentifier];

                        byte[] readBits = readBitsFromBinaryReader(ref huffmanCodesBinaryTree, ref overReadBits, ref binaryReader);
                        CodepointValueAndOverflowLength codepointValueAndOverflowLength = huffmanCodesBinaryTree.binaryTree.findFirstMatchingCodepoint(readBits);
                        UInt16 encodedLengthLength = (UInt16)(readBits.Length - codepointValueAndOverflowLength.overflowLength);

                        currentDCTtable.table[0] = extractValueFromBits(ref readBits, ref binaryReader, ref jpgFileHeaders, codepointValueAndOverflowLength.value, codepointValueAndOverflowLength.overflowLength);
                        currentDCTtable.table[0].originalCodedLength += encodedLengthLength;

                        copyOverflowBits(ref codepointValueAndOverflowLength, ref huffmanCodesBinaryTree, ref overReadBits, ref readBits, codepointValueAndOverflowLength.value);

                        int currentDCTtableIndex = 1;
                        huffmanCodesBinaryTree = jpgFileHeaders.huffmanCodesBinaryTrees[currentDCTtable.ACidentifier];

                        while (currentDCTtableIndex < 64) {
                            readBits = readBitsFromBinaryReader(ref huffmanCodesBinaryTree, ref overReadBits, ref binaryReader);
                            codepointValueAndOverflowLength = huffmanCodesBinaryTree.binaryTree.findFirstMatchingCodepoint(readBits);
                            encodedLengthLength = (UInt16)(readBits.Length - codepointValueAndOverflowLength.overflowLength);

                            byte numberOfZeroes = (byte)((codepointValueAndOverflowLength.value >> 4) & 0x0F);
                            byte ACsignedCodepointLength = (byte)(codepointValueAndOverflowLength.value & 0x0F);

                            if (ACsignedCodepointLength == 0x00) {
                                if (numberOfZeroes == 0x00) {
                                    currentDCTtableIndex = 64;
                                } else if (numberOfZeroes == 0x0F) {
                                    numberOfZeroes = 16;
                                }
                            }

                            currentDCTtableIndex += numberOfZeroes;

                            if (currentDCTtableIndex != 64 && numberOfZeroes != 16) {
                                currentDCTtable.table[currentDCTtableIndex] = extractValueFromBits(ref readBits, ref binaryReader, ref jpgFileHeaders, ACsignedCodepointLength, codepointValueAndOverflowLength.overflowLength);
                                currentDCTtable.table[currentDCTtableIndex].originalCodedLength += encodedLengthLength;
                            }

                            copyOverflowBits(ref codepointValueAndOverflowLength, ref huffmanCodesBinaryTree, ref overReadBits, ref readBits, ACsignedCodepointLength);

                            currentDCTtableIndex++;
                        }

                        decodedDCTtables.Add(currentDCTtable);
                    }
                }
            }

            return decodedDCTtables;
        }

        public static UInt64 convertByteArrayToUInt64 (byte[] array)
        {
            UInt64 result = 0;

            int currentIndex = 0;
            foreach (byte value in array) {
                result |= ((UInt64)value << (currentIndex * 8));
                currentIndex++;
            }

            return result;
        }

        public static byte[] convertUInt64ToByteArray(UInt64 value)
        {
            byte[] result = new byte[8];

            for (int i = 0; i < 8; i++) {
                result[i] = (byte)(value >> (i * 8));
            }

            return result;
        }

        public static void attachLengthInFrontOfArray(ref byte[] array)
        {
            UInt64 dataLength = (UInt64)array.Length;
            byte[] temp = new byte[8 + dataLength];
            Array.Copy(convertUInt64ToByteArray(dataLength), temp, 8);
            Array.Copy(array, 0, temp, 8, (int)dataLength);
            array = temp;
        }

        public static byte[] convertLayerToByteArray(LayerInformation layer, bool attachLengthInFront = true)
        {
            byte[] result = new byte[0];

            switch (layer.type) {
                case (LayerSourceType.Text): {
                    List<byte> byteList = new List<byte>(System.Text.Encoding.ASCII.GetBytes(layer.text));
                    byteList.Add(0);
                    result = byteList.ToArray();

                    if (attachLengthInFront == true) {
                        attachLengthInFrontOfArray(ref result);
                    }
                } break;

                case (LayerSourceType.File): {
                    result = File.ReadAllBytes(layer.filePath);

                    if (attachLengthInFront == true) {
                        attachLengthInFrontOfArray(ref result);
                    }
                } break;

                default: {
                } break;
            }

            return result;
        }
    }
}
