using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Script : Node
{
    private Random m_rng;
    private int m_index; // NB: 'Internal tick', for clarity...

    public Dictionary<string, Characterisation> m_characterisations; // FIXME: Change this to m_characterisations?
    private Dictionary<string, Vertex> m_lines;

    public Script(int seed, Dictionary<string, Characterisation> characterisations)
    {
        m_rng = new Random(seed);
        m_index = 0;

        m_characterisations = new Dictionary<string, Characterisation>();
        foreach (string character in characterisations.Keys)
            m_characterisations.Add(character, new Characterisation(characterisations[character]));

        // 'AUTHORSHIP': Load in a pre-written script for characters to follow...
        string path = (OS.IsDebugBuild()) ? ProjectSettings.GlobalizePath("res://Assets/Narrative/men_in_grey_suits_first_draft.json") : OS.GetExecutablePath().GetBaseDir() + "/Assets/Narrative/men_in_grey_suits_first_draft.json";
        m_lines = LoadVertices(path, characterisations.Keys.ToList());

        // DEBUG: ...
        //PrintAdjencyMatrix();
    }

    public Script(Script script, int seed)
    {
        m_rng = new Random(seed);
        m_index = script.m_index;

        m_characterisations = new Dictionary<string, Characterisation>();
        foreach (string character in script.m_characterisations.Keys)
            m_characterisations.Add(character, new Characterisation(script.m_characterisations[character]));

        m_lines = new Dictionary<string, Vertex>();
        foreach (string ID in script.m_lines.Keys)
            m_lines.Add(ID, new Vertex(script.m_lines[ID]));
    }

    public void Focalise(string focalisation)
    {
        // STEP 1: Erase others' lines...
        foreach (string line in m_lines.Keys) // DEPRECATED: && !m_lines[key].m_defaults.ContainsValue(other)...
            m_lines[line].Focalise(focalisation);
    }

    public void Tick(List<string> audience, Agent.Talk talk)
    {
        if (!m_lines.ContainsKey(talk.m_line))
            return;

        m_lines[talk.m_line].Tick(m_index++, talk.m_tick, talk.m_actor, talk.m_accusation, audience);
    }

    public List<string> GetLines()
    {
        return m_lines.Keys.ToList();
    }

    public List<string> GetRoles()
    {
        List<string> roles = new List<string>();
        foreach (string line in m_lines.Keys)
            roles = roles.Union(m_lines[line].GetRoles()).Distinct().ToList();

        return roles;
    }

    public Characterisation GetCharacterisation(string character)
    {
        return (m_characterisations.ContainsKey(character)) ? new Characterisation(m_characterisations[character]) : new Characterisation();
    }

    // VERTEX ACCESORS //

    private Vertex GetVertex(string line)
    {
        return (m_lines.ContainsKey(line)) ? new Vertex(m_lines[line]) : new Vertex();
    }

    public List<string> GetSegues(string line)
    {
        return GetVertex(line).GetSegues();
    }

    public Dictionary<string, float> GetEdges(string line)
    {
        return GetVertex(line).GetEdges();
    }

    public float GetEdge(string line, string edge)
    {
        return GetVertex(line).GetEdge(edge);
    }

    public List<string> GetCharactersAware(string line)
    {
        return GetVertex(line).GetCharactersAware();
    }

    public List<string> GetCharactersJustAware(string line)
    {
        return GetVertex(line).GetCharactersJustAware();
    }

    public int GetTotalAwareness(string line, string character)
    {
        return GetVertex(line).GetTotalAwareness(character);
    }

    public Dictionary<string, int> GetTotalAwarenesses(string line, List<string> characters)
    {
        return GetVertex(line).GetTotalAwarenesses(characters);
    }

    public Dictionary<string, int> GetTotalAwarenesses(string line)
    {
        return GetVertex(line).GetTotalAwarenesses();
    }

    public List<string> GetRoles(string line)
    {
        return GetVertex(line).GetRoles();
    }

    public List<string> GetIdentifiableRoles(string line)
    {
        return GetVertex(line).GetIdentifiableRoles();
    }

    public List<string> GetImplications(string line, string role)
    {
        return GetVertex(line).GetIdentities(role);
    }

    public List<string> GetAllImplications(string line)
    {
        return GetVertex(line).GetAllImplications();
    }

    public List<string> GetIdentities(string line, string role)
    {
        return GetVertex(line).GetIdentities(role);
    }

    public List<Dictionary<string, string>> GetAllAccusations(string line)
    {
        return GetVertex(line).GetAllAccusations();
    }

    public List<Dictionary<string, string>> GetMaximalAccusations(string line, int flexibility = 0)
    {
        return GetVertex(line).GetMaximalAccusations(flexibility);
    }

    public string GetDiegesis(string line)
    {
        return GetVertex(line).GetDiegesis();
    }

    public string GetMimesis(string line, string character)
    {
        return GetVertex(line).GetMimesis(character);
    }

    public string GetMode(string line)
    {
        return GetVertex(line).GetMode();
    }

    public string GetJokeOrigin(string line, string character)
    {
        return GetVertex(line).GetJokeOrigin(character);
    }

    public bool IsAdjacent(string line, string edge)
    {
        return GetVertex(line).IsAdjacent(edge);
    }

    public bool IsAware(string line, string character)
    {
        return GetVertex(line).IsAware(character);
    }

    public bool IsJustAware(string line, string character)
    {
        return GetVertex(line).IsJustAware(character);
    }

    public bool IsRumour(string line)
    {
        return GetVertex(line).IsRumour();
    }

    public bool IsRumourLearned(string line, string character)
    {
        return GetVertex(line).IsRumourLearned(character);
    }

    public bool IsJoke(string line)
    {
        return GetVertex(line).IsJoke();
    }

    public bool IsJokeStolen(string line, string character)
    {
        return GetVertex(line).IsJokeStolen(character);
    }



    // PRINTING //

    public string PrintInformationState(bool printCharacters = false)
    {
        string text = "";

        // STEP 1: Calculate indents...
        string characterAdjoinder = ": ";
        string informationAdjoinder = " ";

        int characterIndent = 0;
        int informationIndent = (int)Math.Floor(Math.Log10(Math.Max(m_characterisations.Count, 1)) + 1) + informationAdjoinder.Length;
        int metricIndent = (int)Math.Floor(Math.Log10(Math.Max(m_lines.Keys.ToList().FindAll(x => !x.Equals("START")).Count(), 1)));

        if (printCharacters)
        {
            int nonfocalisedIndent = "NONFOCALISED".Length;
            int focalisedIndent = m_characterisations.Keys.Select(x => x.Length).Max();

            characterIndent = Math.Max(nonfocalisedIndent, focalisedIndent) + characterAdjoinder.Length;
        }

        // STEP 2: Print global information...
        if (printCharacters)
            text += ("NONFOCALISED" + characterAdjoinder).PadRight(characterIndent);

        float globalInformation = 0.0f;
        foreach (string line in m_lines.Keys.ToList().FindAll(x => !x.Equals("START")))
        {
            int distributedInformation = m_lines[line].GetCharactersAware().Count;
            globalInformation += distributedInformation;

            text += (distributedInformation + informationAdjoinder).PadLeft(informationIndent);
        }

        int averageInformation = (int)Math.Floor(globalInformation / m_characterisations.Count());
        text += "= " + averageInformation.ToString().PadLeft(metricIndent) + "\n";

        // STEP 3: Print local information...
        foreach (string character in m_characterisations.Keys)
        {
            if (printCharacters)
                text += (character + characterAdjoinder).PadRight(characterIndent);

            int localInformation = 0;
            foreach (string line in m_lines.Keys.ToList().FindAll(x => !x.Equals("START")))
            {
                int distributedInformation = Convert.ToInt32(m_lines[line].IsAware(character));// Math.Min(m_lines[line].GetCharactersKnownTo().Count, 1);
                localInformation += distributedInformation;

                text += (((distributedInformation == 1) ? "*" : "") + informationAdjoinder).PadLeft(informationIndent);
            }

            text += "= " + localInformation.ToString().PadLeft(metricIndent) + "\n";
        }

        return text;
    }
}
