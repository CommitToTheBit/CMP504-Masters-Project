using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static Agent;

public partial class Script
{
    public List<string> GetKnownEdges(string character, string line)
    {
        Predicate<string> predicate = x => GetVertex(x).IsAware(character);
        return GetVertex(line).GetSegues().FindAll(predicate);
    }


    // CHECKING ACROSS VERTICES FOR ROLES //

    private List<Vertex> GetAssociatedVertices(string character, string role)
    {
        Predicate<Vertex> predicate = x => x.IsAware(character) && x.GetRoles().Contains(role);
        return m_lines.Select(x => GetVertex(x.Key)).ToList().FindAll(predicate).ToList();
    }

    private List<(int index, int tick, string actor, Dictionary<string, string> accusation)> GetAssociatedAwarenesses(string character, string role)
    {
        List<Vertex> vertices = GetAssociatedVertices(character, role);

        List<(int index, int tick, string actor, Dictionary<string, string> accusation)> awarenesses = new List<(int index, int tick, string actor, Dictionary<string, string> accusation)>();
        foreach (Vertex vertex in vertices)
            awarenesses = awarenesses.Union(vertex.GetAwarenesses(character)).OrderBy(x => x.index).ToList();

        return awarenesses;
    }

    private List<(int index, int tick, string actor, string identity)> GetAssociatedAccusations(string character, string role)
    {
        // NB: Get the times a name is attached to the role...
        return GetAssociatedAwarenesses(character, role).FindAll(x => x.accusation.ContainsKey(role)).Select(x => (x.index, x.tick, x.actor, x.accusation[role])).ToList();
    }

    private bool IsMultirole(string character, string role)
    {
        // WARNING: This implementation has not been fully tested! Should be sufficient for its intended purpose...
        // ...But definitely not robust!
        return !GetAssociatedVertices(character, role).Select(x => x.IsMultirole(role)).Contains(false);
    }

    private List<(int index, int tick, string actor, string identity)> GetInternalisedAccusations(string character, string role)
    {
        // NB: Get the first time a character has 'internalised' a role...
        // MVP: Characters internalise beliefs about themselves in 'before-time', and about others at any time...
        Predicate<(int index, int tick, string actor, string identity)> internalisations = x => x.tick < 0 || !x.identity.Equals(character);

        return GetAssociatedAccusations(character, role).FindAll(internalisations).ToList();
    }

    private (string actor, string identity) GetInternalBelief(string character, string role)
    {
        Func<(int index, int tick, string actor, string identity), (string, string)> asBeliefs = x => (x.actor, x.identity);

        return (HasInternalBelief(character, role)) ? GetInternalisedAccusations(character, role).Select(asBeliefs).First() : (string.Empty, string.Empty);
    }

    private bool HasInternalBelief(string character, string role)
    {
        return GetInternalisedAccusations(character, role).Count > 0;
    }

    private bool IsInternalBelief(string character, string role, string identity)
    {
        // FIXME: 'Multibelief' is ill-defined!
        return HasInternalBelief(character, role) && GetInternalBelief(character, role).identity.Equals(identity) && !IsMultirole(character, role);
    }

    private bool IsInternallyConsistent(string character, string role, string identity, bool multibelief = true)
    {
        // FIXME: 'Multibelief' is ill-defined!
        return !HasInternalBelief(character, role) || GetInternalBelief(character, role).identity.Equals(identity) || (multibelief && IsMultirole(character, role));
    }

    private List<(int index, int tick, string actor, string identity)> GetExternalisedAccusations(string characterA, string characterB, string role)
    {
        // NB: Get the times a name is associated to the role, done by the specified character, as witnessed by the specified focus...
        List<(int index, int tick, string actor, string identity)> accusationsByA = GetAssociatedAccusations(characterB, role).FindAll(x => x.actor.Equals(characterA) && !x.identity.Equals(characterB)); // WARNING: Second clause is experiemental!
        List<(int index, int tick, string actor, string identity)> accusationsByB = GetAssociatedAccusations(characterA, role).FindAll(x => x.actor.Equals(characterB) && !x.identity.Equals(characterA)); // WARNING: Second clause is experiemental!

        return accusationsByA.Union(accusationsByB).ToList();
    }

    private (string actor, string identity) GetExternalBelief(string characterA, string characterB, string role)
    {
        Func<(int index, int tick, string actor, string identity), (string, string)> asBeliefs = x => (x.actor, x.identity);

        return (HaveExternalBelief(characterA, characterB, role)) ? GetExternalisedAccusations(characterA, characterB, role).Select(asBeliefs).First() : (string.Empty, string.Empty);
    }

    private bool HaveExternalBelief(string characterA, string characterB, string role)
    {
        return GetExternalisedAccusations(characterA, characterB, role).Count > 0;
    }

    private bool HaveSingularExternalBelief(string characterA, string characterB, string role)
    {
        Func<(int index, int tick, string actor, string identity), string> asIdentities = x => x.identity;

        return GetExternalisedAccusations(characterA, characterB, role).Select(asIdentities).Distinct().Count() == 1;
    }

    private bool IsExternalBelief(string characterA, string characterB, string role, string identity)
    {
        // FIXME: 'Multibelief' is ill-defined!
        return HaveExternalBelief(characterA, characterB, role) && GetExternalBelief(characterA, characterB, role).identity.Equals(identity) && (!IsMultirole(characterA, role) || !IsMultirole(characterB, role));
    }

