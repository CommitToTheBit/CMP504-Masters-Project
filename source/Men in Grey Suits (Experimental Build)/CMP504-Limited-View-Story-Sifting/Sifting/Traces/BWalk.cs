using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BWalk : Beat
{
    public BWalk(int tick, string focalisation, Agent.Walk walk, List<string> audience)
    {
        m_attributes = new Dictionary<string, List<string>>();
        m_attributes.Add("focalisation", new List<string>() { focalisation });
        m_attributes.Add("action", new List<string>() { "walk" });
        m_attributes.Add("actor", new List<string>() { walk.m_actor });
        m_attributes.Add("initial setting", new List<string>() { walk.m_initialSetting });
        m_attributes.Add("final setting", new List<string>() { walk.m_finalSetting });
        m_attributes.Add("audience", new List<string>(audience));

        m_tick = tick;

        m_utility = 1.0;
    }
}