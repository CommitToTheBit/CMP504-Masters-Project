using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Agent;
using static System.Net.Mime.MediaTypeNames;

public class Play
{
    private Random m_rng;
    public int m_tick;

    public List<string> m_focalCharacters, m_primaryCharacters, m_secondaryCharacters;
    public Dictionary<string, Agent> m_agents;

    public Script m_script;
    public Stage m_stage;

    public Trace m_trace;

    // Constructor...
    public Play(int seed, Dictionary<string, Characterisation> characterisations)
    {
        m_rng = new Random(seed);

        m_focalCharacters = new List<string>() { "#A", "#F", "#H", "#I", "#J", "#K" }.Intersect(characterisations.Keys).ToList();

        m_script = new Script(characterisations, m_rng.Next());
        m_stage = new Stage(characterisations, m_rng.Next());

        m_agents = new Dictionary<string, Agent>();
        foreach (string character in characterisations.Keys)
            m_agents.Add(character, new Agent(characterisations[character], m_script, m_stage, m_rng.Next()));

        m_trace = new Trace();
    }

    public void Tick(string progress = "", bool debug = false)
    {
        // UI: Showing simulation progress...
        if (progress.Length > 0)
        {
            Console.Write("\r" + String.Join(" ", (progress + ": " + PrintTime(m_tick) + "...").Split(" ").ToList().FindAll(x => x.Length > 0).ToList()).PadRight(Console.WindowWidth));
        }

        // DEBUG:
        if (debug)
            Console.WriteLine("\n");

        TickPausing(m_tick, debug);
        TickWalking(m_tick, debug);
        TickTalking(m_tick, debug);

        m_tick++;
    }

    private void TickPausing(int tick, bool debug = false)
    {
        // STEP 1: Updating world model with time interval...
        foreach (string character in m_agents.Keys)
            m_stage.UpdateCharacter(m_tick, character, m_agents[character].m_setting);

        // STEP 2: Updating 'stage directions' from world model...
        foreach (string setting in m_stage.m_settings.Keys)
            m_trace.AddPause(tick, "NONFOCALISED", setting, m_stage.GetCharactersAt(tick, setting));

        // STEP 3: Focalising update...
        foreach (string focalisation in m_agents.Keys)
            m_agents[focalisation].FocalisePause(tick, ref m_stage);
    }

    private void TickWalking(int tick, bool debug = false)
    {
        foreach (string character in m_agents.Keys)
        {
            // STEP 1: Modelling idealised walk...
            Agent.Walk walk = m_agents[character].GetWalk(tick);
            if (!walk.IsNontrivial())
            {
                // NB: Still updating strategy from mental model...
                m_agents[character].TickWalkingBudgets(tick);
                continue;
            }

            // STEP 2: Pre-processing from world model...
            List<string> audience = m_stage.GetCharactersLOS(tick, walk.m_initialSetting);

            // STEP 3: Updating world model with character action...
            m_stage.UpdateCharacter(tick, walk.m_actor, walk.m_finalSetting);

            // STEP 4: Post-processing from world model...
            audience = audience.Union(m_stage.GetCharactersLOS(tick, walk.m_finalSetting)).ToList();

            // STEP 5: Updating trace from world model...
            m_trace.AddWalk(tick, "NONFOCALISED", walk, audience);

            // STEP 6: Focalising update...
            foreach (string focalisation in audience)
                m_agents[focalisation].FocaliseWalk(tick, walk, ref m_stage);

            // NB: Updating strategy from mental model...
            m_agents[character].TickWalkingBudgets(tick);
        }

        // DEBUG:
        //if (debug)
        //    Console.WriteLine();

        //if (debug)
        //    Console.WriteLine(m_stage.PrintPhysicalState("NONFOCALISED", false, false));

        //if (debug)
        //    Console.WriteLine(m_agents["#C"].m_stage.PrintPhysicalState("#C", false, false));
    }

