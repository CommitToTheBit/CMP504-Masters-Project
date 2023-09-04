using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

public class Characterisation
{
    public string m_character;
    public Dictionary<string, List<string>> m_attributes;
    public Dictionary<string, double> m_statistics;

    public Characterisation(string character, GreenRoom.JsonCharacterisation jsonCharacterisation)
    {
        m_character = character;

        m_attributes = new Dictionary<string, List<string>>();
        if (jsonCharacterisation.ATTRIBUTES is not null)
            foreach (string attribute in jsonCharacterisation.ATTRIBUTES.Keys)
                m_attributes.Add(attribute, new List<string>(jsonCharacterisation.ATTRIBUTES[attribute]));

        m_statistics = new Dictionary<string, double>();
        if (jsonCharacterisation.STATISTICS is not null)
            foreach (string statistic in jsonCharacterisation.STATISTICS.Keys)
                m_statistics.Add(statistic, jsonCharacterisation.STATISTICS[statistic]);
    }

    public Characterisation(Characterisation characterisation)
    {
        m_character = characterisation.m_character;

        m_attributes = new Dictionary<string, List<string>>();
        foreach (string attribute in characterisation.m_attributes.Keys)
            m_attributes.Add(attribute, new List<string>(characterisation.m_attributes[attribute]));

        m_statistics = new Dictionary<string, double>();
        foreach (string statistic in characterisation.m_statistics.Keys)
            m_statistics.Add(statistic, characterisation.m_statistics[statistic]);
    }

    public List<string> GetAttribute(string attribute)
    {
        return (m_attributes.ContainsKey(attribute)) ? new List<string>(m_attributes[attribute]) : new List<string>();
    }

    public double GetStatistic(string statistic)
    {
        return (m_statistics.ContainsKey(statistic)) ? m_statistics[statistic] : 1.0;
    }

    // BESPOKE, 'AUTHORED' FUNCTIONALITY //

    public List<string> GetLoyalties(bool knowns, bool unknowns)
    {
        Predicate<string> unknownOverride = (unknowns) ? x => GetAttribute("UNKNOWN VENDETTAS").Contains(x) : x => false;

        List<string> knownLoyalties = (knowns) ? GetAttribute("KNOWN LOYALTIES").FindAll(x => !unknownOverride(x)) : new List<string>();
        List<string> unknownLoyalties = (unknowns) ? GetAttribute("UNKNOWN LOYALTIES") : new List<string>();

        return knownLoyalties.Union(unknownLoyalties).Distinct().ToList();
    }

    public List<string> GetVendettas(bool knowns, bool unknowns)
    {
        Predicate<string> unknownOverride = (unknowns) ? x => GetAttribute("UNKNOWN LOYALTIES").Contains(x) : x => false;

        List<string> knownLoyalties = (knowns) ? GetAttribute("KNOWN VENDETTAS").FindAll(x => !unknownOverride(x)) : new List<string>();
        List<string> unknownLoyalties = (unknowns) ? GetAttribute("UNKNOWN VENDETTAS") : new List<string>();

        return knownLoyalties.Union(unknownLoyalties).Distinct().ToList();
    }
}
