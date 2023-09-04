using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

public partial class Agent
{
    public struct Talk
    {
        // TONE VARIAB:ES //
        public string m_interlocution;

        // CONTEXT VARIABLES //
        public int m_tick;
        public string m_actor;
        public List<string> m_internalAudience;
        public string m_setting;

        public (string actor, string setting, string line, string diegesis) m_origin;

        // ACTION VARIABLES //
        public string m_line, m_diegesis, m_mimesis;
        public Dictionary<string, string> m_accusation;

        // STRATEGY VARIABLES //
        public double m_utility;

        public Talk()
        {
            m_interlocution = string.Empty;

            m_tick = int.MinValue;
            m_actor = string.Empty;
            m_internalAudience = new List<string>();
            m_setting = string.Empty;

            m_origin = (string.Empty, string.Empty, "START", string.Empty);

            m_line = "START";
            m_diegesis = string.Empty;
            m_mimesis = string.Empty;
            m_accusation = new Dictionary<string, string>();

            m_utility = 0.0;
        }

        public Talk(int tick, string actor, List<string> internalAudience, string setting, (Talk talk, string interlocution) origin)
        {
            m_interlocution = origin.interlocution;

            m_tick = tick;
            m_actor = actor;
            m_internalAudience = new List<string>(internalAudience);
            m_setting = setting;

            m_origin = (origin.talk.m_actor, origin.talk.m_setting, origin.talk.m_line, origin.talk.m_diegesis);

            m_line = string.Empty;
            m_diegesis = string.Empty;
            m_mimesis = string.Empty;
            m_accusation = new Dictionary<string, string>();

            m_utility = 0.0;
        }

        public Talk(Talk talk)
        {
            m_interlocution = talk.m_interlocution;

            m_tick = talk.m_tick;
            m_actor = talk.m_actor;
            m_internalAudience = new List<string>(talk.m_internalAudience);
            m_setting = talk.m_setting;

            m_origin = talk.m_origin;

            m_line = talk.m_line;
            m_diegesis = talk.m_diegesis;
            m_mimesis = talk.m_mimesis;
            m_accusation = new Dictionary<string, string>(talk.m_accusation);

            m_utility = talk.m_utility;
        }

        public void SetInternalAudience(List<string> internalAudience)
        {
            m_internalAudience = new List<string>(internalAudience);
        }

        public void SetLine(string line, string diegesis, string mimesis)
        {
            m_line = line;
            m_diegesis = diegesis;
            m_mimesis = mimesis;
        }

        public void SetAccusation(Dictionary<string, string> accusation)
        {
            m_accusation = new Dictionary<string, string>(accusation);
        }

        public void SetUtility(double utility)
        {
            m_utility = utility;
        }

        public bool IsNontrivial()
        {
            return m_utility > 0.0;
        }
    }

    public Talk GetTalk(int tick)
    {
        // IDENTIFYING OPTIMAL MOVE BASED ON STATIC MENTAL MODEL //

        // STEP 1: Cancel out if there's no one to talk to...
        List<string> internalAudience = m_stage.GetCharactersAt(tick, m_setting);
        if (internalAudience.Count < 2)
            return new Talk();

        // STEP 2: Consider all 'legal' lines, the most useful first...
        Func<(Talk, string), Talk> selection = x => new Talk(tick, m_ID, internalAudience, m_setting, x);
        List<Talk> initials = m_attention.Get().Select(selection).ToList();

        List<Talk> edges = GetHighUtilityEdges(initials);

        // STEP 3: Find the optimal role-assignment for each line, accepting the top 4...
        List<Talk> talks = new List<Talk>();
        while (talks.Count < 4 && edges.Count > 0)
        {
            List<Talk> options = GetHighUtilityRumours(edges.First());
            if (options.Count > 0 && options.First().IsNontrivial())
                talks.Add(options.First());

            edges.RemoveAt(0);
        }
        talks = talks.OrderByDescending(x => x.m_utility).ThenBy(x => m_rng.Next()).ToList();

        if (talks.Count == 0)
            return new Talk();

        Talk talk = talks.First();
        //talk.SetUtility(WaitingUtility(talk) * RepetitionUtility(talk));
        talk.SetUtility(WaitingUtility(talk) * EdgeUtility(talk));

        return talk;
    }

