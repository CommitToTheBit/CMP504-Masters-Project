using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Script
{
    private class Vertex
    {
        // GRAPH VARIABLES //
        private string m_line;
        private Dictionary<string, float> m_edges; // NB: "Edge" is used as shorthand for 'out edges'... presumably, there's no need for 'in edges,' or even an 'in degree'? 

        // ROLE VARIABLES //
        private Dictionary<string, List<string>> m_implications, m_identities;
        private Dictionary<string, bool> m_multiroles;

        // EPISTEMOLOGICAL VARIABLES //
        private Dictionary<string, List<(int index, int tick, string actor, Dictionary<string, string> accusation)>> m_awareness; // NB: Specific instances of each role...

        // AESTHETIC VARIABLES //
        private string m_diegesis;
        private Dictionary<string, string> m_mimeses;

        private string m_mode;

        public Vertex()
        {
            m_line = string.Empty;
            m_edges = new Dictionary<string, float>();

            m_awareness = new Dictionary<string, List<(int, int, string, Dictionary<string, string>)>>();

            m_implications = new Dictionary<string, List<string>>();
            m_identities = new Dictionary<string, List<string>>();
            m_multiroles = new Dictionary<string, bool>();

            m_diegesis = string.Empty;
            m_mimeses = new Dictionary<string, string>();

            m_mode = string.Empty;
        }

        public Vertex(string line, JsonVertex jsonVertex, List<string> characters, int seed)
        {
            Random rng = new Random(seed);

            m_line = line;

            m_edges = new Dictionary<string, float>();
            if (jsonVertex.EDGES is not null)
                foreach (string edge in jsonVertex.EDGES)
                    m_edges.Add(edge, 1.0f);
            if (jsonVertex.WEIGHTED_EDGES is not null)
                foreach (string edge in jsonVertex.WEIGHTED_EDGES.Keys)
                    m_edges.Add(edge, jsonVertex.WEIGHTED_EDGES[edge]);

            m_implications = new Dictionary<string, List<string>>();
            m_identities = new Dictionary<string, List<string>>();
            m_multiroles = new Dictionary<string, bool>();
            if (jsonVertex.ROLES is not null)
            {
                foreach (string role in jsonVertex.ROLES.Keys)
                {
                    m_implications.Add(role, new List<string>());
                    if (jsonVertex.ROLES[role].IMPLICATIONS is not null)
                        m_implications[role] = jsonVertex.ROLES[role].IMPLICATIONS!;

                    m_identities.Add(role, new List<string>());
                    if (jsonVertex.ROLES[role].IDENTITIES is not null)
                        m_identities[role] = jsonVertex.ROLES[role].IDENTITIES!;

                    bool multirole = jsonVertex.ROLES[role].MULTIROLE is not null && (bool)jsonVertex.ROLES[role].MULTIROLE!; // NB: Returns False if null!
                    m_multiroles.Add(role, multirole);
                }
            }

            m_diegesis = (jsonVertex.DIEGESIS is not null) ? jsonVertex.DIEGESIS : string.Empty;
            m_mode = (jsonVertex.MODE is not null) ? jsonVertex.MODE : string.Empty;

            m_awareness = new Dictionary<string, List<(int, int, string, Dictionary<string, string>)>>();
            m_mimeses = new Dictionary<string, string>();
            foreach (string character in characters)
            {
                m_awareness.Add(character, new List<(int, int, string, Dictionary<string, string>)>());
                if (jsonVertex.KNOWLEDGE is not null && jsonVertex.KNOWLEDGE.Contains(character))
                {
                    // NB: Each character who starts out aware will have an 'axiomatic' accusation in mind...
                    Dictionary<string, string> axiom = GetMaximalAccusations().OrderBy(x => rng.Next()).First();
                    Tick(-1, -1, character, axiom, character);
                }

                m_mimeses.Add(character, string.Empty);
                if (jsonVertex.MIMESES is not null && jsonVertex.MIMESES.ContainsKey(character))
                    m_mimeses[character] = jsonVertex.MIMESES[character];
            }
        }

        public Vertex(Vertex vertex)
        {
            m_line = vertex.m_line;
            m_edges = new Dictionary<string, float>(vertex.m_edges);

            m_awareness = new Dictionary<string, List<(int, int, string, Dictionary<string, string>)>>();
            foreach (string character in vertex.m_awareness.Keys)
            {
                m_awareness.Add(character, new List<(int, int, string, Dictionary<string, string>)>());
                foreach ((int index, int tick, string actor, Dictionary<string, string> accusation) in vertex.m_awareness[character])
                    m_awareness[character].Add((index, tick, actor, new Dictionary<string, string>(accusation)));
            }

            m_implications = new Dictionary<string, List<string>>();
            foreach (string role in vertex.m_implications.Keys)
                m_implications.Add(role, new List<string>(vertex.m_implications[role]));

            m_identities = new Dictionary<string, List<string>>();
            foreach (string role in vertex.m_identities.Keys)
                m_identities.Add(role, new List<string>(vertex.m_identities[role]));

            m_multiroles = new Dictionary<string, bool>(vertex.m_multiroles);

            m_diegesis = vertex.m_diegesis;
            m_mimeses = new Dictionary<string, string>(vertex.m_mimeses);

            m_mode = vertex.m_mode;
        }

        public void Focalise(string focalisation)
        {
            foreach (string character in GetCharacters())
                if (!character.Equals(focalisation))
                    m_awareness[character] = new List<(int, int, string, Dictionary<string, string>)>();
        }

        public void Tick(int index, int tick, string actor, Dictionary<string, string> accusation, string audience)
        {
            Tick(index, tick, actor, accusation, new List<string>() { audience });
        }

        public void Tick(int index, int tick, string actor, Dictionary<string, string> accusation, List<string> audience)
        {
            foreach (string character in audience)
                if (m_awareness.ContainsKey(character))
                    m_awareness[character].Add((index, tick, actor, new Dictionary<string, string>(accusation)));
        }

        public void AddEdge(string line, float length)
        {
            if (!IsAdjacent(line))
                m_edges.Add(line, length);
            else if (m_edges[line] > length)
                m_edges[line] = length;
        }

        public string GetLine()
        {
            return m_line;
        }

        public List<string> GetSegues()
        {
            return new List<string>(m_edges.Keys);
        }

        public Dictionary<string, float> GetEdges()
        {
            return new Dictionary<string, float>(m_edges);
        }

        public float GetEdge(string line)
        {
            return (IsAdjacent(line)) ? m_edges[line] : float.MaxValue;
        }

        public List<string> GetCharacters()
        {
            return m_awareness.Keys.ToList();
        }

        public List<string> GetCharactersAware()
        {
            return GetCharacters().FindAll(x => IsAware(x));
        }

        public List<string> GetCharactersJustAware()
        {
            return GetCharacters().FindAll(x => IsJustAware(x));
        }

        public int GetTotalAwareness(string character)
        {
            return (m_awareness.ContainsKey(character)) ? m_awareness[character].Count : 0;
        }

        public Dictionary<string, int> GetTotalAwarenesses(List<string> characters)
        {
            Dictionary<string, int> awarenesses = new Dictionary<string, int>();
            foreach (string character in characters)
                awarenesses.Add(character, GetTotalAwareness(character));

            return awarenesses;
        }

        public Dictionary<string, int> GetTotalAwarenesses()
        {
            return GetTotalAwarenesses(GetCharacters());
        }

        public List<string> GetRoles()
        {
            List<string> roles = new List<string>();
            roles = roles.Union(m_implications.Keys).ToList();
            roles = roles.Union(m_identities.Keys).ToList();

            return roles;
        }

        public List<string> GetIdentifiableRoles()
        {
            return m_identities.ToList().FindAll(x => x.Value.Count > 0).Select(x => x.Key).ToList();
        }

        public List<string> GetImplications(string role)
        {
            return (m_implications.ContainsKey(role)) ? new List<string>(m_implications[role]) : new List<string>();
        }

        public List<string> GetAllImplications()
        {
            List<string> implications = new List<string>();
            foreach (string role in m_implications.Keys)
                foreach (string implication in m_implications[role])
                    if (!implications.Contains(implication))
                        implications.Add(implication);

            return implications;
        }

        public List<string> GetIdentities(string role)
        {
            return (m_identities.ContainsKey(role)) ? new List<string>(m_identities[role]) : new List<string>();
        }

        public List<string> GetAllIdentities()
        {
            List<string> identities = new List<string>();
            foreach (string role in GetRoles())
                foreach (string identity in GetIdentities(role))
                    if (!identities.Contains(identity))
                        identities.Add(identity);

            return identities;
        }

        public List<Dictionary<string, string>> GetAllAccusations()
        {
            List<Dictionary<string, string>> accusations = new List<Dictionary<string, string>>() { new Dictionary<string, string>() };

            List<Dictionary<string, string>> iteratedAccusations;
            foreach (string role in m_identities.Keys)
            {
                iteratedAccusations = new List<Dictionary<string, string>>();

                foreach (Dictionary<string, string> accusation in accusations)
                {
                    // STEP 1: Allow unassigned roles...
                    iteratedAccusations.Add(new Dictionary<string, string>(accusation));

                    // STEP 2: ...
                    // NB: Forces different characters to fill all roles; worth it, since all rumours will be short snippets?
                    Predicate<string> uninvolved = x => !accusation.ContainsValue(x);
                    foreach (string identity in m_identities[role].FindAll(uninvolved))
                    {
                        Dictionary<string, string> iteratedAccusation = new Dictionary<string, string>(accusation);
                        iteratedAccusation.Add(role, identity);

                        iteratedAccusations.Add(iteratedAccusation);
                    }
                }

                accusations = iteratedAccusations;
            }

            return accusations;
        }

        public List<Dictionary<string, string>> GetMaximalAccusations(int flexibility = 0)
        {
            List<Dictionary<string, string>> accusations = GetAllAccusations();
            int maxAccused = accusations.Select(x => x.Count).Max();

            return accusations.FindAll(x => x.Count >= maxAccused - flexibility);
        }

        public string GetDiegesis()
        {
            return m_diegesis;
        }

        public string GetMimesis(string character)
        {
            return (m_mimeses.ContainsKey(character)) ? m_mimeses[character] : string.Empty;
        }

        public string GetMode()
        {
            return m_mode;
        }

        public string GetJokeOrigin(string character)
        {
            if (!IsJoke()) return string.Empty;

            // NB: If you came up with the joke in 'before-time' and didn't make it fast enough - tough!
            List<(int index, int tick, string actor, Dictionary<string, string> accusation)> awarenesses = GetAwarenessesSince(character, 0);
            return (awarenesses.Count > 0) ? awarenesses.First().actor : string.Empty;
        }

        public bool IsAdjacent(string line)
        {
            return m_edges.ContainsKey(line);
        }

        public bool IsAware(string character)
        {
            return character.Equals("NONFOCALISED") || (m_awareness.ContainsKey(character) && m_awareness[character].Count > 0);
        }

        public bool IsJustAware(string character)
        {
            // NB: Does this account for an instance where the first time a character is observed hearing a line...
            // ...and they're the one saying it? Yes - in BTalk!
            return m_awareness.ContainsKey(character) && m_awareness[character].Count == 1;
        }

        public bool IsRumour()
        {
            List<string> modes = new List<string>() { "RUMOUR" };
            return modes.Contains(m_mode.ToUpper());
        }

        public bool IsRumourLearned(string character)
        {
            return IsRumour() && IsJustAware(character);
        }

        public bool IsJoke()
        {
            List<string> modes = new List<string>() { "JOKE" };
            return modes.Contains(m_mode.ToUpper());
        }

        public bool IsJokeStolen(string character)
        {
            string origin = GetJokeOrigin(character);
            return origin.Length > 0 && !origin.Equals(character);
        }



        // ACCESSORS FOR SCRIPT ONLY //

        public List<(int index, int tick, string actor, Dictionary<string, string> accusation)> GetAwarenesses(string character)
        {
            if (!m_awareness.ContainsKey(character)) return new List<(int, int, string, Dictionary<string, string>)>();

            List<(int, int, string, Dictionary<string, string>)> awareness = new List<(int, int, string, Dictionary<string, string>)>();
            foreach ((int index, int tick, string actor, Dictionary<string, string> accusation) in m_awareness[character])
                awareness.Add((index, tick, actor, new Dictionary<string, string>(accusation)));

            return awareness;
        }

        public List<(int index, int tick, string actor, Dictionary<string, string> accusation)> GetAwarenessesSince(string character, int tick)
        {
            return GetAwarenesses(character).FindAll(x => x.tick >= tick);
        }

        public bool IsMultirole(string role)
        {
            return m_multiroles[role];
        }
    }
}
