using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FightNight.EditorTools
{
    /// <summary>
    /// One-click setup for the Mestre Alien fighter in UFE's demos, using Robot_Kyle as the template:
    ///   1. Model: Humanoid rig, no animation, textures and materials extracted, sized like Kyle.
    ///   2. Prefab: the model + Kyle's Animator settings + Kyle's hit boxes moved onto our bones.
    ///   3. Character file: a copy of Kyle's, renamed and pointed at the new prefab and avatar.
    ///   4. Details: a description, plus portraits rendered from the model in Kyle's portrait style.
    ///   5. Roster: added to the demo's character list and made selectable.
    /// Each demo gets its own prefab and character file, built from that demo's version of Kyle
    /// (its hit boxes and move set differ). The model and portraits are shared.
    /// Safe to run again: finished parts are reused, and portraits or a description you've
    /// replaced yourself are left alone.
    /// </summary>
    public static class MestreAlienSetup
    {
        /// What differs between the demos.
        class Target
        {
            public string Label;
            public string KylePrefabPath;     // this demo's Robot_Kyle prefab (template)
            public string KyleCharacterPath;  // this demo's Robot_Kyle character file (template)
            public string PrefabPath;
            public string PrefabResourcePath; // PrefabPath relative to the Resources folder, UFE style
            public string CharacterFilePath;
            public string[] ConfigPaths;      // the demo configs whose roster he joins
            public string ScenePath;          // where to try him out
        }

        const string DisplayName = "Mestre Alien";
        const string CharacterFolder = "Assets/FightNight/Characters/MestreAlien";
        const string ModelPath = CharacterFolder + "/Model/mestrealien.fbx";
        const string TexturesFolder = CharacterFolder + "/Model/Textures";
        const string MaterialsFolder = CharacterFolder + "/Model/Materials";
        const string PrefabFolder = "Assets/FightNight/Resources/Characters";

        static readonly Target Fighter2D = new Target
        {
            Label = "2D Fighter",
            KylePrefabPath = "Assets/UFE/Demos/Resources/_2DFighter/Characters/Robot_Kyle.prefab",
            KyleCharacterPath = "Assets/UFE/Demos/_2DFighter/Characters/Robot_Kyle/Robot_Kyle.asset",
            PrefabPath = PrefabFolder + "/MestreAlien.prefab",
            PrefabResourcePath = @"Characters\MestreAlien",
            CharacterFilePath = CharacterFolder + "/MestreAlien.asset",
            ConfigPaths = new[] { "Assets/UFE/Demos/_2DFighter/Config_2D_Fighter.asset" },
            ScenePath = "Assets/UFE/Demos/_2DFighter/Demo 2D Fighter.unity",
        };

        static readonly Target Fighter3D = new Target
        {
            Label = "3D Fighter",
            KylePrefabPath = "Assets/UFE/Demos/Resources/_3DFighter/Characters/Robot_Kyle.prefab",
            KyleCharacterPath = "Assets/UFE/Demos/_3DFighter/Characters/Robot_Kyle/Robot_Kyle3D.asset",
            PrefabPath = PrefabFolder + "/MestreAlien3D.prefab",
            PrefabResourcePath = @"Characters\MestreAlien3D",
            CharacterFilePath = CharacterFolder + "/MestreAlien3D.asset",
            ConfigPaths = new[]
            {
                "Assets/UFE/Demos/_3DFighter/Config_3D_Fighter_T1.asset",
                "Assets/UFE/Demos/_3DFighter/Config_3D_Fighter_T2.asset",
            },
            ScenePath = "Assets/UFE/Demos/_3DFighter/Demo 3D Fighter (Type 1).unity",
        };

        const string HitBoxesScriptName = "HitBoxesScript";
        const int SelectScreenSlots = 8; // Button_Character_0..7 on the demos' character select screens

        const string Description = "Project: FightNight\n\nModel Type: 3D Model\n\nAnimation Type: Mecanim";
        const string PortraitSmallPath = CharacterFolder + "/UI/PortraitSmall.png";
        const string PortraitBigPath = CharacterFolder + "/UI/PortraitBig.png";
        static readonly Vector2Int PortraitSmallSize = new Vector2Int(100, 122); // same sizes as Kyle's
        static readonly Vector2Int PortraitBigSize = new Vector2Int(307, 496);
        const int Supersample = 3; // render bigger, then shrink, for smooth edges

        const string Log = "[FightNight] ";

        [MenuItem("Tools/FightNight/Set Up Mestre Alien (2D Fighter)")]
        static void Run2D() => Run(Fighter2D);

        [MenuItem("Tools/FightNight/Set Up Mestre Alien (3D Fighter)")]
        static void Run3D() => Run(Fighter3D);

        static void Run(Target target)
        {
            try
            {
                Progress("Setting up the model…", 0.1f);
                Avatar avatar = SetUpModel(target);

                Progress("Building the prefab…", 0.6f);
                BuildPrefab(avatar, target);

                Progress("Creating the character file…", 0.8f);
                Object character = CreateCharacterFile(avatar, target);

                Progress("Making portraits…", 0.9f);
                FillInDetails(character, target);

                Progress("Adding him to the roster…", 0.97f);
                AddToRoster(character, target);

                AssetDatabase.SaveAssets();
                Selection.activeObject = character;
                EditorGUIUtility.PingObject(character);

                Debug.Log(Log + $"Mestre Alien is set up for the {target.Label} demo.\n" +
                          $"Prefab: {target.PrefabPath}\nCharacter file: {target.CharacterFilePath}");
                EditorUtility.DisplayDialog($"{DisplayName} ({target.Label})",
                    "Done!\n\n" +
                    $"• Prefab: {target.PrefabPath}\n" +
                    $"• Character file: {target.CharacterFilePath}\n" +
                    $"• Portraits: {CharacterFolder}/UI/\n" +
                    $"• Added to the {target.Label} roster\n\n" +
                    $"Next: open {target.ScenePath}, press Play and pick Mestre Alien.",
                    "OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Mestre Alien setup failed",
                    e.Message + "\n\nSee the Console for details.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ---------------------------------------------------------------- Step 2: model

        static Avatar SetUpModel(Target target)
        {
            ModelImporter importer = GetModelImporter();

            // Humanoid rig so Kyle's (humanoid) animations play on this model.
            // The animation in the Mixamo file isn't needed.
            if (importer.animationType != ModelImporterAnimationType.Human ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                importer.importAnimation)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
                importer.SaveAndReimport();
            }

            MatchKylesSize(target.KylePrefabPath);
            ExtractTextures();
            ExtractMaterials();

            Avatar avatar = LoadAvatar();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new Exception("Unity couldn't create a humanoid avatar for the model. " +
                                    "Select mestrealien.fbx, open Rig → Configure… and check the bone mapping.");
            return avatar;
        }

        static void MatchKylesSize(string kylePrefabPath)
        {
            GameObject kyle = PrefabUtility.LoadPrefabContents(kylePrefabPath);
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                float kyleHeight = BodyHeight(kyle, kyle.GetComponent<Animator>().avatar);
                var ours = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(), scene);
                float ourHeight = BodyHeight(ours, LoadAvatar());
                if (ourHeight <= 0.0001f)
                    throw new Exception("Couldn't measure the model's height.");

                float ratio = kyleHeight / ourHeight;
                if (Mathf.Abs(ratio - 1f) < 0.1f) return; // already close enough

                ModelImporter importer = GetModelImporter();
                float newScale = importer.globalScale * ratio;
                Debug.Log(Log + $"Model was {ratio:0.##}x off from Robot_Kyle's size. " +
                          $"Scale Factor {importer.globalScale:0.####} → {newScale:0.####}.");
                importer.globalScale = newScale;
                importer.SaveAndReimport();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(kyle);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// Ankle-to-head height, a fair way to compare two humanoids.
        static float BodyHeight(GameObject root, Avatar avatar)
        {
            Dictionary<string, string> humanToBone = HumanToBone(avatar);
            Transform head = FindDeep(root.transform, humanToBone["Head"]);
            Transform foot = FindDeep(root.transform, humanToBone["LeftFoot"]);
            if (head == null || foot == null)
                throw new Exception($"Couldn't find the head or foot bone on {root.name}.");
            return head.position.y - foot.position.y;
        }

        static void ExtractTextures()
        {
            EnsureFolder(TexturesFolder);
            bool changed = false;
            if (!HasAssets(TexturesFolder, "t:Texture2D"))
            {
                AssetDatabase.StartAssetEditing();
                try { GetModelImporter().ExtractTextures(TexturesFolder); }
                finally { AssetDatabase.StopAssetEditing(); }
                AssetDatabase.Refresh();
                changed = true;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool isNormalMap = Path.GetFileNameWithoutExtension(path)
                    .IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isNormalMap && AssetImporter.GetAtPath(path) is TextureImporter texture &&
                    texture.textureType != TextureImporterType.NormalMap)
                {
                    texture.textureType = TextureImporterType.NormalMap;
                    texture.SaveAndReimport();
                    changed = true;
                }
            }

            // Reimport so the model's materials pick up the extracted textures.
            if (changed) AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        }

        static void ExtractMaterials()
        {
            List<Material> embedded = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().ToList();
            if (embedded.Count == 0) return; // already extracted

            EnsureFolder(MaterialsFolder);
            var usedPaths = new HashSet<string>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (Material material in embedded)
                {
                    string baseName = SafeFileName(material.name);
                    string destination = $"{MaterialsFolder}/{baseName}.mat";
                    for (int i = 1; usedPaths.Contains(destination) || File.Exists(destination); i++)
                        destination = $"{MaterialsFolder}/{baseName} {i}.mat";
                    usedPaths.Add(destination);

                    string error = AssetDatabase.ExtractAsset(material, destination);
                    if (!string.IsNullOrEmpty(error))
                        Debug.LogWarning(Log + $"Couldn't extract material '{material.name}': {error}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.WriteImportSettingsIfDirty(ModelPath);
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        }

        // ---------------------------------------------------------------- Step 3: prefab

        static void BuildPrefab(Avatar avatar, Target target)
        {
            GameObject kyle = PrefabUtility.LoadPrefabContents(target.KylePrefabPath);
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                Animator kyleAnimator = kyle.GetComponent<Animator>();
                if (kyleAnimator == null || kyleAnimator.avatar == null)
                    throw new Exception("Robot_Kyle's prefab has no Animator with an avatar.");
                MonoBehaviour kyleHitBoxes = kyle.GetComponents<MonoBehaviour>()
                    .FirstOrDefault(c => c != null && c.GetType().Name == HitBoxesScriptName);
                if (kyleHitBoxes == null)
                    throw new Exception("Couldn't find UFE's Hit Boxes Script on Robot_Kyle's prefab.");

                var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(), scene);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                go.name = Path.GetFileNameWithoutExtension(target.PrefabPath);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                // Animator: same settings as Kyle's (UFE's controller etc.), with our avatar.
                Animator animator = go.GetComponent<Animator>();
                if (animator == null) animator = go.AddComponent<Animator>();
                EditorUtility.CopySerialized(kyleAnimator, animator);
                animator.avatar = avatar;

                // Hit boxes: copy Kyle's, then move each one onto the matching bone of our model.
                Component hitBoxes = go.AddComponent(kyleHitBoxes.GetType());
                EditorUtility.CopySerialized(kyleHitBoxes, hitBoxes);
                var bones = new BoneMapper(kyle.transform, kyleAnimator.avatar, go.transform, avatar);
                int moved = RetargetReferences(hitBoxes, bones);
                Debug.Log(Log + $"Moved {moved} hit box links from Robot_Kyle's bones to Mestre Alien's.");

                EnsureFolder(Path.GetDirectoryName(target.PrefabPath)?.Replace('\\', '/'));
                PrefabUtility.SaveAsPrefabAsset(go, target.PrefabPath, out bool saved);
                if (!saved) throw new Exception("Couldn't save the prefab at " + target.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(kyle);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// Points every reference to Kyle's skeleton at the matching bone of our model.
        static int RetargetReferences(Component component, BoneMapper bones)
        {
            var so = new SerializedObject(component);
            SerializedProperty it = so.GetIterator();
            int count = 0;
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                Object value = it.objectReferenceValue;
                Transform from = value switch
                {
                    Transform t => t,
                    GameObject g => g.transform,
                    Component c => c.transform,
                    _ => null,
                };
                if (from == null || !bones.IsFromSource(from)) continue;

                Transform to = bones.Map(from);
                it.objectReferenceValue = value switch
                {
                    Transform _ => to,
                    GameObject _ => to.gameObject,
                    Component c => to.GetComponent(c.GetType()),
                    _ => null,
                };
                Debug.Log(Log + $"Hit box: {from.name} → {to.name}");
                count++;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return count;
        }

        /// Maps bones between two humanoid skeletons through their avatars.
        class BoneMapper
        {
            readonly Transform fromRoot, toRoot, fromHips, toHips;
            readonly Dictionary<string, string> fromBoneToHuman;
            readonly Dictionary<string, string> toHumanToBone;

            public BoneMapper(Transform fromRoot, Avatar fromAvatar, Transform toRoot, Avatar toAvatar)
            {
                this.fromRoot = fromRoot;
                this.toRoot = toRoot;
                fromBoneToHuman = HumanToBone(fromAvatar).ToDictionary(kv => kv.Value, kv => kv.Key);
                toHumanToBone = HumanToBone(toAvatar);
                fromHips = FindDeep(fromRoot, HumanToBone(fromAvatar)["Hips"]);
                toHips = FindDeep(toRoot, toHumanToBone["Hips"]);
            }

            public bool IsFromSource(Transform t) => t.IsChildOf(fromRoot);

            public Transform Map(Transform from)
            {
                if (from == fromRoot) return toRoot;

                // A humanoid bone, or the nearest humanoid bone above it.
                for (Transform t = from; t != null && t != fromRoot; t = t.parent)
                {
                    if (fromBoneToHuman.TryGetValue(t.name, out string human) &&
                        toHumanToBone.TryGetValue(human, out string bone))
                    {
                        Transform found = FindDeep(toRoot, bone);
                        if (found != null) return found;
                    }
                }

                // A joint above the hips (like Kyle's "Root"): same number of steps above our hips.
                if (fromHips != null && toHips != null && fromHips.IsChildOf(from))
                {
                    Transform to = toHips;
                    for (Transform t = fromHips; t != from && to != toRoot; t = t.parent)
                        to = to.parent;
                    return to;
                }

                Debug.LogWarning(Log + $"No matching bone for '{from.name}', using the root instead.");
                return toRoot;
            }
        }

        // ---------------------------------------------------------------- Step 4: character file

        static Object CreateCharacterFile(Avatar avatar, Target target)
        {
            bool isNew = AssetDatabase.LoadMainAssetAtPath(target.CharacterFilePath) == null;
            if (isNew && !AssetDatabase.CopyAsset(target.KyleCharacterPath, target.CharacterFilePath))
                throw new Exception($"Couldn't copy {target.KyleCharacterPath}.");

            Object character = AssetDatabase.LoadMainAssetAtPath(target.CharacterFilePath);
            var so = new SerializedObject(character);

            Prop(so, "characterName").stringValue = DisplayName;
            // UFE keeps its own copy of the file's ID inside the file. A copy must not keep Kyle's.
            Prop(so, "guid").stringValue = AssetDatabase.AssetPathToGUID(target.CharacterFilePath);
            Prop(so, "prefabResourcePath").stringValue = target.PrefabResourcePath;
            Prop(so, "avatar").objectReferenceValue = avatar;

            // Kyle's animation maps are hit box positions recorded on Kyle's body. Let a new copy's
            // hit boxes follow Mestre Alien's own bones instead (you can switch it back on later).
            if (isNew)
            {
                SerializedProperty maps = Prop(so, "useAnimationMaps");
                if (maps.propertyType == SerializedPropertyType.Boolean) maps.boolValue = false;
                else maps.intValue = 0;
            }

            // Kyle's alternate colour costume pointed at Kyle's prefab; point it at ours.
            SerializedProperty costumes = Prop(so, "alternativeCostumes");
            for (int i = 0; i < costumes.arraySize; i++)
            {
                SerializedProperty path = costumes.GetArrayElementAtIndex(i).FindPropertyRelative("prefabResourcePath");
                if (path != null) path.stringValue = target.PrefabResourcePath;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(character);
            AssetDatabase.SaveAssets();
            return character;
        }

        // ---------------------------------------------------------------- Step 5: description and portraits

        static void FillInDetails(Object character, Target target)
        {
            var so = new SerializedObject(character);
            var kyle = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(target.KyleCharacterPath));

            // Only replace what is still Kyle's (or empty), so your own text and pictures survive a re-run.
            bool StillKyles(string field) =>
                Prop(so, field).objectReferenceValue == null ||
                Prop(so, field).objectReferenceValue == Prop(kyle, field).objectReferenceValue;

            SerializedProperty description = Prop(so, "characterDescription");
            if (description.stringValue == "" || description.stringValue == Prop(kyle, "characterDescription").stringValue)
                description.stringValue = Description;

            bool replaceSmall = StillKyles("profilePictureSmall");
            bool replaceBig = StillKyles("profilePictureBig");
            if (replaceSmall || replaceBig)
            {
                // The portraits are shared by every demo; only render the ones that don't exist yet.
                bool renderSmall = replaceSmall && AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitSmallPath) == null;
                bool renderBig = replaceBig && AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitBigPath) == null;
                if (renderSmall || renderBig)
                {
                    // Pose him like on the select screen, using the selection animation from the character file.
                    var pose = Prop(so, "selectionAnimation").objectReferenceValue as AnimationClip;
                    MakePortraits(pose, renderSmall, renderBig, target.PrefabPath);
                }
                if (replaceSmall)
                    Prop(so, "profilePictureSmall").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitSmallPath);
                if (replaceBig)
                    Prop(so, "profilePictureBig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitBigPath);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(character);
            AssetDatabase.SaveAssets();
        }

        static void MakePortraits(AnimationClip pose, bool small, bool big, string prefabPath)
        {
            var preview = new PreviewRenderUtility();
            PlayableGraph graph = default;
            try
            {
                GameObject go = preview.InstantiatePrefabInScene(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
                Animator animator = go.GetComponent<Animator>();

                if (pose != null && pose.humanMotion)
                {
                    graph = PlayableGraph.Create("Mestre Alien portrait pose");
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var clip = AnimationClipPlayable.Create(graph, pose);
                    clip.SetApplyFootIK(false);
                    clip.SetTime(pose.length * 0.25f);
                    AnimationPlayableOutput.Create(graph, "Pose", animator).SetSourcePlayable(clip);
                    graph.Evaluate();
                }

                // Measure the body from its bones.
                Dictionary<string, string> bones = HumanToBone(animator.avatar);
                Vector3 Bone(string human) => FindDeep(go.transform, bones[human]).position;
                Vector3 head = Bone("Head"), hips = Bone("Hips");
                float footY = Mathf.Min(Bone("LeftFoot").y, Bone("RightFoot").y);
                float size = head.y - footY;                       // ankle to base of skull
                float groundY = footY - 0.05f * size;
                float crownY = head.y + 0.14f * size;              // top of the head
                Vector3 facing = Vector3.Cross(Vector3.up, Bone("LeftUpperArm") - Bone("RightUpperArm"));
                facing = facing.sqrMagnitude > 0.0001f ? facing.normalized : go.transform.forward;

                // Three-quarter view from slightly above, like Kyle's portraits.
                Vector3 view = (Quaternion.AngleAxis(25f, Vector3.up) * facing + Vector3.up * 0.08f).normalized;

                preview.cameraFieldOfView = 25f;
                preview.camera.fieldOfView = 25f;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.ambientColor = new Color(0.32f, 0.32f, 0.35f);
                preview.lights[0].color = Color.white;
                preview.lights[0].intensity = 1.15f;
                preview.lights[0].transform.rotation = Quaternion.LookRotation(
                    -(Quaternion.AngleAxis(-35f, Vector3.up) * facing) + Vector3.down * 0.7f);
                preview.lights[1].color = new Color(0.85f, 0.9f, 1f);
                preview.lights[1].intensity = 0.5f;
                preview.lights[1].transform.rotation = Quaternion.LookRotation(
                    -(Quaternion.AngleAxis(70f, Vector3.up) * facing) + Vector3.down * 0.3f);

                if (small)
                {
                    // Head and shoulders, on a grey gradient (no transparency), like Kyle's small portrait.
                    float bottom = head.y - 0.32f * size, top = crownY + 0.04f * size;
                    Color[] cutout = RenderCutout(preview, new Vector3(head.x, (top + bottom) / 2, head.z),
                                                  top - bottom, view, PortraitSmallSize);
                    SavePortrait(cutout, PortraitSmallSize, PortraitSmallPath, (u, v) =>
                        Color.Lerp(new Color(0.36f, 0.36f, 0.38f), new Color(0.64f, 0.64f, 0.66f), v));
                }

                if (big)
                {
                    // Almost full body, on a slanted dark panel with a transparent background, like Kyle's big portrait.
                    float bottom = groundY + 0.10f * size, top = crownY + 0.05f * size;
                    Color[] cutout = RenderCutout(preview, new Vector3(hips.x, (top + bottom) / 2, hips.z),
                                                  top - bottom, view, PortraitBigSize);
                    float h = PortraitBigSize.y;
                    SavePortrait(cutout, PortraitBigSize, PortraitBigPath, (u, v) =>
                    {
                        float inside = Mathf.Clamp01(((0.93f + 0.07f * u) - v) * h + 0.5f) *
                                       Mathf.Clamp01((v - (0.04f + 0.11f * u)) * h + 0.5f);
                        return new Color(0.2f, 0.2f, 0.2f, inside);
                    });
                }
            }
            finally
            {
                if (graph.IsValid()) graph.Destroy();
                preview.Cleanup();
            }
        }

        /// Renders the character on black and on white; the difference gives a clean transparent cut-out.
        static Color[] RenderCutout(PreviewRenderUtility preview, Vector3 center, float frameHeight,
                                    Vector3 view, Vector2Int size)
        {
            Camera camera = preview.camera;
            float distance = frameHeight * 0.5f / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            camera.transform.position = center + view * distance;
            camera.transform.rotation = Quaternion.LookRotation(-view, Vector3.up);
            camera.nearClipPlane = distance * 0.05f;
            camera.farClipPlane = distance * 10f;

            int w = size.x * Supersample, h = size.y * Supersample;
            Color[] onBlack = RenderOnce(preview, w, h, Color.black);
            Color[] onWhite = RenderOnce(preview, w, h, Color.white);

            // Premultiplied colour (what shows on black) and alpha, shrunk with a box filter.
            var result = new Color[size.x * size.y];
            float samples = Supersample * Supersample;
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                float r = 0, g = 0, b = 0, a = 0;
                for (int sy = 0; sy < Supersample; sy++)
                for (int sx = 0; sx < Supersample; sx++)
                {
                    int i = (y * Supersample + sy) * w + x * Supersample + sx;
                    Color k = onBlack[i], wt = onWhite[i];
                    float alpha = Mathf.Clamp01(1f - ((wt.r - k.r) + (wt.g - k.g) + (wt.b - k.b)) / 3f);
                    r += k.r; g += k.g; b += k.b; a += alpha;
                }
                a /= samples;
                result[y * size.x + x] = a > 0.001f
                    ? new Color(Mathf.Clamp01(r / samples / a), Mathf.Clamp01(g / samples / a), Mathf.Clamp01(b / samples / a), a)
                    : Color.clear;
            }
            return result;
        }

        static Color[] RenderOnce(PreviewRenderUtility preview, int w, int h, Color background)
        {
            preview.camera.backgroundColor = background;
            preview.BeginStaticPreview(new Rect(0, 0, w, h));
            preview.Render(true, false);
            Texture2D texture = preview.EndStaticPreview();
            Color[] pixels = texture.GetPixels();
            Object.DestroyImmediate(texture);
            return pixels;
        }

        /// Puts the cut-out over a background (u, v go 0..1 from the bottom-left) and saves it as a sprite.
        static void SavePortrait(Color[] cutout, Vector2Int size, string path, Func<float, float, Color> background)
        {
            var pixels = new Color[cutout.Length];
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                int i = y * size.x + x;
                Color fg = cutout[i];
                Color bg = background((x + 0.5f) / size.x, (y + 0.5f) / size.y);
                float a = fg.a + bg.a * (1f - fg.a);
                pixels[i] = a > 0.001f
                    ? new Color((fg.r * fg.a + bg.r * bg.a * (1f - fg.a)) / a,
                                (fg.g * fg.a + bg.g * bg.a * (1f - fg.a)) / a,
                                (fg.b * fg.a + bg.b * bg.a * (1f - fg.a)) / a, a)
                    : Color.clear;
            }

            var texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite; // same import settings as Kyle's portraits
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        // ---------------------------------------------------------------- Step 6: roster

        static void AddToRoster(Object character, Target target)
        {
            foreach (string configPath in target.ConfigPaths)
                AddToRoster(character, configPath);
        }

        static void AddToRoster(Object character, string configPath)
        {
            string configName = Path.GetFileNameWithoutExtension(configPath);
            Object config = AssetDatabase.LoadMainAssetAtPath(configPath)
                            ?? throw new Exception($"Couldn't find the UFE config at {configPath}.");
            var so = new SerializedObject(config);
            SerializedProperty characters = so.FindProperty("characters")
                                            ?? throw new Exception("The UFE config has no 'characters' list.");

            int index = -1, emptySlot = -1;
            for (int i = 0; i < characters.arraySize; i++)
            {
                Object entry = characters.GetArrayElementAtIndex(i).objectReferenceValue;
                if (entry == character) index = i;
                if (entry == null && emptySlot < 0) emptySlot = i;
            }

            if (index < 0)
            {
                index = emptySlot >= 0 ? emptySlot : characters.arraySize++;
                characters.GetArrayElementAtIndex(index).objectReferenceValue = character;
                Debug.Log(Log + $"Added Mestre Alien to the {configName} roster (slot {index + 1}).");
            }
            else
            {
                Debug.Log(Log + $"Mestre Alien is already in the {configName} roster.");
            }

            // Being in the roster isn't enough: UFE only offers the roster positions listed here.
            AddIndex(so, "storyMode.selectableCharactersInVersusMode", index, configName);
            AddIndex(so, "storyMode.selectableCharactersInStoryMode", index, configName);
            AddEmptyStory(so, index);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            if (characters.arraySize > SelectScreenSlots)
                Debug.LogWarning(Log + $"{configName} now has {characters.arraySize} fighters, but the demo's " +
                                 $"select screen only has {SelectScreenSlots} slots. Some won't be pickable.");
        }

        /// Adds a roster position to one of UFE's int lists (if it isn't there yet).
        static void AddIndex(SerializedObject so, string path, int index, string configName)
        {
            SerializedProperty list = so.FindProperty(path);
            if (list == null || !list.isArray)
            {
                Debug.LogWarning(Log + $"{configName} has no '{path}' list; skipped it.");
                return;
            }
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).intValue == index) return;

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).intValue = index;
            Debug.Log(Log + $"Mestre Alien is now selectable in {configName}: {path.Substring(path.LastIndexOf('.') + 1)}.");
        }

        /// Story mode keeps one (here empty) story per roster position, like the Global Editor does.
        static void AddEmptyStory(SerializedObject so, int index)
        {
            SerializedProperty keys = so.FindProperty("storyMode.characterStories._keys");
            SerializedProperty values = so.FindProperty("storyMode.characterStories._values");
            if (keys == null || values == null || !keys.isArray || !values.isArray || keys.arraySize != values.arraySize)
                return;
            for (int i = 0; i < keys.arraySize; i++)
                if (keys.GetArrayElementAtIndex(i).intValue == index) return;

            keys.arraySize++;
            values.arraySize++; // copies the previous entry, so clear it
            keys.GetArrayElementAtIndex(keys.arraySize - 1).intValue = index;
            SerializedProperty story = values.GetArrayElementAtIndex(values.arraySize - 1);
            SerializedProperty opening = story.FindPropertyRelative("opening");
            SerializedProperty ending = story.FindPropertyRelative("ending");
            SerializedProperty fights = story.FindPropertyRelative("fightsGroups");
            if (opening != null) opening.objectReferenceValue = null;
            if (ending != null) ending.objectReferenceValue = null;
            if (fights != null && fights.isArray) fights.arraySize = 0;
        }

        // ---------------------------------------------------------------- helpers

        static ModelImporter GetModelImporter() =>
            AssetImporter.GetAtPath(ModelPath) as ModelImporter
            ?? throw new Exception($"Couldn't find the model at {ModelPath}. " +
                                   "Make sure Unity has finished importing it.");

        static GameObject LoadModel() => AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);

        static Avatar LoadAvatar() => AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();

        static Dictionary<string, string> HumanToBone(Avatar avatar)
        {
            var map = new Dictionary<string, string>();
            foreach (HumanBone bone in avatar.humanDescription.human)
                map[bone.humanName] = bone.boneName;
            return map;
        }

        static Transform FindDeep(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        static SerializedProperty Prop(SerializedObject so, string name) =>
            so.FindProperty(name)
            ?? throw new Exception($"UFE's character file has no '{name}' field. Your UFE version may differ.");

        static bool HasAssets(string folder, string filter) =>
            AssetDatabase.IsValidFolder(folder) && AssetDatabase.FindAssets(filter, new[] { folder }).Length > 0;

        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static string SafeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => invalid.Contains(c) || c == ':' ? '_' : c).ToArray());
        }

        static void Progress(string message, float progress) =>
            EditorUtility.DisplayProgressBar(DisplayName, message, progress);
    }
}
