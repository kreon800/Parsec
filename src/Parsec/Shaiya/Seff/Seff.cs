﻿using Newtonsoft.Json;
using Parsec.Extensions;
using Parsec.Serialization;
using Parsec.Shaiya.Core;

namespace Parsec.Shaiya.Seff;

public sealed class Seff : FileBase
{
    public int Format { get; set; }

    public SeffTimeStamp TimeStamp;

    public List<SeffRecord> Records { get; set; } = new();

    [JsonIgnore]
    public override string Extension => "seff";

    protected override void Read(SBinaryReader binaryReader)
    {
        Format = binaryReader.ReadInt32();
        binaryReader.SerializationOptions.ExtraOption = Format;

        TimeStamp = binaryReader.Read<SeffTimeStamp>();
        Records = binaryReader.ReadList<SeffRecord>().ToList();
    }

    protected override void Write(SBinaryWriter binaryWriter)
    {
        binaryWriter.SerializationOptions.ExtraOption = Format;

        binaryWriter.Write(Format);
        binaryWriter.Write(TimeStamp);
        binaryWriter.Write(Records.ToSerializable());
    }
}
