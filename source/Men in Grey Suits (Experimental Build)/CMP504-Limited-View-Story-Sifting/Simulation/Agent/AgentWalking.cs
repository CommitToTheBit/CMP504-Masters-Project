using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class Agent
{
    public struct Walk
    {
        public string m_actor;
        public string m_initialSetting, m_finalSetting;
        public float m_speed;

        public Walk(string actor, string initialSetting, string finalSetting, float speed)
        {
            m_actor = actor;
            m_initialSetting = initialSetting;
            m_finalSetting = finalSetting;
            
            m_speed = speed;
        }

        public bool IsNontrivial()
        {
            return m_speed > 0;
        }
    }

    public Walk GetWalk(int tick)
    {
        // IDENTIFYING OPTIMAL MOVE BASED ON STATIC MENTAL MODEL //

        string setting = m_setting;
        float speed = 0.0f;

        // STEP 1: Check if the path needs updating...
        if (m_following.ToList().FindAll(x => x.Value < 0).Count > 0)
        {
            List<List<KeyValuePair<string, float>>> paths = new List<List<KeyValuePair<string, float>>>();

            foreach (string character in m_following.Keys)
            {
                // DEBUG: Checking what assumptions searcher is working off of... 
                //Console.WriteLine(m_ID + ": " + character + " (budget: " + m_following[character] + ") " + ((m_stage.IsLost(m_ID, character)) ? "is" : "isn't") + " lost; last seen: " + m_stage.GetLastAt(character));

                if (m_following[character] >= 0)
                    continue;

                List<KeyValuePair<string, float>> path = (!m_stage.IsLost(m_ID, character)) ? BeelineToLastAt(m_setting, character) : BeelineToSearchAt(m_setting, tick, character);
                paths.Add(path);
            }

            if (paths.Count > 0)
            {
                Func<List<KeyValuePair<string, float>>, float> lengthOrdering = x => x.Select(x => x.Value).Sum();
                m_path = paths.OrderBy(lengthOrdering).First();
            }
        }
        else
        {
            if (m_path.Count == 0)
            {
                List<string> excludedSettings = new List<string>() { "the corridor", "the downstairs", "the larder" }; // NB: Can't idle to 'liminal spaces'...
                List<string> choices = m_stage.m_settings.Keys.ToList().FindAll(x => !excludedSettings.Contains(x));

                if (choices.Count > 0)
                {
                    choices = choices.FindAll(x => m_rng.NextDouble() < 0.04 / choices.Count).OrderBy(x => m_rng.Next()).ToList();
                    if (choices.Count > 0)
                    {
                        Predicate<Stage.Vertex> predicate = x => x.m_key.Equals(choices[0]);
                        m_path = m_stage.GetPath(m_setting, predicate);
                    }
                }
            }

            // DEBUG: Consistency with the above...!
            //foreach (string character in m_following.Keys) 
            //    Console.WriteLine(m_ID + ": " + character + " (budget: " + m_following[character] + ") " + ((m_stage.IsLost(m_ID, character)) ? "is" : "isn't") + " lost; last seen: " + m_stage.GetLastAt(character));
        }

        // DEBUG: Applying scripted overrides...
        // ...

        // DEBUG: If a path is set, print it...
        //if (m_path.Count > 0)
        //{
        //    Console.Write(m_ID + ": Pathfinding from " + m_setting + " through ");
        //    foreach (KeyValuePair<string, float> step in m_path)
        //        Console.Write(step.Key + "... ");
        //    Console.WriteLine();
        //}
        //else
        //{
        //    Console.WriteLine(m_ID + ": Stationary at " + m_setting + "...");
        //}

        // STEP 2: Act...
        if (m_path.Count > 0)
        {
            setting = FollowBeeline();
            speed = (float)m_characterisation.GetStatistic("SPEED");
        }

        return new Walk(m_ID, m_setting, setting, speed);
    }

    private List<KeyValuePair<string, float>> BeelineToLastAt(string setting, string character)
    {
        Predicate<Stage.Vertex> predicate = x => m_stage.GetLastAt(character).Key.Equals(x.m_key);
        return m_stage.GetPath(setting, predicate);
    }

    private List<KeyValuePair<string, float>> BeelineToSearchAt(string setting, int tick, string character) 
    {
        int lostAtTime = m_stage.GetLastAt(character).Value;

        Predicate<Stage.Vertex> searchPredicate = x => x.m_characterLastLOS[m_ID] < lostAtTime || x.m_characterLastLOS[m_ID] < tick - m_stage.m_settings.Count;
        List<string> searchSettings = m_stage.m_settings.Values.ToList().FindAll(searchPredicate).Select(x => x.m_key).ToList();

        Predicate<Stage.Vertex> predicate = x => searchSettings.Intersect(x.m_sights.Keys).Count() > 0;
        return m_stage.GetPath(setting, predicate);
    }

    private List<KeyValuePair<string, float>> BeelineOutOfEarshot(string setting, int tick, string character) 
    {
        Predicate<Stage.Vertex> outOfEarshotPredicate = x => !m_stage.GetCharactersAt(tick, x.m_key).Contains(character) && !m_stage.GetCharactersEarshot(tick, x.m_key).Contains(character);
        List<string> outOfEarshotSettings = m_stage.m_settings.Values.ToList().FindAll(outOfEarshotPredicate).Select(x => x.m_key).ToList();

        Dictionary<string, List<KeyValuePair<string, float>>> paths = new Dictionary<string, List<KeyValuePair<string, float>>>();
        foreach (string outOfEarshotSetting in outOfEarshotSettings)
            paths.Add(outOfEarshotSetting, m_stage.GetPath(setting, x => outOfEarshotSetting.Equals(x.m_key)));

        if (paths.Count == 0)
            return new List<KeyValuePair<string, float>>();

        Func<KeyValuePair<string, List<KeyValuePair<string, float>>>, float> utility = x => (float)Math.Pow(0.5f, x.Value.Select(y => y.Value).Sum()) / (1.0f + 10.0f * m_stage.m_settings[x.Key].m_sounds.Count);
        return paths.OrderByDescending(utility).First().Value;
    }

    private string FollowBeeline()
    {
        string setting = m_setting;

        // NB: We assume all speeds are <= 1.0, al edges >= 1.0...

        float range = (float)m_characterisation.GetStatistic("SPEED");
        while (m_path.Count > 0)
        {

            if (range - m_path.First().Value >= 0.0f) // NB: Walkd to next room!
            {
                setting = m_path.First().Key;
                range -= m_path.First().Value;

                m_path.RemoveAt(0);
            }
            else
            {
                m_path[0] = new KeyValuePair<string, float>(m_path.First().Key, m_path.First().Value - range);
                break;
            }

        }

        return setting;
    }
}