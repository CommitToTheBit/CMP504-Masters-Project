using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Stage : Node
{
    public class Vertex
    {
        // Graph details
        public string m_key;
        public Dictionary<string, float> m_edges, m_sights, m_sounds;

        // Stats details
        public Dictionary<string, int> m_characterLastAt;
        public Dictionary<string, int> m_characterLastLOS;

        // Aesthetic details
        // FIXME: Does anything go here?

        public Vertex()
        {
            m_key = "";
            m_edges = new Dictionary<string, float>();
            m_sights = new Dictionary<string, float>();
            m_sounds = new Dictionary<string, float>();

            m_characterLastAt = new Dictionary<string, int>();
            m_characterLastLOS = new Dictionary<string, int>();
        }

        public Vertex(string key, List<string> edges, List<string> sights, List<string> sounds, List<string> characters)
        {
            m_key = key;

            m_edges = new Dictionary<string, float>();
            foreach (string edge in edges)
                m_edges.Add(edge, 1.0f);

            m_sights = new Dictionary<string, float>() { { m_key, 0.0f } }; // NB: Characters have sightlines to their own room...
            foreach (string sight in sights)
                if (!m_sights.ContainsKey(sight))
                    m_sights.Add(sight, 1.0f);

            m_sounds = new Dictionary<string, float>() { { m_key, 0.0f } };
            foreach (string sound in sounds)
                if (!m_sounds.ContainsKey(sound))
                    m_sounds.Add(sound, 1.0f);

            m_characterLastAt = new Dictionary<string, int>();
            m_characterLastLOS = new Dictionary<string, int>();

            foreach (string character in characters)
            {
                m_characterLastAt.Add(character, int.MinValue);
                m_characterLastLOS.Add(character, int.MinValue);
            }
        }

        public Vertex(Vertex vertex)
        {
            m_key = vertex.m_key;
            m_edges = new Dictionary<string, float>(vertex.m_edges);
            m_sights = new Dictionary<string, float>(vertex.m_sights);
            m_sounds = new Dictionary<string, float>(vertex.m_sounds);

            m_characterLastAt = new Dictionary<string, int>(vertex.m_characterLastAt);
            m_characterLastLOS = new Dictionary<string, int>(vertex.m_characterLastAt);
        }
    }

    private Random m_rng;

    public List<string> m_characters;
    public Dictionary<string, Vertex> m_settings;

    public Stage(int seed, Dictionary<string, Characterisation> characterisations)
    {
        m_rng = new Random(seed);

        m_characters = new List<string>(characterisations.Keys);

        m_settings = ConstructStage(characterisations.Keys.ToList());
    }

    public Stage(int seed, Stage stage)
    {
        m_rng = new Random(seed);

        m_characters = new List<string>(stage.m_characters);

        m_settings = new Dictionary<string, Vertex>();
        foreach (string ID in stage.m_settings.Keys)
            m_settings.Add(ID, new Vertex(stage.m_settings[ID]));
    }

    public Dictionary<string, Vertex> ConstructStage(List<string> characters)
    {
        Dictionary<string, Vertex> vertices = new Dictionary<string, Vertex>();

        /* ------------------------------------------- */
        /* NB: Stage directions, describing the set... */

        // NB: A basic, first draft, without any intriguing details...

        // THE CORRIDOR: The door to the billiards room is firmly shut...

        List<string> corridorEdges = new List<string>() { "the drawing room", "the lounge", "the billiards room", "the library", "the downstairs" };
        List<string> corridorSights = new List<string>() { "the drawing room", "the lounge", "the library" };
        List<string> corridorSounds = new List<string>() { "the drawing room", "the lounge", "the library", "the downstairs" };
        Vertex corridor = new Vertex("the corridor", corridorEdges, corridorSights, corridorSounds, characters);
        vertices.Add(corridor.m_key, corridor);

        /*List<string> corridorEdges = new List<string>() { "the drawing room", "the lounge", "the billiards room", "the library" };
        List<string> corridorSights = new List<string>() { "the drawing room", "the lounge", "the library" };
        Vertex corridor = new Vertex("the corridor", corridorEdges, corridorSights, corridorSights, characters);
        vertices.Add(corridor.m_key, corridor);*/

        // THE DRAWING ROOM: The connecting door to the lounge is closed, as is the French window to the balcony...

        List<string> drawingRoomEdges = new List<string>() { "the corridor", "the lounge", "the balcony" };
        List<string> drawingRoomSights = new List<string>() { "the corridor", "the balcony" };
        List<string> drawingRoomSounds = new List<string>() { "the corridor" };
        Vertex drawingRoom = new Vertex("the drawing room", drawingRoomEdges, drawingRoomSights, drawingRoomSounds, characters);
        vertices.Add(drawingRoom.m_key, drawingRoom);

        List<string> loungeEdges = new List<string>() { "the corridor", "the drawing room" };
        List<string> loungeSights = new List<string>() { "the corridor" };
        Vertex lounge = new Vertex("the lounge", loungeEdges, loungeSights, loungeSights, characters);
        vertices.Add(lounge.m_key, lounge);

        List<string> billiardsRoomEdges = new List<string>() { "the corridor" };
        List<string> billiardsRoomSights = new List<string>() { };
        Vertex billiardsRoom = new Vertex("the billiards room", billiardsRoomEdges, billiardsRoomSights, billiardsRoomSights, characters);
        vertices.Add(billiardsRoom.m_key, billiardsRoom);

        List<string> libraryEdges = new List<string>() { "the corridor" };
        Vertex library = new Vertex("the library", libraryEdges, libraryEdges, libraryEdges, characters);
        vertices.Add(library.m_key, library);

        List<string> balconyEdges = new List<string>() { "the drawing room" };
        List<string> balconySounds = new List<string>() { };
        Vertex balcony = new Vertex("the balcony", balconyEdges, balconyEdges, balconySounds, characters);
        vertices.Add(balcony.m_key, balcony);

        List<string> downstairsEdges = new List<string>() { "the corridor", "the dining room", "the kitchens" };
        List<string> downstairsSights = new List<string>() { "the dining room" };
        List<string> downstairsSounds = new List<string>() { "the corridor", "the dining room" };
        Vertex downstairs = new Vertex("the downstairs", downstairsEdges, downstairsSights, downstairsSounds, characters);
        vertices.Add(downstairs.m_key, downstairs);

        List<string> diningRoomEdges = new List<string>() { "the downstairs", "the kitchens" };
        List<string> diningRoomSights = new List<string>() { "the downstairs" };
        Vertex diningRoom = new Vertex("the dining room", diningRoomEdges, diningRoomSights, diningRoomSights, characters);
        vertices.Add(diningRoom.m_key, diningRoom);

        List<string> kitchensEdges = new List<string>() { "the downstairs", "the dining room", "the larder" };
        List<string> kitchensSights = new List<string>() { };
        Vertex kitchens = new Vertex("the kitchens", kitchensEdges, kitchensSights, kitchensSights, characters);
        vertices.Add(kitchens.m_key, kitchens);

        List<string> larderEdges = new List<string>() { "the kitchens" };
        List<string> larderSights = new List<string>() { };
        Vertex larder = new Vertex("the larder", larderEdges, larderSights, larderSights, characters);
        vertices.Add(larder.m_key, larder);

        /* ------------------------------------------- */

        return vertices;
    }

    public void UpdateCharacter(int tick, string character, string setting)
    {
        if (!m_characters.Contains(character) || !m_settings.ContainsKey(setting) || tick < GetLastAt(character).Value)
            return;

        // STEP 1: Update the setting...
        if (m_settings.ContainsKey(GetLastAt(character).Key))
            m_settings[GetLastAt(character).Key].m_characterLastAt[character] = int.MinValue;

        m_settings[setting].m_characterLastAt[character] = tick;

        // STEP 2: Update the setting's last 'check'...
        foreach (string sight in m_settings[setting].m_sights.Keys) // NB: Replace this with sightlines in a certain distance?
            m_settings[sight].m_characterLastLOS[character] = tick;
    }

    public KeyValuePair<string, int> GetLastAt(string character)
    {
        string setting = m_settings.OrderByDescending(x => x.Value.m_characterLastAt[character]).ToList()[0].Key;
        return new KeyValuePair<string, int>((m_settings[setting].m_characterLastAt[character] >= 0) ? setting : "offstage", m_settings[setting].m_characterLastAt[character]);
    }

    public List<string> GetCharactersAt(int tick, string setting)
    {
        return (m_settings.ContainsKey(setting)) ? m_characters.FindAll(x => GetLastAt(x).Key.Equals(setting) && GetLastAt(x).Value >= tick).ToList() : new List<string>();
    }

    public List<string> GetCharactersLOS(int tick, string setting)
    {
        return (m_settings.ContainsKey(setting)) ? m_characters.FindAll(x => m_settings.ContainsKey(GetLastAt(x).Key) && m_settings[GetLastAt(x).Key].m_sights.ContainsKey(setting) && GetLastAt(x).Value >= tick).ToList() : new List<string>();
    }

    public List<string> GetCharactersEarshot(int tick, string setting)
    {
        return (m_settings.ContainsKey(setting)) ? m_characters.FindAll(x => m_settings.ContainsKey(GetLastAt(x).Key) && m_settings[GetLastAt(x).Key].m_sounds.ContainsKey(setting) && GetLastAt(x).Value >= tick).ToList() : new List<string>();
    }

    public bool IsLost(string focus, string subject)
    {
        // Has the focus been checked since...
        string setting = GetLastAt(subject).Key;
        return !m_settings.ContainsKey(setting) || m_settings[setting].m_characterLastLOS[focus] > m_settings[setting].m_characterLastAt[subject];
    }

    public List<KeyValuePair<string, float>> GetPath(string key, Predicate<Vertex> predicate)
    {
        List<KeyValuePair<string, float>> path = new List<KeyValuePair<string, float>>();

        Dictionary<string, string> sources = new Dictionary<string, string>() { { key, string.Empty } };
        Dictionary<string, float> costs = new Dictionary<string, float>() { { key, 0.0f } };

        PriorityQueue<string, float> frontier = new PriorityQueue<string, float>();
        frontier.Enqueue(key, 0.0f);

        // STEP 1: Run A* algorithm...
        string stepKey = key;
        while (frontier.Count > 0)
        {
            stepKey = frontier.Dequeue();

            if (predicate(m_settings[stepKey]))
                break;

            foreach (string target in m_settings[stepKey].m_edges.Keys)
            {
                float edgeCost = m_settings[stepKey].m_edges[target];
                float targetCost = costs[stepKey] + edgeCost;

                if (!sources.ContainsKey(target))
                {
                    sources.Add(target, stepKey);
                    costs.Add(target, targetCost);

                    // FIXME: Does PriorityQueue work with anything other than a singular float?
                    float tempCost = targetCost;// + HeuristicValue(targetID);
                    frontier.Enqueue(target, tempCost);
                }
                else if (targetCost < costs[target])
                {
                    sources[target] = stepKey;
                    costs[target] = targetCost;

                    // FIXME: Does PriorityQueue work with anything other than a singular float?
                    float tempCost = targetCost;// + HeuristicValue(targetID);
                    frontier.Enqueue(target, tempCost);
                }
            }
        }
        if (!predicate(m_settings[stepKey]))
        {
            return path;
        }

        // STEP 2: Reverse engineer path...
        while (!stepKey.Equals(key))
        {
            KeyValuePair<string, float> step = new KeyValuePair<string, float>(stepKey, m_settings[sources[stepKey]].m_edges[stepKey]);
            path.Insert(0, step);

            stepKey = sources[stepKey];
        }

        return path;
    }

    public float GetPathLength(string key, Predicate<Vertex> predicate)
    {
        return GetPath(key, predicate).Select(x => x.Value).Sum();
    }

    public void PrintCharactersAt(string focus = "NONFOCALISED")
    {
        bool search = m_characters.Contains(focus);

        foreach (string setting in m_settings.Keys)
        {
            List<string> characters = GetCharactersAt(0, setting);
            if (characters.Count == 0)
                continue;
        }
    }

    public string PrintPhysicalState(string focus = "NONFOCALISED", bool printCharacters = false)
    {
        string text = "";

        // STEP 1: Calculate indents...
        string characterAdjoinder = ": ";
        string informationAdjoinder = " ";

        int characterIndent = 0;
        int informationIndent = informationAdjoinder.Length;

        if (printCharacters)
        {
            int nonfocalisedIndent = "NONFOCALISED".Length;
            int focalisedIndent = m_characters.Select(x => x.Length).Max();

            characterIndent = Math.Max(nonfocalisedIndent, focalisedIndent) + characterAdjoinder.Length;
        }

        // STEP 3: Print local information...
        bool focalised = m_characters.Contains(focus);
        foreach (string character in m_characters)
        {
            if (printCharacters)
                text += (character + characterAdjoinder).PadRight(characterIndent);

            string lastAt = GetLastAt(character).Key;
            foreach (string setting in m_settings.Keys)
            {
                text += (((lastAt.Equals(setting)) ? ((!focalised || !IsLost(focus, character)) ? "*" : "?") : "-")  + informationAdjoinder).PadLeft(informationIndent);
            }

            text += "\n";
        }

        return text;
    }
}
