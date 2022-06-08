﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parsec.Attributes;
using Parsec.Common;
using Parsec.Extensions;
using Parsec.Readers;

namespace Parsec.Shaiya.Core
{
    public class Binary
    {
        public static object ReadProperty(SBinaryReader binaryReader, PropertyInfo propertyInfo, Episode episode = Episode.Unknown)
        {
            var type = propertyInfo.PropertyType;

            var attributes = propertyInfo.GetCustomAttributes().ToList();

            // If property isn't marked as a ShaiyaProperty, it must be skipped
            if (!attributes.Exists(a => a.GetType() == typeof(ShaiyaPropertyAttribute)))
                return null;

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case ShaiyaPropertyAttribute shaiyaPropertyAttribute:
                        // If episode limits weren't specified, then property must be read
                        if (shaiyaPropertyAttribute.MinEpisode == Episode.Unknown && shaiyaPropertyAttribute.MaxEpisode == Episode.Unknown)
                            break;

                        if (shaiyaPropertyAttribute.MaxEpisode == Episode.Unknown && episode != shaiyaPropertyAttribute.MinEpisode)
                            return null;

                        if (episode < shaiyaPropertyAttribute.MinEpisode || episode > shaiyaPropertyAttribute.MaxEpisode)
                            return null;

                        break;

                    case LengthPrefixedListAttribute lengthPrefixedListAttribute:
                        int length = 0;

                        var lengthType = lengthPrefixedListAttribute.LengthType;

                        if (lengthType == typeof(int))
                        {
                            length = binaryReader.Read<int>();
                        }
                        else if (lengthType == typeof(uint))
                        {
                            length = (int)binaryReader.Read<uint>();
                        }
                        else if (lengthType == typeof(short))
                        {
                            length = binaryReader.Read<short>();
                        }
                        else if (lengthType == typeof(ushort))
                        {
                            length = binaryReader.Read<ushort>();
                        }
                        else if (lengthType == typeof(byte))
                        {
                            length = binaryReader.Read<byte>();
                        }
                        else if (lengthType == typeof(sbyte))
                        {
                            length = binaryReader.Read<sbyte>();
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }

                        // Create generic list
                        var listType = typeof(List<>);
                        var constructedListType = listType.MakeGenericType(lengthPrefixedListAttribute.ItemType);
                        var list = (IList)Activator.CreateInstance(constructedListType);

                        var properties = lengthPrefixedListAttribute.ItemType.GetProperties();

                        for (int i = 0; i < length; i++)
                        {
                            var item = Activator.CreateInstance(lengthPrefixedListAttribute.ItemType);

                            foreach (var property in properties)
                            {
                                // skip non ShaiyaProperty properties
                                if (!property.IsDefined(typeof(ShaiyaPropertyAttribute)))
                                    continue;

                                var propertyValue = ReadProperty(binaryReader, property, episode);
                                property.SetValue(item, propertyValue);
                            }

                            list.Add(item);
                        }

                        return list;

                    case FixedLengthListAttribute fixedLengthListAttribute:
                        // Create generic list
                        var fixedLengthListType = typeof(List<>);
                        var constructedFixedListType = fixedLengthListType.MakeGenericType(fixedLengthListAttribute.ItemType);
                        var itemList = (IList)Activator.CreateInstance(constructedFixedListType);

                        var typeProperties = fixedLengthListAttribute.ItemType.GetProperties();

                        for (int i = 0; i < fixedLengthListAttribute.Length; i++)
                        {
                            var item = Activator.CreateInstance(fixedLengthListAttribute.ItemType);

                            foreach (var property in typeProperties)
                            {
                                // skip non ShaiyaProperty properties
                                if (!property.IsDefined(typeof(ShaiyaPropertyAttribute)))
                                    continue;

                                var propertyValue = ReadProperty(binaryReader, property, episode);
                                property.SetValue(item, propertyValue);
                            }

                            itemList.Add(item);
                        }

                        return itemList;

                    case LengthPrefixedStringAttribute lengthPrefixedStringAttribute:
                        var lengthPrefixedStr = binaryReader.ReadString(lengthPrefixedStringAttribute.Encoding,
                                                                        lengthPrefixedStringAttribute.IncludeStringTerminator);

                        return lengthPrefixedStr;

                    case FixedLengthStringAttribute fixedLengthStringAttribute:
                        var fixedLengthStr = binaryReader.ReadString(fixedLengthStringAttribute.Encoding, fixedLengthStringAttribute.Length,
                                                                     fixedLengthStringAttribute.IncludeStringTerminator);

                        return fixedLengthStr;
                }
            }

            // If property implements IBinary, the IBinary must be instantiated through its single parameter constructor with takes the SBinaryReader instance
            // this is the case for types Vector, Quaternion, Matrix, BoundingBox, etc.
            if (type.GetInterfaces().Contains(typeof(IBinary)))
            {
                var binary = (IBinary)Activator.CreateInstance(type, binaryReader);
                return binary;
            }

