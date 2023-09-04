using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

public struct Trace
{
    private List<Beat> m_beats;

    public Trace()
    {
        m_beats = new List<Beat>();
    }

    public void AddBeat(Beat beat)
    {
        m_beats.Add(beat);
    }

    public void AddBeat(int tick, Dictionary<string, List<string>> attributes, float utility)
    {
        AddBeat(new Beat(tick, attributes, utility));
    }

    public void AddPause(int tick, string focalisation, string setting, List<string> characters)
    {
        AddBeat(new BPause(tick, focalisation, setting, characters));
    }

    public void AddWalk(int tick, string focalisation, Agent.Walk walk, List<string> audience)
    {
        AddBeat(new BWalk(tick, focalisation, walk, audience));
    }

    public void AddTalk(string focalisation, Agent.Talk talk, List<string> externalAudience, Script script, Stage stage)
    {
        AddBeat(new BTalk(focalisation, talk, externalAudience, script, stage));
    }

    public List<Beat> FindBeats(Predicate<Beat> query)
    {
        return m_beats.FindAll(query);
    }

    public List<int> IndexBeats(Predicate<Beat> query)
    {
        List<Beat> indexedBeats = FindBeats(x => true);
        List<Beat> beats = FindBeats(query);

        return beats.Select(x => indexedBeats.IndexOf(x)).OrderBy(x => x).ToList();
    }

    private string PrintTrace(bool print = true)
    {
        string text = "";

        int indentLength = 9;
        for (int i = 0; i < m_beats.Count; i++)
            text += m_beats[i].PrintBeat(indentLength, "", true, false) + ((i == m_beats.Count - 1 || m_beats[i].GetTick() != m_beats[i + 1].GetTick()) ? "\n" : "");

        if (print)
            Console.Write(text);

        return text;
    }

    public void WriteTraceToFile(string timestamp, string focalisation, bool temporaries = false, bool print = true)
    {
        string text = PrintTrace(print);

        File.WriteAllText(Environment.CurrentDirectory.Split("bin")[0] + "Logs/" + focalisation + ".txt", text);
        if (temporaries)
            File.WriteAllText(Environment.CurrentDirectory.Split("bin")[0] + "Logs/" + timestamp + " " + focalisation + ".txt", text);
    }
}
