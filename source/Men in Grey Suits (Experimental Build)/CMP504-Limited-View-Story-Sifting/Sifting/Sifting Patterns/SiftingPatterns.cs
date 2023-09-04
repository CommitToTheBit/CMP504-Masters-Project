using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using System.Xml.Serialization;
using System.Diagnostics;

public class SiftingPatterns
{
    public struct JsonMicrostory
    {
        public Dictionary<string, List<string>>? SLOTS { get; set; }
        public List<List<string>>? CONSTRAINTS { get; set; }
        public List<List<string>>? RELATIONS { get; set; }
    }

    public Dictionary<string, Microstory> m_patterns;

    public SiftingPatterns()
    {
        m_patterns = LoadPatterns(Environment.CurrentDirectory.Split("bin")[0] + "Assets/Patterns/sifting_patterns.json");
    }

    private Dictionary<string, Microstory> LoadPatterns(string path)
    {
        Dictionary<string, Microstory> patterns = new Dictionary<string, Microstory>();

        // STEP 1: Parse the JSON into an intermediary dictionary...
        Dictionary<string, JsonMicrostory>? jsonPatterns = JsonSerializer.Deserialize<Dictionary<string, JsonMicrostory>>(File.ReadAllText(path))!;

        // STEP 2: Construct vertex structs...
        foreach (string key in jsonPatterns.Keys)
        {
            patterns.Add(key, new Microstory(
                (jsonPatterns[key].SLOTS is not null) ? jsonPatterns[key].SLOTS! : new Dictionary<string, List<string>>(),
                (jsonPatterns[key].CONSTRAINTS is not null) ? jsonPatterns[key].CONSTRAINTS! : new List<List<string>>(),
                (jsonPatterns[key].RELATIONS is not null) ? jsonPatterns[key].RELATIONS! : new List<List<string>>()
            ));
        }

        return patterns;
    }

    // --------------------------------------- //
    // FIXME: This code is incredibly messy... //

    public Microanthology SiftMicrostories(Dictionary<string, Trace> traces, string progress = "")
    {
        //microanthology microanthologies = new microanthology(); //List<Pattern>() { pattern };

        Dictionary<string, Dictionary<string, List<Microstory>>> microstories = new Dictionary<string, Dictionary<string, List<Microstory>>>();

        foreach (string focalisation in traces.Keys)
        {
            microstories.Add(focalisation, new Dictionary<string, List<Microstory>>());

            foreach (string pattern in m_patterns.Keys)
            {
                // UI: Showing simulation progress...
                if (progress.Length > 0)
                {
                    Console.Write("\r" + String.Join(" ", (progress + ": " + focalisation + ": Sifting \"" + pattern + "\" microstories...").Split(" ").ToList().FindAll(x => x.Length > 0).ToList()).PadRight(Console.WindowWidth));
                }

                microstories[focalisation].Add(pattern, new List<Microstory>() { m_patterns[pattern] });

                foreach (string slot in m_patterns[pattern].m_beatSlots.Keys)
                {
                    List<Microstory> iteratedMicrostories = new List<Microstory>();
                    foreach (Microstory microstory in microstories[focalisation][pattern])
                    {
                        // If the slot is empty, verify all possibilities; otherwise, verify the entry... // FIXME: Check 'Contains' is working as intended...
                        List<Beat> beats = (microstory.m_beatSlots[slot].GetAttributes().Count() == 0) ? traces[focalisation].FindBeats(x => !microstory.m_beatSlots.Values.Contains(x)) : new List<Beat>() { microstory.m_beatSlots[slot] };
                        foreach (Beat beat in beats)
                        {
                            Microstory iteratedMicrostory = new Microstory(microstory);
                            iteratedMicrostory.m_beatSlots[slot] = beat;

                            // Fill in attributes with all valid values...
                            List<Microstory> enrichedMicrostories = new List<Microstory>() { iteratedMicrostory };
                            foreach (KeyValuePair<string, string> constraint in iteratedMicrostory.m_valueConstraints[slot])
                                enrichedMicrostories = EnrichValue(enrichedMicrostories, slot, constraint);

                            // Check all relations are still fully/partially satisfied...
                            foreach (List<string> relation in iteratedMicrostory.m_relations)
                                for (int i = enrichedMicrostories.Count() - 1; i >= 0; i--)
                                    if (!CheckRelation(traces[focalisation], enrichedMicrostories[i], relation))
                                        enrichedMicrostories.RemoveAt(i);

                            foreach (Microstory enrichedMicrostory in enrichedMicrostories)
                            {
                                //if (iteratedMicrostories.Count >= 1000)
                                //    break;

                                iteratedMicrostories.Add(enrichedMicrostory);
                            }

                            // DEBUG: 'Timeout' clause?
                            //if (iteratedMicrostories.Count >= 1000)
                            //    break;
                        }

                        // DEBUG: 'Timeout' clause?
                        //if (iteratedMicrostories.Count >= 1000)
                        //    break;
                    }

                    microstories[focalisation][pattern] = new List<Microstory>(iteratedMicrostories);

                    // DEBUG: Checking progress of sift...
                    //Console.WriteLine(slot);
                    //Console.WriteLine(microstories[pattern].Count());
                }
            }
        }

        return new Microanthology(microstories);
    }

