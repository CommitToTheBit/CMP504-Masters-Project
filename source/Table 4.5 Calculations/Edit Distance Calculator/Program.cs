using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        List<List<KeyValuePair<string, int>>> timelines = new List<List<KeyValuePair<string, int>>>()
        {
            new List<KeyValuePair<string, int>>() { new KeyValuePair<string,int>("C1", 0), new KeyValuePair<string, int>("C2", 0), new KeyValuePair<string, int>("B1", 0), new KeyValuePair<string, int>("A1", 0), new KeyValuePair<string, int>("A2", 0), new KeyValuePair<string, int>("A3", 0), new KeyValuePair<string, int>("B2", 0), new KeyValuePair<string, int>("A4", 0), new KeyValuePair<string, int>("B3", 0), new KeyValuePair<string, int>("C3", 0), new KeyValuePair<string, int>("A5", 1), new KeyValuePair<string, int>("C4", 1), new KeyValuePair<string, int>("B4", 0), new KeyValuePair<string, int>("B5", 0), new KeyValuePair<string, int>("C5", 0), },
            new List<KeyValuePair<string, int>>() { new KeyValuePair<string,int>("A1", 0), new KeyValuePair<string, int>("B1", 0), new KeyValuePair<string, int>("C1", 0), new KeyValuePair<string, int>("A2", 0), new KeyValuePair<string, int>("A3", 1), new KeyValuePair<string, int>("B2", 1), new KeyValuePair<string, int>("B3", 0), new KeyValuePair<string, int>("C2", 0), new KeyValuePair<string, int>("A4", 2), new KeyValuePair<string, int>("C3", 2), new KeyValuePair<string, int>("A5", 3), new KeyValuePair<string, int>("C4", 3), new KeyValuePair<string, int>("B4", 3), new KeyValuePair<string, int>("B5", 4), new KeyValuePair<string, int>("C5", 4), },
            new List<KeyValuePair<string, int>>() { new KeyValuePair<string,int>("A1", 0), new KeyValuePair<string, int>("A2", 0), new KeyValuePair<string, int>("A3", 0), new KeyValuePair<string, int>("C1", 0), new KeyValuePair<string, int>("B1", 0), new KeyValuePair<string, int>("B2", 0), new KeyValuePair<string, int>("B3", 0), new KeyValuePair<string, int>("C2", 0), new KeyValuePair<string, int>("C3", 0), new KeyValuePair<string, int>("A4", 0), new KeyValuePair<string, int>("B4", 0), new KeyValuePair<string, int>("B5", 0), new KeyValuePair<string, int>("A5", 1), new KeyValuePair<string, int>("C4", 1), new KeyValuePair<string, int>("C5", 0), },
            new List<KeyValuePair<string, int>>() { new KeyValuePair<string,int>("A2", 0), new KeyValuePair<string, int>("B1", 0), new KeyValuePair<string, int>("C2", 0), },
            new List<KeyValuePair<string, int>>() { new KeyValuePair<string,int>("A1", 0), new KeyValuePair<string, int>("C2", 0), new KeyValuePair<string, int>("A2", 0), new KeyValuePair<string, int>("C1", 0), new KeyValuePair<string, int>("A3", 0), new KeyValuePair<string, int>("B2", 0), new KeyValuePair<string, int>("A4", 0), new KeyValuePair<string, int>("B4", 0), new KeyValuePair<string, int>("B5", 0), new KeyValuePair<string, int>("A5", 0), new KeyValuePair<string, int>("C5", 0), },
            new List<KeyValuePair<string, int>>() { new KeyValuePair<string,int>("A3", 0), new KeyValuePair<string, int>("A1", 0), new KeyValuePair<string, int>("B1", 0), new KeyValuePair<string, int>("C1", 0), new KeyValuePair<string, int>("A2", 0), new KeyValuePair<string, int>("B2", 0), new KeyValuePair<string, int>("A5", 0), new KeyValuePair<string, int>("B3", 0), new KeyValuePair<string, int>("C3", 0), new KeyValuePair<string, int>("A4", 0), new KeyValuePair<string, int>("C4", 0), new KeyValuePair<string, int>("B4", 0), new KeyValuePair<string, int>("B5", 0), },
            new List<KeyValuePair<string, int>>() { new KeyValuePair<string,int>("A4", 0), new KeyValuePair<string, int>("A2", 0), new KeyValuePair<string, int>("A3", 1), new KeyValuePair<string, int>("B2", 1), new KeyValuePair<string, int>("B1", 0), new KeyValuePair<string, int>("C1", 0), new KeyValuePair<string, int>("C3", 0), new KeyValuePair<string, int>("A5", 2), new KeyValuePair<string, int>("C4", 2), new KeyValuePair<string, int>("A1", 0), new KeyValuePair<string, int>("B4", 0), new KeyValuePair<string, int>("B5", 0), new KeyValuePair<string, int>("B3", 0), new KeyValuePair<string, int>("C5", 0), new KeyValuePair<string, int>("C2", 0), },
            new List<KeyValuePair<string, int>>() { new KeyValuePair<string,int>("A1", 0), new KeyValuePair<string, int>("A2", 0), new KeyValuePair<string, int>("A3", 0), new KeyValuePair<string, int>("A2", 0), new KeyValuePair<string, int>("C2", 0), new KeyValuePair<string, int>("B2", 0), new KeyValuePair<string, int>("A3", 0), new KeyValuePair<string, int>("B3", 0), new KeyValuePair<string, int>("C3", 0), new KeyValuePair<string, int>("C4", 0), new KeyValuePair<string, int>("A5", 0), new KeyValuePair<string, int>("C5", 0), new KeyValuePair<string, int>("B4", 1), new KeyValuePair<string, int>("B5", 1), new KeyValuePair<string, int>("A4", 0), },
            new List<KeyValuePair<string, int>>() { },
            new List<KeyValuePair<string, int>>() { new KeyValuePair<string,int>("A1", 0), new KeyValuePair<string, int>("B3", 0), new KeyValuePair<string, int>("A3", 0), new KeyValuePair<string, int>("C5", 0), new KeyValuePair<string, int>("C1", 0), new KeyValuePair<string, int>("C4", 0), new KeyValuePair<string, int>("A2", 0), new KeyValuePair<string, int>("C2", 0), new KeyValuePair<string, int>("B1", 0), new KeyValuePair<string, int>("B2", 0), new KeyValuePair<string, int>("C3", 0), new KeyValuePair<string, int>("B5", 0), new KeyValuePair<string, int>("B4", 1), new KeyValuePair<string, int>("A4", 1), new KeyValuePair<string, int>("A1", 1), },
        };

        for (int i = 1; i < timelines.Count; i++)
        {
            Console.Write("#" + String.Format("{0," + (1 + Math.Floor(Math.Log10(Math.Max(timelines.Count - 1, 1)))) + "}", i) + ":");
            for (int j = 1; j <= timelines.Count; j++)
            {
                Console.Write(" " + String.Format("{0,3}", LCSDistance(timelines[i], timelines[j % timelines.Count])));
            }
            Console.WriteLine();
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- //
    // This enclosed section has been adapted from: GeeksforGeeks (no date) Longest Common Subsequence (LCS). Available at: https://www.geeksforgeeks.org/longest-common-subsequence-dp-4/ (Accessed: 1 September 2023) //
    
    static public int LCS(List<string> a, List<string> b)
    {
        int[,] L = new int[a.Count + 1, b.Count + 1];

        // Following steps build L[m+1][n+1]
        // in bottom up fashion.
        // Note that L[i][j] contains length of
        // LCS of X[0..i-1] and Y[0..j-1]
        for (int i = 0; i <= a.Count; i++)
        {
            for (int j = 0; j <= b.Count; j++)
            {
                if (i == 0 || j == 0)
                    L[i, j] = 0;
                else if (a[i - 1] == b[j - 1])
                    L[i, j] = L[i - 1, j - 1] + 1;
                else
                    L[i, j] = Math.Max(L[i - 1, j], L[i, j - 1]);
            }
        }
        return L[a.Count, b.Count];
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- //

    static public int LCSDistance(List<string> a, List<string> b)
    {
        int lcs = LCS(a, b);
        return (a.Count - lcs) + (b.Count - lcs) + Math.Abs(a.Count - b.Count);
    }

    static public int LCSDistance(List<KeyValuePair<string, int>> a, List<KeyValuePair<string, int>> b)
    {
        List<List<string>> aPermutations = GetGroupedPermutations(a);
        List<List<string>> bPermutations = GetGroupedPermutations(b);

        int lcsDistance = int.MaxValue;
        foreach (List<string> aPermutation in aPermutations)
        {
            foreach (List<string> bPermutation in bPermutations)
            {
                lcsDistance = Math.Min(LCSDistance(aPermutation, bPermutation), lcsDistance);
                if (lcsDistance <= 0)
                    return lcsDistance;
            }
        }

        return lcsDistance; // NB: Edit distances w/o permutations given by LCSDistance(a.Select(x => x.Key).ToList(), b.Select(x => x.Key).ToList());...
    }

    static public List<List<string>> GetGroupedPermutations(List<KeyValuePair<string, int>> a)
    {
        List<List<string>> groupedPermutations = new List<List<string>>() { a.Select(x => x.Key).ToList() };
        foreach (int group in a.Select(x => x.Value).ToList().FindAll(x => x > 0).Distinct())
        {
            Dictionary<int, string> indices = new Dictionary<int, string>(a.FindAll(x => x.Value == group).Select(x => new KeyValuePair<int, string>(a.IndexOf(x), x.Key)));
            List<List<string>> permutations = GetPermutations(indices.Values.ToList());

            List<List<string>> iterations = new List<List<string>>();
            foreach (List<string> groupedPermutation in groupedPermutations)
            {
                foreach (List<string> permutation in permutations)
                {
                    List<string> iteration = new List<string>(groupedPermutation);
                    for (int i = 0; i < indices.Count; i++)
                        iteration[indices.Keys.ElementAt(i)] = permutation[i];

                    iterations.Add(iteration);
                }
            }
            groupedPermutations = iterations;
        }

        return groupedPermutations;
    }

    static public List<List<string>> GetPermutations(List<string> a)
    {
        List<List<string>> permutations = new List<List<string>>() { new List<string>() };
        for (int i = 0; i < a.Count; i++)
        {
            List<List<string>> iterations = new List<List<string>>();
            foreach (List<string> permutation in permutations)
            {
                foreach (string element in a.Except(permutation)) 
                {
                    List<string> iteration = new List<string>(permutation);
                    iteration.Add(element);

                    iterations.Add(iteration);
                }
            }
            permutations = iterations;
        }

        return permutations;
    }
}