            return ReadPrimitive(binaryReader, type);
        }

        private static object ReadPrimitive(SBinaryReader binaryReader, Type type) => binaryReader.Read(type);

        public static IEnumerable<byte> GetPropertyBytes(object obj, PropertyInfo propertyInfo, Episode episode = Episode.Unknown)
        {
            var attributes = propertyInfo.GetCustomAttributes().ToList();

            // If property isn't marked as a ShaiyaAttribute, it must be skipped
            if (!attributes.Exists(a => a.GetType() == typeof(ShaiyaPropertyAttribute)))
                return Array.Empty<byte>();

            var propertyValue = propertyInfo.GetValue(obj);

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case ShaiyaPropertyAttribute shaiyaProperty:
                        var ep = episode;

                        // FileBase instances include the Episode property
                        if (obj is FileBase fileBase)
                        {
                            if (episode == Episode.Unknown)
                                ep = fileBase.Episode;
                        }

                        // if format can't be determined, nothing else should be done here
                        if (ep == Episode.Unknown)
                            break;

                        // if the ShaiyaProperty didn't specify an episode, then it's present in all of them
                        if (shaiyaProperty.MinEpisode == Episode.Unknown && shaiyaProperty.MaxEpisode == Episode.Unknown)
                            break;

                        // single episode check
                        if (shaiyaProperty.MaxEpisode == Episode.Unknown && ep != shaiyaProperty.MinEpisode)
                            return Array.Empty<byte>();

                        // multiple episode check
                        if (ep <= shaiyaProperty.MinEpisode || ep >= shaiyaProperty.MaxEpisode)
                            return Array.Empty<byte>();

                        break;

                    case FixedLengthListAttribute fixedLengthListAttribute:
                        var listItems = (propertyValue as IEnumerable).Cast<object>().Take(fixedLengthListAttribute.Length);

                        var buf = new List<byte>();

                        foreach (var item in listItems)
                        {
                            foreach (var property in fixedLengthListAttribute.ItemType.GetProperties())
                            {
                                if (!property.IsDefined(typeof(ShaiyaPropertyAttribute)))
                                    continue;

                                buf.AddRange(GetPropertyBytes(item, property, episode));
                            }
                        }

                        return buf.ToArray();

                    case LengthPrefixedListAttribute lengthPrefixedListAttribute:
                        var lengthType = lengthPrefixedListAttribute.LengthType;

                        var items = propertyValue as IEnumerable;
                        var itemCount = items.Cast<object>().Count();

                        var buffer = new List<byte>();

                        if (lengthType == typeof(int))
                        {
                            buffer.AddRange(itemCount.GetBytes());
                        }
                        else if (lengthType == typeof(short))
                        {
                            buffer.AddRange(((short)itemCount).GetBytes());
                        }
                        else if (lengthType == typeof(byte))
                        {
                            buffer.Add((byte)itemCount);
                        }
                        else
                        {
                            // only int, short and byte lengths are expected
                            throw new NotImplementedException();
                        }

                        foreach (var item in items)
                        {
                            foreach (var property in lengthPrefixedListAttribute.ItemType.GetProperties())
                            {
                                if (!property.IsDefined(typeof(ShaiyaPropertyAttribute)))
                                    continue;

                                buffer.AddRange(GetPropertyBytes(item, property, episode));
                            }
                        }

                        return buffer.ToArray();

                    case SuffixedStringAttribute suffixedStringAttribute:
                        propertyValue += suffixedStringAttribute.Suffix;
                        break;

                    case LengthPrefixedStringAttribute lengthPrefixedStringAttribute:
                        return ((string)propertyValue).GetLengthPrefixedBytes(lengthPrefixedStringAttribute.IncludeStringTerminator);

                    case FixedLengthStringAttribute:
                        return ((string)propertyValue).GetBytes();
                }
            }

            var type = propertyInfo.PropertyType;

            // If property implements IBinary, bytes can be retrieved by calling GetBytes()
            // this is the case for types Vector, Quaternion, Matrix, BoundingBox, etc.
            if (type.GetInterfaces().Contains(typeof(IBinary)))
                return ((IBinary)propertyValue).GetBytes();

            return GetPrimitiveBytes(type, propertyValue);
        }

        private static IEnumerable<byte> GetPrimitiveBytes(Type type, object value)
        {
            if (type == typeof(byte))
                return new[] { (byte)value };

            if (type == typeof(bool))
                return new[] { (byte)value };

            if (type == typeof(int))
                return ((int)value).GetBytes();

            if (type == typeof(uint))
                return ((uint)value).GetBytes();

            if (type == typeof(short))
                return ((short)value).GetBytes();

            if (type == typeof(ushort))
                return ((ushort)value).GetBytes();

            if (type == typeof(long))
                return ((long)value).GetBytes();

            if (type == typeof(ulong))
                return ((ulong)value).GetBytes();

            if (type == typeof(float))
                return ((float)value).GetBytes();

            throw new ArgumentException();
        }
    }
}
