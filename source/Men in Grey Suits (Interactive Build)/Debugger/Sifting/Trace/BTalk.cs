using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class BTalk : Beat
{
    public BTalk(string focalisation, Agent.Talk talk, List<string> externalAudience, Script script, Stage stage)
    {
        string line = talk.m_line;

        m_attributes = new Dictionary<string, List<string>>();
        m_attributes.Add("focalisation", new List<string>() { focalisation });
        m_attributes.Add("action", new List<string>() { "talk" });
        m_attributes.Add("actor", new List<string>() { talk.m_actor });
        m_attributes.Add("initial setting", new List<string>() { talk.m_setting });
        m_attributes.Add("initial line", new List<string>() { talk.m_origin.line });
        m_attributes.Add("final line", new List<string>() { talk.m_line });

        m_attributes.Add("audience", new List<string>(talk.m_internalAudience));
        m_attributes.Add("internal audience", new List<string>(talk.m_internalAudience));
        if (externalAudience.Count > 0) 
        {
            m_attributes.Add("external audience", new List<string>(externalAudience));
            m_attributes["audience"] = m_attributes["internal audience"].Union(m_attributes["external audience"]).Distinct().ToList();

            m_attributes.Add("overheard", new List<string>() { true.ToString() });
        }

        // Modifiers...
        if (talk.m_interlocution.Length > 0)
        {
            m_attributes.Add("interlocution", new List<string>() { talk.m_interlocution });

            if (talk.m_interlocution.Equals("ignore"))
                m_attributes.Add("ignored actor", new List<string>() { talk.m_origin.actor });
        }

        // Role assignments...
        List<string> roles = script.GetRoles(line);
        if (roles.Count > 0) 
        {
            m_attributes.Add("roles", new List<string>(roles));
        }

        List<string> allImplications = script.GetAllImplications(line);
        if (allImplications.Count > 0) 
        {
            m_attributes.Add("all implications", new List<string>(allImplications));
        }

        List<string> allAccused = talk.m_accusation.Values.Distinct().ToList();
        if (allAccused.Count > 0)
        {
            m_attributes.Add("all accused", new List<string>(allAccused));
        }

        foreach (string role in roles)
        {
            string identification = (talk.m_accusation.ContainsKey(role)) ? talk.m_accusation[role] : "UNIDENTIFIED";
            m_attributes.Add("accusation (" + role + ")", new List<string>() { identification });

            // DEBUG: Trying to add 'implicit understandings', but maybe a bit too unclear for now?
            if (identification.Equals("UNIDENTIFIED"))
            {
                m_attributes.Add("implicit accusation (" + role + ")", new List<string>() { script.GetCorrectBelief((!focalisation.Equals("NONFOCALISED")) ? focalisation : talk.m_actor, role).identity });
            }
        }

        // "lied" attribute...
        Dictionary<string, bool> incorrectnesses = new Dictionary<string, bool>(script.IsAccusationCorrect(focalisation, talk.m_actor, talk.m_accusation).ToList().FindAll(x => !x.Value));
        if (incorrectnesses.Count > 0)
        {
            m_attributes.Add("incorrect", new List<string>() { true.ToString() });
            m_attributes.Add("incorrect roles", new List<string>());

            foreach (string role in incorrectnesses.Keys)
            {
                m_attributes["incorrect roles"].Add(role);

                m_attributes.Add("correct " + role + " accusation", new List<string>() { script.GetCorrectBelief(focalisation, role).identity });
                m_attributes.Add("correct " + role + " actor", new List<string>() { script.GetCorrectBelief(focalisation, role).actor });
                m_attributes.Add("correct " + role + " receiver", new List<string>() { script.GetCorrectBelief(focalisation, role).receiver });

                m_attributes.Add("incorrect " + role + " accusation", new List<string>() { talk.m_accusation[role] });
            }
        }

        Dictionary<string, bool> dishonesties = new Dictionary<string, bool>(script.IsAccusationHonest(focalisation, talk.m_actor, talk.m_accusation).ToList().FindAll(x => !x.Value));
        if (dishonesties.Count > 0)
        {
            m_attributes.Add("dishonest", new List<string>() { true.ToString() });
            m_attributes.Add("dishonest roles", new List<string>());

            foreach (string role in dishonesties.Keys)
            {
                m_attributes["dishonest roles"].Add(role);

                m_attributes.Add("honest " + role + " accusation", new List<string>() { script.GetConsistentBelief(focalisation, talk.m_actor, role).identity });
                m_attributes.Add("honest " + role + " actor", new List<string>() { script.GetConsistentBelief(focalisation, talk.m_actor, role).actor });
                m_attributes.Add("honest " + role + " receiver", new List<string>() { script.GetConsistentBelief(focalisation, talk.m_actor, role).receiver });

                m_attributes.Add("dishonest " + role + " accusation", new List<string>() { talk.m_accusation[role] });
            }
        }

        Dictionary<string, bool> accidentalHonesties = new Dictionary<string, bool>(script.IsAccusationAccidentallyHonest(focalisation, talk.m_actor, talk.m_accusation).ToList().FindAll(x => x.Value));
        if (accidentalHonesties.Count > 0)
        {
            m_attributes.Add("accidentally honest", new List<string>() { true.ToString() });
            m_attributes.Add("accidentally honest roles", new List<string>());

            foreach (string role in accidentalHonesties.Keys)
                m_attributes["accidentally honest roles"].Add(role);

            // NB: No need for specific role here; "dishonest " + role + " accusation" is sufficient...
        }

        // 'rumour learned' attribute...
        Predicate<string> rumourFocalisation = x => focalisation.Equals("NONFOCALISED") || focalisation.Equals(x);
        List<string> learnedRumour = m_attributes["audience"].FindAll(x => rumourFocalisation(x) && !x.Equals(talk.m_actor) && script.IsRumourLearned(talk.m_line, x));
        if (learnedRumour.Count > 0)
        {
            m_attributes.Add("rumour learned", new List<string>() { true.ToString() });
            m_attributes.Add("rumour learned by", new List<string>(learnedRumour));
        }

        // 'joke stolen' attribute...
        if (script.IsJokeStolen(line, talk.m_actor))
        {
            m_attributes.Add("joke stolen", new List<string>() { true.ToString() });
            m_attributes.Add("joke stolen from", new List<string>() { script.GetJokeOrigin(line, talk.m_actor) });
        }

        // 'confidentiality' attribute...
        if (stage.m_settings[talk.m_setting].m_sights.Keys.ToList().FindAll(x => !talk.m_setting.Equals(x)).Count == 0)
        {
            if (!m_attributes.ContainsKey("confidentiality"))
                m_attributes.Add("confidentiality", new List<string>());
            m_attributes["confidentiality"].Add("behind closed doors");
        }

        if (talk.m_internalAudience.Count == 2)
        {
            if (!m_attributes.ContainsKey("confidentiality"))
                m_attributes.Add("confidentiality", new List<string>());
            m_attributes["confidentiality"].Add("one-on-one");
        }

        if (stage.GetCharactersLOS(talk.m_tick, talk.m_setting).FindAll(x => !talk.m_internalAudience.Contains(x)).Count() == 0)
        {
            if (!m_attributes.ContainsKey("confidentiality"))
                m_attributes.Add("confidentiality", new List<string>());
            m_attributes["confidentiality"].Add("with no one else around");
        }

        // 'vendettas' attributes...
        List<string> actorVendettas = script.m_characterisations[talk.m_actor].GetVendettas(true, rumourFocalisation(talk.m_actor)).Intersect(talk.m_accusation.Values).ToList() ;
        if (actorVendettas.Count > 0)
        {
            m_attributes.Add("actor vendetta exercised", new List<string>() { true.ToString() });
            m_attributes.Add("actor vendettas", new List<string>(actorVendettas));
        }

        List<string> actorLoyalties = script.m_characterisations[talk.m_actor].GetLoyalties(true, rumourFocalisation(talk.m_actor)).Intersect(talk.m_accusation.Values).ToList();
        if (actorLoyalties.Count > 0)
        {
            m_attributes.Add("actor loyalty tested", new List<string>() { true.ToString() });
            m_attributes.Add("actor loyalties", new List<string>(actorLoyalties));
        }

        // FIXME: Add more details to the beat here...

        // FIXME: Parse these... here?
        if (talk.m_origin.diegesis.Length > 0)
            m_attributes.Add("initial diegesis", new List<string>() { talk.m_origin.diegesis });

        if (talk.m_diegesis.Length > 0)
            m_attributes.Add("final diegesis", new List<string>() { talk.m_diegesis });

        if (talk.m_mimesis.Length > 0)
            m_attributes.Add("mimesis", new List<string>() { talk.m_mimesis });

        m_tick = talk.m_tick;
        m_utility = talk.m_utility;
    }
}