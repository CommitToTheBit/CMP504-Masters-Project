using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Grammar
{
	private class ProductionRule
	{
		public string m_production;
		public float m_weight;
		public int m_recency;

		public ProductionRule(string production, float weight)
        {
			m_production = production;
			m_weight = weight;
			m_recency = 0;
        }
	};

	private Random m_rng;
	private int m_generations;

	private Dictionary<string, List<ProductionRule>> m_productionRules;

	public Grammar(int seed)
	{
		m_rng = new Random(seed);
		m_generations = 0;

		m_productionRules = new Dictionary<string, List<ProductionRule>>();
	}

	~Grammar()
	{

	}

    public void InitialiseProductionRule(string letter, List<string> productions)
    {
        /* ------------------------------------------------------------- */
        /* Add 'basic' production rules into the grammar, for parsing... */

		foreach (string production in productions)
			AddProductionRule(letter, new ProductionRule(production, 1.0f));

        /* ------------------------------------------------------------- */
    }

    public void InitialiseProductionRule(string letter, string production)
	{
		/* ------------------------------------------------------------- */
		/* Add 'basic' production rules into the grammar, for parsing... */

		AddProductionRule(letter, new ProductionRule(production, 1.0f));

		/* ------------------------------------------------------------- */
	}

	public string GenerateSentence(string axiom, bool nested = false)
	{
		if (!nested) m_generations++;

		string sentence = axiom;

		string iteratedSentence, nestedSentence;
		while (sentence.IndexOf("{") != -1)
		{
			iteratedSentence = "";
			while (sentence.IndexOf("{") != -1)
			{
				iteratedSentence += sentence.Substring(0, sentence.IndexOf("{"));
				sentence = sentence.Remove(0, sentence.IndexOf("{"));

				int index = FindClosingBracket(sentence);
				if (index < 1) // THIS IS THE ISSUE
					continue;

				sentence = sentence.Remove(0, 1); // NB: Must wait for FindClosingBracket call to erase opening bracket...
				index--;

				nestedSentence = GenerateSentence(sentence.Substring(0, index), true); // NB: Recursive calls like this aren't ideal, but there'll never be too many nested brackets at once...

				iteratedSentence += GetProductionRule(nestedSentence); // NB: nullptr passed in as 'forgetfulness override'... // NB: "Suit" is currently a special case, but this too can be generalised!

				sentence = sentence.Remove(0, index + 1);
			}
			iteratedSentence += sentence; // NB: Adds the remainder of the sentence, which doesn't need parsed...
			sentence = iteratedSentence;
		}

		if (nested) return sentence.ToUpper();

		//return sentence;
		return PostProcessSentence(sentence);
	}

	private string PostProcessSentence(string sentence)
	{
		int index;
		string iteratedSentence;

		// STEP 1: Remove unnecessary spaces...
		index = sentence.IndexOf(" ");
		iteratedSentence = "";
		while (index != -1)
		{
			iteratedSentence += sentence.Substring(0, index + 1);
			sentence = sentence.Remove(0, index + 1);

			while (sentence.IndexOf(" ") == 0) sentence = sentence.Remove(0, 1);

			index = sentence.IndexOf(" ");
		}
		iteratedSentence += sentence;
		sentence = iteratedSentence;

		if (sentence.IndexOf(" ") == 0) sentence = sentence.Remove(0, 1);
		if (sentence.LastIndexOf(" ") == sentence.Length - 1 && sentence.Length > 0) sentence = sentence.Remove(sentence.Length - 1, 1);

		// STEP 2: Capitalise sentence...
		// After the start of the sentence and after every full stop, ensure the next character that can be capitalised *is* capitalised...
		// NB: Is "\n" treated as a single character? If so, should be handled without any special case...
		/*index = 0;
		while (index < sentence.Length && index != -1)
		{
			if (char.ToLower(sentence[index]) == char.ToUpper(sentence[index]))
			{
				index++;
			}
			else
			{
				sentence = sentence.Remove(index, 1).Insert(1, char.ToUpper(sentence[index]).ToString());
				index = sentence.IndexOf(".", index + 1);
			}
		}*/

		// STEP N: Add line breaks...
		// FIXME: Handle this with -1/non-negative int case, with overflow allowed...

		return sentence;
	}

	private void AddProductionRule(string letter, ProductionRule productionRule)
    {
		if (!m_productionRules.ContainsKey(letter))
			m_productionRules.Add(letter, new List<ProductionRule>() { productionRule });
		else if (!m_productionRules[letter].Contains(productionRule)) // NB: Does this go by reference or by value?
			m_productionRules[letter].Add(productionRule);
	}

	private string GetProductionRule(string letter, bool generation = true)
	{
		if (!m_productionRules.ContainsKey(letter))
			return "";

        double totalWeight = m_productionRules[letter].Select(x => GetWeight(x, letter)).Sum();
        double randomWeight = totalWeight * m_rng.NextDouble();

        double cumulativeWeight = 0.0;
		for (int i = 0; i < m_productionRules[letter].Count; i++)
        {
			if (cumulativeWeight + GetWeight(m_productionRules[letter][i], letter) >= randomWeight)
			{
                if (generation) // NB: Remember this has been used...
                    m_productionRules[letter][i].m_recency = m_generations;

                return m_productionRules[letter][i].m_production;
			}

            cumulativeWeight += GetWeight(m_productionRules[letter][i], letter);
        }

        return (m_productionRules[letter].Count > 0) ? m_productionRules[letter].Last().m_production : "";
	}

    private float GetWeight(ProductionRule productionRule, string letter)
	{
		float weight = productionRule.m_weight;

		// STEP 1: Scale by recency...
		List<int> generations = new List<int>();
		foreach (ProductionRule rule in m_productionRules[letter])
		{
			generations.Add(rule.m_recency);
		}
		generations.OrderByDescending(x => x);

		float minimumWeighting = 0.01f; // NB: The most recently-used production rule will be minimumWeighting times as likely to be surfaced as the least recent...
		float exponent = ((float)generations.LastIndexOf(productionRule.m_recency)) / Math.Max(generations.Count, 1);
		weight *= (productionRule.m_recency < m_generations) ? (float)Math.Pow(minimumWeighting, exponent) : 0.0f; // NB: Explicitely blocks the uses of the same production rule (though not the same word!) in one generation (unless there are no other options)...

		return weight;
	}

	private int FindClosingBracket(string sentence)
	{
		if (sentence.IndexOf("{") != 0)
			return -1;

		int depth = 1;
		int index = 0;
		int openingIndex, closingIndex;
		while (depth > 0)
		{
			openingIndex = sentence.IndexOf("{", index + 1);
			closingIndex = sentence.IndexOf("}", index + 1);

			if (closingIndex == -1)
				return closingIndex;

			if (closingIndex < openingIndex || openingIndex == -1)
			{
				index = closingIndex;
				depth--;
			}
			else
			{
				index = openingIndex;
				depth++;
			}
		}

		return index;
	}
}