    public List<Talk> GetHighUtilityEdges(List<Talk> initialTalks)
    {
        Dictionary<string, Talk> talks = new Dictionary<string, Talk>();

        foreach (Talk initial in initialTalks)//.OrderBy(x => m_rng.Next()))
        {
            List<string> edges = m_script.GetKnownEdges(initial.m_actor, initial.m_origin.line);
            foreach (string edge in edges)
            {
                Talk option = new Talk(initial);
                option.SetLine(edge, m_script.GetDiegesis(edge), m_script.GetMimesis(edge, m_ID));

                double utility = EdgeUtility(option);
                option.SetUtility(utility);

                if (!talks.ContainsKey(edge))
                    talks.Add(edge, option);
                else if (utility > talks[edge].m_utility)
                    talks[edge] = option;
            }
        }

        return talks.Values.ToList().OrderByDescending(x => x.m_utility).ThenBy(x => m_rng.Next()).ToList();
    }

    public List<Talk> GetHighUtilityRumours(Talk talk)
    {
        // RETURNS THE BEST POSSIBLE DELIVERY OF A GIVEN LINE //

        // DEBUG: Fill in with role assignment/utility considerations...
        List<Talk> talks = new List<Talk>();

        List<Dictionary<string, string>> accusations = m_script.GetAllAccusations(talk.m_line);
        foreach (Dictionary<string, string> accusation in accusations)
        {
            Talk option = new Talk(talk);
            option.SetAccusation(accusation);

            double utility = RumourUtility(option);
            option.SetUtility(utility);

            talks.Add(option);
        }

        return talks.OrderByDescending(x => x.m_accusation.Count).ThenByDescending(x => x.m_utility).ThenBy(x => m_rng.Next()).ToList();
    }

    private double EdgeUtility(Talk talk)
    {
        return RepetitionUtility(talk) * DistanceUtility(talk) * ModifierUtility(talk);
    }

    private double DistanceUtility(Talk talk)
    {
        // INITUITION: Each degree of separation makes a segue 1/expobase as useful...
        double distance = m_script.GetDistance(talk.m_origin.line, talk.m_line);

        //Console.WriteLine(UtilityCurves.ExponentialAsymptote(0.33, distance, 1.0, 0.01));
        return UtilityCurves.ExponentialAsymptote(0.33, distance, 1.0, 0.01);
    }

    private double RepetitionUtility(Talk talk)
    {
        // INTUITION: A piece of gossip 1/expobase of its value if everyone has heard it once...
        List<string> others = talk.m_internalAudience.FindAll(x => !talk.m_actor.Equals(x));
        double repetitivity = (double)m_script.GetTotalAwarenesses(talk.m_line, others).Select(x => Math.Pow(x.Value, 5.0)).Sum() / others.Count;

        return UtilityCurves.ExponentialAsymptote(0.2, repetitivity, 1.0, 0.01);
    }

    private double ModifierUtility(Talk talk)
    {
        Dictionary<string, double> map = new Dictionary<string, double>();
        map.Add("ignore", 0.25);

        return UtilityCurves.DiscreteMap(talk.m_interlocution, map, 1.0);
    }

    private double RumourUtility(Talk talk)
    {
        //Console.WriteLine(ExplorationUtility(talk) * AmbiguityUtility(talk) * DirectnessUtility(talk) * FifthAmendmentUtility(talk) * ConsistencyUtility(talk) * VendettaUtility(talk) * EdgeUtility(talk));

        // FIXME: 'Identity' function' for now...
        return ExplorationUtility(talk) * ExploitationUtility(talk) * AmbiguityUtility(talk) * DirectnessUtility(talk) * FifthAmendmentUtility(talk) * ConsistencyUtility(talk) * LoyaltyUtility(talk) * VendettaUtility(talk) * EdgeUtility(talk);
    }

