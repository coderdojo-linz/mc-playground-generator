namespace GeneratorFunction; 

public class NameBuilder
{
    private readonly string[] Prefix = new[]
{
            "The",
            "A",
            "Our",
            "Your",
            "One",
            "His",
            "Her",
            "Their",
            "Mothers"
        };

    private readonly string[] MinecraftWords = new[]
    {
            "Creeper",
            "Biome",
            "Enderman",
            "Enderdragon",
            "Bedrock",
            "Nether",
            "Portal",
            "Fortress",
            "Redstone",
            "Mobs",
            "Zombie",
            "Pigman",
            "Skeleton",
            "Spawn",
            "Silverfish",
            "Witches",
            "Sandstone",
            "Lava",
            "TurtleEgg",
            "Chicken",
            "Wool",
            "DragonEgg",
            "Trapdoor",
            "Torch"
        };

    private readonly string[] Attributes = new[]
    {
            "Beautiful",
            "Angry",
            "Smiling",
            "Red",
            "Green",
            "Blue",
            "Dark",
            "Mysterious",
            "Dangerous",
            "Flying",
            "Running",
            "Marvelous",
            "Spectacular",
            "Singing",
            "Dancing",
            "Jumping"
        };

    public string GenerateRandomName()
    {
        var rand = new Random();
        return $"{Prefix[rand.Next(Prefix.Length)]}-{Attributes[rand.Next(Attributes.Length)]}-{MinecraftWords[rand.Next(MinecraftWords.Length)]}".ToLower();
    }
}
