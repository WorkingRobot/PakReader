using System;

namespace PakReader.Parsers.Objects
{
    public readonly struct FText
    {
        public readonly ETextFlag Flags;
        public readonly FTextHistory Text;

        bool IsBaseType => Text is FTextHistory.Base;
        FTextHistory.Base BaseText => Text.As<FTextHistory.Base>();
        public string Key => IsBaseType ? BaseText.Key : null;
        public string Namespace => IsBaseType ? BaseText.Namespace : null;
        public string SourceString => IsBaseType ? BaseText.SourceString : null;

        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/Core/Private/Internationalization/Text.cpp#L769
        internal FText(PackageReader reader)
        {
            Flags = (ETextFlag)reader.ReadUInt32();

            // "Assuming" the reader/archive is persistent
            Flags &= ETextFlag.ConvertedProperty | ETextFlag.InitializedFromString;

            // Execute if UE4 version is at least VER_UE4_FTEXT_HISTORY

            // The type is serialized during the serialization of the history, during deserialization we need to deserialize it and create the correct history
            var HistoryType = (ETextHistoryType)reader.ReadSByte();

            // Create the history class based on the serialized type
            switch (HistoryType)
            {
                case ETextHistoryType.Base:
                    Text = new FTextHistory.Base(reader);
                    break;
                case ETextHistoryType.AsDateTime:
                    Text = new FTextHistory.DateTime(reader);
                    break;
                // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/Core/Private/Internationalization/TextHistory.cpp
                // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/Core/Private/Internationalization/TextData.h
                case ETextHistoryType.NamedFormat:
                case ETextHistoryType.OrderedFormat:
                case ETextHistoryType.ArgumentFormat:
                case ETextHistoryType.AsNumber:
                case ETextHistoryType.AsPercent:
                case ETextHistoryType.AsCurrency:
                case ETextHistoryType.AsDate:
                case ETextHistoryType.AsTime:
                case ETextHistoryType.Transform:
                case ETextHistoryType.StringTableEntry:
                case ETextHistoryType.TextGenerator:
                    // Let me know if you find a package that has an unsupported text history type.
                    throw new NotImplementedException($"Parsing of {HistoryType} history type isn't supported yet.");
                default:
                    Text = new FTextHistory.None(reader);
                    break;
            }
        }
    }
}
