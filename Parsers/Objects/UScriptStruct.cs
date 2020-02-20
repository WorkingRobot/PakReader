namespace PakReader.Parsers.Objects
{
    public readonly struct UScriptStruct
    {
        public readonly IUStruct Struct;

        // Binary serialization, tagged property serialization otherwise
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L2146
        internal UScriptStruct(PackageReader reader, FName structName) =>
            Struct = structName.String switch
            {
                "GameplayTagContainer" => new FGameplayTagContainer(reader),
                "Quat" => new FQuat(reader),
                "Vector2D" => new FVector2D(reader),
                "Vector" => new FVector(reader),
                "Rotator" => new FRotator(reader),
                "IntPoint" => new FIntPoint(reader),
                "Guid" => new FGuid(reader),
                "SoftObjectPath" => new FSoftObjectPath(reader),
                "Color" => new FColor(reader),
                "LinearColor" => new FLinearColor(reader),
                _ => new UObject(reader, true),
            };
    }
}
