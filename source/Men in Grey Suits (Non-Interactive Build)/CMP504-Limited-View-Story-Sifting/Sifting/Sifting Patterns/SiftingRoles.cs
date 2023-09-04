using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SiftingRoles : SiftingPatterns
{
    public SiftingRoles(ref Script script)
    {
        m_patterns = GetRolesPatterns(script.GetRoles());
    }

    private Dictionary<string, Microstory> GetRolesPatterns(List<string> roles)
    {
        Dictionary<string, Microstory> patterns = new Dictionary<string, Microstory>();

        foreach (string role in roles)
        {
            patterns.Add("Allusion to " + role, new Microstory(
                new Dictionary<string, List<string>>() { { "events", new List<string>() { "?event", "?next" } }, { "values", new List<string>() { "?actor" } } },
                new List<List<string>>() { new List<string>() { "?event", "action", "talk" }, new List<string>() { "?event", "actor", "?actor" }, new List<string>() { "?event", "roles", role }, new List<string>() { "?next", "action", "talk" }, new List<string>() { "?next", "audience", "?actor" } },
                new List<List<string>>() { new List<string>() { "sequence", "?event", "?next" }, new List<string>() { "without interruption", "?event", "?next", "action=talk", "audience=?actor" } }
            ));
        }

        return patterns;
    }
}
