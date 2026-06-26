// ====================================
// <copyright file="MemoryHeader.cs" company="Spicco D'Aura">
// Copyright (c) Spicco D'Aura. All rights reserved.
// Licensed under the CC BY-SA 1.0 License.
// </copyright>
// ====================================

using System.IO.MemoryMappedFiles;
using Vapor.Core.Common;

namespace Vapor.Core.Memory
{
    internal class MemoryHeader
    {
        public static int GetWritePosition(MemoryMappedViewAccessor accessor)
        {
            return accessor.ReadInt32(VaporConstants.WritePositionOffset);
        }

        public static void SetWritePosition(MemoryMappedViewAccessor accessor, int position)
        {
            accessor.Write(VaporConstants.WritePositionOffset, position);
        }

        public static int GetReadPosition(MemoryMappedViewAccessor accessor)
        {
            return accessor.ReadInt32(VaporConstants.ReadPositionOffset);
        }

        public static void SetReadPosition(MemoryMappedViewAccessor accessor, int position)
        {
            accessor.Write(VaporConstants.ReadPositionOffset, position);
        }

        public static void Reset(MemoryMappedViewAccessor accessor)
        {
            accessor.Write(VaporConstants.WritePositionOffset, 0);
            accessor.Write(VaporConstants.ReadPositionOffset, 0);
        }
    }
}