    private void TickTalking(int tick, bool debug = false)
    {
        foreach (string setting in m_stage.m_settings.Keys)
        {
            List<string> characters = m_stage.GetCharactersAt(tick, setting); // FIXME: Prevent 'talking to self' within Talking.cs...

            // STEP 1: Modelling idealised talk...
            Dictionary<Agent.Talk, double> talks = new Dictionary<Agent.Talk, double>();
            foreach (string character in characters)
            {
                Agent.Talk choice = m_agents[character].GetTalk(tick);
                if (!choice.IsNontrivial())
                    continue;

                talks.Add(choice, choice.m_utility);
            }

            if (talks.Count == 0)
                continue;

            Agent.Talk talk = GetWeightedChoice(talks);

            // STEP 2: Pre-processing from world model...

            // "Overhearing" processing...
            // FIXME: To what extend will these need walkd into the characters' ticks?
            List<string> internalAudience = new List<string>(talk.m_internalAudience); // NB: This is OBJECTIVELY who was INTENDED to hear... 

            Predicate<string> exclusionPredicate = x => !internalAudience.Contains(x);
            Predicate<string> overhearingPredicate = x => m_rng.NextDouble() < 0.2;
            Predicate<string> eavesdroppingPredicate = x => m_agents[x].m_stage.GetCharactersAt(tick, talk.m_setting).Intersect(m_agents[x].m_characterisation.GetAttribute("EAVESDROPPING")).Count() > 0;

            Predicate<string> externalAudiencePredicate = x => exclusionPredicate(x) && (overhearingPredicate(x) || eavesdroppingPredicate(x));
            List<string> externalAudience = m_stage.GetCharactersEarshot(tick, setting).FindAll(externalAudiencePredicate);

            List<string> audience = internalAudience.Union(externalAudience).Distinct().ToList();

            // STEP 3: Updating world model with character action...
            m_script.Tick(audience, talk); // FIXME: Assumes all characters hear line...

            // STEP 4: Post-process...

            // STEP 5: Updating trace from world model...
            m_trace.AddTalk("NONFOCALISED", talk, externalAudience, m_script, m_stage);

            // STEP 6: Focalising update...
            foreach (string focalisation in audience)
                m_agents[focalisation].FocaliseTalk(tick, talk);

            // NB: Updating strategy from mental model...
            // FIXME: Can walk inside 'FocaliseWalk'?

            // DEPRECATED... //
            /*// STEP 6: Post-process...

            // ----------------------------- //
            // FIXME: Incredibly messy code! //

            // 'role learned' attribute...
            foreach (string role in beat.GetFirstSubvalues("line role").Keys)
            {
                List<string> learnedRole = characters.FindAll(x => !x.Equals(beat.GetFirstValue("actor")) && m_script.m_suspicions[x][role].JustLearned()).ToList();
                if (learnedRole.Count > 0)
                {
                    // GENERAL: Checking which charcaters learn anything...
                    if (!attributes.ContainsKey("role learned"))
                        attributes.Add("role learned", new List<string>() { "true" });

                    if (!attributes.ContainsKey("role learned by"))
                        attributes.Add("role learned by", new List<string>(learnedRole));
                    else
                        attributes["role learned by"] = attributes["role learned by"].Union(learnedRole).Distinct().ToList();

                    // SPECIFIC: Setting up subvalues...
                    if (!attributes.ContainsKey("line role learned"))
                        attributes.Add("line role learned", new List<string>() { role });
                    else
                        attributes["line role learned"].Add(role);

                    attributes.Add("line role learned (" + role + ")", new List<string>(learnedRole));
                }
            }



            // 'lied' attribute...
            Dictionary<string, string> identification = m_script.m_lines[beat.GetFirstValue("final line")].m_characterKnowledge[beat.GetFirstValue("actor")].Last().Key;
            foreach (string role in identification.Keys)
            {
                if (m_script.m_suspicions[beat.GetFirstValue("actor")][role].BelievesIdentity() && !m_script.m_suspicions[beat.GetFirstValue("actor")][role].GetIdentity().Equals(identification[role]))
                {
                    // IMPLEMENTATION 1: Treat accidental honesty as a second factor, compounding the unlikeliness of a lie...
                    if (!attributes.ContainsKey("lied"))
                        attributes.Add("lied", new List<string>() { "true" });

                    // 'accidentally told truth' attribute...
                    if (m_script.m_lines[beat.GetFirstValue("final line")].m_defaults[role].Contains(identification[role]))
                        if (!attributes.ContainsKey("accidentally honest"))
                            attributes.Add("accidentally honest", new List<string>() { "true" });

                    // IMPLEMENTATION 2: Treat accidental honesty as a separate version of a lie...
                }
            }

            /*if (m_script.m_lines[beat.GetFirstValue("final line")].m_characterKnowledge[beat.GetFirstValue("actor")].Count >= 2)
            {
                Dictionary<string, string> before = m_script.m_lines[beat.GetFirstValue("final line")].m_characterKnowledge[beat.GetFirstValue("actor")][0].Key; // NB: Models... anchoring bias, right?
                Dictionary<string, string> after = m_script.m_lines[beat.GetFirstValue("final line")].m_characterKnowledge[beat.GetFirstValue("actor")][m_script.m_lines[beat.GetFirstValue("final line")].m_characterKnowledge[beat.GetFirstValue("actor")].Count - 1].Key;

                if (m_script.m_lines[beat.GetFirstValue("final line")].m_roles.Keys.ToList().FindAll(x => x.Equals("MESSENGER") || x.Equals("ATTRIBUTION") || after[x].Equals(before[x])).Count() < m_script.m_lines[beat.GetFirstValue("final line")].m_roles.Count)
                {
                    // IMPLEMENTATION 1: Treat accidental honesty as a second factor, compounding the unlikeliness of a lie...
                    attributes.Add("lied", new List<string>() { "true" });

                    // 'accidentally told truth' attribute...
                    if (m_script.m_lines[beat.GetFirstValue("final line")].m_roles.Keys.ToList().FindAll(x => x.Equals("MESSENGER") || x.Equals("ATTRIBUTION") || after[x].Equals(m_script.m_lines[beat.GetFirstValue("final line")].m_defaults[x])).Count() == m_script.m_lines[beat.GetFirstValue("final line")].m_roles.Count)
                        attributes.Add("accidentally honest", new List<string>() { "true" });

                    // IMPLEMENTATION 2: Treat accidental honesty as a separate version of a lie...
                    //if (m_script.m_lines[beat.GetFirstValue("final line")].m_roles.Keys.ToList().FindAll(x => x.Equals("MESSENGER") || x.Equals("ATTRIBUTION") || after[x].Equals(m_script.m_lines[beat.GetFirstValue("final line")].m_defaults[x])).Count() == m_script.m_lines[beat.GetFirstValue("final line")].m_roles.Count)
                    //    attributes.Add("lied", new List<string>() { "accidentally honest" });
                    //else
                    //    attributes.Add("lied", new List<string>() { "true" });
                }
            }*//*

            // ----------------------------- //
        }*/

        }

        if (debug)
            Console.WriteLine(m_script.PrintInformationState(false, false));

        // DEBUG: ...
        //Console.WriteLine(m_script.PrintRole("MARXIST INTELLECTUAL", false));
    }

