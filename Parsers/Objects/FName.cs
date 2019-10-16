using System;
using System.IO;
using System.Text;

namespace PakReader.Parsers.Objects
{
    // The only values this contains from the original FNameEntrySerialized is the isWide (unused here since C# strings are always 16 bit anyway) and the Index (some typedef of an int which was unused anyway)
    // FNames are passed into a pool, but I don't think this has any impact or difference on ther resolving of these values. I could make a Dictionary or Lookup for values having the same hash or something..?

    // FName is a class due to the value typing that C# has for structs. This is for memory performance to reduce duplicate strings in memory. Refrain from saving the FName's value (Name) and opt for the class instead
    public class FName
    {
        public string Name;

        // The parser is basically the same as FString. Let me know if there are any breaking test cases here
        public FName(BinaryReader reader)
        {
            Name = reader.ReadFString();
            // skip DummyHashes (case and non-case preserving hashes)
            reader.BaseStream.Position += 4;
        }
    }
}
