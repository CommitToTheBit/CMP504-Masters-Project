using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

public class AttentionSpan
{
    // CHARACTERSATION VARIABLES //
    private string m_character;

    // TALK VARIABLES //
    private List<(Agent.Talk talk, string interlocution)> m_queue, m_options;

    // PATIENCE VARIABLES //
    //private int m_impatience;
    //private bool m_impatienceReset;

    public AttentionSpan(string character)
    {
        m_character = character;

        m_queue = new List<(Agent.Talk, string)>();
        m_options = new List<(Agent.Talk, string)>();

        //m_impatience = 0;
        //m_impatienceReset = true;
    }

    public void Reset()
    {
        m_queue = new List<(Agent.Talk, string)>();
        m_options = new List<(Agent.Talk, string)>();

        //m_impatience = (!m_impatienceReset) ? m_impatience + 1 : 0;
        //m_impatienceReset = false;
    }

    public void Queue(Agent.Talk talk, string interlocution = "")
    {
        m_queue.Add((new Agent.Talk(talk), interlocution));
    }

    public void Tick()
    {
        // STEP 1: Add 'ignores' to queue...
        // If 'the interlocuted' has just spoken to you directly...
        foreach ((Agent.Talk talk, string interlocution) option in m_options.FindAll(x => x.interlocution.Length == 0 && !x.talk.m_actor.Equals(m_character)))
            Queue(option.talk, "ignore");

        // STEP 2: Transfer queue into options...
        m_options = new List<(Agent.Talk, string)>();
        foreach ((Agent.Talk talk, string interlocution) option in m_queue)
            m_options.Add((new Agent.Talk(option.talk), option.interlocution));

        m_queue = new List<(Agent.Talk, string)>();
    }

    public List<(Agent.Talk, string)> Get()
    {
        List<(Agent.Talk, string)> options = new List<(Agent.Talk, string)>();
        foreach ((Agent.Talk talk, string interlocution) option in m_options)
            options.Add((new Agent.Talk(option.talk), option.interlocution));

        return (options.Count > 0) ? options : new List<(Agent.Talk, string)>() { (new Agent.Talk(), string.Empty) };
    }
}
