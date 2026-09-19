using System;

namespace TheLongDriveSyncRadio.Models
{
    [Serializable]
    public struct AudioFilePacket
    {
        public string fileName;
        public byte[] data;
        public long fileSize;
        public bool requiresStreaming;
        public string filePath;
    }

    [Serializable]
    public struct PacketPart
    {
        public uint size;
        public uint pId;
        public uint pIndex;
        public byte[] data;
    }
    
    [Serializable]
    public struct AudioFileRequestPacket
    {
        public string[] excludes;
    }

    [Serializable]
    public struct RadioPacket
    {
        public string fileName;
    }

    [Serializable]
    public struct AudioSyncPacket
    {
        public string fileName;
        public double currentTime;  // Temps de lecture en secondes
        public long timestamp;      // Timestamp serveur pour compenser le délai réseau
        public int playbackState;   // 0 = stopped, 1 = playing, 2 = paused
    }

    [Serializable]
    public struct AudioSyncRequestPacket
    {
        public string currentSongFileName;
    }

    [Serializable]
    public struct StreamingAudioPacket
    {
        public string fileName;
        public byte[] audioData;
        public int sequenceNumber;
        public bool isLastChunk;
        public long totalSize;
    }
}
