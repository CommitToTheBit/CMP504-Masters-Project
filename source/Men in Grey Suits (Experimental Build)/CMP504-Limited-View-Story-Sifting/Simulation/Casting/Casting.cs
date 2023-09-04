using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

public class Casting
{
    /*public class Role
    {
        private string m_role;

        private Dictionary<string, List<string>> m_characterRoleImplications;
        private Dictionary<string, List<(int tick, string actor, string identity)>> m_characterKnowledge;

        public Role() 
        {
            m_role = string.Empty;

            m_characterRoleImplications = new Dictionary<string, List<string>>();
            m_characterKnowledge = new Dictionary<string, List<(int, string, string)>>();
        }

        public Role(string role, List<string> characters)
        {
            m_role = role;

            m_characterRoleImplications = new Dictionary<string, List<string>>();
            m_characterKnowledge = new Dictionary<string, List<(int tick, string actor, string identity)>();
            foreach (string character in characters)
            {
                m_characterRoleImplications.Add(character, new List<string>());
                m_characterKnowledge.Add(character, new List<(int tick, string actor, string identity)> ());
            }
        }

        public Role(Role role)
        {
            m_role = role.m_role;

            m_characterRoleImplications = new Dictionary<string, List<string>>();
            foreach (string character in role.m_characterRoleImplications.Keys)
                m_characterRoleImplications.Add(character, new List<string>(role.m_characterRoleImplications[character]));

            m_characterKnowledge = new Dictionary<string, List<KeyValuePair<int, string>>>();
            foreach (string character in role.m_characterKnowledge.Keys)
            {
                m_characterKnowledge.Add(character, new List<KeyValuePair<int, string>>());
                foreach (KeyValuePair<int, string> pair in role.m_characterKnowledge[character])
                    m_characterKnowledge[character].Add(new KeyValuePair<int, string>(pair.Key, pair.Value));
            }
        }

        public void Tick(int tick, List<string> audience, List<string> implications, string identity)
        {
            // STEP 1: Tick role implications...
            foreach (string character in audience)
                if (m_characterRoleImplications.ContainsKey(character))
                    foreach (string implication in implications)
                        if (!m_characterRoleImplications[character].Contains(implication))
                            m_characterRoleImplications[character].Add(implication);

            // STEP 2: Tick character knowledge...
            foreach (string character in audience)
                if (m_characterKnowledge.ContainsKey(character))
                    m_characterKnowledge[character].Add(new KeyValuePair<int, string>(tick, character));
        }

        public string GetPresumedIdentity(string character)
        {
            // MVP IMPLEMENTATION: 'Anchoring bias'!
            List<string>

            if (m_characterKnowledge.ContainsKey(character))
                foreach (KeyValuePair in m_characterKnowledge[character])
                    if (talk.m_identities.ContainsKey(m_role))
                        return talk.m_identities[m_role];

            return string.Empty;
        }

        public List<string> GetPreviousIdentifications(string transmitter, string receiver)
        {
            if (!m_characterKnowledge.ContainsKey(receiver))
                return new List<string>();

            // INTUITION: 'Consistency' means 'transmitter much agree with self and receiver'
            Predicate<Agent.Talk> predicate = x => (x.m_actor.Equals(transmitter) || x.m_actor.Equals(receiver)) && x.m_identities.ContainsKey(m_role);
            return m_characterKnowledge[receiver].FindAll(predicate).Select(x => x.m_identities[m_role]).Distinct().ToList();
        }

        public bool IsConsistent(string transmitter, string receiver)
        {
            return GetPreviousIdentifications(transmitter, receiver).Count < 2;
        }

        public string PrintEpistemologicalHistory(bool print = true)
        {
            string text = string.Empty;

            foreach (string character in m_characterKnowledge.Keys)
            {
                text += character + ": ";
                for (int i = 0; i < m_characterKnowledge[character].Count; i++)
                {
                    text += m_characterKnowledge[character][i].m_finalLine + " (" + m_characterKnowledge[character][i].m_tick + ")";
                    if (i < m_characterKnowledge[character].Count - 1)
                        text += ", ";
                }
                text += "\n";

                text += "=> ";
                for (int i = 0; i < m_characterRoleImplications[character].Count; i++)
                {
                    text += m_characterRoleImplications[character][i];
                    if (i < m_characterRoleImplications[character].Count - 1)
                        text += ", ";
                }
                text += "\n";

                text += "=> PRESUMED IDENTITY: " + GetPresumedIdentity(character) + "\n";
            }

            if (print)
                Console.Write(text);

            return text;
        }
    }

    private List<string> m_characters;
    public Dictionary<string, Role> m_roles;

    public Casting(Dictionary<string, Characterisation> characterisations)
    {
        m_characters = new List<string>(characterisations.Keys);

        m_roles = new Dictionary<string, Role>();
    }

    public Casting(Casting casting)
    {
        m_characters = new List<string>(casting.m_characters);

        m_roles = new Dictionary<string, Role>();
        foreach (string role in casting.m_roles.Keys) 
            m_roles.Add(role, new Role(casting.m_roles[role]));
    }

    public void Tick(List<string> audience, Agent.Talk talk, Script.Vertex vertex)
    {
        foreach (string role in vertex.GetRoles())
        {
            if (!m_roles.ContainsKey(role))
                m_roles.Add(role, new Role(role, m_characters));

            m_roles[role].Tick(audience, talk, vertex);
        }
    }

    public bool IsCorrect(Agent.Talk talk, string focalisation)
    {
        foreach (string role in talk.m_identities.Keys)
        {
            if (!m_roles.ContainsKey(role))
                continue;

            string identity = m_roles[role].GetPresumedIdentity(focalisation);
            if (identity.Length > 0 && !identity.Equals(talk.m_identities[role]))
                return false;
        }

        return true;
    }

    public bool IsLying(Agent.Talk talk)
    {
        return !IsCorrect(talk, talk.m_actor);
    }

    public bool IsConsistentIdentification(string transmitter, string receiver, Agent.Talk talk, Script.Vertex vertex)
    {
        foreach (string role in vertex.GetRoles())
        {
            if (!m_roles.ContainsKey(role))
                continue;

            if (!m_roles[role].IsConsistent(transmitter, receiver))
                return false;

            if (talk.m_identities.ContainsKey(role) && !m_roles[role].GetPreviousIdentifications(transmitter, receiver).Contains(talk.m_identities[role]))
                return false;
        }

        return true;
    }

    public string PrintEpistemologicalHistory(bool print = true)
    {
        string text = string.Empty;

        foreach (string role in m_roles.Keys.ToList().OrderBy(x => x))
            text += "ROLE: " + role + ":\n" + m_roles[role].PrintEpistemologicalHistory(false) + "\n";

        if (print)
            Console.Write(text);

        return text;
    }*/
}
