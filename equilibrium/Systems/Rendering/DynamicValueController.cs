using System.Numerics;
using Equilibrium.Components;

namespace Equilibrium.Systems.Rendering;

public class DynamicValueController
{
    private Dictionary<float, ColorVector3> keyMap = new Dictionary<float, ColorVector3>();

    public DynamicValueController(Dictionary<float, ColorVector3> keyMap)
    {
        this.keyMap = keyMap;
    }

    public void Clear()
    {
        keyMap.Clear();
    }

    public ColorVector3 GetValue(float time)
    {
        var itUpper = GetUpperBound(keyMap, time + 1e-6f);
        var itLower = GetLowerBound(keyMap, time + 1e-6f);

        if (itLower.Key == itUpper.Key)
        {
            return itUpper.Value;
        }

        if (itLower.Key == GetEnd(keyMap).Key)
        {
            return itUpper.Value;
        }

        if (itUpper.Key == GetEnd(keyMap).Key)
        {
            return itLower.Value;
        }

        float lowerTime = itLower.Key;
        var lowerVal = itLower.Value;
        float upperTime = itUpper.Key;
        var upperVal = itUpper.Value;

        if (lowerTime == upperTime)
        {
            return lowerVal;
        }

        return Interpolate(lowerTime, lowerVal, upperTime, upperVal, time);
    }

    private ColorVector3 Interpolate(float lowerTime, ColorVector3 lowerVal, float upperTime, ColorVector3 upperVal, float time)
    {
        float tt = (time - lowerTime) / (upperTime - lowerTime);
        return new ColorVector3 { Value = Vector3.Lerp(lowerVal.Value, upperVal.Value, tt) };
    }

    private static KeyValuePair<float, ColorVector3> GetUpperBound(Dictionary<float, ColorVector3> dict, float key)
    {
        KeyValuePair<float, ColorVector3>? found = null;
        foreach (var entry in dict)
        {
            if (entry.Key > key)
            {
                if (!found.HasValue || entry.Key < found.Value.Key)
                {
                    found = entry;
                }
            }
        }

        return found.HasValue ? found.Value : new KeyValuePair<float, ColorVector3>();
    }

    private static KeyValuePair<float, ColorVector3> GetLowerBound(Dictionary<float, ColorVector3> dict, float key)
    {
        KeyValuePair<float, ColorVector3>? found = null;
        foreach (var entry in dict)
        {
            if (entry.Key <= key)
            {
                if (!found.HasValue || entry.Key > found.Value.Key)
                {
                    found = entry;
                }
            }
        }

        return found.HasValue ? found.Value : new KeyValuePair<float, ColorVector3>();
    }

    private static KeyValuePair<float, ColorVector3> GetEnd(Dictionary<float, ColorVector3> dict)
    {
        return new KeyValuePair<float, ColorVector3>();
    }
}