using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Microstory
{
    public Dictionary<string, Beat> m_beatSlots;
    public Dictionary<string, string> m_valueSlots;

    public Dictionary<string, List<string>> m_attributeConstraints;
    public Dictionary<string, List<KeyValuePair<string, string>>> m_valueConstraints;
    public List<List<string>> m_relations;

    public Microstory(Dictionary<string, List<string>> slots, List<List<string>> constraints, List<List<string>> relations)
    {
        // FIXME: Creates some authorial burden in the JSON...
        m_beatSlots = new Dictionary<string, Beat>();
        if (slots.ContainsKey("events"))
            foreach (string slot in slots["events"])
                m_beatSlots.Add(slot, new Beat());

        m_valueSlots = new Dictionary<string, string>();
        if (slots.ContainsKey("values"))
            foreach (string slot in slots["values"])
                m_valueSlots.Add(slot, "");

        m_valueConstraints = new Dictionary<string, List<KeyValuePair<string, string>>>();
        foreach (string slot in m_beatSlots.Keys)
        {
            m_valueConstraints.Add(slot, new List<KeyValuePair<string, string>>());
            foreach (List<string> constraint in constraints.FindAll(x => x.Count() == 3 && x[0].Equals(slot)))
                m_valueConstraints[slot].Add(new KeyValuePair<string, string>(constraint[1], constraint[2]));
        }

        m_relations = new List<List<string>>();
        foreach (List<string> relation in relations)
            m_relations.Add(new List<string>(relation));
    }

    public Microstory(Microstory microstory)
    {
        m_beatSlots = new Dictionary<string, Beat>(microstory.m_beatSlots);
        m_valueSlots = new Dictionary<string, string>(microstory.m_valueSlots);

        m_valueConstraints = new Dictionary<string, List<KeyValuePair<string, string>>>();
        foreach (string slot in microstory.m_valueConstraints.Keys)
        {
            m_valueConstraints.Add(slot, new List<KeyValuePair<string, string>>());
            foreach (KeyValuePair<string, string> constraint in microstory.m_valueConstraints[slot])
                m_valueConstraints[slot].Add(new KeyValuePair<string, string>(constraint.Key, constraint.Value));
        }

        m_relations = new List<List<string>>();
        foreach (List<string> relation in microstory.m_relations)
            m_relations.Add(new List<string>(relation));
    }

    public bool Equals(Microstory microstory)
    {
        foreach (Beat beat in microstory.m_beatSlots.Values)
            if (m_beatSlots.Values.ToList().FindAll(x => beat.Equals(x)).Count() == 0)
                return false;

        foreach (Beat beat in m_beatSlots.Values)
            if (microstory.m_beatSlots.Values.ToList().FindAll(x => beat.Equals(x)).Count() == 0)
                return false;

        return true;
    }

    // NB: Minimal attempt at extracting properties - *will* refactor!
    // What if these properties are calculated beforehand...
    public Dictionary<string, List<string>> GetStatisticalProperties()
    {
        Dictionary<string, List<string>> statisticalProperties = new Dictionary<string, List<string>>();

        Dictionary<string, List<string>> beatProperties = GetBeatProperties();
        foreach (string property in beatProperties.Keys)
            statisticalProperties.Add(property, beatProperties[property]);

        Dictionary<string, string> sameValues = GetSameValueProperty();
        foreach (string property in sameValues.Keys)
            statisticalProperties.Add(property, new List<string>() { sameValues[property] });

        return statisticalProperties;
    }

    // FIXME: Add a 'Get attribute properties' for generally interesting moments?

    private Dictionary<string, List<string>> GetBeatProperties()
    {
        List<string> PROPERTIES = new List<string>()
        {
            "interlocution",

            "rumour learned",
            "joke stolen",

            "incorrect",
            "dishonest",
            "accidentally honest",
            "confidentiality",

            "actor vendetta exercised",
            "actor loyalty tested"
        };

        Dictionary<string, List<string>> beatProperties = new Dictionary<string, List<string>>();

        foreach (string slot in m_beatSlots.Keys)
        {
            Dictionary<string, List<string>> beatAttributes = m_beatSlots[slot].GetAttributes();

            foreach (string attribute in PROPERTIES)
            {
                if (beatAttributes.ContainsKey(attribute))
                    beatProperties.Add(slot + "/" + attribute, new List<string>(beatAttributes[attribute]));

                // IMPLEMENTATION: Only used under 'XOR' view of expectedness...
                //else
                //    beatProperties.Add(slot + "/" + attribute, new List<string>() { "N/A" });
            }
        }

        return beatProperties;
    }

    private Dictionary<string, string> GetSameValueProperty()
    {
        // FIXME: Why can't we multiply these properties? Treat each as a boolean, etc...
        // FIXME: Add this after improving liars' strategies...

        Dictionary<string, string> sameValues = new Dictionary<string, string>();

        // FIXME: Does this extend to... non-slotted values? Surely not...
        // FIXME: Especially poor performance under conditionality...
        List<string> slots = m_valueSlots.ToList().FindAll(x => x.Value.Length > 0).Select(x => x.Key).ToList();
        for (int i = 0; i < slots.Count; i++)
        {
            // STEP 1: 'Same value as slot'...
            for (int j = i + 1; j < slots.Count; j++)
                sameValues.Add(slots[i] + " == " + slots[j], m_valueSlots[slots[i]].Equals(m_valueSlots[slots[j]]).ToString());

            foreach (string beatSlot in m_beatSlots.Keys)
            {
                // STEP 2: 'Same value as eavesdropper'...
                sameValues.Add(slots[i] + " == external (" + beatSlot + ")", false.ToString());
                foreach (string character in m_beatSlots[beatSlot].GetAllValues("external audience"))
                    if (m_valueSlots[slots[i]].Equals(character))
                        sameValues[slots[i] + " == external (" + beatSlot + ")"] = true.ToString();

                // STEP 3: 'Same value as accused'...
                sameValues.Add(slots[i] + " == accused (" + beatSlot + ")", false.ToString());
                foreach (string character in m_beatSlots[beatSlot].GetAllValues("all accused"))
                    if (m_valueSlots[slots[i]].Equals(character))
                        sameValues[slots[i] + " == accused (" + beatSlot + ")"] = true.ToString();

                // STEP 4: 'Same value as focalisation'
                // NB: Only counting this once, as no single microstory has mixed focalisation!
                if (beatSlot.Equals(m_beatSlots.Keys.First()))
                {
                    sameValues.Add(slots[i] + " == focalisation", false.ToString());
                    foreach (string character in m_beatSlots[beatSlot].GetAllValues("focalisation"))
                        if (m_valueSlots[slots[i]].Equals(character))
                            sameValues[slots[i] + " == focalisation"] = true.ToString();
                }
            }
        }

        return sameValues;
    }

    public string PrintMicrostory(bool print = true)
    {
        string text = "";

        Func<KeyValuePair<string, Beat>, bool> nonfocalisation = x => x.Value.GetFirstValue("focalisation").Equals("NONFOCALISED");
        Func<KeyValuePair<string, Beat>, string> focuses = x => x.Value.GetFirstValue("focalisation");
        List<string> focalisations = m_beatSlots.ToList().OrderBy(nonfocalisation).ThenBy(focuses).Select(focuses).Distinct().ToList();

        Func<KeyValuePair<string, Beat>, string, bool> predicate = (x, y) => x.Value.GetFirstValue("focalisation").Equals(y);
        Func<KeyValuePair<string, Beat>, int> chronology = x => x.Value.GetTick();
        Dictionary<string, List<string>> slots = new Dictionary<string, List<string>>();
        foreach (string focalisation in focalisations)
            slots.Add(focalisation, m_beatSlots.ToList().FindAll(x => predicate(x, focalisation)).OrderBy(chronology).Select(x => x.Key).ToList());

        int indentLength = 9;
        foreach (string focalisation in slots.Keys)
        {
            text += new String(' ', indentLength) + ((!focalisation.Equals("NONFOCALISED")) ? "NARRATIVE OF " + focalisation : "CHRONOLOGY OF EVENTS") +":\n";
            for (int i = 0; i < slots[focalisation].Count; i++)
            {
                string slot = slots[focalisation][i];
                string prefix = (!focalisation.Equals("NONFOCALISED") && i > 0) ? "Later, in " : string.Empty; ;
                text += m_beatSlots[slot].PrintBeat(indentLength, prefix, false, false);
            }
            text += "\n";
        }

        if (print)
            Console.WriteLine(text);

        return text;
    }
}