    private bool IsExternallyConsistent(string characterA, string characterB, string role, string identity, bool multibelief = true)
    {
        // FIXME: 'Multibelief' is ill-defined!
        return !HaveExternalBelief(characterA, characterB, role) || GetExternalBelief(characterA, characterB, role).identity.Equals(identity) || (multibelief && IsMultirole(characterA, role) && IsMultirole(characterB, role));
    }

    private bool IsCorrect(string focalisation, string actor, string role, string identity, bool strict = false, bool autocorrect = true)
    {
        // DEBUG: 'Hard' override for multiroles; unnecessary?
        //if (IsMultirole(focalisation, role))
        //    return true;

        bool focalised = !focalisation.Equals("NONFOCALISED");
        autocorrect &= focalisation.Equals(actor);
        if (autocorrect)// || IsMultirole(focalisation, role)) // Autocorrect - if the focal character is the one speaking, do we treat it as automatically correct?
        {
            return true;
        }
        else if (!strict) // Strictness - are we looking for an exact belief, or just anything that fits?
        {
            return IsInternallyConsistent((focalised) ? focalisation : actor, role, identity);
        }
        else
        {
            return IsInternalBelief((focalised) ? focalisation : actor, role, identity);
        }
    }

    public (string actor, string receiver, string identity) GetCorrectBelief(string focalisation, string role)
    {
        (string actor, string identity) belief = GetInternalBelief(focalisation, role);
        return (belief.actor, focalisation, belief.identity);
    }

    private bool IsConsistent(string focalisation, string actor, string role, string identity)
    {
        // DEBUG: 'Hard' override for multiroles; unnecessary?
        //if (IsMultirole(focalisation, role))
        //    return true;

        bool focalised = !focalisation.Equals("NONFOCALISED");
        bool internalView = focalisation.Equals(actor) || !focalised;
        if (internalView)
        {
            return IsCorrect(focalisation, actor, role, identity, false, false);
        }
        else if (IsInternalBelief(focalisation, role, actor))
        {
            return actor.Equals(identity);
        }
        else
        {
            return IsExternallyConsistent(focalisation, actor, role, identity);
        }
    }

    // FIXME: How to add (stirng actor, string identity, string believer)?
    public (string actor, string receiver, string identity) GetConsistentBelief(string focalisation, string actor, string role)
    {
        bool focalised = !focalisation.Equals("NONFOCALISED");
        bool internalView = focalisation.Equals(actor) || !focalised;
        if (internalView)
        {
            (string actor, string identity) belief = GetInternalBelief((focalised) ? focalisation : actor, role);
            return (belief.actor, (focalised) ? focalisation : actor, belief.identity);
        }
        else if (IsInternalBelief(focalisation, role, actor))
        {
            return (actor, actor, actor);
        }
        else
        {
            (string actor, string identity) belief = GetExternalBelief(focalisation, actor, role);
            return (belief.actor, (belief.actor.Equals(actor)) ? focalisation : actor, belief.identity);
        }
    }


    public double GetConsistency(string actor, List<string> audience, Dictionary<string, string> accusation)
    {
        // NB: We factor the actor in to the audience, to bias them towards internal consistency!
        double normalisation = audience.Count * accusation.Count;
        if (normalisation == 0.0)
            return 1.0;

        double consistency = 0.0;
        foreach (string focus in audience)
            foreach (string role in accusation.Keys)
                consistency += Convert.ToDouble(IsConsistent(focus, actor, role, accusation[role]));

        return consistency / normalisation;
    }

    private Dictionary<string, bool> IsAccusationCorrect(string focalisation, string actor, Dictionary<string, string> accusation, bool strict = false, bool autocorrect = true)
    {
        Func<KeyValuePair<string, string>, bool> asCorrectness = x => IsCorrect(focalisation, actor, x.Key, x.Value, strict, autocorrect);
        return new Dictionary<string, bool>(accusation.Select(x => new KeyValuePair<string, bool>(x.Key, asCorrectness(x))));
    }

    public Dictionary<string, bool> IsAccusationCorrect(string focalisation, string actor, Dictionary<string, string> accusation)
    {
        // NB: Note that "NONFOCALISED" focalisation automatically returns true, since presumption is not required...
        // NB: ...Reflects the subjectivity of the simulation, that it won't impose an 'objective' truth!
        // NB: Moreover, when autocorrect is on, one's own lies are never registered as incorrect (only dishonest)...
        return IsAccusationCorrect(focalisation, actor, accusation, false, true);
    }

    public Dictionary<string, bool> IsAccusationHonest(string focalisation, string actor, Dictionary<string, string> accusation)
    {
        Func<KeyValuePair<string, string>, bool> asHonesty = x => IsConsistent(focalisation, actor, x.Key, x.Value);
        return new Dictionary<string, bool>(accusation.Select(x => new KeyValuePair<string, bool>(x.Key, asHonesty(x))));
    }

    public Dictionary<string, bool> IsAccusationAccidentallyHonest(string focalisation, string actor, Dictionary<string, string> accusation)
    {
        // NB: A subtlety to turning 'autocorrect' off here - one can never realise oneself has been honest by accident...
        // NB: Likewise, turning on 'strict' means nonfocalisations are never accidentally correct...
        Dictionary<string, bool> correctnesses = IsAccusationCorrect(focalisation, actor, accusation, true, false);
        Dictionary<string, bool> honesties = IsAccusationHonest(focalisation, actor, accusation);

        return new Dictionary<string, bool>(accusation.Select(x => new KeyValuePair<string, bool>(x.Key, correctnesses[x.Key] && !honesties[x.Key])));
    }
}
