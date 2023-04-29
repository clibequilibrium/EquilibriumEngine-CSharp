using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Assimp;
using Assimp.Configs;
using Assimp.Unmanaged;
using Bgfx;
using Equilibrium.Components;
using static Bgfx.bgfx;
using Material = Equilibrium.Components.Material;
using Mesh = Equilibrium.Components.Mesh;

public static class AssimpUtils
{
    struct PosNormalTangentTexcoordVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector2 Uv;
    }

    static Material[] materials = new Material[128];

    public static bool LoadScene(this World world, string name)
    {
        string filePath = string.Empty;
        string modelPath = @$"{AppContext.BaseDirectory}/content/models";
        name = $"{name}.gltf";

        filePath = Path.Combine(modelPath, name);

        var store = AssimpLibrary.Instance.CreatePropertyStore();

        // Settings for aiProcess_SortByPType
        // only take triangles or higher (polygons are triangulated during import)
        AssimpLibrary.Instance.SetImportPropertyInteger(store, AiConfigs.AI_CONFIG_PP_SBP_REMOVE, (int)(PrimitiveType.Line | PrimitiveType.Point));

        // Settings for aiProcess_SplitLargeMeshes
        // Limit vertices to 65k (we use 16-bit indices)
        AssimpLibrary.Instance.SetImportPropertyInteger(store, AiConfigs.AI_CONFIG_PP_SLM_VERTEX_LIMIT, ushort.MaxValue);

        var flags = PostProcessPreset.TargetRealTimeQuality | // some optimizations and
                                                              // safety checks
                                PostProcessSteps.OptimizeMeshes |               // minimize number of meshes
                                PostProcessSteps.PreTransformVertices |         // apply node matrices
                                PostProcessSteps.FixInFacingNormals |
                                PostProcessSteps.TransformUVCoords | // apply UV transformations
                                                                     // aiProcess_FlipWindingOrder   | // we cull clock-wise, keep the
                                                                     // default CCW winding order
                                 PostProcessSteps.MakeLeftHanded | // we set GLM_FORCE_LEFT_HANDED and use
                                                                   // left-handed bx matrix functions
                                 PostProcessSteps.FlipUVs; // bimg loads textures with flipped Y (top left is
                                                           // 0,0)


        var scenePtr = AssimpLibrary.Instance.ImportFile(filePath, flags, store);
        var scene = Scene.FromUnmanagedScene(scenePtr);
        AssimpLibrary.Instance.ReleasePropertyStore(store);

        // If the import failed, report it
        if (scene == default)
        {
            Console.Error.WriteLine(AssimpLibrary.Instance.GetErrorString());
            return false;
        }

        // Now we can access the file's contents
        if (!scene.SceneFlags.HasFlag(SceneFlags.Incomplete))
        {
            for (int i = 0; i < scene.MaterialCount; i++)
            {
                materials[i] = LoadMaterial(world, scene.Materials[i], modelPath);
            }

            for (int i = 0; i < scene.MeshCount; i++)
            {
                int material_index = 0;
                Mesh mesh = default;

                try
                {
                    mesh = LoadMesh(world, scene.Meshes[i], ref material_index);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                var meshEntity = world.Create(mesh);
                meshEntity.Add(new Position { Value = Vector3.Zero });
                meshEntity.Add(new Rotation { Value = System.Numerics.Quaternion.Identity });
                meshEntity.Add(new Scale { Value = Vector3.One });

                Material material = materials[material_index];
                meshEntity.Add(material);
            }
        }
        else
        {
            Console.Error.WriteLine("Scene is incomplete or invalid");
            return false;
        }

        // We're done. Release all resources associated with this import
        AssimpLibrary.Instance.ReleaseImport(scenePtr);

        return true;
    }

    private unsafe static Mesh LoadMesh(World world, Assimp.Mesh mesh, ref int material_index)
    {
        Mesh result;

        if (mesh.PrimitiveType != PrimitiveType.Triangle)
            Console.Error.WriteLine("Mesh has incompatible primitive type");

        if (mesh.VertexCount > (ushort.MaxValue + 1u))
            Console.Error.WriteLine("Mesh has too many vertices %d", ushort.MaxValue + 1);

        int coords = 0;
        bool hasTexture = mesh.UVComponentCount[coords] == 2 && mesh.TextureCoordinateChannels[coords] != null;

        // vertices
        VertexLayout pcvDecl = default;
        pcvDecl = pcvDecl.Begin().Add(Attrib.Position, 3, AttribType.Float).Add(Attrib.Normal, 3, AttribType.Float)
                                 .Add(Attrib.Tangent, 3, AttribType.Float).Add(Attrib.TexCoord0, 2, AttribType.Float).End();

        ushort stride = pcvDecl.stride;

        Memory* vertexMem = bgfx.alloc((uint)(mesh.VertexCount * stride));

        for (int i = 0; i < mesh.VertexCount; i++)
        {
            PosNormalTangentTexcoordVertex* vertex =
                (PosNormalTangentTexcoordVertex*)(vertexMem->data + (i * stride));

            var pos = mesh.Vertices[i];
            vertex->Position = new Vector3(pos.X, pos.Y, pos.Z);

            // minBounds = glm::min(minBounds, {pos.x, pos.y, pos.z});
            // maxBounds = glm::max(maxBounds, {pos.x, pos.y, pos.z});

            var normal = mesh.Normals[i];
            vertex->Normal = new Vector3(normal.X, normal.Y, normal.Z);

            if (mesh.Tangents != null && mesh.Tangents.Count > 0)
            {
                var tangent = mesh.Tangents[i];
                vertex->Tangent = new Vector3(tangent.X, tangent.Y, tangent.Z);
            }
            else
            {
                vertex->Tangent = Vector3.Zero;
            }

            // ecs_trace("Vertex: %f, %f, %f| Normal: %f, %f, %f, | "
            //           "Tangent: %f, %f, %f",
            //           vertex->position[0], vertex->position[1],
            //           vertex->position[2], vertex->normal[0], vertex->normal[1],
            //           vertex->normal[2], vertex->tangent[0], vertex->tangent[1],
            //           vertex->tangent[2]);

            if (hasTexture)
            {
                var uv = mesh.TextureCoordinateChannels[coords][i];
                vertex->Uv = new Vector2(uv.X, uv.Y);
            }
        }

        result.VertexBuffer = BgfxUtils.CreateVertexBuffer(world, vertexMem, pcvDecl);

        Memory* iMem = bgfx.alloc((uint)(mesh.FaceCount * 3 * sizeof(ushort)));
        ushort* indices = (ushort*)iMem->data;

        for (int i = 0; i < mesh.FaceCount; i++)
        {
            Debug.Assert(mesh.Faces[i].IndexCount == 3);

            indices[(3 * i) + 0] = (ushort)mesh.Faces[i].Indices[0];
            indices[(3 * i) + 1] = (ushort)mesh.Faces[i].Indices[1];
            indices[(3 * i) + 2] = (ushort)mesh.Faces[i].Indices[2];
        }

        result.IndexBuffer = BgfxUtils.CreateIndexBuffer(world, iMem);
        material_index = mesh.MaterialIndex;

        return result;
    }

    private static Equilibrium.Components.Material LoadMaterial(World world, Assimp.Material material, string dir)
    {
        Material result = new Material();
        TextureHandle invalid = new TextureHandle { idx = BgfxConstants.InvalidHandle };

        result.BaseColorTexture = invalid;
        result.NormalTexture = invalid;
        result.EmissiveTexture = invalid;
        result.OcclusionTexture = invalid;
        result.MetallicRoughnessTexture = invalid;

        result.MetallicFactor = 0.0f;
        result.RoughnessFactor = 1.0f;
        result.NormalScale = 1.0f;
        result.OcclusionStrength = 1.0f;

        // technically there is a difference between MASK and BLEND mode
        // but for our purposes it's enough if we sort properly

        var alphaProperty = material.GetProperty("$mat.gltf.alphaMode,0,0");

        if (alphaProperty != null)
        {
            result.Blend = alphaProperty.GetStringValue().Equals("OPAQUE") ? false : true;
        }

        var twoSidedProperty = material.GetProperty("$mat.twosided,0,0");

        if (twoSidedProperty != null)
        {
            result.DoubleSided = BitConverter.ToBoolean(twoSidedProperty.RawData);
        }

        // texture files

        TextureSlot fileBaseColor, fileMetallicRoughness, fileNormals, fileOcclusion, fileEmissive;

        material.GetMaterialTexture(TextureType.Lightmap, 0, out fileOcclusion);

        // diffuse

        if (material.GetMaterialTexture(TextureType.Diffuse, 0, out fileBaseColor))
        {
            result.BaseColorTexture = LoadTexture(world, Path.Combine(dir, fileBaseColor.FilePath));
        }

        result.BaseColorFactor = new(material.ColorDiffuse.R, material.ColorDiffuse.G, material.ColorDiffuse.B, material.ColorDiffuse.A);
        result.BaseColorFactor = Vector4.Clamp(result.BaseColorFactor, Vector4.Zero, Vector4.One);

        // metallic/roughness

        if (material.GetMaterialTexture(TextureType.Unknown, 0, out fileMetallicRoughness))
        {
            result.MetallicRoughnessTexture = LoadTexture(world, Path.Combine(dir, fileMetallicRoughness.FilePath));
        }

        var metallicFactor = material.GetProperty("$mat.gltf.pbrMetallicRoughness.metallicFactor,0,0");
        var roughnessFactor = material.GetProperty("$mat.gltf.pbrMetallicRoughness.roughnessFactor,0,0");

        if (metallicFactor != null)
        {
            result.MetallicFactor = Math.Clamp(metallicFactor.GetFloatValue(), 0.0f, 1.0f);
        }

        if (roughnessFactor != null)
        {
            result.RoughnessFactor = Math.Clamp(roughnessFactor.GetFloatValue(), 0.0f, 1.0f);
        }

        if (material.GetMaterialTexture(TextureType.Normals, 0, out fileNormals))
        {
            result.NormalTexture = LoadTexture(world, Path.Combine(dir, fileNormals.FilePath));
        }

        result.NormalScale = 1;

        // occlusion texture

        if (fileOcclusion.FilePath == fileMetallicRoughness.FilePath)
        {
            // some GLTF files combine metallic/roughness and occlusion values into
            // one texture don't load it twice
            result.OcclusionTexture = result.MetallicRoughnessTexture;
        }
        else if (fileOcclusion.FilePath != null)
        {
            result.OcclusionTexture = LoadTexture(world, Path.Combine(dir, fileOcclusion.FilePath));
        }


        result.OcclusionStrength = 1;
        // ai_real occlusionStrength;
        // if (AI_SUCCESS == aiGetMaterialFloatArray(
        //                       material, AI_MATKEY_GLTF_TEXTURE_STRENGTH(aiTextureType_LIGHTMAP, 0),
        //                       &occlusionStrength, NULL))
        // {
        // out.occlusion_strength = glm_clamp(occlusionStrength, 0.0f, 1.0f);
        // }


        // emissive texture

        if (material.GetMaterialTexture(TextureType.Emissive, 0, out fileEmissive))
        {
            result.EmissiveTexture = LoadTexture(world, Path.Combine(dir, fileEmissive.FilePath));

            // // assimp doesn't define this
            // # ifndef AI_MATKEY_GLTF_EMISSIVE_FACTOR
            // #define AI_MATKEY_GLTF_EMISSIVE_FACTOR AI_MATKEY_COLOR_EMISSIVE
            // #endif

            // struct aiColor4D emissive_color;

            // if (AI_SUCCESS ==
            //     aiGetMaterialColor(material, AI_MATKEY_GLTF_EMISSIVE_FACTOR, &emissive_color))
            // {
            //         out.emissive_factor[0] = emissive_color.r;
            //         out.emissive_factor[1] = emissive_color.g;
            //         out.emissive_factor[2] = emissive_color.b;
            // }

            // glm_vec3_clamp(out.emissive_factor, 0.0f, 1.0f);

        }

        return result;
    }

    private unsafe static TextureHandle LoadTexture(World world, string path)
    {
        TextureHandle result = new TextureHandle { idx = BgfxConstants.InvalidHandle };

        if (File.Exists(path))
        {
            ulong textureFlags =
                      (ulong)bgfx.TextureFlags.None | (ulong)SamplerFlags.MinAnisotropic | (ulong)SamplerFlags.MagAnisotropic;


            fixed (byte* pBytes = File.ReadAllBytes(path))
            {
                result = world.CreateTexture(bgfx.copy(pBytes, (uint)new FileInfo(path).Length), textureFlags);
            }
        }

        return result;
    }
}