    private List<Microstory> EnrichValue(List<Microstory> microstories, string slot, KeyValuePair<string, string> constraint)
    {
        List<Microstory> enrichedMicrostories = new List<Microstory>();

        foreach (Microstory microstory in microstories)
        {
            if (constraint.Value.Length > 0 && constraint.Value[0].Equals('?'))
            {
                if (microstory.m_valueSlots[constraint.Value].Length == 0)
                {
                    if (microstory.m_beatSlots[slot].ContainsAttribute(constraint.Key))//.m_valueConstraints.ContainsKey(attribute.Key))
                    {
                        foreach (string value in microstory.m_beatSlots[slot].GetAllValues(constraint.Key))
                        {
                            Microstory enrichedMicrostory = new Microstory(microstory);
                            enrichedMicrostory.m_valueSlots[constraint.Value] = value;
                            enrichedMicrostories.Add(enrichedMicrostory);
                        }
                    }
                }
                else if (microstory.m_beatSlots[slot].ContainsAttribute(constraint.Key) && microstory.m_beatSlots[slot].GetAllValues(constraint.Key).Contains(microstory.m_valueSlots[constraint.Value]))
                {
                    enrichedMicrostories.Add(new Microstory(microstory));
                }
            }
            else if (microstory.m_beatSlots[slot].ContainsAttribute(constraint.Key) && microstory.m_beatSlots[slot].GetAllValues(constraint.Key).Contains(constraint.Value))
            {
                enrichedMicrostories.Add(new Microstory(microstory));
            }
        }

        return enrichedMicrostories;
    }

