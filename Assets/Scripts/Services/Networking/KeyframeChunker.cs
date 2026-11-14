using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Splits one tick's keyframes into chunks that fit <see cref="NetworkTickSettings.CHUNK_BYTE_BUDGET"/>.</summary>
    public static class KeyframeChunker
    {
        /// <summary>Tick, time, chunk index/count and array length.</summary>
        public const int HEADER_BYTES = 24;
        public const int KEYFRAMES_PER_CHUNK =
            (NetworkTickSettings.CHUNK_BYTE_BUDGET - HEADER_BYTES) / EntityKeyframeCodec.ENCODED_BYTES;

        /// <summary>Always at least one (possibly empty) chunk, so clients keep a steady render timeline.</summary>
        public static List<EntityKeyframe[]> Split(IReadOnlyList<EntityKeyframe> keyframes)
        {
            List<EntityKeyframe[]> chunks = new List<EntityKeyframe[]>();
            Split(keyframes, chunks);
            return chunks;
        }

        /// <summary>Replaces the destination's contents. Nonempty chunk arrays belong to the resulting messages.</summary>
        public static void Split(IReadOnlyList<EntityKeyframe> keyframes, List<EntityKeyframe[]> chunks)
        {
            chunks.Clear();
            int start = 0;
            do
            {
                int count = Math.Min(KEYFRAMES_PER_CHUNK, keyframes.Count - start);
                EntityKeyframe[] chunk = count == 0 ? Array.Empty<EntityKeyframe>() : new EntityKeyframe[count];
                for (int i = 0; i < count; i++) chunk[i] = keyframes[start + i];
                chunks.Add(chunk);
                start += count;
            } while (start < keyframes.Count);
        }
    }
}
