using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Agent : Node
{
    // "FOLLOW" behaviours...
    int m_followingSurplus, m_followingDeficit;
    Dictionary<string, int> m_following;

    // "WAIT" behaviours...
    int m_waitingSince;

    private void InitialiseStrategies()
    {
        m_followingSurplus = (int)m_characterisation.GetStatistic("FOLLOWING SURPLUS") + 1;
        m_followingDeficit = (int)m_characterisation.GetStatistic("FOLLOWING DEFICIT") - 1;
        
        m_following = new Dictionary<string, int>();
        foreach (string character in m_characterisation.GetAttribute("FOLLOWING"))
            m_following.Add(character, m_followingSurplus);

        m_waitingSince = int.MinValue/2; // NB: Dividing by 2 to best avoid rollover in 'tick - m_lastSpeaking'!
    }

    public void TickWalkingBudgets(int tick)
    {
        Predicate<string> followingPredicate = x => m_stage.GetCharactersLOS(tick, m_setting).Contains(x);
        TickBudget(ref m_following, m_followingSurplus, m_followingDeficit, followingPredicate);
    }

    private void TickBudget(ref Dictionary<string, int> budget, int surplus, int deficit, Predicate<string> predicate)
    {
        foreach (string character in budget.Keys)
        {
            if (budget[character] > 0 && m_path.Count == 0) // NB: Don't worry about the deficit if actively doing something else! // FIXME: Does this disadvantage 'stop-start' walkment?
            {
                if (!predicate(character))
                    budget[character]--;

                if (budget[character] <= 0)
                    budget[character] = deficit;
            }
            else if (budget[character] < 0)
            {
                if (predicate(character))
                    budget[character]++;

                if (budget[character] >= 0)
                    budget[character] = surplus;
            }
        }
    }

    private void ResetWaiting(int tick)
    {
        m_attention.Reset();
        m_waitingSince = Math.Max(tick - 1, m_waitingSince);
    }

    private void UpdateWaiting(int tick)
    {
        m_waitingSince = Math.Max(tick, m_waitingSince);
    }
}
