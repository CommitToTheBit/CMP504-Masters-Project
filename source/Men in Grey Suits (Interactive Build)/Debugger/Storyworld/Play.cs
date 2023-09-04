using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;


public partial class Play : Node
{
    // DEPRECATED: ...
    [Signal]
    public delegate void BeatEventHandler(string text);

    [Signal]
    public delegate void ChooseEventHandler(string[] texts, string[] places, bool passable);

    [Signal]
    public delegate void ChoosingEventHandler();

    [Signal]
    public delegate void ChosenEventHandler();

    private Random m_rng;
    private int m_tick;

    private GreenRoom m_greenRoom;
    private Script m_script;
    private Stage m_stage;

    public Dictionary<string, Agent> m_agents;

    public Trace m_trace;

    // PLAYER INTERACTION //
    public string m_player;
    private bool m_chosenTalking;
    private int m_choice;

    public Play(int seed)
    {
        Initialise(seed);
    }

    public void Initialise(int seed)
    {
        m_rng = new Random(seed);
        m_tick = 0;

        m_greenRoom = new GreenRoom();
        m_script = new Script(m_rng.Next(), m_greenRoom.GetCharacterisations());
        m_stage = new Stage(m_rng.Next(), m_greenRoom.GetCharacterisations());

        m_player = "NONFOCALISED";
        m_agents = new Dictionary<string, Agent>();
        foreach (string character in m_greenRoom.GetCharacterisations().Keys)
        {
            m_agents.Add(character, new Agent(m_rng.Next(), character.Equals(m_player), m_greenRoom.GetCharacterisations()[character], m_script, m_stage));
            m_agents[character].Connect("Beat", new Callable(this, "EmitBeat"));
        }

        m_trace = new Trace();

        m_choice = -1;
    }

    public override void _Ready()
	{

    }

    public void SelectCharacter(string player)
    {
        m_player = player;
        foreach (string character in m_agents.Keys)
            m_agents[character].m_player = m_player.Equals(character);
    }

    public async Task Tick()
    {
        await TickPausing(m_tick); 
        // FIXME: Hacky, but handles stop calls?
        if (m_choice == int.MinValue)
            return;

        await TickWalking(m_tick);
        // FIXME: Hacky, but handles stop calls?
        if (m_choice == int.MinValue)
            return;

        await TickChoosing(m_tick);
        // FIXME: Hacky, but handles stop calls?
        if (m_choice == int.MinValue)
            return;

        await TickTalking(m_tick);
        // FIXME: Hacky, but handles stop calls?
        if (m_choice == int.MinValue)
            return;

        m_tick++;
    }

    private async Task TickPausing(int tick, bool debug = false)
    {
        // STEP 1: Updating world model with time interval...
        foreach (string character in m_agents.Keys)
            m_stage.UpdateCharacter(m_tick, character, m_agents[character].m_setting);

        // STEP 2: Updating 'stage directions' from world model...
        foreach (string setting in m_stage.m_settings.Keys)
            m_trace.AddPause(tick, "NONFOCALISED", setting, m_stage.GetCharactersAt(tick, setting));

        // STEP 3: Focalising update...
        foreach (string focalisation in m_agents.Keys)
        {
            m_agents[focalisation].FocalisePause(tick, ref m_stage, true, tick == 0);
            if (tick == 0 && m_agents[focalisation].m_player)
                await ToSignal(GetParent<DialogueDebugger>(), "BeatTweened");

            // FIXME: Hacky, but handles stop calls?
            if (m_choice == int.MinValue)
                return; 
        }
    }

    private async Task TickWalking(int tick, bool debug = false)
    {
        foreach (string character in m_agents.Keys)
        {
            if (m_agents[character].m_player)
                continue;

            // STEP 1: Modelling idealised walk...
            Agent.Walk walk = m_agents[character].GetWalk(tick);
            if (!walk.IsNontrivial())
            {
                // NB: Still updating strategy from mental model...
                m_agents[character].TickWalkingBudgets(tick);
                continue;
            }

            // STEP 2: ...
            await EmitWalk(tick, walk);
        }
    }

