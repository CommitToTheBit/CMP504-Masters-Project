using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SiftingDialogue : SiftingPatterns
{
    public SiftingDialogue(ref Script script)
    {
        m_patterns = GetDialoguePatterns(script.GetLines());
    }

    private Dictionary<string, Microstory> GetDialoguePatterns(List<string> lines)
    {
        Dictionary<string, Microstory> patterns = new Dictionary<string, Microstory>();

        foreach (string line in lines)
        {
            patterns.Add("Segue to " + line, new Microstory(
                new Dictionary<string, List<string>>() { { "events", new List<string>() { "?event", "?next" } }, { "values", new List<string>() { "?actor" } } },
                new List<List<string>>() { new List<string>() { "?event", "action", "talk" }, new List<string>() { "?event", "actor", "?actor" }, new List<string>() { "?event", "final line", line }, new List<string>() { "?next", "action", "talk" }, new List<string>() { "?next", "audience", "?actor" } },
                new List<List<string>>() { new List<string>() { "sequence", "?event", "?next" }, new List<string>() { "without interruption", "?event", "?next", "action=talk", "audience=?actor" } }
            ));
        }

        return patterns;
    }
}
