using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SurfacingRandomness
{
    private Random m_rng;

    public SurfacingRandomness(int seed)
    {
        m_rng = new Random(seed);
    }

    public Microanthology SurfaceMicrostories(Microanthology microanthology, int count = int.MaxValue)
    {
        Dictionary<string, Dictionary<string, List<Microstory>>> surfacedMicrostories = new Dictionary<string, Dictionary<string, List<Microstory>>>();

        // NB: Duplicate microstories are not considered; going for uniform randomness...
        Dictionary<string, Dictionary<string, List<Microstory>>> microstories = microanthology.GetMicrostories(true);
        foreach (string focalisation in microstories.Keys)
        {
            surfacedMicrostories.Add(focalisation, new Dictionary<string, List<Microstory>>());
            foreach (string pattern in microstories[focalisation].Keys)
                surfacedMicrostories[focalisation].Add(pattern, microstories[focalisation][pattern].OrderBy(x => m_rng.Next()).Take(count).ToList());
        }

        return new Microanthology(surfacedMicrostories);
    }
}
