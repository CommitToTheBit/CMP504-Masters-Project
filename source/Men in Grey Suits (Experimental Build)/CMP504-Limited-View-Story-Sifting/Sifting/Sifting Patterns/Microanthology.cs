using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Microanthology
{
    private Dictionary<string, Dictionary<string, List<Microstory>>> m_microstories; // KEYS: Focalisation, then pattern, then instances...

    public Microanthology(Dictionary<string, Dictionary<string, List<Microstory>>> microstories)
    {
        m_microstories = new Dictionary<string, Dictionary<string, List<Microstory>>>();
        foreach (string focalisation in microstories.Keys)
        {
            m_microstories.Add(focalisation, new Dictionary<string, List<Microstory>>());
            foreach (string pattern in microstories[focalisation].Keys)
            {
                m_microstories[focalisation].Add(pattern, new List<Microstory>());
                foreach (Microstory microstory in microstories[focalisation][pattern])
                    m_microstories[focalisation][pattern].Add(microstory);
            }
        }
    }

    public Dictionary<string, Dictionary<string, List<Microstory>>> GetMicrostories(bool distinct = false, int limit = int.MaxValue)
    {
        Dictionary<string, Dictionary<string, List<Microstory>>> microstories = new Dictionary<string, Dictionary<string, List<Microstory>>>();
        foreach (string focalisation in m_microstories.Keys)
        {
            microstories.Add(focalisation, new Dictionary<string, List<Microstory>>());
            foreach (string pattern in m_microstories[focalisation].Keys)
                microstories[focalisation].Add(pattern, ((distinct) ? new List<Microstory>(m_microstories[focalisation][pattern]).Distinct().ToList() : new List<Microstory>(m_microstories[focalisation][pattern])).Take(limit).ToList());
        }

        return microstories;
    }

    public Dictionary<string, Dictionary<string, Dictionary<string, int>>> GetStatisticalPropertyCounts(string focalisation) 
    {
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> propertyCounts = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
        foreach (string pattern in m_microstories[focalisation].Keys)
        {
            propertyCounts.Add(pattern, new Dictionary<string, Dictionary<string, int>>());
            foreach (Microstory microstory in m_microstories[focalisation][pattern]) // FIXME: '5% certainty!'
            {
                Dictionary<string, List<string>> properties = microstory.GetStatisticalProperties();
                foreach (string property in properties.Keys)
                {
                    if (!propertyCounts[pattern].ContainsKey(property))
                        propertyCounts[pattern].Add(property, new Dictionary<string, int>());

                    foreach (string value in properties[property])
                    {
                        if (!propertyCounts[pattern][property].ContainsKey(value))
                            propertyCounts[pattern][property].Add(value, 1);
                        else
                            propertyCounts[pattern][property][value]++;
                    }
                }
            }
        }

        return propertyCounts;
    }

    public Dictionary<string, int> GetPatternCounts(string focalisation)
    {
        Dictionary<string, int> patternCounts = new Dictionary<string, int>();
        foreach (string pattern in m_microstories[focalisation].Keys)
            patternCounts.Add(pattern, m_microstories[focalisation][pattern].Count);

        return patternCounts;
    }

    private string PrintMicroanthology(string focalisation, bool print = true)
    {
        string text = "";

        // NB: Limit imposed, to speed up writing to file!
        Dictionary<string, Dictionary<string, List<Microstory>>> microstories = GetMicrostories(false, 100); // NB: Turning off distinctness for simplicity's sake...
        foreach (string pattern in microstories[focalisation].Keys)
        {
            for (int i = 0; i < microstories[focalisation][pattern].Count(); i++)
            {
                text += pattern.ToUpper()+", "+ "MICROSTORY #" + (i + 1) + ":\n";
                text += microstories[focalisation][pattern][i].PrintMicrostory(false);
            }
        }

        if (print)
            Console.Write(text);

        return text;
    }

    private void WriteMicroanthologyToFile(string timestamp, string focalisation, string surfacing, bool temporaries = false, bool print = true)
    {
        string text = PrintMicroanthology(focalisation, false);

        File.WriteAllText(Environment.CurrentDirectory.Split("bin")[0] + "Logs/" + focalisation + " (" + surfacing + ").txt", text);
        if (temporaries)
            File.WriteAllText(Environment.CurrentDirectory.Split("bin")[0] + "Logs/" + timestamp + " " + focalisation + " (" + surfacing + ").txt", text);
    }

    public void WriteMicroanthologyToFile(string timestamp, string surfacing, bool temporaries = false, bool print = true)
    {
        foreach (string focalisation in m_microstories.Keys)
            WriteMicroanthologyToFile(timestamp, focalisation, surfacing, temporaries, print && focalisation.Equals("NONFOCALISED"));
    }

    private string PrintPerspective(string pattern, bool print = true)
    {
        string text = "";

        Dictionary<string, Dictionary<string, List<Microstory>>> microstories = GetMicrostories(true);
        foreach (string focalisation in microstories.Keys.ToList().FindAll(x => !x.Equals("NONFOCALISED")))
        {
            for (int i = 0; i < microstories[focalisation][pattern].Count; i++)
            {
                text += microstories[focalisation][pattern][i].PrintMicrostory(false);
            }
        }

        if (print)
            Console.Write(text);

        return text;
    }

    private void WritePerspectiveToFile(string timestamp, string pattern, string surfacing, bool temporaries = false, bool print = true)
    {
        string text = PrintPerspective(pattern, false);

        File.WriteAllText(Environment.CurrentDirectory.Split("bin")[0] + "Logs/Perspectives/" + pattern + " (" + surfacing + ").txt", text);
        if (temporaries)
            File.WriteAllText(Environment.CurrentDirectory.Split("bin")[0] + "Logs/Perspectives/" + timestamp + " " + pattern + " (" + surfacing + ").txt", text);
    }

    public void WritePerspectiveToFile(string timestamp, string surfacing, bool temporaries = false, bool print = true)
    {
        if (m_microstories.ContainsKey("NONFOCALISED"))
            foreach (string pattern in m_microstories["NONFOCALISED"].Keys)
                WritePerspectiveToFile(timestamp, pattern, surfacing, temporaries, print && pattern.Equals("NONFOCALISED"));
    }
}