    private bool CheckRelation(Trace trace, Microstory microstory, List<string> relation)
    {
        // ------------------------------ //
        // FIXME : Especially messy code! //

        if (relation.Count() == 0)
            return true;

        string constraint = relation[0];
        List<string> slots = relation.GetRange(1, relation.Count() - 1);

        if (constraint.Equals("sequence"))
        {
            // Find all beats that are mentioned in the relation and assigned so far, ordered 'in sequence'...
            List<Beat> beats = microstory.m_beatSlots.ToList().FindAll(x => slots.Contains(x.Key) && x.Value.GetAttributes().Count() > 0).OrderBy(x => slots.IndexOf(x.Key)).Select(x => x.Value).ToList();

            // Check chronology agrees with...
            for (int i = 0; i < beats.Count() - 1; i++)
                if (beats[i].GetTick() > beats[i + 1].GetTick())
                    return false;
        }

        if (constraint.Equals("simultaneous")) // NB: Some sketchiness given walks on the same turn does happen 'in sequence'
        {
            // Find all values that are mentioned in the relation and assigned so far...
            List<Beat> beats = microstory.m_beatSlots.ToList().FindAll(x => slots.Contains(x.Key) && x.Value.GetAttributes().Count() > 0).OrderBy(x => slots.IndexOf(x.Key)).Select(x => x.Value).ToList();

            // Check chronology agrees with...
            for (int i = 0; i < beats.Count() - 1; i++)
                if (beats[i].GetTick() != beats[i + 1].GetTick())
                    return false;
        }

        if (constraint.Equals("within"))
        {
            // Find all values that are mentioned in the relation and assigned so far... // FIXME: Add this same generalisation to other relations!
            List<string> values = slots.FindAll(x => !microstory.m_valueSlots.ContainsKey(x) || microstory.m_valueSlots[x].Length > 0).Select(x => (microstory.m_valueSlots.ContainsKey(x)) ? microstory.m_valueSlots[x] : x).ToList();
            if (values.Count < slots.Count)
                return true;

            // Checkwise for equality, pairwise...
            if (values.Count > 0)
                for (int i = 1; i < values.Count; i++)
                    if (values.First().Equals(values[i]))
                        return true;

            return false;
        }

        if (constraint.Equals("distinct"))
        {
            // Find all values that are mentioned in the relation and assigned so far...
            List<string> values = microstory.m_valueSlots.ToList().FindAll(x => slots.Contains(x.Key) && !x.Value.Equals("")).Select(x => x.Value).ToList();

            // Checkwise for equality, pairwise...
            for (int i = 0; i < values.Count(); i++)
                for (int j = i + 1; j < values.Count(); j++)
                    if (values[i].Equals(values[j]))
                        return false;
        }

        if (constraint.Equals("without interruption"))
        {
            // ...
            List<string> beatSlots = slots.GetRange(0, 2);
            List<string> valueSlots = slots.GetRange(2, slots.Count - 2);

            List<Beat> beats = microstory.m_beatSlots.ToList().FindAll(x => beatSlots.Contains(x.Key) && x.Value.GetAttributes().Count() > 0).OrderBy(x => beatSlots.IndexOf(x.Key)).Select(x => x.Value).ToList();
            if (beats.Count < 2)
                return true;

            List<string> attributes = valueSlots.Select(x => x.Split('=').ElementAt(0)).ToList();
            List<string> values = valueSlots.Select(x => x.Split('=').ElementAt(1)).ToList();

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Length > 0 && values[i].ElementAt(0).Equals('?'))
                {
                    if (microstory.m_valueSlots[values[i]].Length > 0)
                        values[i] = microstory.m_valueSlots[values[i]];
                    else // NB: If the slots isn't full, not comparison is possible...
                        return true;
                }
            }

            List<int> indices = new List<int>();
            foreach (Beat beat in beats)
                foreach (int index in trace.IndexBeats(x => beat.Equals(x)))
                    if (!indices.Contains(index))
                        indices.Add(index);
            indices = indices.OrderBy(x => x).ToList();

            // Checkwise for equality, pairwise...
            // 'AND': Assuring no event satisfies ALL criteria...
            List<Beat> intervals = trace.FindBeats(x => true);
            for (int i = 0; i < indices.Count() - 1; i++)
            {
                for (int j = indices[i] + 1; j < indices[i + 1]; j++)
                {
                    bool interruption = true;
                    for (int k = 0; k < attributes.Count; k++)
                    {
                        if (!intervals[j].ContainsValue(attributes[k], values[k]))
                        {
                            interruption = false;
                            break;
                        }
                    }
                    if (interruption)
                    {
                        return false;
                    }
                }
            }
        }

        return true;

        // ------------------------------ //
    }

    // --------------------------------------- //
}
