using System.Text.RegularExpressions;
using PKHeX.Core;
using static EtumrepMMO.Lib.SpawnerType;

namespace EtumrepMMO.Lib;

public static class GroupSeedFinder
{
    public const byte MaxRolls = 32;

    #region Seed Detection

    /// <inheritdoc cref="FindSeed(IEnumerable{PKM},byte,SpawnerType)"/>
    public static (ulong Seed, int FirstIndex) FindSeed(string folder, byte maxRolls = MaxRolls, SpawnerType mode = All)
        => FindSeed(GetInputs(folder), maxRolls, mode);

    /// <inheritdoc cref="FindSeed(IEnumerable{PKM},byte,SpawnerType)"/>
    public static (ulong Seed, int FirstIndex) FindSeed(IEnumerable<string> files, byte maxRolls = MaxRolls, SpawnerType mode = All)
        => FindSeed(GetInputs(files), maxRolls, mode);

    /// <inheritdoc cref="FindSeed(IEnumerable{PKM},byte,SpawnerType)"/>
    public static (ulong Seed, int FirstIndex) FindSeed(IEnumerable<byte[]> data, byte maxRolls = MaxRolls, SpawnerType mode = All)
        => FindSeed(GetInputs(data), maxRolls, mode);

    #endregion

    #region Data Fetching

    /// <summary> Gets entities from the provided input source. </summary>
    public static IReadOnlyList<PKM> GetInputs(string folder) => GetInputs(Directory.EnumerateFiles(folder));

    /// <inheritdoc cref="GetInputs(string)"/>
    public static IReadOnlyList<PKM> GetInputs(IEnumerable<string> files) => GetInputs(files.Select(File.ReadAllBytes));

    /// <inheritdoc cref="GetInputs(string)"/>
    public static IReadOnlyList<PKM> GetInputs(IEnumerable<byte[]> data) => data.Select(PKMConverter.GetPKMfromBytes).OfType<PKM>().ToArray();

    #endregion

    /// <summary>
    /// Returns all valid Group Seeds (should only be one) that generated the input data.
    /// </summary>
    /// <param name="data">Entities that were generated</param>
    /// <param name="maxRolls">Max amount of PID re-rolls for shiny odds.</param>
    /// <param name="mode">Group seed validation mode</param>
    /// <returns>Default if no result found, otherwise a single seed (no duplicates are possible).</returns>
    public static (ulong Seed, int FirstIndex) FindSeed(IEnumerable<PKM> data, byte maxRolls = MaxRolls, SpawnerType mode = All)
    {
        var entities = data.ToArray();
        var ecs = Array.ConvertAll(entities, z => z.EncryptionConstant);

        // Backwards we go! Reverse the pkm data -> seed first (this takes the longest, so we only do one at a time).
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            Console.WriteLine($"Checking entity {i+1}/{entities.Length} for group seeds...");
            var pokeResult = RuntimeReversal.GetSeeds(entity, maxRolls);

            foreach (var (pokeSeed, rolls) in pokeResult)
            {
                // Get seed for slot-pkm
                var genSeeds = GenSeedReversal.FindPotentialGenSeeds(pokeSeed);
                foreach (var genSeed in genSeeds)
                {
                    // Get the group seed - O(1) calc
                    var groupSeed = GroupSeedReversal.GetGroupSeed(genSeed);
                    if (mode.HasFlag(MultiSpawn) && GroupSeedValidator.IsMultiInitial(groupSeed, ecs, i))
                        Console.WriteLine($"Found a multi-spawn group seed with PID roll count = {rolls}");
                    else if (mode.HasFlag(SingleSpawn) && GroupSeedValidator.IsSingleSingle(groupSeed, ecs, i))
                        Console.WriteLine($"Found a single-spawn group seed with PID roll count = {rolls}");
                    else if (mode.HasFlag(MixedSpawn) && GroupSeedValidator.IsSingleMulti(groupSeed, ecs, i))
                        Console.WriteLine($"Found a 1+{ecs.Length-1} spawn group seed with PID roll count = {rolls}");
                    else
                        continue;

                    return (groupSeed, i);
                }
            }
        }

        return (default, -1);
    }

    public static IReadOnlyList<PKM> GetInputsFromText(string text)
    {
        var inputs = new List<PKM>();
        try
        {
            var lines = Regex.Split(text.Trim(), "\\s").Where(str => !string.IsNullOrWhiteSpace(str))
                .ToList();
            PA8 pa8 = new PA8();
            foreach (var line in lines)
            {
                var splitArray = line.Trim().Split(":");
                if (splitArray[0] == "species" && pa8.Species != 0)
                {
                    inputs.Add(pa8);
                    pa8 = new PA8();
                }

                switch (splitArray[0])
                {
                    case "species":
                        pa8.Species = int.Parse(splitArray[1]);
                        break;
                    case "pid":
                        pa8.PID = uint.Parse(splitArray[1]);
                        break;
                    case "ec":
                        pa8.EncryptionConstant = uint.Parse(splitArray[1]);
                        break;
                    case "IVs":
                        pa8.IVs = splitArray[1].Split(",").Select(int.Parse).ToArray();
                        break;
                    case "TID":
                        pa8.TID = int.Parse(splitArray[1]);
                        break;
                    case "SID":
                        pa8.SID = int.Parse(splitArray[1]);
                        break;
                    case "extra":
                        // abilityNumber gender nature isAlpha HeightScalar WeightScalar
                        //          3bit   2bit   8bit    1bit         8bit         8bit
                        var extra = uint.Parse(splitArray[1]);
                        pa8.WeightScalar = (int) (extra & 0xFF);
                        pa8.HeightScalar = (int) ((extra >> 8) & 0xFF);
                        pa8.IsAlpha = ((extra >> 16) & 1) == 1;
                        pa8.Nature = (int) ((extra >> 17) & 0xFF);
                        pa8.Gender = (int) ((extra >> 25) & 0x3);
                        pa8.AbilityNumber = (int) ((extra >> 27) & 0x7);
                        break;
                }
            }

            if (pa8.Species != 0) inputs.Add(pa8);
        }
        catch (Exception)
        {
        }

        return inputs;
    }
}