    private double ExplorationUtility(Talk talk)
    {
        double distance = m_script.GetDistanceFromUnknown(talk.m_line, m_ID);
        //double distance = m_script.GetDistanceFromUnknownImplication(talk.m_line, m_ID, m_characterisation.GetAttribute("EXPLORATION"));

        return UtilityCurves.ExponentialAsymptote(0.75, distance, 1.0, 0.5);
    }

    private double ExploitationUtility(Talk talk)
    {
        double distance = m_script.GetDistanceFromKnownAccusation(talk.m_line, m_ID, m_characterisation.GetVendettas(true, true));

        return UtilityCurves.ExponentialAsymptote(0.25, distance, 1.0, 0.5);
    }

    private double AmbiguityUtility(Talk talk)
    {
        // INTUITION: Being coy isn't a *bad* option, but be explicit when its possible!
        double ambiguities = m_script.GetIdentifiableRoles(talk.m_line).Count - talk.m_accusation.Count;

        return UtilityCurves.ExponentialAsymptote(0.4, ambiguities, 1.0, 0.5);
    }

    private double DirectnessUtility(Talk talk)
    {
        // INTUITION: Avoid accusing people of things directly... unless everyone knows you have a vendetta against them!
        double directs = talk.m_internalAudience.Intersect(talk.m_accusation.Values).ToList().FindAll(x => !m_characterisation.GetLoyalties(true, false).Contains(x)).Count();

        return UtilityCurves.ExponentialAsymptote(0.2, directs, 1.0, 0.5);
    }

    private double FifthAmendmentUtility(Talk talk)
    {
        // INTUITION: Never incrimate oneself...
        List<string> nonincrimination = new List<string>() { m_ID };
        bool incriminated = nonincrimination.Intersect(talk.m_accusation.Values).Count() > 0;

        return UtilityCurves.Binary(incriminated, 0.0, 1.0);
    }

    private double ConsistencyUtility(Talk talk)
    {
        // FIXME: 'Quick' option for debug...
        // NB: Consciously using this for the survey, for a 'concentrated blast' of lies!
        return 1.0;

        // INTUITION: Really disincentivises any inconsistency...
        // FIXME: Causes a massive slowdown!
        double consistency = m_script.GetConsistency(talk.m_actor, talk.m_internalAudience, talk.m_accusation);

        //Console.WriteLine(m_ID + " --- " + talk.m_line + " --- " + consistency);

        return Math.Pow(consistency, 0.5); // NB: < 1.0, as lies are less... noticeable when the rest of a group agrees?
    }

    private double LoyaltyUtility(Talk talk)
    {
        List<string> loyalties = m_characterisation.GetLoyalties(true, true);

        int missteps = 0;
        foreach (string role in talk.m_accusation.Keys)
        {
            List<string> identities = m_script.GetIdentities(talk.m_line, role);
            if (identities.Intersect(loyalties).Count() == 0)
                continue;

            // FIXME: Add a "m_casting.GetImplications(role)" check!

            if (loyalties.Contains(talk.m_accusation[role]))
                missteps++;
        }

        return UtilityCurves.ExponentialAsymptote(0.01, missteps, 1.0, 0.0);
    }

    private double VendettaUtility(Talk talk)
    {
        List<string> vendettas = m_characterisation.GetVendettas(true, true);

        int opportunities = 0;
        foreach (string role in talk.m_accusation.Keys)
        {
            List<string> identities = m_script.GetIdentities(talk.m_line, role);
            if (identities.Intersect(vendettas).Count() == 0)
                continue;

            // FIXME: Add a "m_casting.GetImplications(role)" check!

            if (vendettas.Contains(talk.m_accusation[role]))
                opportunities++;
        }

        return UtilityCurves.ExponentialAsymptote(0.6, opportunities, 0.5, 1.0);
    }

    private double WaitingUtility(Talk talk)
    {
        int n = Math.Max(talk.m_internalAudience.Count - 1, 1);
        int u = Math.Clamp(talk.m_tick - (m_waitingSince + 1), 0, n);

        double expobase = 0.5f; // NB: Must be < 1.0f
        double exponent = (double)(n - u) / n;

        double power = Math.Pow(expobase, exponent);
        return (power - expobase) / (1.0f - expobase);
    }
}