    private async Task EmitWalk(int tick, Agent.Walk walk)
    {
        // STEP 1: Pre-processing from world model...
        List<string> audience = m_stage.GetCharactersLOS(tick, walk.m_initialSetting);

        // STEP 2: Updating world model with character action...
        m_stage.UpdateCharacter(tick, walk.m_actor, walk.m_finalSetting);

        // STEP 3: Post-processing from world model...
        audience = audience.Union(m_stage.GetCharactersLOS(tick, walk.m_finalSetting)).ToList();

        // STEP 4: Updating trace from world model...
        m_trace.AddWalk(tick, "NONFOCALISED", walk, audience);

        // STEP 5: Focalising update...
        foreach (string focalisation in audience)
        {
            m_agents[focalisation].FocaliseWalk(tick, walk, ref m_stage);            
            if (m_agents[focalisation].m_player)
                await ToSignal(GetParent<DialogueDebugger>(), "BeatTweened");

            // FIXME: Hacky, but handles stop calls?
            if (m_choice == int.MinValue)
                return;

            if (focalisation.Equals(walk.m_actor) && !walk.m_initialSetting.Equals(walk.m_finalSetting))
            {
                m_agents[focalisation].FocalisePause(tick, ref m_stage, false, true);            
                if (m_agents[focalisation].m_player)
                    await ToSignal(GetParent<DialogueDebugger>(), "BeatTweened");

                // FIXME: Hacky, but handles stop calls?
                if (m_choice == int.MinValue)
                    return;
            }
        }    

        // NB: Updating strategy from mental model...
        m_agents[walk.m_actor].TickWalkingBudgets(tick);
    }

    private async Task TickChoosing(int tick)
    {
        m_chosenTalking = false;

        List<Agent.Talk> choices = new List<Agent.Talk>();
        List<Agent.Walk> moves = new List<Agent.Walk>();

        int index = m_agents.Keys.ToList().FindIndex(x => m_agents[x].m_player);
        if (index >= 0 && tick > 0)
        {
            choices = m_agents[m_player].GetTalkChoices(tick);
            moves = m_agents[m_player].GetWalkChoices(tick);
        }    

        // FIXME: GET CHOICE...
        List<string> texts = choices.Select(x => ParseForUI("Segue to " + x.m_diegesis + "...")).ToList();
        List<string> places = moves.Select(x => "Move to " + x.m_finalSetting + "...").ToList();

        EmitSignal("Choose", texts.ToArray(), places.ToArray(), m_agents.ContainsKey(m_player) && m_stage.GetCharactersAt(tick, m_stage.GetLastAt(m_player).Key).Count > 2);
        await ToSignal(this, "Choosing");

        // FIXME: Hacky, but handles stop calls?
        if (m_choice == int.MinValue)
            return;

        // FIXME: choice FEEDBACK NEEDS TO BECOME MORE ADVANCED!
        if (m_choice >= 0 && m_choice < choices.Count)
        {    
            m_chosenTalking = true;
            await EmitTalk(choices[m_choice], m_agents[m_player].m_setting);
        }
        else if (m_choice >= choices.Count && m_choice < choices.Count + moves.Count)
        {
            await EmitWalk(tick, moves[m_choice - choices.Count]);
        }
    }

    private async void RegisterChoosing(int choice)
    {
        // FIXME: choice FEEDBACK NEEDS TO BECOME MORE ADVANCED!
        m_choice = choice;
        EmitSignal("Choosing");
    }

    private async Task TickTalking(int tick)
    {
        foreach (string setting in m_stage.m_settings.Keys)
        {
            List<string> characters = m_stage.GetCharactersAt(tick, setting); // FIXME: Prevent 'talking to self' within Talking.cs...
            if (m_chosenTalking && characters.Contains(m_player))
                continue;

            // STEP 1: Modelling idealised talk...
            Dictionary<Agent.Talk, double> talks = new Dictionary<Agent.Talk, double>();
            foreach (string character in characters.FindAll(x => !x.Equals(m_player)))
            {
                Agent.Talk choice = m_agents[character].GetTalk(tick);
                if (!choice.IsNontrivial())
                    continue;

                talks.Add(choice, choice.m_utility);
            }

            if (talks.Count == 0)
                continue;

            Agent.Talk talk = GetWeightedChoice(talks);

            // STEP 2: ...
            await EmitTalk(talk, setting);
        }
    }

