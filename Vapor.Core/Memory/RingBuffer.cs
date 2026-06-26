// ====================================
// <copyright file="RingBuffer.cs" company="Spicco D'Aura">
// Copyright (c) Spicco D'Aura. All rights reserved.
// Licensed under the CC BY-SA 1.0 License.
// </copyright>
// ====================================

using System;
using System.IO.MemoryMappedFiles;
using Vapor.Core.Common;

namespace Vapor.Core.Memory;

internal static class RingBuffer
{
    public static bool WriteMessage(MemoryMappedViewAccessor accessor, int bufferSize, ReadOnlySpan<byte> message)
    {
        int writePos = MemoryHeader.GetWritePosition(accessor);
        int readPos = MemoryHeader.GetReadPosition(accessor);

        int requiredSpace = 4 + message.Length; 
        int availableSpace;

        if (writePos >= readPos)
        {
            availableSpace = bufferSize - writePos + readPos - 1;
        }
        else
        {
            availableSpace = readPos - writePos - 1;
        }

        if (requiredSpace > availableSpace)
            return false; 
        
        long writeOffset = (long)VaporConstants.HeaderSize + writePos;

        if (writePos + requiredSpace > bufferSize)
        {
            accessor.Write(writeOffset, -1);
            writePos = 0;
            writeOffset = (long)VaporConstants.HeaderSize + writePos;
        }
        
        accessor.Write(writeOffset, message.Length);

        byte[] rawBytes = message.ToArray();
        
        accessor.WriteArray(writeOffset + 4, rawBytes, 0, rawBytes.Length);

        MemoryHeader.SetWritePosition(accessor, writePos + requiredSpace);
        return true;
    }

    public static byte[]? ReadNextMessage(MemoryMappedViewAccessor accessor, int bufferSize)
    {
        int writePos = MemoryHeader.GetWritePosition(accessor);
        int readPos = MemoryHeader.GetReadPosition(accessor);

        if (writePos == readPos)
            return null;

        long readOffset = (long)VaporConstants.HeaderSize + readPos;
        int lengthOrMarker = accessor.ReadInt32(readOffset);

        if (lengthOrMarker == -1)
        {
            readPos = 0;
            readOffset = (long)VaporConstants.HeaderSize + readPos;
            lengthOrMarker = accessor.ReadInt32(readOffset);
        }

        byte[] messageBuffer = new byte[lengthOrMarker];
        accessor.ReadArray(readOffset + 4, messageBuffer, 0, lengthOrMarker);

        MemoryHeader.SetReadPosition(accessor, readPos + 4 + lengthOrMarker);

        return messageBuffer;
    }
}