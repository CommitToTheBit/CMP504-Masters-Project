﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;
using static Agent;
using static System.Net.Mime.MediaTypeNames;

// AGENT CLASS: Perceives, interprets, and devises beats... 
public partial class Agent
{
    private Random m_rng;

    public string m_ID;// m_character;
    public Characterisation m_characterisation;

    // Physical Strategy
    List<KeyValuePair<string, float>> m_path;

    // Epistemological framework...
    public Script m_script;   // NB: Subjective view of character knowledge...
    public AttentionSpan m_attention;

    public Stage m_stage;     // NB: Subjective view of surroundings... // FIXME: Include 'Hide and Sneak'-style location tracking?
    public string m_setting;

    // Story sifting... // NB: Not used to make decisions, for efficiency?
    public Trace m_trace;

    // Constructor...
    public Agent(Characterisation characterisation, Script script, Stage stage, int seed)
    {
        m_rng = new Random(seed);

        m_ID = characterisation.m_character;
        m_characterisation = new Characterisation(characterisation);

        m_path = new List<KeyValuePair<string, float>>();

        m_script = new Script(script, m_rng.Next());
        m_script.Focalise(m_ID);
        m_attention = new AttentionSpan(m_ID); //"START"; // FIXME: Where should initial conditions be set?

        m_stage = new Stage(stage, m_rng.Next());
        m_setting = InitialiseSetting();

        m_trace = new Trace();

        // VARIABLES IN OTHER PARTIAL CLASSES... //

        m_following = new Dictionary<string, int>();

        InitialiseStrategies();
    }

    public string InitialiseSetting()
    {
        Predicate<string> validity = x => m_stage.m_settings.Keys.Contains(x);
        List<string> settings = m_characterisation.GetAttribute("INITIAL SETTING").FindAll(validity);

        if (settings.Count == 0)
            settings = new List<string>(m_stage.m_settings.Keys);

        return settings.OrderBy(x => m_rng.Next()).First();
    }

    public void FocalisePause(int tick, ref Stage stage)
    {
        // UPDATING MENTAL MODEL AND TRACE AT A DESCRIPTIVE PAUSE //

        // STEP 1: Updating mental model through observation...
        foreach (string character in stage.GetCharactersLOS(tick, m_setting))
            m_stage.UpdateCharacter(tick, character, stage.GetLastAt(character).Key);

        m_attention.Tick();

        // STEP 2: Updating 'stage directions' from mental model...
        m_trace.AddPause(tick, m_ID, m_setting, m_stage.GetCharactersAt(tick, m_setting));
    }

    public void FocaliseWalk(int tick, Walk walk, ref Stage stage)
    {
        // UPDATING MENTAL MODEL AND TRACE WHEN A 'NEARBY' CHARACTER WALKS //

        // STEP 1: Pre-processing from mental model... // NB: Mental model will be updated on every nearby walk, so this audience should be up-to-date...
        List<string> audience = m_stage.GetCharactersLOS(tick, walk.m_initialSetting);

        // STEP 2: Updating mental model with character action...
        m_stage.UpdateCharacter(tick, walk.m_actor, walk.m_finalSetting);

        if (walk.m_actor.Equals(m_ID))
        {
            m_setting = walk.m_finalSetting;
            foreach (string character in stage.GetCharactersLOS(tick, m_setting))
                m_stage.UpdateCharacter(tick, character, stage.GetLastAt(character).Key);

            ResetWaiting(tick);
        }

        // STEP 4: Post-processing from mental model...
        audience = audience.Union(m_stage.GetCharactersLOS(tick, walk.m_finalSetting)).ToList();

        // STEP 5: Updating trace from mental model...
        // FIXME: Is it okay that this doesn't update from world model? I'm happy with it; adds some minor fallibility... 
        m_trace.AddWalk(tick, m_ID, walk, audience);

        if (walk.m_actor.Equals(m_ID))
        {
            m_trace.AddPause(tick, m_ID, m_setting, m_stage.GetCharactersAt(tick, m_setting));
        }
    }
    public void FocaliseTalk(int tick, Talk talk)
    {
        // UPDATING MENTAL MODEL AND TRACE WHEN A 'NEARBY' CHARACTER TALKS //

        Talk focalisedTalk = new Talk(talk);

        // STEP 1: Pre-processing from emtnal model...

        // "Overhearing" processing...
        // FIXME: Too post-hoc? // FIXME: Should we take "intended audience" as a given? Surely not...
        List<string> internalAudience = new List<string>(m_stage.GetCharactersAt(tick, talk.m_setting)); // NB: This is SUBJECTIVELY who was INTENDED to hear... 
        if (!internalAudience.Contains(talk.m_actor))
            internalAudience.Add(talk.m_actor);

        List<string> externalAudience = new List<string>();
        if (!internalAudience.Contains(m_ID))
            externalAudience.Add(m_ID);

        List<string> audience = internalAudience.Union(externalAudience).Distinct().ToList();

        focalisedTalk.SetInternalAudience(internalAudience); // NB: Override needs included, given sightlines!

        // STEP 3: Updating world model with character action...
        m_script.Tick(audience, focalisedTalk); // FIXME: Assumes all characters hear line...

        if (talk.m_actor.Equals(m_ID))
        {
            UpdateWaiting(tick);            
        }

        if (internalAudience.Contains(m_ID))
        {
            // NB: If you know everyone knows what's just been said, the conversation is drying up...
            //bool repetitive = m_script.GetCharactersJustAware(talk.m_line).Intersect(internalAudience).Count() == 0;

            m_attention.Queue(talk);
        }

        // STEP 4: Post-process...

        // STEP 5: Updating trace from world model...
        m_trace.AddTalk(m_ID, focalisedTalk, externalAudience, m_script, m_stage);
    }
}
