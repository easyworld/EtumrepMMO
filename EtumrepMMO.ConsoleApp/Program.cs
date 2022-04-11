using System.Text.RegularExpressions;
using EtumrepMMO.Lib;
using PKHeX.Core;

//const string entityFolderName = "mons";
//var inputs = GroupSeedFinder.GetInputs(entityFolderName);
const string fileName = "mons.txt";
var inputs = new List<PKM>();
try
{
    var lines = Regex.Split(File.ReadAllText(fileName), "\\s").Where(str => !string.IsNullOrWhiteSpace(str)).ToList();
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
                pa8.Species = int.Parse(splitArray[1]); break;
            case "pid":
                pa8.PID = uint.Parse(splitArray[1]); break;
            case "ec":
                pa8.EncryptionConstant = uint.Parse(splitArray[1]); break;
            case "IVs":
                pa8.IVs = splitArray[1].Split(",").Select(int.Parse).ToArray(); break;
            case "TID":
                pa8.TID = int.Parse(splitArray[1]); break;
            case "SID":
                pa8.SID = int.Parse(splitArray[1]); break;
        }
    }
    if (pa8.Species != 0) inputs.Add(pa8);
}
catch (Exception)
{
    Console.WriteLine("Invalid data in mons.txt");
    return;
}

if (inputs.Count < 2)
{
    Console.WriteLine("Insufficient inputs found in folder. Needs to have two (2) or more dumped files.");
}
else if (inputs.Count > 4)
{
    Console.WriteLine("Too many inputs found in folder. Needs to have only the first four (4) Pokémon.");
}
else
{
    var result = GroupSeedFinder.FindSeed(inputs);
    if (result is default(ulong))
    {
        Console.WriteLine($"No group seeds found with the input data. Double check your inputs (valid inputs: {inputs.Count}).");
    }
    else
    {
        Console.WriteLine("Found seed!");
        Console.WriteLine(result);
    }
}

Console.WriteLine();
Console.WriteLine("Press [ENTER] to exit.");
Console.ReadLine();