    private Agent.Talk GetWeightedChoice(Dictionary<Agent.Talk, double> distribution)
    {
        double totalWeight = distribution.Values.ToList().Sum();
        double randomWeight = totalWeight * m_rng.NextDouble();

        double cumulativeWeight = 0.0;
        foreach (Agent.Talk choice in distribution.Keys)
        {
            if (cumulativeWeight + distribution[choice] >= randomWeight)
                return choice;

            cumulativeWeight += distribution[choice];
        }

        return (distribution.Count > 0) ? distribution.Last().Key : new Agent.Talk();
    }

    public Dictionary<string, Trace> GetTraces()
    {
        Dictionary<string, Trace> traces = new Dictionary<string, Trace>();// { { "NONFOCALISED", m_trace } };
        foreach (string focalisation in m_focalCharacters)
            if (!traces.ContainsKey(focalisation))
                traces.Add(focalisation, m_agents[focalisation].m_trace);

        return traces;
    }

    public void WriteTracesToFile(string timestamp, bool temporaries = false, bool print = true)
    {
        Dictionary<string, Trace> traces = GetTraces();
        foreach (string focalisation in traces.Keys)
            traces[focalisation].WriteTraceToFile(timestamp, focalisation, temporaries, print && focalisation.Equals("NONFOCALISED"));
    }

    private string PrintTime(int tick)
    {
        const int HOUR = 7;
        const int MINUTE = 0;
        const int MERIDIEM = 1;
        const int INTERVAL = 1;

        string hour = ((HOUR + (MINUTE + INTERVAL * tick) / 60 + 11) % 12 + 1).ToString(); // NB: Shifting 0 to 12!
        if (hour.Length == 1)
            hour = " " + hour;

        string minute = ((MINUTE + INTERVAL * tick) % 60).ToString();
        if (minute.Length == 1)
            minute = "0" + minute;

        string meridiem = ((MERIDIEM + (HOUR + (MINUTE + INTERVAL * tick) / 60) / 12) % 2 == 0) ? "am" : "pm";

        return hour + "." + minute + meridiem;
    }
}
