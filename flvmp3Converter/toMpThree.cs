using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;


namespace toMpThree
{
    interface IAudioWriter
    {
        void WriteChunk(byte[] chunk, uint timeStamp);
        void Finish();
        string Path { get; }
    }

    public delegate bool OverwriteDelegate(string destPath);

    public class FLVFile : IDisposable
    {
        static readonly string[] _outputExtensions = new string[] { ".avi", ".mp3", ".264", ".aac", ".spx", ".txt" };

        string _inputPath, _outputDirectory, _outputPathBase;
        OverwriteDelegate _overwrite;
        FileStream _fs;
        long _fileOffset, _fileLength;
        IAudioWriter _audioWriter;
        List<uint> _videoTimeStamps;
        bool _extractAudio, _extractVideo, _extractTimeCodes;
        bool _extractedAudio;

        List<string> _warnings;

        public FLVFile(string path)
        {
            _inputPath = path;
            _outputDirectory = Path.GetDirectoryName(path);
            _warnings = new List<string>();
            _fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            _fileOffset = 0;
            _fileLength = _fs.Length;
        }

        public void Dispose()
        {
            if (_fs != null)
            {
                _fs.Close();
                _fs = null;
            }
            CloseOutput(true);
        }


        public void ExtractStreams(bool extractAudio, bool extractVideo, bool extractTimeCodes, OverwriteDelegate overwrite)
        {
            uint dataOffset, flags, prevTagSize;

            _outputPathBase = Path.Combine(_outputDirectory, Path.GetFileNameWithoutExtension(_inputPath));
            _overwrite = overwrite;
            _extractAudio = extractAudio;
            _extractVideo = extractVideo;
            _extractTimeCodes = extractTimeCodes;
            _videoTimeStamps = new List<uint>();

            Seek(0);
            if (_fileLength < 4 || ReadUInt32() != 0x464C5601)
            {
                if (_fileLength >= 8 && ReadUInt32() == 0x66747970)
                {
                    throw new Exception("This is a MP4 file. ");
                }
                else
                {
                    throw new Exception("This isn't a FLV file.");
                }
            }


            flags = ReadUInt8();
            dataOffset = ReadUInt32();

            Seek(dataOffset);

            prevTagSize = ReadUInt32();
            while (_fileOffset < _fileLength)
            {
                if (!ReadTag()) break;
                if ((_fileLength - _fileOffset) < 4) break;
                prevTagSize = ReadUInt32();
            }


            CloseOutput(false);
        }

        private void CloseOutput(bool disposing)
        {

            if (_audioWriter != null)
            {
                _audioWriter.Finish();
                if (disposing && (_audioWriter.Path != null))
                {
                    try { File.Delete(_audioWriter.Path); }
                    catch { }
                }
                _audioWriter = null;
            }

        }

        private bool ReadTag()
        {
            uint tagType, dataSize, timeStamp, streamID, mediaInfo;
            byte[] data;

            if ((_fileLength - _fileOffset) < 11)
            {
                return false;
            }

            // Read tag header
            tagType = ReadUInt8();
            dataSize = ReadUInt24();
            timeStamp = ReadUInt24();
            timeStamp |= ReadUInt8() << 24;
            streamID = ReadUInt24();

            // Read tag data
            if (dataSize == 0)
            {
                return true;
            }
            if ((_fileLength - _fileOffset) < dataSize)
            {
                return false;
            }
            mediaInfo = ReadUInt8();
            dataSize -= 1;
            data = ReadBytes((int)dataSize);

            if (tagType == 0x8)
            {  // Audio
                if (_audioWriter == null)
                {
                    _audioWriter = _extractAudio ? GetAudioWriter(mediaInfo) : new DummyAudioWriter();
                    _extractedAudio = !(_audioWriter is DummyAudioWriter);
                }
                _audioWriter.WriteChunk(data, timeStamp);
            }
            return true;
        }

        private IAudioWriter GetAudioWriter(uint mediaInfo)
        {
            uint format = mediaInfo >> 4;
            uint rate = (mediaInfo >> 2) & 0x3;
            uint bits = (mediaInfo >> 1) & 0x1;
            uint chans = mediaInfo & 0x1;
            string path;
            // MP3
            path = _outputPathBase + ".mp3";
            if (!CanWriteTo(path)) return new DummyAudioWriter();
            return new MP3Writer(path, _warnings);
        }


        private bool CanWriteTo(string path)
        {
            if (File.Exists(path) && (_overwrite != null))
            {
                return _overwrite(path);
            }
            return true;
        }


        private void Seek(long offset)
        {
            _fs.Seek(offset, SeekOrigin.Begin);
            _fileOffset = offset;
        }

