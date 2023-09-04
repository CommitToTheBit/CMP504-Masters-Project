using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

public partial class SiftingUnexpectedness : Node
{
    private Random m_rng;

    private Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, float>>>>? m_likelihoods;

    public SiftingUnexpectedness(int seed) 
    { 
        m_rng = new Random(seed);
    }

    public bool LoadLikelihoods()
    {
        string path = (OS.IsDebugBuild()) ? ProjectSettings.GlobalizePath("Assets/Narrative/LIKELIHOODS.json") : OS.GetExecutablePath().GetBaseDir() + "/Assets/Narrative/LIKELIHOODS.json";
        if (!System.IO.File.Exists(path))
            return false;

        m_likelihoods = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, float>>>>>(System.IO.File.ReadAllText(path))!;

        return m_likelihoods is not null;
    }

    // DEPRECATED: I've commented this function out, since I'm just using pre-generated likelihoods from the non-interactive build...
    /*public void SaveLikelihoods(string timestamp, List<Microanthology> controlMicroanthologies, bool temporaries = false)
    {
        Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, float>>>> totalPropertyCounts = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, float>>>>();
        Dictionary<string, Dictionary<string, int>> totalPatternCounts = new Dictionary<string, Dictionary<string, int>>();

        foreach (Microanthology control in controlMicroanthologies)
        {
            Dictionary<string, Dictionary<string, List<Microstory>>> microstories = control.GetMicrostories();
            foreach (string focalisation in microstories.Keys)
            {
                if (!totalPropertyCounts.ContainsKey(focalisation))
                {
                    totalPropertyCounts.Add(focalisation, new Dictionary<string, Dictionary<string, Dictionary<string, float>>>());
                    totalPatternCounts.Add(focalisation, new Dictionary<string, int>());
                }

                Dictionary<string, Dictionary<string, Dictionary<string, int>>> propertyCounts = control.GetStatisticalPropertyCounts(focalisation);
                Dictionary<string, int> patternCounts = control.GetPatternCounts(focalisation);

                // STEP 1: Sum property counts...
                foreach (string pattern in propertyCounts.Keys)
                {
                    if (!totalPropertyCounts[focalisation].ContainsKey(pattern))
                        totalPropertyCounts[focalisation].Add(pattern, new Dictionary<string, Dictionary<string, float>>());

                    foreach (string property in propertyCounts[pattern].Keys)
                    {
                        if (!totalPropertyCounts[focalisation][pattern].ContainsKey(property))
                            totalPropertyCounts[focalisation][pattern].Add(property, new Dictionary<string, float>());

                        foreach (string value in propertyCounts[pattern][property].Keys)
                        {
                            if (!totalPropertyCounts[focalisation][pattern][property].ContainsKey(value))
                                totalPropertyCounts[focalisation][pattern][property].Add(value, propertyCounts[pattern][property][value]);
                            else
                                totalPropertyCounts[focalisation][pattern][property][value] += propertyCounts[pattern][property][value];
                        }
                    }
                }

                // STEP 2: Sum property totals...
                foreach (string pattern in patternCounts.Keys)
                {
                    if (!totalPatternCounts[focalisation].ContainsKey(pattern))
                        totalPatternCounts[focalisation].Add(pattern, microstories[focalisation][pattern].Count);
                    else
                        totalPatternCounts[focalisation][pattern] += microstories[focalisation][pattern].Count;
                }
            }
        }

        // STEP 3: Produce likelihoods...
        foreach (string focalisation in totalPropertyCounts.Keys)
            foreach (string pattern in totalPropertyCounts[focalisation].Keys)
                foreach (string property in totalPropertyCounts[focalisation][pattern].Keys)
                    foreach (string value in totalPropertyCounts[focalisation][pattern][property].Keys)
                        totalPropertyCounts[focalisation][pattern][property][value] /= totalPatternCounts[focalisation][pattern];

        // STEP 4: Save likelihoods...
        string text = "";

        foreach (string focalisation in totalPropertyCounts.Keys)
        {
            text += focalisation + "\n{\n";
            foreach (string pattern in totalPropertyCounts[focalisation].Keys)
            {
                text += "   " + pattern + "\n   {\n";
                foreach (string property in totalPropertyCounts[focalisation][pattern].Keys)
                {
                    text += "      " + property + "\n";
                    foreach (string value in totalPropertyCounts[focalisation][pattern][property].Keys)
                    {
                        text += "        " + value + ": " + totalPropertyCounts[focalisation][pattern][property][value].ToString("0.0000");
                        if (!totalPropertyCounts[focalisation][pattern][property].Keys.Last().Equals(value))
                            text += "\n";
                    }
                    if (!totalPropertyCounts[focalisation][pattern].Keys.Last().Equals(property))
                        text += "\n\n";
                }
                text += "\n   }";
                if (!totalPropertyCounts[focalisation].Keys.Last().Equals(pattern))
                    text += "\n\n";
            }
            text += "\n}\n\n";
        }

        File.WriteAllText(Environment.CurrentDirectory.Split("bin")[0] + "Sifting/Sifting Heuristics/LIKELIHOODS.txt", text);
        if (temporaries)
            File.WriteAllText(Environment.CurrentDirectory.Split("bin")[0] + "Sifting/Sifting Heuristics/" + timestamp + " LIKELIHOODS.txt", text);

        File.WriteAllText(Environment.CurrentDirectory.Split("bin")[0] + "Sifting/Sifting Heuristics/LIKELIHOODS.json", JsonSerializer.Serialize(totalPropertyCounts));
    }*/

