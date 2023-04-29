using System.Numerics;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using ImGuiNET;

namespace Equilibrium.Systems.UI.Tools;

public partial class EntityInspectorSystem : GuiSystem
{
    static Entity selectedEntity;

    public EntityInspectorSystem(World world) : base(world) { }

    void DrawEntityButton(in Entity entity)
    {
        ImGui.PushID(entity.Id);

        if (entity.IsAlive())
        {

            string label = "";

            if (entity.Has<Name>())
            {
                label = entity.Get<Name>().Value;
            }
            else
            {
                label = $"ID: {entity.Id}";
            }

            if (ImGui.Button(label))
            {
                selectedEntity = entity;
            }

        }
        else
        {
            ImGui.Text("Invalid Entity");
        }

        ImGui.PopID();
    }

    void DrawSystemButton(in Entity entity)
    {
        ImGui.PushID(entity.Id);

        if (entity.IsAlive())
        {
            bool enabled = true;

            if (ImGui.Checkbox("", ref enabled))
            {
                // if (!enabled)
                // registry.emplace<Disabled>(entity);
                // else
                // registry.remove<Disabled>(entity);
            }

            ImGui.SameLine();

            string label = "";

            if (entity.Has<Name>())
            {
                label = entity.Get<Name>().Value;
            }
            else
            {
                label = $"ID: {entity.Id}";
            }

            if (ImGui.Button(label))
            {
                selectedEntity = entity;
            }

        }
        else
        {
            ImGui.Text("Invalid Entity");
        }

        ImGui.PopID();
    }

    void DrawComponent(ref object obj)
    {
        var type = obj.GetType();
        FieldInfo[] fields = type.GetFields();

        foreach (FieldInfo field in fields)
        {
            ImGui.PushID(field.Name);

            object? value = field.GetValue(obj);

            if (value == null)
                continue;

            Type fieldType = value.GetType();

            if (fieldType.IsEnum)
            {
                Array enumValues = Enum.GetValues(fieldType);
                string[] enumNames = Enum.GetNames(fieldType);

                int selectedIndex = Array.IndexOf(enumNames, value.ToString());

                if (ImGui.Combo(field.Name, ref selectedIndex, enumNames, enumNames.Length))
                {
                    object? selectedValue = enumValues.GetValue(selectedIndex);
                    field.SetValue(obj, selectedValue);
                }
            }
            else if (fieldType.IsPrimitive || fieldType == typeof(string))
            {
                switch (value)
                {
                    case nint i:
                        var pointer = i.ToString();
                        ImGui.InputText(field.Name, ref pointer, 64);
                        break;
                    case int i:
                        ImGui.InputInt(field.Name, ref i);
                        field.SetValue(obj, i);
                        break;
                    case float f:
                        ImGui.InputFloat(field.Name, ref f);
                        field.SetValue(obj, f);
                        break;
                    case double d:
                        ImGui.InputDouble(field.Name, ref d);
                        field.SetValue(obj, d);
                        break;
                    case bool b:
                        ImGui.Checkbox(field.Name, ref b);
                        field.SetValue(obj, b);
                        break;
                    case string s:
                        ImGui.InputText(field.Name, ref s, 512);
                        field.SetValue(obj, s);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                ImGui.BeginGroup();
                ImGui.Text(field.Name);
                DrawComponent(ref value);
                field.SetValue(obj, value);
                ImGui.EndGroup();
            }

            ImGui.PopID();
        }
    }

    void RenderEditor()
    {
        ImGui.TextUnformatted("Editing:");
        ImGui.SameLine();

        ImGui.PushID(selectedEntity.Id);

        if (selectedEntity.IsAlive())
        {
            if (selectedEntity.Has<Name>())
            {
                ImGui.Text($"{selectedEntity.Get<Name>().Value} | ID: {selectedEntity.Id}");
            }
            else
            {
                ImGui.Text($"ID: {selectedEntity.Id}");
            }

        }
        else
        {
            ImGui.Text("Please select entity");
        }

        ImGui.PopID();

        if (ImGui.Button("New"))
        {
            selectedEntity = World.Create<Name>(new Name { Value = "Entity" });
        }

        if (selectedEntity.IsAlive())
        {
            ImGui.SameLine();

            // clone would go here
            // if (ImGui.Button("Clone")) {
            // auto old_e = e;
            // e = registry.create();
            //}

            ImGui.Dummy(new Vector2(10, 0)); // space destroy a bit, to not accidentally click it
            ImGui.SameLine();

            // red button
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.15f, 0.15f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered,
                                  new Vector4(0.8f, 0.3f, 0.3f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 0.2f, 0.2f, 1));
            if (ImGui.Button("Destroy"))
            {
                World.Destroy(selectedEntity);
                selectedEntity = Entity.Null;
            }
            ImGui.PopStyleColor(3);
        }

        ImGui.Separator();

        if (selectedEntity.IsAlive())
        {
            ImGui.PushID(selectedEntity.Id);

            var components = World.GetAllComponents(selectedEntity);

            // std::map<EntityEditor<entt::entity>::ComponentTypeID,
            //          EntityEditor<entt::entity>::ComponentInfo>
            //     has_not;

            foreach (var component in components)
            {
                ComponentRegistry.TryGet(component.GetType(), out var componentType);

                if (selectedEntity.Has(componentType))
                {
                    ImGui.PushID(componentType.Id);
                    // if (ImGui.Button("-"))
                    // {
                    //     // selectedEntity.Remove(componentType);
                    //     ImGui.PopID();
                    //     continue; // early out to prevent access to deleted data
                    // }
                    // else
                    // {
                    //     ImGui.SameLine();
                    // }

                    if (ImGui.CollapsingHeader(componentType.Type.Name))
                    {
                        ImGui.Indent(30);
                        ImGui.PushID("Widget");
                        var componentRef = selectedEntity.Get(componentType);
                        DrawComponent(ref componentRef);
                        selectedEntity.Set(componentRef);
                        ImGui.PopID();
                        ImGui.Unindent(30);
                    }
                    ImGui.PopID();
                }
                else
                {
                    // has_not[component_type_id] = ci;
                }
            }

            // if (!has_not.empty())
            // {
            //     if (ImGui.Button("+ Add Component"))
            //     {
            //         ImGui.OpenPopup("Add Component");
            //     }

            //     if (ImGui.BeginPopup("Add Component"))
            //     {
            //         ImGui.TextUnformatted("Available:");
            //         ImGui.Separator();

            //         for (auto &[component_type_id, ci] : has_not)
            //         {
            //             ImGui.PushID(component_type_id);
            //             if (ImGui.Selectable(ci.name.c_str()))
            //             {
            //                 ci.create(registry, e);
            //             }
            //             ImGui.PopID();
            //         }
            //         ImGui.EndPopup();
            //     }
            // }
            ImGui.PopID();
        }
    }

