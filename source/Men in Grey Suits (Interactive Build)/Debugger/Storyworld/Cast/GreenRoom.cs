using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

public partial class GreenRoom : Node
{
    public struct JsonCharacterisation
    {
        public Dictionary<string, List<string>>? ATTRIBUTES { get; set; }
        public Dictionary<string, double>? STATISTICS { get; set; }
    }

    private Dictionary<string, Characterisation> m_characterisations;

    public GreenRoom()
    {
        string path = (OS.IsDebugBuild()) ? ProjectSettings.GlobalizePath("res://Assets/Narrative/character_traits.json") : OS.GetExecutablePath().GetBaseDir() + "/Assets/Narrative/character_traits.json";
        m_characterisations = LoadCharacterisations(path);// FIXME: Replace with Godot's means of getting assets... LoadCharacterisations(System.Environment.CurrentDirectory.Split("bin")[0] + "Assets/character_traits.json");
    }

    private Dictionary<string, Characterisation> LoadCharacterisations(string path)
    {
        Dictionary<string, Characterisation> characterisations = new Dictionary<string, Characterisation>();

        // STEP 1: Parse the JSON into an intermediary dictionary...
        Dictionary<string, JsonCharacterisation> jsonCharacterisations = JsonSerializer.Deserialize<Dictionary<string, JsonCharacterisation>>(System.IO.File.ReadAllText(path));
        
        if (jsonCharacterisations is null)
            return characterisations;

        // STEP 2: Construct character dictionaries...
        foreach (string character in jsonCharacterisations.Keys)
            characterisations.Add(character, new Characterisation(character, jsonCharacterisations[character]));

        return characterisations;
    }

    public Dictionary<string, Characterisation> GetCharacterisations()
    {
        Dictionary<string, Characterisation> characterisations = new Dictionary<string, Characterisation>();
        foreach (string character in m_characterisations.Keys)
            characterisations.Add(character, new Characterisation(m_characterisations[character]));

        return characterisations;
    }
}