    private async Task EmitTalk(Agent.Talk talk, string setting)
    {
        // STEP 1: Pre-processing from world model...

        // "Overhearing" processing...
        // FIXME: To what extend will these need walkd into the characters' ticks?
        List<string> internalAudience = new List<string>(talk.m_internalAudience); // NB: This is OBJECTIVELY who was INTENDED to hear... 

        Predicate<string> exclusionPredicate = x => !internalAudience.Contains(x);
        Predicate<string> overhearingPredicate = x => m_rng.NextDouble() < 0.2;
        Predicate<string> eavesdroppingPredicate = x => m_agents[x].m_stage.GetCharactersAt(talk.m_tick, talk.m_setting).Intersect(m_agents[x].m_characterisation.GetAttribute("EAVESDROPPING")).Count() > 0;

        Predicate<string> externalAudiencePredicate = x => exclusionPredicate(x) && (overhearingPredicate(x) || eavesdroppingPredicate(x));
        List<string> externalAudience = m_stage.GetCharactersEarshot(talk.m_tick, setting).FindAll(externalAudiencePredicate);

        List<string> audience = internalAudience.Union(externalAudience).Distinct().ToList();

        // STEP 2: Updating world model with character action...
        m_script.Tick(audience, talk); // FIXME: Assumes all characters hear line...

        // STEP 3: Post-process...

        // STEP 4: Updating trace from world model...
        m_trace.AddTalk("NONFOCALISED", talk, externalAudience, m_script, m_stage);

        // STEP 5: Focalising update...
        foreach (string focalisation in audience)
        {
            m_agents[focalisation].FocaliseTalk(talk.m_tick, talk);
            if (m_agents[focalisation].m_player)
                await ToSignal(GetParent<DialogueDebugger>(), "BeatTweened");

            // FIXME: Hacky, but handles stop calls?
            if (m_choice == int.MinValue)
                return;
        }    

        // DEBUG: Make this the player's focalisation, at least...
        //EmitBeat(new BTalk("NONFOCALISED", talk, externalAudience, m_script, m_stage).PrintBeat(0));
        //await ToSignal(GetParent<DialogueDebugger>(), "BeatTweened");
    }

    private void EmitBeat(string text)
    {
        // FIXME: Incredibly specific parsing!
        EmitSignal("Beat", ParseForUI(text));
    }

    public int GetTick()
    {
        return m_tick;
    }