        private uint ReadUInt8()
        {
            _fileOffset += 1;
            return (uint)_fs.ReadByte();
        }

        private uint ReadUInt24()
        {
            byte[] x = new byte[4];
            _fs.Read(x, 1, 3);
            _fileOffset += 3;
            return BitConverterBE.ToUInt32(x, 0);
        }

        private uint ReadUInt32()
        {
            byte[] x = new byte[4];
            _fs.Read(x, 0, 4);
            _fileOffset += 4;
            return BitConverterBE.ToUInt32(x, 0);
        }

        private byte[] ReadBytes(int length)
        {
            byte[] buff = new byte[length];
            _fs.Read(buff, 0, length);
            _fileOffset += length;
            return buff;
        }
    }

    class DummyAudioWriter : IAudioWriter
    {
        public DummyAudioWriter()
        {
        }

        public void WriteChunk(byte[] chunk, uint timeStamp)
        {
        }

        public void Finish()
        {
        }

        public string Path
        {
            get
            {
                return null;
            }
        }
    }


    class MP3Writer : IAudioWriter
    {
        string _path;
        FileStream _fs;
        List<string> _warnings;
        List<byte[]> _chunkBuffer;
        List<uint> _frameOffsets;
        uint _totalFrameLength;
        bool _isVBR;
        bool _delayWrite;
        bool _hasVBRHeader;
        bool _writeVBRHeader;
        int _firstBitRate;
        int _mpegVersion;
        int _sampleRate;
        int _channelMode;
        uint _firstFrameHeader;

        public MP3Writer(string path, List<string> warnings)
        {
            _path = path;
            _fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 65536);
            _warnings = warnings;
            _chunkBuffer = new List<byte[]>();
            _frameOffsets = new List<uint>();
            _delayWrite = true;
        }

        public void WriteChunk(byte[] chunk, uint timeStamp)
        {
            _chunkBuffer.Add(chunk);
            ParseMP3Frames(chunk);
            if (_delayWrite && _totalFrameLength >= 65536)
            {
                _delayWrite = false;
            }
            if (!_delayWrite)
            {
                Flush();
            }
        }

        public void Finish()
        {
            Flush();
            if (_writeVBRHeader)
            {
                _fs.Seek(0, SeekOrigin.Begin);
                WriteVBRHeader(false);
            }
            _fs.Close();
        }

        public string Path
        {
            get
            {
                return _path;
            }
        }

        private void Flush()
        {
            foreach (byte[] chunk in _chunkBuffer)
            {
                _fs.Write(chunk, 0, chunk.Length);
            }
            _chunkBuffer.Clear();
        }

