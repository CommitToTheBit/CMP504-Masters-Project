using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Beat
{
    protected Dictionary<string, List<string>> m_attributes;
    protected int m_tick;

    protected double m_utility;

    public Beat()
    {
        m_attributes = new Dictionary<string, List<string>>();
        m_tick = 0;

        m_utility = 0.0;
    }

    public Beat(int tick, Dictionary<string, List<string>> attributes, float utility) // Attributes will be extracted beforehand...
    {
        m_attributes = new Dictionary<string, List<string>>();
        foreach (string attribute in attributes.Keys)
            m_attributes.Add(attribute, new List<string>(attributes[attribute].OrderBy(x => x)));

        m_tick = tick;

        m_utility = utility;
    }

    public bool Equals(Beat beat)
    {
        // NB: Utility has no bearing on 'equality' as far as the user sees it, right?
        // NB: Thinking about equality in terms of surfacing, 'The Tale-Spin effect', etc...
        if (beat.m_tick != m_tick)
            return false;

        foreach (string attribute in beat.m_attributes.Keys)
        {
            if (m_attributes.ContainsKey(attribute))
            {
                if (beat.m_attributes[attribute].Count != m_attributes[attribute].Count || beat.m_attributes[attribute].Intersect(m_attributes[attribute]).Distinct().Count() != m_attributes[attribute].Count)
                    return false;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public bool ContainsAttribute(string attribute)
    {
        return m_attributes.ContainsKey(attribute);
    }

    public bool ContainsValue(string attribute, string value)
    {
        return ContainsAttribute(attribute) && m_attributes[attribute].Contains(value);
    }

    public bool ContainsValue(string value)
    {
        foreach (string attribute in m_attributes.Keys)
            if (ContainsValue(attribute, value))
                return true;

        return false;
    }

    public Dictionary<string, List<string>> GetAttributes()
    {
        Dictionary<string, List<string>> attributes = new Dictionary<string, List<string>>();
        foreach (string attribute in m_attributes.Keys.OrderBy(x => x))
            if (m_attributes[attribute].Count > 0) // NB: Filter added for N/A to work! Happy that attributes cannot exist without values?
                attributes.Add(attribute, new List<string>(m_attributes[attribute]));

        return attributes;
    }

    public string GetFirstValue(string attribute)
    {
        return (ContainsAttribute(attribute)) ? m_attributes[attribute].First() : string.Empty; // NB: Will always check attribute exists *before* this point...
    }

    public List<string> GetAllValues(string attribute)
    {
        return (ContainsAttribute(attribute)) ? new List<string>(m_attributes[attribute].OrderBy(x => x)) : new List<string>(); // NB: Will always check attribute exists *before* this point...
    }

    public int GetTick()
    {
        return m_tick;
    }

    public double GetUtility()
    {
        return m_utility;
    }

    public string PrintBeat(int indent, string prefix = "", bool data = false, bool print = true)
    {
        string text = string.Empty;

        // STEP 1: Print header...
        bool timeHeader = true || data || GetFirstValue("focalisation").Equals("NONFOCALISED");
        bool spaceHeader = ContainsAttribute("initial setting");

        string header = prefix;
        if (timeHeader)
            header += PrintTime();
        if (timeHeader && spaceHeader)
            header += ", ";
        if (spaceHeader)
            header += GetFirstValue("initial setting");
        if (header.Length > 0)
            header = header.First().ToString().ToUpper() + header.Substring(1);

        // STEP 2: Print prose...
        foreach (string value in m_attributes["action"])
        {
            if (value.Equals("walk"))
                text += PrintWalk(indent, header);
            else if (value.Equals("talk"))
                text += PrintTalk(indent, header);
            else if (value.Equals("pause"))
                text += PrintPause(indent, header);
            else
                text += PrintSubheader(indent) + header + ": undefined \"" + value + "\" action...\n";
        }

        // STEP 3: Print data...
        if (data)
        {
            foreach (string attribute in GetAttributes().Keys)
                text += PrintSubheader(indent, 3) + attribute + ": " + PrintList(GetAllValues(attribute)) + ".\n";
        }

        if (print)
            Console.Write(text);

        return text;
    }

    private string PrintWalk(int indent, string header)
    {
        string text = string.Empty;

        // STEP 0: Retrieve values...
        string focalisation = GetFirstValue("focalisation");
        string actor = GetFirstValue("actor");
        string initialSetting = GetFirstValue("initial setting");
        string finalSetting = GetFirstValue("final setting");
        List<string> audience = GetAllValues("audience").FindAll(x => !x.Equals(actor));

        // STEP 1: Write action...
        text += PrintSubheader(indent) + header + ": ";
        if (!initialSetting.Equals(finalSetting))
        {
            text += actor + " walks from " + initialSetting + " to " + finalSetting + ".\n";
        }
        else
        {
            text += actor + " is walking through " + initialSetting + ".\n";
        }

        // STEP 2: Write (non-actor) audience...
        if (audience.Count > 0)
        {
            text += PrintSubheader(indent, 2) + "This is seen by " + PrintList(audience) + ".\n";
        }

        return text;
    }

    private string PrintTalk(int indent, string header)
    {
        string text = string.Empty;

        // STEP 0: Retrieve values...
        string focalisation = GetFirstValue("focalisation");
        string actor = GetFirstValue("actor");

        bool focalised = !focalisation.Equals("NONFOCALISED");
        string primary = (focalised) ? focalisation : actor;
        string secondary = (GetAllValues("internal audience").Contains(primary)) ? primary : actor;

        string initialLine = GetFirstValue("initial line");
        string finalLine = GetFirstValue("final line");
        string mode = GetFirstValue("mode").ToLower();

        string initialDiegesis = GetFirstValue("initial diegesis");
        string finalDiegesis = GetFirstValue("final diegesis");
        string mimesis = GetFirstValue("mimesis");

        bool rumourLearned = GetFirstValue("rumour learned").Equals(true.ToString());
        List<string> rumourLearners = GetAllValues("rumour learned by");

        bool jokeStolen = GetFirstValue("joke stolen").Equals(true.ToString());
        string jokeOrigin = GetFirstValue("joke stolen from");

        bool discrepancy = GetFirstValue("incorrect").Equals(true.ToString()) || GetFirstValue("dishonest").Equals(true.ToString());
        List<string> roles = GetAllValues("roles");
        List<string> accused = GetAllValues("all accused");

        List<string> confidentiality = GetAllValues("confidentiality");

        List<string> internalAudience = GetAllValues("internal audience").FindAll(x => !x.Equals(secondary));
        List<string> externalAudience = GetAllValues("external audience").FindAll(x => !internalAudience.Contains(x));

        // STEP 1: Write initial conditions...
        text += PrintSubheader(indent) + header + ": " + secondary + " is talking with " + PrintList(internalAudience, "someone out of sight") + ".\n";

        if (initialDiegesis.Length > 0)
        {
            text += PrintSubheader(indent) + "They are " + initialDiegesis + ".\n";
        }

        // STEP 2: Write notable relationships within the room... (focalisation, and actor's?)

        // STEP 3: Write action...
        text += PrintSubheader(indent) + actor;//
        if (GetFirstValue("interlocution").Equals("ignore"))
        {
            text += ", not really paying attention to whatever " + GetFirstValue("ignored actor") + " just said,";
        }
        // NB: Only allowing one parenthesis here - it's cleaner!
        else if (GetAllValues("internal audience").Intersect(accused).Count() > 0)
        {
            text += ", confronting " + PrintList(GetAllValues("internal audience").Intersect(accused).ToList(), "no one involved") + " directly,";
        }
        text += " segues";
        if (finalDiegesis.Length > 0)
        {   
            text += " to " + finalDiegesis;

        }
        else if (mode.Length > 0)
        {
            text += " to a " + mode;
        }
        if (rumourLearned)
        {
            text += " (a rumour hitherto unheard by " + PrintList(rumourLearners) + ")";
        }
        else if (jokeStolen)
        {
            text += " (a joke stolen from " + jokeOrigin + ")";
        }
        if (mimesis.Length > 0)
        {
            text += ": \"" + text + "\"";
        }
        text += ".\n";

        // STEP 4: Write notable relationships to the accused... (focalisation, and actor's?)
        List<string> actorVendettas = GetAllValues("actor vendettas");
        List<string> actorLoyalties = GetAllValues("actor loyalties");
        if (actorVendettas.Count > 0 || actorLoyalties.Count > 0)
        {
            text += PrintSubheader(indent) + ((!primary.Equals(actor)) ? primary + " knows " : "") + actor + " ";
            if (actorVendettas.Count > 0)
            {
                text += "has " + ((actorVendettas.Count <= 1) ? "a longstanding vendetta" : "longstanding vendettas") + " against " + PrintList(actorVendettas, "no one accused");
            }
            if (actorVendettas.Count > 0 && actorLoyalties.Count > 0)
            {
                text += ", but ";
            }
            if (actorLoyalties.Count > 0)
            {
                text += "is supposedly loyal to " + PrintList(actorLoyalties, "no one accused");
            }
            text += ".\n";
        }

        // STEP 5: Write discrepancies...
        if (discrepancy)
        {
            bool rolesPreambled = false;

            text += PrintSubheader(indent) + ((!primary.Equals(actor)) ? primary + " can tell " : "") + actor + " ";
            foreach (string role in roles)
            {
                string rolePreamble = (!rolesPreambled) ? "the '" + role + "' in their midst" : "'" + role + "'";
                if (ContainsValue("accidentally honest roles", role))
                {
                    // NB: Not totally sure how to 'standardise' the causal bookkeping for this edge case, but it'd be a really clunky read with more info anyways!
                    text += "thinks they're lying about " + GetFirstValue("dishonest " + role + " accusation") + " being " + rolePreamble + " (whereas " + primary + " knows this is actually completely true); ";
                
                }
                else if (ContainsValue("dishonest roles", role))
                {
                    string beliefActor = GetFirstValue("honest " + role + " actor");
                    string beliefReceiver = GetFirstValue("honest " + role + " receiver");
                    string beliefIdentity = GetFirstValue("honest " + role + " accusation");

                    string causalBookkeeping = "(earlier, " + beliefActor + " told " + beliefReceiver + " this was actually " + beliefIdentity + ")";
                    if (beliefActor.Equals(beliefIdentity))
                        causalBookkeeping = "(this is actually " + beliefActor + " themself!)";
                    else if (beliefActor.Equals(beliefReceiver))
                        causalBookkeeping = "(" + beliefActor + " knows this is actually " + beliefIdentity + ")";

                    text += "is lying about " + GetFirstValue("dishonest " + role + " accusation") + " being " + rolePreamble + " " + causalBookkeeping +  "; ";
                }
                else if (ContainsValue("incorrect roles", role))
                {
                    string beliefActor = GetFirstValue("correct " + role + " actor");
                    string beliefReceiver = GetFirstValue("correct " + role + " receiver");
                    string beliefIdentity = GetFirstValue("correct " + role + " accusation");

                    string causalBookkeeping = "(earlier, " + beliefActor + " told " + beliefReceiver + " this was actually " + beliefIdentity + ")";
                    if (beliefActor.Equals(beliefIdentity))
                        causalBookkeeping = "(this is actually " + beliefActor + " themself!)";
                    else if (beliefActor.Equals(beliefReceiver))
                        causalBookkeeping = "(" + beliefActor + " knows this is actually " + beliefIdentity + ")";

                    text += "is misinformed about " + GetFirstValue("incorrect " + role + " accusation") + " being " + rolePreamble + " " + causalBookkeeping + "; ";
                }
            }
            text = text.Substring(0, text.Length - 2) + ".\n";
        }

        // STEP 6: Write the confidentiality of the meeting...
        text += PrintSubheader(indent) + "The conversation takes place " + PrintList(confidentiality, "openly") + ". ";
        if ((confidentiality.Count > 0) == externalAudience.Count > 0)
        {
            text += "Still, " + PrintList(externalAudience, "no one") + " overhear" + ((externalAudience.Count <= 1) ? "s" : "") + ".\n";
        }
        else
        {
            text += PrintList(externalAudience, "No one") + " overhear" + ((externalAudience.Count <= 1) ? "s" : "") + ".\n";
        }

        // DEBUG: Write the role assignnments...
        // FIXME: Unnecessary with a proper parser!
        foreach (string role in roles)
            text += PrintSubheader(indent) + "* " + role + ": " + GetFirstValue("accusation (" + role + ")") + ".\n"; 

        return text;
    }

    private string PrintPause(int indent, string header)
    {
        string text = string.Empty;

        string focalisation = GetFirstValue("focalisation");
        List<string> audience = GetAllValues("audience").OrderByDescending(x => focalisation.Equals(x)).ToList();

        text += PrintSubheader(indent) + header + ": " + PrintList(audience, "The room is empty") + ".\n";

        return text;
    }

    private string PrintTime()
    {
        const int HOUR = 7;
        const int MINUTE = 0;
        const int MERIDIEM = 1;
        const int INTERVAL = 1;

        string hour = ((HOUR + (MINUTE + INTERVAL * m_tick) / 60 + 11) % 12 + 1).ToString(); // NB: Shifting 0 to 12!
        //if (hour.Length == 1)
        //    hour = " " + hour;

        string minute = ((MINUTE + INTERVAL * m_tick) % 60).ToString();
        if (minute.Length == 1)
            minute = "0" + minute;

        string meridiem = ((MERIDIEM + (HOUR + (MINUTE + INTERVAL * m_tick) / 60) / 12) % 2 == 0) ? "am" : "pm";

        return hour + "." + minute + meridiem;
    }

    private string PrintSubheader(int indentLength = 9, int indentCount = 1)
    {
        return new String(' ', indentCount * indentLength);
    }

    private string PrintList(List<string> list, string empty = "Attribute contains no values", string delimiter = ", ")
    {
        string text = empty;

        if (list.Count > 0)
        {
            text = string.Empty;
            for (int i = 0; i < list.Count; i++)
                text += list[i] + ((i < list.Count - 1) ? delimiter : "");
        }

        return text;
    }

    /*private string PrintTalk(ref Dictionary<string, bool> attributesPrinted) // NB: 'Special cases' for printing may be complex...
    {
        string text = "";

        if (ContainsAttribute("actor") && ContainsAttribute("initial line") && ContainsAttribute("final line"))
        {
            string actor = GetFirstValue("actor");
            string initialLine = GetFirstValue("initial line");
            string finalLine = GetFirstValue("final line");

            text += PrintSubheader() + PrintHeader(ref attributesPrinted) + actor;

            if (ContainsValue("interlocution", "ignore"))
            {
                text += ", not really listening to whatever " + GetFirstValue("ignored actor") + " just said,";

                attributesPrinted["ignored actor"] = true;
            }

            text += " segues from ";

            attributesPrinted["action"] = true;
            attributesPrinted["actor"] = true;
            attributesPrinted["initial line"] = true;
            attributesPrinted["final line"] = true;

            if (ContainsAttribute("initial diegesis"))
            {
                string lineText = GetFirstValue("initial diegesis");

                text += lineText;

                attributesPrinted["initial diegesis"] = true;
            }
            else
            {
                text += initialLine;
            }

            text += " to ";

            if (ContainsAttribute("final diegesis"))
            {
                string lineText = GetFirstValue("final diegesis");

                text += lineText;

                attributesPrinted["final diegesis"] = true;
            }
            else
            {
                text += finalLine;
            }

            if (ContainsAttribute("joke stolen"))
            {
                text += " (a joke stolen from " + GetFirstValue("joke stolen from") + ")";

                attributesPrinted["joke stolen"] = true;
                attributesPrinted["joke stolen from"] = true;
            }

            if (ContainsAttribute("mimesis"))
            {
                string lineDialogue = GetFirstValue("mimesis");

                text += ": \"" + lineDialogue + "\"";

                attributesPrinted["mimesis"] = true;
            }

            text += ".\n";

            if (attributesPrinted["focalisation"].Equals("NONFOCALISED") || attributesPrinted["focalisation"].Equals(actor))
                text += PrintInaccuracies(ref attributesPrinted);

            if (ContainsAttribute("interlocution"))
                attributesPrinted["interlocution"] = true;

            if (ContainsAttribute("internal audience"))
            {
                List<string> internalAudience = GetAllValues("internal audience").FindAll(x => !x.Equals(actor));

                text += PrintSubheader(2) + actor + " is talking with " + PrintList(internalAudience, "colleagues" + ((GetAllValues("external audience").Contains(actor)) ? ", out of sight" : "")) + ".\n"; // FIXME: This feels like a catch-all that will become deprecated soon...

                if (ContainsAttribute("audience"))
                    attributesPrinted["audience"] = true;
                attributesPrinted["internal audience"] = true;
            }

            if (ContainsAttribute("confidentiality"))
            {
                List<string> confidentiality = GetAllValues("confidentiality");

                text += PrintSubheader(2) + "The conversation takes place " + PrintList(confidentiality) + ".\n";

                attributesPrinted["confidentiality"] = true;
            }

            if (ContainsAttribute("external audience"))
            {
                List<string> externalAudience = GetAllValues("external audience").FindAll(x => !x.Equals(actor));

                text += PrintSubheader(2) + "Nevertheless " + PrintList(externalAudience) + " overhears.\n";

                if (ContainsAttribute("audience"))
                    attributesPrinted["audience"] = true;
                attributesPrinted["external audience"] = true;

                if (ContainsAttribute("overheard"))
                    attributesPrinted["overheard"] = true;
            }

            if (!(attributesPrinted["focalisation"].Equals("NONFOCALISED") || attributesPrinted["focalisation"].Equals(actor)))
                text += PrintInaccuracies(ref attributesPrinted);
        }

        return text;
    }

    private string PrintInaccuracies(ref Dictionary<string, bool> attributesPrinted)
    {
        string text = string.Empty;

        if (ContainsAttribute("incorrect") || ContainsAttribute("dishonest"))
        {
            text += PrintSubheader(2) + GetFirstValue("actor") + " ";
            foreach (string role in GetAllValues("roles"))
            {
                if (ContainsValue("dishonest roles", role)) 
                {
                    text += ((ContainsValue("accidentally honest roles", role)) ? "tells what they think is a lie " : "lies " ) + "about " + GetFirstValue("dishonest " + role + " accusation") + " being " + role + " ";
                    if (ContainsValue("accidentally honest roles", role))
                    {
                        text += "(but is accidentally honest); ";

                        attributesPrinted["accidentally honest"] = true;
                        attributesPrinted["accidentally honest roles"] = true;
                    }
                    else 
                    {
                        text += "(" + ((!GetFirstValue("actor").Equals(GetFirstValue("focalisation")) ? GetFirstValue("actor") + " believes " : "")) + "this is actually " + GetFirstValue("honest " + role + " accusation") + "); ";

                        // DRAFTS: Is there a clearer phrasing of this? 
                        //string reasoning = GetFirstValue("honest " + role + " reasoning");
                        //string reasoningText = ((reasoning.Equals(GetFirstValue("actor"))) ? GetFirstValue("actor") + " previously told " + reasoning : reasoning + " explicitely told " + GetFirstValue("actor")) + " ";
                        //
                        //text += "(" + ((!GetFirstValue("actor").Equals(GetFirstValue("focalisation")) ? reasoningText : "")) + "this is actually " + GetFirstValue("honest " + role + " accusation") + "); ";
                    }

                    attributesPrinted["dishonest"] = true;
                    attributesPrinted["dishonest roles"] = true;
                    attributesPrinted["dishonest " + role + " accusation"] = true;
                    attributesPrinted["honest " + role + " accusation"] = true;

                    // NB: For clarity, we won't contrast honesty with correctness?
                    if (ContainsValue("incorrect roles", role))
                    {
                        attributesPrinted["incorrect"] = true;
                        attributesPrinted["incorrect roles"] = true;
                        attributesPrinted["incorrect " + role + " accusation"] = true;
                        attributesPrinted["correct " + role + " accusation"] = true;
                    }
                }
                else if (ContainsValue("incorrect roles", role))
                {
                    text += "is wrong about " + GetFirstValue("incorrect " + role + " accusation") + " being " + role + " ";
                    text += "(this is actually " + GetFirstValue("correct " + role + " accusation") + "); ";

                    // DRAFTS: Is there a clearer phrasing of this? 
                    //text += "(" + GetFirstValue("focalisation") + " knows it is actually " + GetFirstValue("correct " + role + " accusation") + ");";

                    attributesPrinted["incorrect"] = true;
                    attributesPrinted["incorrect roles"] = true;
                    attributesPrinted["incorrect " + role + " accusation"] = true;
                    attributesPrinted["correct " + role + " accusation"] = true;
                }
            }
            text = text.Substring(0, text.Length - 2) + ".\n";
        }

        if (ContainsAttribute("roles"))
            attributesPrinted["roles"] = true;

        return text;
    }

    private string PrintPause(ref Dictionary<string, bool> attributesPrinted)
    {
        string text = "";

        if (ContainsAttribute("audience"))
        {
            List<string> audience = GetAllValues("audience");

            text += PrintHeader(ref attributesPrinted) + PrintList(audience, "The room is empty") + ".\n";

            attributesPrinted["action"] = true;
            attributesPrinted["audience"] = true;
        }

        return text;
    }*/
}