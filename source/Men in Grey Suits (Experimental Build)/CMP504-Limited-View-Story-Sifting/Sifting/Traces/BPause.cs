using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

public class BPause : Beat
{
    public BPause(int tick, string focalisation, string setting, List<string> characters) 
    {
        m_attributes = new Dictionary<string, List<string>>();
        m_attributes.Add("focalisation", new List<string>() { focalisation });
        m_attributes.Add("action", new List<string>() { "pause" });
        m_attributes.Add("initial setting", new List<string>() { setting });
        m_attributes.Add("audience", new List<string>(characters));

        m_tick = tick;

        m_utility = 0.0;
    }
}