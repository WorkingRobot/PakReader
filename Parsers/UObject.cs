using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;

namespace PakReader.Parsers
{
    public class UObject : IUStruct, IReadOnlyDictionary<string, BaseProperty>
    {
        public FObjectExport ExportInfo { get; internal set; }

        // This isn't exposed for ease of use to the properties instead of always referencing Dict
        readonly Dictionary<string, BaseProperty> Dict;

        readonly FGuid GUID;

        // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L930
        public UObject(PackageReader reader) : this(reader, false) { }

        // Structs that don't use binary serialization
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L2197
        internal UObject(PackageReader reader, bool structFallback)
        {
            var props = new Dictionary<string, BaseProperty>();

            while (true)
            {
                var Tag = new FPropertyTag(reader);
                if (Tag.Name.IsNone)
                    break;

                var pos = reader.Position;
                props[Tag.Name.String] = BaseProperty.ReadProperty(reader, Tag, Tag.Type, ReadType.NORMAL);
                if (Tag.Size + pos != reader.Position)
                {
                    Console.WriteLine($"Didn't read {Tag.Type.String} correctly (at {reader.Position}, should be {Tag.Size + pos}, {Tag.Size + pos - reader.Position} behind)");
                    reader.Position = Tag.Size + pos;
                }
            }
            Dict = props;

            if (!structFallback && reader.ReadInt32() != 0)
            {
                GUID = new FGuid(reader);
            }
        }

        public BaseProperty this[string key] => Dict[key];
        public IEnumerable<string> Keys => Dict.Keys;
        public IEnumerable<BaseProperty> Values => Dict.Values;
        public int Count => Dict.Count;
        public bool ContainsKey(string key) => Dict.ContainsKey(key);
        public IEnumerator<KeyValuePair<string, BaseProperty>> GetEnumerator() => Dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Dict.GetEnumerator();

        public bool TryGetValue(string key, out BaseProperty value) => Dict.TryGetValue(key, out value);

        public T Deserialize<T>()
        {
            var ret = ReflectionHelper.NewInstance<T>();
            var map = ReflectionHelper.GetActionMap<T>();
            foreach (var kv in Dict)
            {
                (var baseType, var typeGetter) = ReflectionHelper.GetPropertyInfo(kv.Value.GetType());
                if (map.TryGetValue((kv.Key.ToLowerInvariant(), baseType), out Action<object, object> setter))
                {
                    setter(ret, typeGetter(kv.Value));
                }
            }
            return ret;
        }
    }
}
