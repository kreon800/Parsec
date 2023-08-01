﻿using Parsec.Attributes;
using Parsec.Extensions;
using Parsec.Readers;
using Parsec.Shaiya.Core;

namespace Parsec.Shaiya.Common;

/// <summary>
/// </summary>
public struct Unk7Floats : IBinary
{
    /// <summary>
    /// Point used as reference for the rectangle
    /// </summary>
    [ShaiyaProperty]
    public Vector3 Unk1 { get; set; }

    /// <summary>
    /// Point used as reference for the rectangle
    /// </summary>
    [ShaiyaProperty]
    public Vector3 Unk2 { get; set; }

    /// <summary>
    /// Unknown float
    /// </summary>
    [ShaiyaProperty]
    public float Unk { get; set; }

    public Unk7Floats(Vector3 unk1, Vector3 unk2, float unk)
    {
        Unk1 = unk1;
        Unk2 = unk2;
        Unk = unk;
    }

    public Unk7Floats(SBinaryReader binaryReader)
    {
        Unk1 = new Vector3(binaryReader);
        Unk2 = new Vector3(binaryReader);
        Unk = binaryReader.Read<float>();
    }

    /// <inheritdoc />
    public IEnumerable<byte> GetBytes(params object[] options)
    {
        var buffer = new List<byte>();
        buffer.AddRange(Unk1.GetBytes());
        buffer.AddRange(Unk2.GetBytes());
        buffer.AddRange(Unk.GetBytes());
        return buffer;
    }
}
