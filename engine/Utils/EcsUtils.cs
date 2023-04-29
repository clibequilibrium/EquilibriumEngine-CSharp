using System.Reflection;
using Arch.Core;
using Arch.Core.Utils;

namespace Engine.Utils;

public static class EcsUtils
{
    public static void UpdateComponentRegistry(Assembly assembly)
    {
        var oldNewTypeDictionary = new Dictionary<Type, Type>();

        // Find obsolete types
        foreach (var kvp in ComponentRegistry.Types)
        {
            var newType = assembly.GetType(kvp.Key.FullName!);

            if (newType == null)
                continue;

            if ((kvp.Key != newType) && kvp.Key.FullName == newType.FullName)
            {
                oldNewTypeDictionary.Add(kvp.Key, newType);
            }
        }

        // Replace type registry with the new types
        foreach (var kvp in oldNewTypeDictionary)
        {
            var archComponentType = ComponentRegistry.Types[kvp.Key];
            ComponentRegistry.Remove(kvp.Key);
            ComponentRegistry.Types.Add(kvp.Value, new ComponentType(archComponentType.Id, kvp.Value, archComponentType.ByteSize, archComponentType.ZeroSized));
        }

        // Replace entity components with the new types for the Archetype and replace Chunk components with deep copies
        Engine.World.Archetypes.ForEach(archetype =>
        {
            if (archetype != null)
            {
                SetComponentTypes(archetype, oldNewTypeDictionary);
                SetChunkComponents(archetype, oldNewTypeDictionary);
            }
        });
    }

    private static object CopyData(Type type, object source)
    {
        object target = Activator.CreateInstance(type)!;

        if (source == null)
        {
            return target;
        }

        Type sourceType = source.GetType();

        PropertyInfo[] sourceProperties = sourceType.GetProperties();
        PropertyInfo[] targetProperties = type.GetProperties();

        foreach (PropertyInfo sourceProperty in sourceProperties)
        {
            PropertyInfo targetProperty = targetProperties.FirstOrDefault(p => p.Name == sourceProperty.Name)!;
            if (targetProperty != null && targetProperty.CanWrite)
            {
                targetProperty.SetValue(target, sourceProperty.GetValue(source));
            }
        }

        FieldInfo[] sourceFields = sourceType.GetFields();
        FieldInfo[] targetFields = type.GetFields();

        foreach (FieldInfo sourceField in sourceFields)
        {
            FieldInfo targetField = targetFields.FirstOrDefault(f => f.Name == sourceField.Name)!;

            if (targetField != null)
            {
                // If field types are from the same assembly (not reloaded) otherwise we need to recreate an instance
                // to match the types and avoid ArgumentException
                if (targetField.FieldType.Assembly == sourceField.FieldType.Assembly || targetField.FieldType.IsPrimitive || targetField.FieldType.IsEnum)
                {
                    targetField.SetValue(target, sourceField.GetValue(source));
                }
                else
                {
                    object newValue = CopyData(targetField.FieldType, sourceField?.GetValue(source)!);
                    targetField.SetValue(target, newValue);
                }
            }
        }

        return target;
    }

    private static void SetComponentTypes(Archetype archetype, Dictionary<Type, Type> oldNewTypeDictionary)
    {
        var componentTypes = archetype.Types;

        for (int i = 0; i < componentTypes?.Length; i++)
        {
            ComponentType copy = componentTypes[i];

            if (oldNewTypeDictionary.TryGetValue(copy.Type, out var newType))
            {
                componentTypes[i] = new ComponentType(copy.Id, newType, copy.ByteSize, copy.ZeroSized);
            }
            else
            {
                continue;
            }
        }
    }

    private static void SetChunkComponents(Archetype archetype, Dictionary<Type, Type> oldNewTypeDictionary)
    {
        var chunks = archetype.Chunks;

        // Replace entity components with the new types for the Chunks
        for (int i = 0; i < chunks?.Length; i++)
        {
            var chunkComponentsProperty = chunks[i].GetType().GetProperty("Components");

            if (chunkComponentsProperty == null)
            {
                Console.Error.WriteLine("Error getting Types from Chunk");
                return;
            }

            var components = chunkComponentsProperty.GetValue(chunks[i])! as Array[];

            for (int j = 0; j < components?.Length; j++)
            {
                Type oldComponentType = components[j]?.GetType().GetElementType()!;

                if (oldNewTypeDictionary.TryGetValue(oldComponentType, out var newType))
                {
                    // Recreate the chunk components and perform deep copy via reflection
                    Array newArray = Array.CreateInstance(newType, chunks[i].Capacity);
                    for (int k = 0; k < components[j].Length; k++)
                    {
                        newArray.SetValue(CopyData(newType, components[j]?.GetValue(k)!), k);
                    }

                    components[j] = newArray;
                }
                else
                {
                    continue;
                }
            }
        }
    }
}