        private void ParseMP3Frames(byte[] buff)
        {
            int[] MPEG1BitRate = new int[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
            int[] MPEG2XBitRate = new int[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 };
            int[] MPEG1SampleRate = new int[] { 44100, 48000, 32000, 0 };
            int[] MPEG20SampleRate = new int[] { 22050, 24000, 16000, 0 };
            int[] MPEG25SampleRate = new int[] { 11025, 12000, 8000, 0 };

            int offset = 0;
            int length = buff.Length;

            while (length >= 4)
            {
                ulong header;
                int mpegVersion, layer, bitRate, sampleRate, padding, channelMode;
                int frameLen;

                header = (ulong)BitConverterBE.ToUInt32(buff, offset) << 32;
                if (BitHelper.Read(ref header, 11) != 0x7FF)
                {
                    break;
                }
                mpegVersion = BitHelper.Read(ref header, 2);
                layer = BitHelper.Read(ref header, 2);
                BitHelper.Read(ref header, 1);
                bitRate = BitHelper.Read(ref header, 4);
                sampleRate = BitHelper.Read(ref header, 2);
                padding = BitHelper.Read(ref header, 1);
                BitHelper.Read(ref header, 1);
                channelMode = BitHelper.Read(ref header, 2);

                if ((mpegVersion == 1) || (layer != 1) || (bitRate == 0) || (bitRate == 15) || (sampleRate == 3))
                {
                    break;
                }

                bitRate = ((mpegVersion == 3) ? MPEG1BitRate[bitRate] : MPEG2XBitRate[bitRate]) * 1000;

                if (mpegVersion == 3)
                    sampleRate = MPEG1SampleRate[sampleRate];
                else if (mpegVersion == 2)
                    sampleRate = MPEG20SampleRate[sampleRate];
                else
                    sampleRate = MPEG25SampleRate[sampleRate];

                frameLen = GetFrameLength(mpegVersion, bitRate, sampleRate, padding);
                if (frameLen > length)
                {
                    break;
                }

                bool isVBRHeaderFrame = false;
                if (_frameOffsets.Count == 0)
                {
                    // Check for an existing VBR header just to be safe (I haven't seen any in FLVs)
                    int o = offset + GetFrameDataOffset(mpegVersion, channelMode);
                    if (BitConverterBE.ToUInt32(buff, o) == 0x58696E67)
                    { // "Xing"
                        isVBRHeaderFrame = true;
                        _delayWrite = false;
                        _hasVBRHeader = true;
                    }
                }

                if (isVBRHeaderFrame) { }
                else if (_firstBitRate == 0)
                {
                    _firstBitRate = bitRate;
                    _mpegVersion = mpegVersion;
                    _sampleRate = sampleRate;
                    _channelMode = channelMode;
                    _firstFrameHeader = BitConverterBE.ToUInt32(buff, offset);
                }
                else if (!_isVBR && (bitRate != _firstBitRate))
                {
                    _isVBR = true;
                    if (_hasVBRHeader) { }
                    else if (_delayWrite)
                    {
                        WriteVBRHeader(true);
                        _writeVBRHeader = true;
                        _delayWrite = false;
                    }
                    else
                    {
                        _warnings.Add("Detected VBR too late, cannot add VBR header.");
                    }
                }

                _frameOffsets.Add(_totalFrameLength + (uint)offset);

                offset += frameLen;
                length -= frameLen;
            }

            _totalFrameLength += (uint)buff.Length;
        }

        private void WriteVBRHeader(bool isPlaceholder)
        {
            byte[] buff = new byte[GetFrameLength(_mpegVersion, 64000, _sampleRate, 0)];
            if (!isPlaceholder)
            {
                uint header = _firstFrameHeader;
                int dataOffset = GetFrameDataOffset(_mpegVersion, _channelMode);
                header &= 0xFFFF0DFF; // Clear bitrate and padding fields
                header |= 0x00010000; // Set protection bit (indicates that CRC is NOT present)
                header |= (uint)((_mpegVersion == 3) ? 5 : 8) << 12; // 64 kbit/sec
                GeneralManupulation.CopyBytes(buff, 0, BitConverterBE.GetBytes(header));
                GeneralManupulation.CopyBytes(buff, dataOffset, BitConverterBE.GetBytes((uint)0x58696E67)); // "Xing"
                GeneralManupulation.CopyBytes(buff, dataOffset + 4, BitConverterBE.GetBytes((uint)0x7)); // Flags
                GeneralManupulation.CopyBytes(buff, dataOffset + 8, BitConverterBE.GetBytes((uint)_frameOffsets.Count)); // Frame count
                GeneralManupulation.CopyBytes(buff, dataOffset + 12, BitConverterBE.GetBytes((uint)_totalFrameLength)); // File length
                for (int i = 0; i < 100; i++)
                {
                    int frameIndex = (int)((i / 100.0) * _frameOffsets.Count);
                    buff[dataOffset + 16 + i] = (byte)((_frameOffsets[frameIndex] / (double)_totalFrameLength) * 256.0);
                }
            }
            _fs.Write(buff, 0, buff.Length);
        }

        private int GetFrameLength(int mpegVersion, int bitRate, int sampleRate, int padding)
        {
            return ((mpegVersion == 3) ? 144 : 72) * bitRate / sampleRate + padding;
        }

        private int GetFrameDataOffset(int mpegVersion, int channelMode)
        {
            return 4 + ((mpegVersion == 3) ?
                ((channelMode == 3) ? 17 : 32) :
                ((channelMode == 3) ? 9 : 17));
        }
    }   

    public static class GeneralManupulation
    {        
        public static void CopyBytes(byte[] dst, int dstOffset, byte[] src)
        {
            Buffer.BlockCopy(src, 0, dst, dstOffset, src.Length);
        }
    }

    public static class BitHelper
    {
        public static int Read(ref ulong x, int length)
        {
            int r = (int)(x >> (64 - length));
            x <<= length;
            return r;
        }
}

    public static class BitConverterBE
    {
        public static uint ToUInt32(byte[] value, int startIndex)
        {
            return
                ((uint)value[startIndex] << 24) |
                ((uint)value[startIndex + 1] << 16) |
                ((uint)value[startIndex + 2] << 8) |
                ((uint)value[startIndex + 3]);
        }


        public static byte[] GetBytes(uint value)
        {
            byte[] buff = new byte[4];
            buff[0] = (byte)(value >> 24);
            buff[1] = (byte)(value >> 16);
            buff[2] = (byte)(value >> 8);
            buff[3] = (byte)(value);
            return buff;
        }
    }
}