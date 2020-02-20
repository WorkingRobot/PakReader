namespace PakReader.Parsers.Objects
{
    public readonly struct FGameplayTagContainer : IUStruct
    {
        // It's technically a TArray<FGameplayTag> but FGameplayTag is just a fancy wrapper around an FName
        public readonly FName[] GameplayTags;

        internal FGameplayTagContainer(PackageReader reader)
        {
            GameplayTags = reader.ReadTArray(() => reader.ReadFName());
        }
    }
}
