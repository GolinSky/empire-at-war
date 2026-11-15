using System;
using System.IO;
using System.Text;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>Commit before battle loading; incomplete temporary writes never replace the last session.</summary>
    public sealed class ReconnectSessionStore
    {
        private const int FORMAT_VERSION = 1;
        private const int MAX_RECORD_BYTES = 16384;
        private const string FILE_NAME = "reconnect-session.dat";
        private readonly string _path;

        public ReconnectSessionStore(string directory)
        {
            _path = Path.Combine(directory, FILE_NAME);
        }

        public void Save(ReconnectSessionRecord record)
        {
            Validate(record);
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            string temporaryPath = _path + ".tmp";
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(FORMAT_VERSION);
                    writer.Write((int)record.Transport);
                    writer.Write(record.Host);
                    writer.Write(record.Port);
                    writer.Write(record.LobbyId);
                    writer.Write(record.SteamId);
                    writer.Write(record.MatchId);
                    writer.Write(record.Credential);
                    writer.Write(record.SlotIndex);
                }
                stream.Flush(true);
            }

            if (File.Exists(_path)) File.Replace(temporaryPath, _path, null);
            else File.Move(temporaryPath, _path);
        }

        /// <summary>Missing records are normal. Invalid records are discarded with a user-facing reason.</summary>
        public bool TryLoad(out ReconnectSessionRecord record, out string error)
        {
            record = null;
            error = string.Empty;
            if (!File.Exists(_path)) return false;
            byte[] bytes;
            using (var file = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (file.Length > MAX_RECORD_BYTES) bytes = Array.Empty<byte>();
                else
                {
                    bytes = new byte[(int)file.Length];
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        int read = file.Read(bytes, offset, bytes.Length - offset);
                        if (read == 0) throw new IOException("The reconnect record changed while reading it.");
                        offset += read;
                    }
                }
            }
            try
            {
                using (var stream = new MemoryStream(bytes))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (reader.ReadInt32() != FORMAT_VERSION)
                        throw new InvalidDataException("Unsupported reconnect record.");
                    var restored = new ReconnectSessionRecord
                    {
                        Transport = (ReconnectTransport)reader.ReadInt32(),
                        Host = reader.ReadString(),
                        Port = reader.ReadUInt16(),
                        LobbyId = reader.ReadUInt64(),
                        SteamId = reader.ReadUInt64(),
                        MatchId = reader.ReadString(),
                        Credential = reader.ReadString(),
                        SlotIndex = reader.ReadInt32()
                    };
                    Validate(restored);
                    if (stream.Position != stream.Length)
                        throw new InvalidDataException("Unexpected reconnect record data.");
                    record = restored;
                    return true;
                }
            }
            catch (Exception exception) when (exception is InvalidDataException || exception is IOException ||
                                              exception is FormatException)
            {
                Clear();
                error = "The previous match recovery record was invalid or unsupported and has been dismissed.";
                return false;
            }
        }

        public void Clear()
        {
            File.Delete(_path);
            File.Delete(_path + ".tmp");
        }

        private static void Validate(ReconnectSessionRecord record)
        {
            if (record == null ||
                (record.Transport != ReconnectTransport.Kcp && record.Transport != ReconnectTransport.Steam) ||
                string.IsNullOrWhiteSpace(record.Host) || record.Host.Length > 1024 ||
                string.IsNullOrWhiteSpace(record.MatchId) || record.MatchId.Length > 128 ||
                string.IsNullOrWhiteSpace(record.Credential) || record.Credential.Length > 256 ||
                record.SlotIndex < 0 || record.SlotIndex >= OnlineLobby.MAX_MEMBERS ||
                (record.Transport == ReconnectTransport.Kcp && record.Port == 0) ||
                (record.Transport == ReconnectTransport.Steam && record.SteamId == 0))
                throw new InvalidDataException("The reconnect record is incomplete or invalid.");
        }
    }
}
