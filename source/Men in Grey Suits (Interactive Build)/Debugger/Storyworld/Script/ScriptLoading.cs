using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

public partial class Script : Node
{
    private struct JsonVertex
    {
        // GRAPH VARIABLES //
        public List<string> EDGES { get; set; }
        public Dictionary<string, float> WEIGHTED_EDGES { get; set; }

        // ROLE VARIABLES //
        public Dictionary<string, JsonRole> ROLES { get; set; }

        // EPISTEMOLOGICAL VARIABLES //
        public List<string> KNOWLEDGE { get; set; }

        // AESTHETIC VARIABLES //
        public string DIEGESIS { get; set; }
        public Dictionary<string, string> MIMESES { get; set; }

        public string MODE { get; set; }
    }

    private struct JsonRole
    {
        public List<string> IMPLICATIONS { get; set; }
        public List<string> IDENTITIES { get; set; }

        public bool? MULTIROLE { get; set; }
    }

    private Dictionary<string, Vertex> LoadVertices(string path, List<string> characters)
    {
        Dictionary<string, Vertex> vertices = new Dictionary<string, Vertex>();

        // STEP 1: Parse the JSON into an intermediary dictionary...
        Dictionary<string, JsonVertex> jsonVertices = JsonSerializer.Deserialize<Dictionary<string, JsonVertex>>(System.IO.File.ReadAllText(path));

        // STEP 2: Construct vertex structs...
        foreach (string line in (jsonVertices!).Keys)
            vertices.Add(line, new Vertex(line, jsonVertices[line], characters, m_rng.Next()));

        // STEP 3: Generate extra details post-hoc 
        vertices = GenerateManhattanEdges(vertices, 6);
        vertices = GenerateStartEdges(vertices);

        // STEP 4: Brief characters, randomly...
        // FIXME: Lack of role assignment!
        int briefings = (int)Math.Ceiling(0.5f * characters.Count);
        foreach (string line in vertices.Keys)
        {
            if (vertices[line].GetCharactersAware().Count >= 0)
            {
                int take = (vertices[line].GetMode().Equals("SMALL TALK")) ? int.MaxValue : briefings; 
                foreach (string character in vertices[line].GetCharacters().OrderBy(x => m_rng.Next()).Take(take))
                {
                    // NB: Each character who starts out aware will have an 'axiomatic' accusation in mind...
                    Dictionary<string, string> axiom = vertices[line].GetMaximalAccusations().OrderBy(x => m_rng.Next()).First();
                    vertices[line].Tick(-1, -1, character, axiom, character);
                }
            }
        }

        return vertices;
    }

    private Dictionary<string, Vertex> GenerateManhattanEdges(Dictionary<string, Vertex> vertices, int maxDegree = int.MaxValue)
    {
        // STEP 1: ...
        Dictionary<string, Dictionary<string, float>> distanceFromTo = new Dictionary<string, Dictionary<string, float>>();
        foreach (string source in vertices.Keys.ToList().FindAll(x => !x.Equals("START")))
        {
            // FIXME: No 'optimisation' of existing edges, to avoid issues with deltaDegree later on...
            foreach (string target in vertices.Keys.ToList().FindAll(x => !x.Equals("START") && !x.Equals(source) && !vertices[source].IsAdjacent(x)))
            {
                float length = GetDistance(vertices, source, x => target.Equals(x.GetLine()));
                if (length < float.MaxValue)
                {
                    if (!distanceFromTo.ContainsKey(source))
                        distanceFromTo.Add(source, new Dictionary<string, float>());
                    distanceFromTo[source].Add(target, length);
                }
            }
        }

        // STEP 2: ...
        foreach (string source in distanceFromTo.Keys)
        {
            int degree = vertices[source].GetEdges().Count;
            int deltaDegree = Math.Max(maxDegree - degree, 0);

            List<string> edges = distanceFromTo[source].OrderBy(x => x.Value).ThenBy(x => m_rng.Next()).Select(x => x.Key).Take(deltaDegree).ToList();
            foreach (string target in distanceFromTo[source].Keys)
                if (!edges.Contains(target))
                    distanceFromTo[source].Remove(target);
        }

        // STEP 3: ...
        foreach (string source in distanceFromTo.Keys)
        {
            foreach (string target in distanceFromTo[source].Keys)
            {
                vertices[source].AddEdge(target, distanceFromTo[source][target]);
            }
        }

        return vertices;
    }

    private Dictionary<string, Vertex> GenerateStartEdges(Dictionary<string, Vertex> vertices)
    {
        // STEP 1: ...
        Dictionary<string, float> distanceFromStart = new Dictionary<string, float>();
        foreach (string target in vertices.Keys.ToList().FindAll(x => !x.Equals("START")))
        {
            float length = GetDistance(vertices, "START", x => target.Equals(x.GetLine()));
            if (length < float.MaxValue)
                distanceFromStart.Add(target, length);
        }

        // STEP 2: ...
        //List<string> startEdges = vertices["START"].m_edges.Keys.ToList();
        foreach (string source in distanceFromStart.Keys)
        {
            // DEBUG: Minimal approach?
            foreach (string target in vertices["START"].GetEdges().Keys.ToList().FindAll(x => !source.Equals(x)))
            {
                vertices[source].AddEdge(target, 2.5f); // NB: Setting to 1.0f balances distance against repetition better?
            }
        }

        return vertices;
    }
}