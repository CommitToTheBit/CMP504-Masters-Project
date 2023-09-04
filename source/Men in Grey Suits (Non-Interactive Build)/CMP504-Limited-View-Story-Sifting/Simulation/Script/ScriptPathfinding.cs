using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agent;

public partial class Script
{
    public float GetDistance(string origin, string line)
    {
        Predicate<Vertex> predicate = x => !x.Equals(line);
        return GetDistance(origin, predicate);
    }

    public float GetDistanceFromUnknown(string origin, string character)
    {
        Predicate<Vertex> predicate = x => !x.IsAware(character);
        return GetDistance(origin, predicate);
    }

    public float GetDistanceFromUnknownImplication(string origin, string character, List<string> implications)
    {
        Predicate<Vertex> predicate = x => !x.IsAware(character) && x.GetAllImplications().Intersect(implications).Count() > 0;
        return GetDistance(origin, predicate);
    }

    public float GetDistanceFromKnownAccusation(string origin, string character, List<string> accusees)
    {
        Predicate<Vertex> predicate = x => x.IsAware(character) && x.GetAllIdentities().Intersect(accusees).Count() > 0;
        return GetDistance(origin, predicate);
    }

    private float GetDistance(string origin, Predicate<Vertex> predicate)
    {
        return GetDistance(m_lines, origin, predicate);
    }

    private float GetDistance(Dictionary<string, Vertex> vertices, string key, Predicate<Vertex> predicate)
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

            if (predicate(vertices[stepKey]))
                break;

            foreach (string target in vertices[stepKey].GetSegues())
            {
                float edgeCost = vertices[stepKey].GetEdge(target);
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
        if (!predicate(vertices[stepKey]))
        {
            return float.MaxValue;
        }

        // STEP 2: Reverse engineer path...
        while (!stepKey.Equals(key))
        {
            KeyValuePair<string, float> step = new KeyValuePair<string, float>(stepKey, vertices[sources[stepKey]].GetEdge(stepKey));
            path.Insert(0, step);

            stepKey = sources[stepKey];
        }

        return path.Select(x => x.Value).Sum();
    }
}
