using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class UtilityCurves
{
    static UtilityCurves()
    {

    }

    public static double Binary(bool boolean, double uTrue, double uFalse)
    {
        return (boolean) ? uTrue : uFalse;
    }

    public static double DiscreteMap(string key, Dictionary<string, double> uMap, double uDefault)
    {
        return (uMap.ContainsKey(key)) ? uMap[key] : uDefault;
    }

    public static double ExponentialAsymptote(double expobase, double exponent, double uZero, double uInfinite)
    {
        // NB: Exponent is well suited to unbounded, strict positives - like distance!
        // NB: Lack of negative, since we talk 0.0 < expobase <= 1.0!
        double power = Math.Pow(expobase, Math.Abs(exponent));
        double uDelta = uZero - uInfinite;

        return uInfinite + uDelta * power;
    }
}