    void RenderEntityList()
    {
        ImGui.Text("Components Filter:");
        ImGui.SameLine();
        if (ImGui.SmallButton("clear"))
        {
            // inspector.ComponentSet.clear();
        }

        ImGui.Indent();

        //         for (const auto &[component_type_id, ci] :
        //    inspector.Value.GetComponentMap()) {
        //             bool is_in_list = inspector.ComponentSet.count(component_type_id);
        //             bool active = is_in_list;

        //             ImGui.Checkbox(ci.name.c_str(), &active);

        //             if (is_in_list && !active)
        //             { // remove
        //                 inspector.ComponentSet.erase(component_type_id);
        //             }
        //             else if (!is_in_list && active)
        //             { // add
        //                 inspector.ComponentSet.emplace(component_type_id);
        //             }
        //         }

        ImGui.Unindent();
        ImGui.Separator();

        ImGui.Text("Systems:");
        // registry.each([&registry, &inspector](auto e) {
        //     if (registry.any_of<System, ManualSystem>(e))
        //         DrawSystemButton(registry, inspector, e);
        // });

        ImGui.Separator();

        // if (inspector.ComponentSet.empty())
        // {
        ImGui.Text($"Entities: {World.Size}");
        // registry.each([&registry, &inspector](auto e) {
        //     if (!registry.any_of<System, ManualSystem>(e))
        //         DrawEntityButton(registry, inspector, e);
        // });

        QueryDescription query = new QueryDescription();
        List<Entity> entities = new List<Entity>();
        World.GetEntities(in query, entities);

        entities.ForEach(x => DrawEntityButton(in x));
        // }
        // else
        // {
        //     auto view = registry.runtime_view(inspector.ComponentSet.begin(),
        //                                       inspector.ComponentSet.end());

        //     int counter = 0;
        //     for (auto e : view)
        //     {
        //         counter++;
        //     }

        //     ImGui.Text($"{counter} Entities Matching:");

        //     if (ImGui.BeginChild("entity list"))
        //     {
        //         for (auto e : view)
        //         {
        //             DrawEntityButton(registry, inspector, e);
        //         }
        //     }
        //     ImGui.EndChild();
        // }
    }


    protected override void Render(float deltaTime)
    {

        ImGui.SetNextWindowSize(new Vector2(550, 400), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Entity Inspector"))
        {
            if (ImGui.BeginChild("list", new Vector2(300, 0), true))
            {
                RenderEntityList();

                ImGui.EndChild();
                ImGui.SameLine();

                if (ImGui.BeginChild("editor"))
                {
                    RenderEditor();
                }
                ImGui.EndChild();
            }
            ImGui.End();
        }
    }
}