    public Microanthology SurfaceMicrostories(Microanthology microanthology, int count = int.MaxValue)
    {
        // NB: In hindsight, the "Microanthology" structure is absolutely quite clunky - never got around to refactoring it!
        Dictionary<string, Dictionary<string, List<Microstory>>> surfacedMicrostories = new Dictionary<string, Dictionary<string, List<Microstory>>>();

        // NB: Duplicate microstories are still considered; the 'most unexpected' takes priority...
        Dictionary<string, Dictionary<string, List<Microstory>>> microstories = microanthology.GetMicrostories(false);
        foreach (string focalisation in microstories.Keys)
        {
            surfacedMicrostories.Add(focalisation, new Dictionary<string, List<Microstory>>());
            foreach (string pattern in microstories[focalisation].Keys)
            {
                surfacedMicrostories[focalisation].Add(pattern, new List<Microstory>());

                // NB: Finding n most unexpected 'variants' of distinct microstories...
                List<Microstory> unexpectedMicrostories = microstories[focalisation][pattern].OrderByDescending(x => CalculateUnexpectedness(focalisation, pattern, x)).ThenBy(x => m_rng.Next()).ToList();
                for (int i = 0; i < unexpectedMicrostories.Count && surfacedMicrostories[focalisation][pattern].Count < count; i++)
                    if (surfacedMicrostories[focalisation][pattern].FindAll(x => unexpectedMicrostories[i].Equals(x)).Count() == 0)
                        surfacedMicrostories[focalisation][pattern].Add(unexpectedMicrostories[i]);

            }
        }

        return new Microanthology(surfacedMicrostories);
    }

    private double CalculateUnexpectedness(string focalisation, string pattern, Microstory microstory)
    {
        double unexpectedness = 1.0;
        if (!m_likelihoods.ContainsKey(focalisation) || !m_likelihoods[focalisation].ContainsKey(pattern))
            return unexpectedness;

        // STEP 1: ...
        // FIXME: Terms unclear! Does logic hold?
        Dictionary<string, List<string>> properties = microstory.GetStatisticalProperties(); // FIXME: Funnel through microanthology?...
        foreach (string property in m_likelihoods[focalisation][pattern].Keys)
        {
            double expectedness = 1.0;
            if (properties.ContainsKey(property))
            {
                // IMPLEMENTATION 1: Considers least expected event...
                if (properties[property].FindAll(x => !m_likelihoods[focalisation][pattern][property].ContainsKey(x)).Count == 0)
                {
                    string value = properties[property].OrderBy(x => m_likelihoods[focalisation][pattern][property][x]).ElementAt(0);
                    expectedness *= m_likelihoods[focalisation][pattern][property][value];
                }
                else
                {
                    expectedness *= 0.0;
                }
            }
            else
            {
                expectedness *= 1.0f;
            }

            double reduction = 0.75; // INTUITION: A total expected property reduces from 1.0 down to reduction!
            unexpectedness *= 1.0 - (1.0 - reduction) * expectedness; // NB: Using likelihood, not information value... (?)
        }

        return unexpectedness;
    }
}
