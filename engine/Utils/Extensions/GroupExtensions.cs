using System.Reflection;
using Arch.System;

namespace Engine.Utils.Extensions;

public static class GroupExtensions
{
    public static int Count(this Group<float> group)
    {
        List<ISystem<float>>? systems = (List<ISystem<float>>)(typeof(Group<float>)?.GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(group))!;

        if (systems != null)
        {
            return systems.Count;
        }

        return 0;
    }

    public static Group<float> TryRemove(this Group<float> group, ISystem<float> system)
    {
        List<ISystem<float>>? systems = (List<ISystem<float>>)(typeof(Group<float>)?.GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(group))!;

        if (systems != null && systems.Contains(system))
        {
            systems.Remove(system);
        }

        return group;
    }

    public static Group<float> Add(this Group<float> group, ISystem system)
    {
        group.Add((ISystem<float>)system);
        return group;
    }

    public static Group<float> Add(this Group<float> group, params ISystem[] systems)
    {
        foreach (var system in systems)
        {
            group.Add((ISystem<float>)system);
        }

        return group;
    }
}