    public Dictionary<string, string> GetNamedCharacters()
    {
        Predicate<KeyValuePair<string, Characterisation>> predicate = x => x.Value.GetAttribute("NAME").Count > 0;
        Func<KeyValuePair<string, Characterisation>, KeyValuePair<string, string>> select = x => new KeyValuePair<string, string>(x.Key, x.Value.GetValue("NAME"));

        return new Dictionary<string, string>(m_greenRoom.GetCharacterisations().ToList().FindAll(predicate).Select(select).ToList());
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

    // FIXME: Incredibly over-tuned parsing...
    public string ParseForUI(string text)
    {
        Dictionary<string, string> secondaries = new Dictionary<string, string>() { 
            { "#a", "the Prime Minister" },
            { "#b", "the Chancellor of the Exchequer" },
            { "#c", "the Home Secretary" },
            { "#d", "the Foreign Secretary" },
            { "#e", "the Health Secretary" },
            { "*someone*", "*someone*" },
        };

        // STEP 1: Add names...
        foreach (string character in m_script.m_characterisations.Keys.Union(secondaries.Keys))
        {   
            bool player = m_agents.ContainsKey(character) && m_agents[character].m_player;

            string bbColour = "white";
            string bbOutlineColour = "#00D6FF";
            string bbOutlineSize = (player) ? "9" : "8";

            string name = m_script.GetCharacterisation(character).GetValue("NAME");
            name = (!name.Equals(new Characterisation().GetValue("NAME"))) ? name.Split(",").First() : (secondaries.ContainsKey(character)) ? secondaries[character] : "a backbencher";
            if (player)
                name = "[b]" + name + "[/b]";

            text = string.Join("[color="+bbColour+"][outline_color="+bbOutlineColour+"][outline_size="+bbOutlineSize+"]"+name+"[/outline_size][/outline_color][/color]",text.Split(character));
        }

        // STEP 2: Combine backbenchers...
        string backbencher = "[color=white][outline_color=#00D6FF][outline_size=8]a backbencher[/outline_size][/outline_color][/color], ";

        List<string> backbencherRepetition = text.Split("[color=white][outline_color=#00D6FF][outline_size=8]a backbencher[/outline_size][/outline_color][/color].",2).ToList();

        int backbencherCount = 1;
        while (backbencherRepetition[0].Length >= backbencher.Length && backbencherRepetition[0].Substring(backbencherRepetition[0].Length - backbencher.Length, backbencher.Length).Equals(backbencher))
        {
            backbencherRepetition[0] = backbencherRepetition[0].Substring(0, backbencherRepetition[0].Length - backbencher.Length);
            backbencherCount++;
        }

        //if (backbencherRepetition.Count > 1 && backbencherRepetition[1].Substring(1, "[color=white][outline_color=#00D6FF][outline_size=8]a backbencher[/outline_size][/outline_color][/color]".Length).Equals("[color=white][outline_color=#00D6FF][outline_size=8]a backbencher[/outline_size][/outline_color][/color]"))
        //    backbencherRepetition[1] = string.Join("[color=white][outline_color=#00D6FF][outline_size=8]The backbencher[/outline_size][/outline_color][/color]", backbencherRepetition[1].Split("[color=white][outline_color=#00D6FF][outline_size=8]a backbencher[/outline_size][/outline_color][/color]",2).ToList());

        List<string> numerics = new List<string>() { "two", "three", "four", "five", "six", "seven", "eight" , "nine", "many" };
        string pluralityBackbenchers = (backbencherCount > 1) ? numerics[Math.Min(backbencherCount - 2, numerics.Count - 1)] : string.Empty;

        bool andBackbenchers = !backbencherRepetition[0].Substring(backbencherRepetition[0].Length - 5, 5).Equals("with ");
        bool otherBackbenchers = backbencherRepetition[0].Split(":", 2).Count() > 0 && backbencherRepetition[0].Split(":", 2).Last().Length >= backbencher.Length - 1 && backbencherRepetition[0].Split(":", 2).Last().Substring(0, backbencher.Length - 1).Equals(" " + backbencher.Substring(0, backbencher.Length - 2));

        string groupedBackbenchers = (backbencherCount > 1) ? pluralityBackbenchers + ((otherBackbenchers) ? " other" : "") + " backbenchers" : ((otherBackbenchers) ? "another" : "a") + " backbencher"; 
        text = string.Join(((andBackbenchers) ? "and " : string.Empty) + "[color=white][outline_color=00D6FF][outline_size=8]" + groupedBackbenchers + "[/outline_size][/outline_color][/color].", backbencherRepetition);

        // STEP 3: Quick capitalisation...
        text = string.Join(": [color=white][outline_color=#00D6FF][outline_size=8]A", text.Split(": [color=white][outline_color=#00D6FF][outline_size=8]a", 2).ToList());
        text = string.Join(".\n[color=white][outline_color=#00D6FF][outline_size=8]A", text.Split(".\n[color=white][outline_color=#00D6FF][outline_size=8]a").ToList());

        // STEP N: Quick spacing...
        text = string.Join(" ", text.Split(" ").ToList().FindAll(x => x.Length > 0));

        return text;
    }
}
