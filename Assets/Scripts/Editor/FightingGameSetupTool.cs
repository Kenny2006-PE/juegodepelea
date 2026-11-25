#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace FightingGame.EditorTools
{
    /// <summary>
    /// Automates the full Unity setup for the Tanjiro vs Madara 1v1 fighting game:
    /// 1. Humanoid Rig & Character-specific Avatar assignment + Loop/Root-bake + Animation Events
    /// 2. BaseFighterAnimatorController + MadaraAnimatorOverride
    /// 3. TanjiroFighter & MadaraFighter prefabs with bone-attached Hitboxes & Body Hurtbox
    /// 4. 3D Dojo Arena scene, FightingCamera, Main Menu UI, Combat HUD & Victory Screen
    /// </summary>
    public static class FightingGameSetupTool
    {
        private const string TanjiroFolder = "Assets/Animations Characters/Tanjiro";
        private const string MadaraFolder = "Assets/Animations Characters/Madara";

        private const string AnimationsOutputDir = "Assets/Animations";
        private const string LegacyFallbackClipsDir = "Assets/Animations/FallbackClips";
        private const string PrefabsOutputDir = "Assets/Prefabs";
        private const string MaterialsOutputDir = "Assets/Materials";
        private const string UISpritesDir = "Assets/UI/Sprites";
        private const string ScenesOutputDir = "Assets/Scenes";

        private const string BaseControllerPath = AnimationsOutputDir + "/BaseFighterAnimatorController.controller";
        private const string MadaraOverridePath = AnimationsOutputDir + "/MadaraAnimatorOverride.overrideController";
        private const string TanjiroPrefabPath = PrefabsOutputDir + "/TanjiroFighter.prefab";
        private const string MadaraPrefabPath = PrefabsOutputDir + "/MadaraFighter.prefab";
        private const string WhiteSpritePath = UISpritesDir + "/SolidWhite.png";
        private const string RoundedCardSpritePath = UISpritesDir + "/RoundedCard.png";
        private const string RoundedBorderSpritePath = UISpritesDir + "/RoundedBorder.png";
        private const string HealthBarGlossSpritePath = UISpritesDir + "/HealthBarGloss.png";
        private const string DiamondPipSpritePath = UISpritesDir + "/DiamondPip.png";
        private const string HorizontalSlashSpritePath = UISpritesDir + "/HorizontalSlash.png";
        private const string VignetteSpritePath = UISpritesDir + "/VignetteBackdrop.png";
        private const string ArenaScenePath = ScenesOutputDir + "/FightingArena.unity";
        private const string BuildOutputDirectory = "Builds/TanjiroVsMadara";
        private const string BuildExecutableName = "TanjiroVsMadara.exe";

        private struct UISpriteBundle
        {
            public Sprite white;
            public Sprite roundedCard;
            public Sprite roundedBorder;
            public Sprite healthBarGloss;
            public Sprite diamondPip;
            public Sprite horizontalSlash;
            public Sprite vignette;
        }

        private struct CharacterClipSet
        {
            public AnimationClip idle;
            public AnimationClip stepForward;
            public AnimationClip stepBackward;
            public AnimationClip block;
            public AnimationClip jab;
            public AnimationClip kick;
            public AnimationClip lightHit;
            public AnimationClip stunned;
            public AnimationClip gettingUp;
            public AnimationClip knockedOut;
        }

        [MenuItem("Tools/Setup Fighting Game/Full Setup (All Steps)", false, 0)]
        public static void RunFullSetup()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Setup Fighting Game", "1/4 Configurando Rigs Humanoid y Avatars...", 0.15f);
                ConfigureAllCharacterRigs();

                EditorUtility.DisplayProgressBar("Setup Fighting Game", "2/4 Creando Animator Controller y Override...", 0.45f);
                BuildAnimatorControllers();

                EditorUtility.DisplayProgressBar("Setup Fighting Game", "3/4 Construyendo Prefabs con Hitboxes y Hurtboxes...", 0.70f);
                BuildFighterPrefabs();

                EditorUtility.DisplayProgressBar("Setup Fighting Game", "4/4 Construyendo Arena, Menú y HUD de Combate...", 0.90f);
                BuildArenaSceneAndUI();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("<color=#33DD88>[FightingGameSetup] ¡Configuración completa exitosa! Pulsa PLAY para jugar Tanjiro vs Madara.</color>");
                EditorUtility.DisplayDialog(
                    "Setup Fighting Game Completo",
                    "¡Todo se ha configurado correctamente con los 10 FBX reales de Tanjiro y Madara!\n\n" +
                    "• Rigs Humanoid y Avatars de Tanjiro y Madara listos\n" +
                    "• BaseFighterAnimatorController + MadaraAnimatorOverride creados (10 clips reales)\n" +
                    "• Prefabs TanjiroFighter y MadaraFighter con Hitboxes/Hurtboxes listos\n" +
                    "• Escena FightingArena con Menú de Selección y HUD creada\n\n" +
                    "Pulsa PLAY en Unity para jugar.",
                    "Entendido");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FightingGameSetup] Error durante la configuración: {ex}");
                EditorUtility.DisplayDialog("Error en Setup Fighting Game", ex.Message, "Cerrar");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/Setup Fighting Game/1. Configure Character Rigs & Animations", false, 20)]
        public static void ConfigureAllCharacterRigs()
        {
            ConfigureCharacterFolderRigs("Tanjiro", TanjiroFolder);
            ConfigureCharacterFolderRigs("Madara", MadaraFolder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FightingGameSetup] Paso 1 completado: Rigs Humanoid, Avatars, Loop Time y Animation Events configurados.");
        }

        [MenuItem("Tools/Setup Fighting Game/2. Build Animator Controllers (Base + Override)", false, 21)]
        public static void BuildAnimatorControllers()
        {
            EnsureDirectory(AnimationsOutputDir);

            // Clean up any legacy fallback clips folder if present
            if (AssetDatabase.IsValidFolder(LegacyFallbackClipsDir))
            {
                AssetDatabase.DeleteAsset(LegacyFallbackClipsDir);
            }

            CharacterClipSet tanjiroClips = LoadCharacterClips(TanjiroFolder);
            CharacterClipSet madaraClips = LoadCharacterClips(MadaraFolder);

            AnimatorController baseController = CreateBaseAnimatorController(tanjiroClips);
            CreateMadaraOverrideController(baseController, tanjiroClips, madaraClips);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FightingGameSetup] Paso 2 completado: BaseFighterAnimatorController y MadaraAnimatorOverride generados.");
        }

        [MenuItem("Tools/Setup Fighting Game/3. Build Fighter Prefabs (Tanjiro & Madara)", false, 22)]
        public static void BuildFighterPrefabs()
        {
            EnsureDirectory(PrefabsOutputDir);

            RuntimeAnimatorController tanjiroCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);
            RuntimeAnimatorController madaraCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MadaraOverridePath);

            if (tanjiroCtrl == null || madaraCtrl == null)
            {
                BuildAnimatorControllers();
                tanjiroCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);
                madaraCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MadaraOverridePath);
            }

            string tanjiroBaseFbx = FindBaseModelFbxPath(TanjiroFolder);
            string madaraBaseFbx = FindBaseModelFbxPath(MadaraFolder);

            BuildSingleFighterPrefab("TanjiroFighter", FighterId.Tanjiro, tanjiroBaseFbx, tanjiroCtrl, TanjiroPrefabPath);
            BuildSingleFighterPrefab("MadaraFighter", FighterId.Madara, madaraBaseFbx, madaraCtrl, MadaraPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FightingGameSetup] Paso 3 completado: Prefabs TanjiroFighter y MadaraFighter generados.");
        }

        [MenuItem("Tools/Setup Fighting Game/4. Setup Arena Scene, Menu & Combat HUD", false, 23)]
        public static void BuildArenaSceneAndUI()
        {
            EnsureTMPResources();
            EnsureDirectory(MaterialsOutputDir);
            EnsureDirectory(UISpritesDir);
            EnsureDirectory(ScenesOutputDir);

            UISpriteBundle sprites = EnsureUISprites();
            TMP_FontAsset fontAsset = FindDefaultTMPFont();

            FighterController tanjiroPrefab = AssetDatabase.LoadAssetAtPath<FighterController>(TanjiroPrefabPath);
            FighterController madaraPrefab = AssetDatabase.LoadAssetAtPath<FighterController>(MadaraPrefabPath);
            if (tanjiroPrefab == null || madaraPrefab == null)
            {
                BuildFighterPrefabs();
                tanjiroPrefab = AssetDatabase.LoadAssetAtPath<FighterController>(TanjiroPrefabPath);
                madaraPrefab = AssetDatabase.LoadAssetAtPath<FighterController>(MadaraPrefabPath);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLightingAndEnvironment();
            FightingCamera fightingCamera = BuildCamera();
            BuildArenaStage();

            // GameManager + Spawn Points + Feedback
            GameObject gmObj = new GameObject("GameManager");
            GameManager gameManager = gmObj.AddComponent<GameManager>();
            CombatFeedback feedback = gmObj.AddComponent<CombatFeedback>();

            GameObject p1Spawn = new GameObject("P1Spawn");
            p1Spawn.transform.SetParent(gmObj.transform);
            p1Spawn.transform.position = new Vector3(-2.4f, 0f, 0f);

            GameObject p2Spawn = new GameObject("P2Spawn");
            p2Spawn.transform.SetParent(gmObj.transform);
            p2Spawn.transform.position = new Vector3(2.4f, 0f, 0f);

            // EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();

            // UI Canvas + Combat HUD + Selection Menu
            GameObject canvasObj = CreateUICanvas("FightingUICanvas");
            UIManager uiManager = BuildCombatHUD(canvasObj.transform, sprites, fontAsset);
            MenuManager menuManager = BuildMainMenu(canvasObj.transform, sprites, fontAsset, gameManager);

            // Wire GameManager serialized fields
            SerializedObject gmSO = new SerializedObject(gameManager);
            gmSO.FindProperty("tanjiroPrefab").objectReferenceValue = tanjiroPrefab;
            gmSO.FindProperty("madaraPrefab").objectReferenceValue = madaraPrefab;
            gmSO.FindProperty("p1SpawnPoint").objectReferenceValue = p1Spawn.transform;
            gmSO.FindProperty("p2SpawnPoint").objectReferenceValue = p2Spawn.transform;
            gmSO.FindProperty("fightingCamera").objectReferenceValue = fightingCamera;
            gmSO.FindProperty("uiManager").objectReferenceValue = uiManager;
            gmSO.FindProperty("menuManager").objectReferenceValue = menuManager;
            gmSO.FindProperty("combatFeedback").objectReferenceValue = feedback;
            gmSO.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ArenaScenePath);
            EnsureSceneInBuildSettings(ArenaScenePath);

            Debug.Log($"[FightingGameSetup] Paso 4 completado: Escena y UI profesional guardadas en {ArenaScenePath}.");
        }

        [MenuItem("Tools/Setup Fighting Game/5. Rebuild UI Only (Menu & HUD)", false, 24)]
        public static void RebuildUIOnlyInScene()
        {
            EnsureTMPResources();
            EnsureDirectory(UISpritesDir);

            if (!File.Exists(ArenaScenePath))
            {
                BuildArenaSceneAndUI();
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
            GameManager gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                BuildArenaSceneAndUI();
                return;
            }

            // Replace only the Canvas object, preserving Arena/Lights/Camera/GameManager
            GameObject oldCanvas = GameObject.Find("FightingUICanvas");
            if (oldCanvas != null)
            {
                UnityEngine.Object.DestroyImmediate(oldCanvas);
            }

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            UISpriteBundle sprites = EnsureUISprites();
            TMP_FontAsset fontAsset = FindDefaultTMPFont();

            GameObject canvasObj = CreateUICanvas("FightingUICanvas");
            UIManager uiManager = BuildCombatHUD(canvasObj.transform, sprites, fontAsset);
            MenuManager menuManager = BuildMainMenu(canvasObj.transform, sprites, fontAsset, gameManager);

            SerializedObject gmSO = new SerializedObject(gameManager);
            gmSO.FindProperty("uiManager").objectReferenceValue = uiManager;
            gmSO.FindProperty("menuManager").objectReferenceValue = menuManager;
            gmSO.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ArenaScenePath);
            Debug.Log("<color=#33DD88>[FightingGameSetup] ¡Interfaz (Menú y HUD de Combate) rediseñada y guardada con éxito!</color>");
        }

        [MenuItem("Tools/Setup Fighting Game/6. Build Windows Executable", false, 25)]
        public static void BuildWindowsExecutableMenuItem()
        {
            BuildStandaloneWindows();
        }

        public static void BuildStandaloneWindows()
        {
            EnsureSceneInBuildSettings(ArenaScenePath);

            string fullBuildDir = Path.GetFullPath(BuildOutputDirectory);
            if (Directory.Exists(fullBuildDir))
            {
                Directory.Delete(fullBuildDir, true);
            }
            Directory.CreateDirectory(fullBuildDir);

            string exePath = Path.Combine(fullBuildDir, BuildExecutableName);
            string[] scenes = new[] { ArenaScenePath };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"[FightingGameSetup] Iniciando compilación de StandaloneWindows64 en: {exePath}");
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"<color=#33DD88>[FightingGameSetup] ¡Compilación exitosa! Tamaño total: {summary.totalSize} bytes en {exePath}</color>");
            }
            else
            {
                string errorMsg = $"[FightingGameSetup] Error durante la compilación: {summary.result} ({summary.totalErrors} errores)";
                Debug.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
        }

        // =====================================================================
        // STEP 1: RIGS, AVATARS, LOOPING & ANIMATION EVENTS
        // =====================================================================

        private static void ConfigureCharacterFolderRigs(string characterName, string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                throw new DirectoryNotFoundException($"No se encontró la carpeta de assets: {folderPath}");
            }

            string[] fbxPaths = GetFbxAssetPaths(folderPath);
            if (fbxPaths.Length == 0)
            {
                throw new FileNotFoundException($"No se encontraron archivos .fbx dentro de {folderPath}");
            }

            string baseModelPath = FindBaseModelFbxPath(folderPath);
            ModelImporter baseImporter = AssetImporter.GetAtPath(baseModelPath) as ModelImporter;
            if (baseImporter == null)
            {
                throw new InvalidOperationException($"No se pudo obtener ModelImporter para {baseModelPath}");
            }

            // Extract embedded Mixamo textures so characters display their anime materials
            string texturesDir = folderPath + "/Textures";
            EnsureDirectory(texturesDir);
            try
            {
                baseImporter.ExtractTextures(texturesDir);
            }
            catch (Exception)
            {
                // Ignore if textures were already extracted or none are embedded
            }

            baseImporter.animationType = ModelImporterAnimationType.Human;
            baseImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            baseImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            ConfigureClipImporterSettings(baseImporter, Path.GetFileNameWithoutExtension(baseModelPath));
            baseImporter.SaveAndReimport();

            Avatar characterAvatar = AssetDatabase.LoadAllAssetsAtPath(baseModelPath)
                .OfType<Avatar>()
                .FirstOrDefault();

            if (characterAvatar == null)
            {
                Debug.LogWarning($"[FightingGameSetup] No se pudo extraer el Avatar Humanoid de {baseModelPath}.");
            }
            else
            {
                Debug.Log($"[FightingGameSetup] Avatar Humanoid creado para {characterName}: {characterAvatar.name}");
            }

            foreach (string fbxPath in fbxPaths)
            {
                if (string.Equals(fbxPath, baseModelPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer == null) continue;

                importer.animationType = ModelImporterAnimationType.Human;
                if (characterAvatar != null)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = characterAvatar;
                }
                else
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                }

                ConfigureClipImporterSettings(importer, Path.GetFileNameWithoutExtension(fbxPath));
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureClipImporterSettings(ModelImporter importer, string cleanName)
        {
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips == null || clips.Length == 0)
            {
                return;
            }

            string lower = cleanName.ToLowerInvariant();
            bool shouldLoop = lower.Contains("idle")
                || lower.Contains("forward")
                || lower.Contains("backward")
                || lower.Contains("block");

            for (int i = 0; i < clips.Length; i++)
            {
                ModelImporterClipAnimation clip = clips[i];
                clip.name = cleanName;
                clip.loopTime = shouldLoop;
                clip.loopPose = shouldLoop;

                // Keep all Mixamo In-Place clips locked to the 2.5D Z=0 fighting plane and floor Y=0
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = true;
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalPositionXZ = true;

                // Inject Hitbox Animation Events on the two attack clips (time is normalized 0..1)
                if (lower.Contains("jab"))
                {
                    clip.events = CreateAttackEvents(0.24f, 0.64f, 0.94f);
                }
                else if (lower.Contains("kick"))
                {
                    clip.events = CreateAttackEvents(0.26f, 0.64f, 0.94f);
                }
            }

            importer.clipAnimations = clips;
        }

        private static AnimationEvent[] CreateAttackEvents(float enableNorm, float disableNorm, float endNorm)
        {
            return new[]
            {
                new AnimationEvent { functionName = "AnimEvent_EnableHitbox", time = enableNorm },
                new AnimationEvent { functionName = "AnimEvent_DisableHitbox", time = disableNorm },
                new AnimationEvent { functionName = "AnimEvent_AttackEnd", time = endNorm }
            };
        }

        // =====================================================================
        // STEP 2: BASE ANIMATOR CONTROLLER + MADARA OVERRIDE
        // =====================================================================

        private static CharacterClipSet LoadCharacterClips(string folderPath)
        {
            string[] fbxPaths = GetFbxAssetPaths(folderPath);
            return new CharacterClipSet
            {
                idle = FindClipByKeywords(fbxPaths, "fighting idle", "idle"),
                stepForward = FindClipByKeywords(fbxPaths, "short step forward", "step forward", "forward"),
                stepBackward = FindClipByKeywords(fbxPaths, "step backward", "backward"),
                block = FindClipByKeywords(fbxPaths, "center block", "block"),
                jab = FindClipByKeywords(fbxPaths, "jab cross", "jab"),
                kick = FindClipByKeywords(fbxPaths, "roundhouse kick", "kick"),
                lightHit = FindClipByKeywords(fbxPaths, "head hit", "stomach hit", "hit"),
                stunned = FindClipByKeywords(fbxPaths, "stunned"),
                gettingUp = FindClipByKeywords(fbxPaths, "getting up"),
                knockedOut = FindClipByKeywords(fbxPaths, "knocked out", "ko")
            };
        }

        private static AnimatorController CreateBaseAnimatorController(CharacterClipSet clips)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(BaseControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(BaseControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BaseControllerPath);

            // Parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("isBlocking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("isDead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jab", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Kick", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("LightHit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Knockdown", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("KO", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            // 1. Locomotion 1D BlendTree (-1 Step Backward, 0 Fighting Idle, +1 Short Step Forward)
            AnimatorState locomotionState = controller.CreateBlendTreeInController("Locomotion", out BlendTree locoTree, 0);
            locoTree.blendType = BlendTreeType.Simple1D;
            locoTree.blendParameter = "Speed";
            locoTree.useAutomaticThresholds = false;
            if (clips.stepBackward != null) locoTree.AddChild(clips.stepBackward, -1f);
            if (clips.idle != null) locoTree.AddChild(clips.idle, 0f);
            if (clips.stepForward != null) locoTree.AddChild(clips.stepForward, 1f);
            sm.defaultState = locomotionState;

            // 2. Center Block
            AnimatorState blockState = sm.AddState("Center Block", new Vector3(260, -100, 0));
            blockState.motion = clips.block;

            AnimatorStateTransition toBlock = locomotionState.AddTransition(blockState);
            toBlock.AddCondition(AnimatorConditionMode.If, 0, "isBlocking");
            toBlock.hasExitTime = false;
            toBlock.duration = 0.08f;

            AnimatorStateTransition fromBlock = blockState.AddTransition(locomotionState);
            fromBlock.AddCondition(AnimatorConditionMode.IfNot, 0, "isBlocking");
            fromBlock.hasExitTime = false;
            fromBlock.duration = 0.10f;

            // 3. Attacks (Jab Cross + Roundhouse Kick, with direct Jab -> Roundhouse Kick combo transition)
            AnimatorState jabState = sm.AddState("Jab Cross", new Vector3(520, -20, 0));
            jabState.motion = clips.jab;

            AnimatorState kickState = sm.AddState("Roundhouse Kick", new Vector3(520, 90, 0));
            kickState.motion = clips.kick;

            AddTriggerAttackTransitions(locomotionState, jabState, "Jab", 0.05f, 0.90f, 0.08f);
            AddTriggerAttackTransitions(locomotionState, kickState, "Kick", 0.06f, 0.92f, 0.10f);

            // Allow Jab Cross -> Roundhouse Kick combo cancel directly
            AnimatorStateTransition jabToKick = jabState.AddTransition(kickState);
            jabToKick.AddCondition(AnimatorConditionMode.If, 0, "Kick");
            jabToKick.hasExitTime = false;
            jabToKick.duration = 0.06f;

            // 4. LightHit Reaction
            AnimatorState lightHitState = sm.AddState("LightHit", new Vector3(260, 190, 0));
            lightHitState.motion = clips.lightHit;

            AnimatorStateTransition anyToHit = sm.AddAnyStateTransition(lightHitState);
            anyToHit.AddCondition(AnimatorConditionMode.If, 0, "LightHit");
            anyToHit.AddCondition(AnimatorConditionMode.IfNot, 0, "isDead");
            anyToHit.hasExitTime = false;
            anyToHit.duration = 0.05f;
            anyToHit.canTransitionToSelf = true;

            AnimatorStateTransition hitToLoco = lightHitState.AddTransition(locomotionState);
            hitToLoco.hasExitTime = true;
            hitToLoco.exitTime = 0.88f;
            hitToLoco.duration = 0.10f;

            // 5. Knockdown Chain: Stunned -> Getting Up -> Locomotion
            AnimatorState stunnedState = sm.AddState("Stunned", new Vector3(260, 290, 0));
            stunnedState.motion = clips.stunned;

            AnimatorState gettingUpState = sm.AddState("Getting Up", new Vector3(520, 290, 0));
            gettingUpState.motion = clips.gettingUp;

            AnimatorStateTransition anyToKnockdown = sm.AddAnyStateTransition(stunnedState);
            anyToKnockdown.AddCondition(AnimatorConditionMode.If, 0, "Knockdown");
            anyToKnockdown.AddCondition(AnimatorConditionMode.IfNot, 0, "isDead");
            anyToKnockdown.hasExitTime = false;
            anyToKnockdown.duration = 0.08f;
            anyToKnockdown.canTransitionToSelf = false;

            AnimatorStateTransition stunnedToGetUp = stunnedState.AddTransition(gettingUpState);
            stunnedToGetUp.hasExitTime = true;
            stunnedToGetUp.exitTime = 0.94f;
            stunnedToGetUp.duration = 0.12f;

            AnimatorStateTransition getUpToLoco = gettingUpState.AddTransition(locomotionState);
            getUpToLoco.hasExitTime = true;
            getUpToLoco.exitTime = 0.94f;
            getUpToLoco.duration = 0.12f;

            // 6. Knocked Out (Terminal state - no exit transitions)
            AnimatorState koState = sm.AddState("Knocked Out", new Vector3(260, 390, 0));
            koState.motion = clips.knockedOut;

            AnimatorStateTransition anyToKO = sm.AddAnyStateTransition(koState);
            anyToKO.AddCondition(AnimatorConditionMode.If, 0, "KO");
            anyToKO.hasExitTime = false;
            anyToKO.duration = 0.08f;
            anyToKO.canTransitionToSelf = false;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddTriggerAttackTransitions(
            AnimatorState fromState,
            AnimatorState attackState,
            string triggerName,
            float enterDuration,
            float exitTime,
            float exitDuration)
        {
            AnimatorStateTransition enter = fromState.AddTransition(attackState);
            enter.AddCondition(AnimatorConditionMode.If, 0, triggerName);
            enter.hasExitTime = false;
            enter.duration = enterDuration;

            AnimatorStateTransition exit = attackState.AddTransition(fromState);
            exit.hasExitTime = true;
            exit.exitTime = exitTime;
            exit.duration = exitDuration;
        }

        private static void CreateMadaraOverrideController(
            AnimatorController baseController,
            CharacterClipSet tanjiroClips,
            CharacterClipSet madaraClips)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(MadaraOverridePath) != null)
            {
                AssetDatabase.DeleteAsset(MadaraOverridePath);
            }

            AnimatorOverrideController overrideCtrl = new AnimatorOverrideController(baseController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideCtrl.GetOverrides(overrides);

            var mapping = new Dictionary<AnimationClip, AnimationClip>();
            AddMap(mapping, tanjiroClips.idle, madaraClips.idle);
            AddMap(mapping, tanjiroClips.stepForward, madaraClips.stepForward);
            AddMap(mapping, tanjiroClips.stepBackward, madaraClips.stepBackward);
            AddMap(mapping, tanjiroClips.block, madaraClips.block);
            AddMap(mapping, tanjiroClips.jab, madaraClips.jab);
            AddMap(mapping, tanjiroClips.kick, madaraClips.kick);
            AddMap(mapping, tanjiroClips.lightHit, madaraClips.lightHit);
            AddMap(mapping, tanjiroClips.stunned, madaraClips.stunned);
            AddMap(mapping, tanjiroClips.gettingUp, madaraClips.gettingUp);
            AddMap(mapping, tanjiroClips.knockedOut, madaraClips.knockedOut);

            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip key = overrides[i].Key;
                if (key != null && mapping.TryGetValue(key, out AnimationClip replacement) && replacement != null)
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(key, replacement);
                }
            }

            overrideCtrl.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(overrideCtrl, MadaraOverridePath);
            EditorUtility.SetDirty(overrideCtrl);
        }

        private static void AddMap(Dictionary<AnimationClip, AnimationClip> map, AnimationClip from, AnimationClip to)
        {
            if (from != null && to != null && !map.ContainsKey(from))
            {
                map[from] = to;
            }
        }

        // =====================================================================
        // STEP 3: FIGHTER PREFABS WITH HITBOXES & HURTBOX
        // =====================================================================

        private static void BuildSingleFighterPrefab(
            string prefabName,
            FighterId id,
            string baseModelFbxPath,
            RuntimeAnimatorController animatorController,
            string outputPrefabPath)
        {
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(baseModelFbxPath);
            if (fbxAsset == null)
            {
                throw new FileNotFoundException($"No se pudo cargar el modelo base FBX: {baseModelFbxPath}");
            }

            GameObject root = new GameObject(prefabName);
            try
            {
                CharacterController cc = root.AddComponent<CharacterController>();
                cc.height = 1.80f;
                cc.radius = 0.38f;
                cc.center = new Vector3(0f, 0.90f, 0f);
                cc.minMoveDistance = 0f;

                root.AddComponent<HealthSystem>();
                FighterController fighterCtrl = root.AddComponent<FighterController>();
                fighterCtrl.SetFighterId(id);

                // Instantiate visual model as child
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
                model.name = "Model";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                NormalizeModelHeightAndGround(model, targetHeight: 1.80f);

                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim == null)
                {
                    anim = model.AddComponent<Animator>();
                }
                anim.runtimeAnimatorController = animatorController;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                if (anim.gameObject.GetComponent<FighterAnimationEvents>() == null)
                {
                    anim.gameObject.AddComponent<FighterAnimationEvents>();
                }

                // Body Hurtbox (Trigger Capsule + Kinematic Rigidbody for guaranteed trigger contacts)
                GameObject hurtboxObj = new GameObject("BodyHurtbox");
                hurtboxObj.transform.SetParent(root.transform, false);
                CapsuleCollider hurtCol = hurtboxObj.AddComponent<CapsuleCollider>();
                hurtCol.isTrigger = true;
                hurtCol.height = 1.75f;
                hurtCol.radius = 0.44f;
                hurtCol.center = new Vector3(0f, 0.90f, 0f);

                Rigidbody hurtRb = hurtboxObj.AddComponent<Rigidbody>();
                hurtRb.isKinematic = true;
                hurtRb.useGravity = false;

                Hurtbox hurtbox = hurtboxObj.AddComponent<Hurtbox>();
                hurtbox.Configure(fighterCtrl);

                // Bone-attached Hitboxes (Hands & Feet)
                var hitboxList = new List<Hitbox>();
                TryAttachBoneHitbox(anim, HumanBodyBones.RightHand, "Hitbox_RightHand", HitboxLimb.RightHand, 0.24f, fighterCtrl, hitboxList);
                TryAttachBoneHitbox(anim, HumanBodyBones.LeftHand, "Hitbox_LeftHand", HitboxLimb.LeftHand, 0.24f, fighterCtrl, hitboxList);
                TryAttachBoneHitbox(anim, HumanBodyBones.RightFoot, "Hitbox_RightFoot", HitboxLimb.RightFoot, 0.28f, fighterCtrl, hitboxList);
                TryAttachBoneHitbox(anim, HumanBodyBones.LeftFoot, "Hitbox_LeftFoot", HitboxLimb.LeftFoot, 0.28f, fighterCtrl, hitboxList);

                // Fallback hitboxes if bones weren't resolved
                if (hitboxList.Count == 0)
                {
                    Debug.LogWarning($"[FightingGameSetup] No se detectaron huesos Humanoid en {prefabName}; creando hitboxes frontales de respaldo.");
                    hitboxList.Add(CreateAttachedHitbox(root.transform, new Vector3(0f, 1.3f, 0.6f), "Hitbox_RightHand", HitboxLimb.RightHand, 0.28f, fighterCtrl));
                    hitboxList.Add(CreateAttachedHitbox(root.transform, new Vector3(0f, 0.9f, 0.75f), "Hitbox_RightFoot", HitboxLimb.RightFoot, 0.32f, fighterCtrl));
                }

                fighterCtrl.SetReferences(anim, hurtbox, hitboxList);

                PrefabUtility.SaveAsPrefabAsset(root, outputPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void NormalizeModelHeightAndGround(GameObject model, float targetHeight)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return;

            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combined.Encapsulate(renderers[i].bounds);
            }

            float currentHeight = combined.size.y;
            if (currentHeight > 0.001f && (currentHeight < 1.2f || currentHeight > 2.3f))
            {
                float scaleFactor = targetHeight / currentHeight;
                model.transform.localScale *= scaleFactor;

                // Recompute bounds after scale normalization
                combined = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combined.Encapsulate(renderers[i].bounds);
                }
            }

            // Align feet cleanly to y = 0
            float minY = combined.min.y;
            if (Mathf.Abs(minY) > 0.01f)
            {
                model.transform.localPosition += new Vector3(0f, -minY, 0f);
            }
        }

        private static void TryAttachBoneHitbox(
            Animator anim,
            HumanBodyBones bone,
            string hitboxName,
            HitboxLimb limb,
            float worldRadius,
            FighterController owner,
            List<Hitbox> list)
        {
            if (anim == null || !anim.isHuman) return;

            Transform boneTransform = anim.GetBoneTransform(bone);
            if (boneTransform == null) return;

            Hitbox hb = CreateAttachedHitbox(boneTransform, Vector3.zero, hitboxName, limb, worldRadius, owner);
            list.Add(hb);
        }

        private static Hitbox CreateAttachedHitbox(
            Transform parent,
            Vector3 localPos,
            string hitboxName,
            HitboxLimb limb,
            float desiredWorldRadius,
            FighterController owner)
        {
            GameObject obj = new GameObject(hitboxName);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;

            // Compensate for bone hierarchy lossyScale (e.g. Mixamo Armature 0.01x or 100x)
            Vector3 ls = obj.transform.lossyScale;
            float avgScale = (Mathf.Abs(ls.x) + Mathf.Abs(ls.y) + Mathf.Abs(ls.z)) / 3f;
            float localRadius = avgScale > 0.0001f ? desiredWorldRadius / avgScale : desiredWorldRadius;

            SphereCollider sphere = obj.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = localRadius;
            sphere.enabled = false;

            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Hitbox hitbox = obj.AddComponent<Hitbox>();
            hitbox.Configure(owner, limb);
            return hitbox;
        }

        // =====================================================================
        // STEP 4: ARENA ENVIRONMENT, CAMERA, MENU & COMBAT HUD
        // =====================================================================

        private static void BuildLightingAndEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.30f, 0.38f, 1f);

            GameObject keyLightObj = new GameObject("Directional Key Light");
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.94f, 0.85f, 1f);
            keyLight.intensity = 1.25f;
            keyLight.shadows = LightShadows.Soft;
            keyLightObj.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

            GameObject rimLightObj = new GameObject("Directional Rim Light");
            Light rimLight = rimLightObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.45f, 0.65f, 1f, 1f);
            rimLight.intensity = 0.55f;
            rimLight.shadows = LightShadows.None;
            rimLightObj.transform.rotation = Quaternion.Euler(22f, 148f, 0f);
        }

        private static FightingCamera BuildCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.13f, 1f);
            cam.fieldOfView = 42f;
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = new Vector3(0f, 1.65f, -6.2f);
            camObj.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
            return camObj.AddComponent<FightingCamera>();
        }

        private static void BuildArenaStage()
        {
            GameObject stageRoot = new GameObject("FightingArenaStage");

            Material floorMat = CreateStandardMaterial("ArenaFloorMat", new Color(0.16f, 0.15f, 0.19f, 1f), 0.25f);
            Material trimMat = CreateStandardMaterial("ArenaTrimMat", new Color(0.26f, 0.24f, 0.30f, 1f), 0.45f);
            Material tanjiroPillarMat = CreateStandardMaterial("TanjiroAccentMat", MatchSettings.GetAccentColor(FighterId.Tanjiro), 0.6f);
            Material madaraPillarMat = CreateStandardMaterial("MadaraAccentMat", MatchSettings.GetAccentColor(FighterId.Madara), 0.6f);
            Material backdropMat = CreateStandardMaterial("ArenaBackdropMat", new Color(0.10f, 0.11f, 0.17f, 1f), 0.15f);

            // Main floor (top surface exactly at y = 0)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(stageRoot.transform);
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(17f, 0.5f, 7f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Center line accent
            GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name = "CenterDivider";
            centerLine.transform.SetParent(stageRoot.transform);
            centerLine.transform.position = new Vector3(0f, 0.005f, 0f);
            centerLine.transform.localScale = new Vector3(0.08f, 0.01f, 6.2f);
            UnityEngine.Object.DestroyImmediate(centerLine.GetComponent<Collider>());
            centerLine.GetComponent<Renderer>().sharedMaterial = trimMat;

            // Back dojo wall
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "DojoBackdrop";
            backWall.transform.SetParent(stageRoot.transform);
            backWall.transform.position = new Vector3(0f, 3.2f, 3.8f);
            backWall.transform.localScale = new Vector3(18f, 7.2f, 0.4f);
            UnityEngine.Object.DestroyImmediate(backWall.GetComponent<Collider>());
            backWall.GetComponent<Renderer>().sharedMaterial = backdropMat;

            // Left & right stage boundary pillars
            CreatePillar(stageRoot.transform, "LeftBoundaryPillar", new Vector3(-7.4f, 2.2f, 1.4f), tanjiroPillarMat);
            CreatePillar(stageRoot.transform, "RightBoundaryPillar", new Vector3(7.4f, 2.2f, 1.4f), madaraPillarMat);
        }

        private static void CreatePillar(Transform parent, string name, Vector3 pos, Material mat)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = name;
            pillar.transform.SetParent(parent);
            pillar.transform.position = pos;
            pillar.transform.localScale = new Vector3(0.55f, 4.4f, 0.55f);
            UnityEngine.Object.DestroyImmediate(pillar.GetComponent<Collider>());
            pillar.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Material CreateStandardMaterial(string matName, Color color, float smoothness)
        {
            string path = $"{MaterialsOutputDir}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            mat.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject CreateUICanvas(string name)
        {
            GameObject canvasObj = new GameObject(name);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return canvasObj;
        }

        private static UIManager BuildCombatHUD(Transform canvasTransform, UISpriteBundle sprites, TMP_FontAsset font)
        {
            Color goldAccent = new Color(0.98f, 0.74f, 0.22f, 1f);
            Color obsidianBg = new Color(0.06f, 0.07f, 0.11f, 0.92f);

            GameObject hudRoot = new GameObject("CombatHUDPanel", typeof(RectTransform));
            hudRoot.transform.SetParent(canvasTransform, false);
            StretchFull(hudRoot.GetComponent<RectTransform>());

            UIManager uiManager = canvasTransform.gameObject.AddComponent<UIManager>();

            // === Top Arcade HUD Bar ===
            GameObject topBar = new GameObject("TopHUD", typeof(RectTransform));
            topBar.transform.SetParent(hudRoot.transform, false);
            RectTransform topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = new Vector2(0f, -20f);
            topRect.sizeDelta = new Vector2(-84f, 150f);

            // --- PLAYER 1 (LEFT) ---
            GameObject p1Block = new GameObject("P1HealthBlock", typeof(RectTransform));
            p1Block.transform.SetParent(topBar.transform, false);
            RectTransform p1Rect = p1Block.GetComponent<RectTransform>();
            p1Rect.anchorMin = new Vector2(0f, 0f);
            p1Rect.anchorMax = new Vector2(0.435f, 1f);
            p1Rect.offsetMin = Vector2.zero;
            p1Rect.offsetMax = Vector2.zero;

            // P1 Header Row (Role Pill + Fighter Name + Numeric HP)
            GameObject p1BadgeObj = CreateSlicedPanel(p1Block.transform, "P1RoleBadge", sprites.roundedCard, MatchSettings.GetAccentColor(FighterId.Tanjiro), false);
            RectTransform p1BadgeRect = p1BadgeObj.GetComponent<RectTransform>();
            p1BadgeRect.anchorMin = new Vector2(0f, 1f);
            p1BadgeRect.anchorMax = new Vector2(0f, 1f);
            p1BadgeRect.pivot = new Vector2(0f, 1f);
            p1BadgeRect.anchoredPosition = new Vector2(0f, -4f);
            p1BadgeRect.sizeDelta = new Vector2(68f, 30f);
            UnityEngine.UI.Image p1RoleBadgeBg = p1BadgeObj.GetComponent<UnityEngine.UI.Image>();

            TextMeshProUGUI p1RoleBadgeText = CreateTMPText(p1BadgeObj.transform, "P1RoleBadgeText", "P1", 17, TextAlignmentOptions.Center, Color.white, font);
            p1RoleBadgeText.fontStyle = FontStyles.Bold;
            StretchFull(p1RoleBadgeText.rectTransform);

            TextMeshProUGUI p1NameText = CreateTMPText(p1Block.transform, "P1NameText", "TANJIRO", 30, TextAlignmentOptions.Left, Color.white, font);
            p1NameText.fontStyle = FontStyles.Bold;
            p1NameText.characterSpacing = 3f;
            AddTextShadow(p1NameText.gameObject);
            RectTransform p1NameRect = p1NameText.rectTransform;
            p1NameRect.anchorMin = new Vector2(0f, 1f);
            p1NameRect.anchorMax = new Vector2(0.8f, 1f);
            p1NameRect.pivot = new Vector2(0f, 1f);
            p1NameRect.anchoredPosition = new Vector2(82f, -1f);
            p1NameRect.sizeDelta = new Vector2(0f, 36f);

            TextMeshProUGUI p1HpValText = CreateTMPText(p1Block.transform, "P1HealthValueText", "100", 24, TextAlignmentOptions.Right, new Color(0.92f, 0.95f, 1f, 0.92f), font);
            p1HpValText.fontStyle = FontStyles.Bold;
            AddTextShadow(p1HpValText.gameObject);
            RectTransform p1HpValRect = p1HpValText.rectTransform;
            p1HpValRect.anchorMin = new Vector2(0.8f, 1f);
            p1HpValRect.anchorMax = new Vector2(1f, 1f);
            p1HpValRect.pivot = new Vector2(1f, 1f);
            p1HpValRect.anchoredPosition = new Vector2(-4f, -4f);
            p1HpValRect.sizeDelta = new Vector2(0f, 32f);

            // P1 Glossy Health Bar
            GameObject p1BarBg = CreateSlicedPanel(p1Block.transform, "P1HealthBarBG", sprites.roundedCard, obsidianBg, false);
            SetTopStretch(p1BarBg.GetComponent<RectTransform>(), -44f, 40f);

            UnityEngine.UI.Image p1Trail = CreateFilledBar(p1BarBg.transform, "P1DamageTrailFill", sprites.healthBarGloss, new Color(1f, 0.80f, 0.22f, 1f), UnityEngine.UI.Image.OriginHorizontal.Right);
            UnityEngine.UI.Image p1Fill = CreateFilledBar(p1BarBg.transform, "P1HealthFill", sprites.healthBarGloss, MatchSettings.GetAccentColor(FighterId.Tanjiro), UnityEngine.UI.Image.OriginHorizontal.Right);
            UnityEngine.UI.Image p1BarBorder = CreateOverlayBorder(p1BarBg.transform, "P1BarBorder", sprites.roundedBorder, MatchSettings.GetAccentColor(FighterId.Tanjiro));

            UnityEngine.UI.Image[] p1Pips = CreateRoundPips(p1Block.transform, "P1RoundPips", sprites.diamondPip, alignRight: true);

            // --- CENTER TIMER MEDALLION ---
            GameObject centerBlock = CreateSlicedPanel(topBar.transform, "CenterTimerMedallion", sprites.roundedCard, obsidianBg, false);
            RectTransform centerRect = centerBlock.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.445f, 0.04f);
            centerRect.anchorMax = new Vector2(0.555f, 1f);
            centerRect.offsetMin = Vector2.zero;
            centerRect.offsetMax = Vector2.zero;
            CreateOverlayBorder(centerBlock.transform, "TimerMedallionBorder", sprites.roundedBorder, goldAccent);

            // Top golden trim accent inside timer medallion
            GameObject timerTopTrim = CreatePanel(centerBlock.transform, "TimerTopTrim", sprites.white, goldAccent, false);
            SetTopStretch(timerTopTrim.GetComponent<RectTransform>(), -4f, 3f);
            timerTopTrim.GetComponent<RectTransform>().sizeDelta = new Vector2(-24f, 3f);

            TextMeshProUGUI roundInfoText = CreateTMPText(centerBlock.transform, "RoundInfoText", "ROUND 1", 17, TextAlignmentOptions.Center, goldAccent, font);
            roundInfoText.fontStyle = FontStyles.Bold;
            roundInfoText.characterSpacing = 3f;
            SetTopStretch(roundInfoText.rectTransform, -12f, 24f);

            TextMeshProUGUI timerText = CreateTMPText(centerBlock.transform, "TimerText", "99", 58, TextAlignmentOptions.Center, Color.white, font);
            timerText.fontStyle = FontStyles.Bold;
            AddTextShadow(timerText.gameObject);
            SetTopStretch(timerText.rectTransform, -36f, 72f);

            TextMeshProUGUI bo3Label = CreateTMPText(centerBlock.transform, "BestOfLabel", "MEJOR DE 3", 12, TextAlignmentOptions.Center, new Color(0.66f, 0.72f, 0.84f, 0.85f), font);
            bo3Label.characterSpacing = 2f;
            SetTopStretch(bo3Label.rectTransform, -112f, 20f);

            // --- PLAYER 2 / CPU (RIGHT) ---
            GameObject p2Block = new GameObject("P2HealthBlock", typeof(RectTransform));
            p2Block.transform.SetParent(topBar.transform, false);
            RectTransform p2Rect = p2Block.GetComponent<RectTransform>();
            p2Rect.anchorMin = new Vector2(0.565f, 0f);
            p2Rect.anchorMax = new Vector2(1f, 1f);
            p2Rect.offsetMin = Vector2.zero;
            p2Rect.offsetMax = Vector2.zero;

            TextMeshProUGUI p2HpValText = CreateTMPText(p2Block.transform, "P2HealthValueText", "100", 24, TextAlignmentOptions.Left, new Color(0.92f, 0.95f, 1f, 0.92f), font);
            p2HpValText.fontStyle = FontStyles.Bold;
            AddTextShadow(p2HpValText.gameObject);
            RectTransform p2HpValRect = p2HpValText.rectTransform;
            p2HpValRect.anchorMin = new Vector2(0f, 1f);
            p2HpValRect.anchorMax = new Vector2(0.2f, 1f);
            p2HpValRect.pivot = new Vector2(0f, 1f);
            p2HpValRect.anchoredPosition = new Vector2(4f, -4f);
            p2HpValRect.sizeDelta = new Vector2(0f, 32f);

            GameObject p2BadgeObj = CreateSlicedPanel(p2Block.transform, "P2RoleBadge", sprites.roundedCard, MatchSettings.GetAccentColor(FighterId.Madara), false);
            RectTransform p2BadgeRect = p2BadgeObj.GetComponent<RectTransform>();
            p2BadgeRect.anchorMin = new Vector2(1f, 1f);
            p2BadgeRect.anchorMax = new Vector2(1f, 1f);
            p2BadgeRect.pivot = new Vector2(1f, 1f);
            p2BadgeRect.anchoredPosition = new Vector2(0f, -4f);
            p2BadgeRect.sizeDelta = new Vector2(168f, 30f);
            UnityEngine.UI.Image p2RoleBadgeBg = p2BadgeObj.GetComponent<UnityEngine.UI.Image>();

            TextMeshProUGUI p2RoleBadgeText = CreateTMPText(p2BadgeObj.transform, "P2RoleBadgeText", "CPU · NORMAL", 15, TextAlignmentOptions.Center, Color.white, font);
            p2RoleBadgeText.fontStyle = FontStyles.Bold;
            StretchFull(p2RoleBadgeText.rectTransform);

            TextMeshProUGUI p2NameText = CreateTMPText(p2Block.transform, "P2NameText", "MADARA", 30, TextAlignmentOptions.Right, Color.white, font);
            p2NameText.fontStyle = FontStyles.Bold;
            p2NameText.characterSpacing = 3f;
            AddTextShadow(p2NameText.gameObject);
            RectTransform p2NameRect = p2NameText.rectTransform;
            p2NameRect.anchorMin = new Vector2(0.2f, 1f);
            p2NameRect.anchorMax = new Vector2(1f, 1f);
            p2NameRect.pivot = new Vector2(1f, 1f);
            p2NameRect.anchoredPosition = new Vector2(-182f, -1f);
            p2NameRect.sizeDelta = new Vector2(0f, 36f);

            // P2 Glossy Health Bar
            GameObject p2BarBg = CreateSlicedPanel(p2Block.transform, "P2HealthBarBG", sprites.roundedCard, obsidianBg, false);
            SetTopStretch(p2BarBg.GetComponent<RectTransform>(), -44f, 40f);

            UnityEngine.UI.Image p2Trail = CreateFilledBar(p2BarBg.transform, "P2DamageTrailFill", sprites.healthBarGloss, new Color(1f, 0.80f, 0.22f, 1f), UnityEngine.UI.Image.OriginHorizontal.Left);
            UnityEngine.UI.Image p2Fill = CreateFilledBar(p2BarBg.transform, "P2HealthFill", sprites.healthBarGloss, MatchSettings.GetAccentColor(FighterId.Madara), UnityEngine.UI.Image.OriginHorizontal.Left);
            UnityEngine.UI.Image p2BarBorder = CreateOverlayBorder(p2BarBg.transform, "P2BarBorder", sprites.roundedBorder, MatchSettings.GetAccentColor(FighterId.Madara));

            UnityEngine.UI.Image[] p2Pips = CreateRoundPips(p2Block.transform, "P2RoundPips", sprites.diamondPip, alignRight: false);

            // === Bottom Subtle Controls Ribbon ===
            GameObject controlsRibbon = CreatePanel(hudRoot.transform, "BottomControlsRibbon", sprites.horizontalSlash, new Color(0.05f, 0.06f, 0.10f, 0.62f), false);
            RectTransform ribbonRect = controlsRibbon.GetComponent<RectTransform>();
            ribbonRect.anchorMin = new Vector2(0.12f, 0f);
            ribbonRect.anchorMax = new Vector2(0.88f, 0f);
            ribbonRect.pivot = new Vector2(0.5f, 0f);
            ribbonRect.anchoredPosition = new Vector2(0f, 12f);
            ribbonRect.sizeDelta = new Vector2(0f, 34f);

            TextMeshProUGUI hudControlsText = CreateTMPText(controlsRibbon.transform, "HUDControlsText", "P1: [A/D] Mover • [S] Bloqueo • [F] Jab Cross • [G] Patada Fuerte", 16, TextAlignmentOptions.Center, new Color(0.82f, 0.86f, 0.94f, 0.88f), font);
            StretchFull(hudControlsText.rectTransform);

            // === Feathered Center Announcement Slash Banner ===
            GameObject announcementBox = CreatePanel(hudRoot.transform, "AnnouncementBanner", sprites.horizontalSlash, new Color(0.04f, 0.05f, 0.09f, 0.88f), false);
            CanvasGroup annCanvasGroup = announcementBox.AddComponent<CanvasGroup>();
            RectTransform annRect = announcementBox.GetComponent<RectTransform>();
            annRect.anchorMin = new Vector2(0f, 0.5f);
            annRect.anchorMax = new Vector2(1f, 0.5f);
            annRect.pivot = new Vector2(0.5f, 0.5f);
            annRect.sizeDelta = new Vector2(0f, 170f);

            GameObject annTopLine = CreatePanel(announcementBox.transform, "TopAccentLine", sprites.horizontalSlash, goldAccent, false);
            SetTopStretch(annTopLine.GetComponent<RectTransform>(), 0f, 3f);

            GameObject annBotLine = CreatePanel(announcementBox.transform, "BottomAccentLine", sprites.horizontalSlash, goldAccent, false);
            RectTransform botLineRect = annBotLine.GetComponent<RectTransform>();
            botLineRect.anchorMin = new Vector2(0f, 0f);
            botLineRect.anchorMax = new Vector2(1f, 0f);
            botLineRect.pivot = new Vector2(0.5f, 0f);
            botLineRect.anchoredPosition = Vector2.zero;
            botLineRect.sizeDelta = new Vector2(0f, 3f);

            TextMeshProUGUI announcementText = CreateTMPText(announcementBox.transform, "AnnouncementText", "ROUND 1", 82, TextAlignmentOptions.Center, Color.white, font);
            announcementText.fontStyle = FontStyles.Bold;
            announcementText.characterSpacing = 6f;
            AddTextShadow(announcementText.gameObject, new Vector2(4f, -4f));
            StretchFull(announcementText.rectTransform);
            announcementBox.SetActive(false);

            // === Victory Modal Overlay ===
            GameObject victoryOverlay = CreatePanel(hudRoot.transform, "VictoryPanel", sprites.vignette, new Color(0.03f, 0.04f, 0.08f, 0.86f), true);
            StretchFull(victoryOverlay.GetComponent<RectTransform>());

            GameObject victoryCard = CreateSlicedPanel(victoryOverlay.transform, "VictoryCard", sprites.roundedCard, new Color(0.08f, 0.10f, 0.16f, 0.97f), false);
            RectTransform vCardRect = victoryCard.GetComponent<RectTransform>();
            PlaceAnchored(vCardRect, Vector2.zero, new Vector2(820f, 430f));
            CreateOverlayBorder(victoryCard.transform, "VictoryCardBorder", sprites.roundedBorder, goldAccent);

            GameObject vTopAccentObj = CreatePanel(victoryCard.transform, "VictoryTopAccent", sprites.white, MatchSettings.GetAccentColor(FighterId.Tanjiro), false);
            SetTopStretch(vTopAccentObj.GetComponent<RectTransform>(), -4f, 6f);
            vTopAccentObj.GetComponent<RectTransform>().sizeDelta = new Vector2(-24f, 6f);
            UnityEngine.UI.Image vTopAccent = vTopAccentObj.GetComponent<UnityEngine.UI.Image>();

            TextMeshProUGUI vEyebrow = CreateTMPText(victoryCard.transform, "VictoryEyebrow", "COMBATE FINALIZADO", 18, TextAlignmentOptions.Center, goldAccent, font);
            vEyebrow.fontStyle = FontStyles.Bold;
            vEyebrow.characterSpacing = 6f;
            SetTopStretch(vEyebrow.rectTransform, -32f, 28f);

            TextMeshProUGUI victoryTitle = CreateTMPText(victoryCard.transform, "VictoryTitleText", "¡TANJIRO GANA!", 58, TextAlignmentOptions.Center, Color.white, font);
            victoryTitle.fontStyle = FontStyles.Bold;
            victoryTitle.characterSpacing = 3f;
            AddTextShadow(victoryTitle.gameObject, new Vector2(3f, -3f));
            SetTopStretch(victoryTitle.rectTransform, -72f, 82f);

            TextMeshProUGUI victorySub = CreateTMPText(victoryCard.transform, "VictorySubtitleText", "VICTORIA PARA JUGADOR 1  (2 - 0)", 24, TextAlignmentOptions.Center, new Color(0.84f, 0.88f, 0.96f, 1f), font);
            victorySub.characterSpacing = 2f;
            SetTopStretch(victorySub.rectTransform, -166f, 42f);

            GameObject vDivider = CreatePanel(victoryCard.transform, "VictoryDivider", sprites.horizontalSlash, new Color(1f, 1f, 1f, 0.18f), false);
            PlaceAnchored(vDivider.GetComponent<RectTransform>(), new Vector2(0f, -10f), new Vector2(620f, 2f));

            UnityEngine.UI.Button rematchBtn = CreateStyledButton(
                victoryCard.transform, "RematchButton", "REVANCHA",
                sprites, font, new Color(0.84f, 0.20f, 0.16f, 1f), goldAccent, 28,
                new Vector2(-180f, -102f), new Vector2(310f, 76f), out _, pulseIdle: true);

            UnityEngine.UI.Button backMenuBtn = CreateStyledButton(
                victoryCard.transform, "BackToMenuButton", "VOLVER AL MENÚ",
                sprites, font, new Color(0.15f, 0.18f, 0.26f, 1f), new Color(0.45f, 0.52f, 0.66f, 0.8f), 24,
                new Vector2(180f, -102f), new Vector2(310f, 76f), out _, pulseIdle: false);

            victoryOverlay.SetActive(false);

            // Wire UIManager fields
            SerializedObject uiSO = new SerializedObject(uiManager);
            uiSO.FindProperty("hudRoot").objectReferenceValue = hudRoot;
            uiSO.FindProperty("victoryPanel").objectReferenceValue = victoryOverlay;
            uiSO.FindProperty("victoryCardRect").objectReferenceValue = vCardRect;

            uiSO.FindProperty("p1NameText").objectReferenceValue = p1NameText;
            uiSO.FindProperty("p1RoleBadgeText").objectReferenceValue = p1RoleBadgeText;
            uiSO.FindProperty("p1RoleBadgeBg").objectReferenceValue = p1RoleBadgeBg;
            uiSO.FindProperty("p1HealthValueText").objectReferenceValue = p1HpValText;
            uiSO.FindProperty("p1HealthFill").objectReferenceValue = p1Fill;
            uiSO.FindProperty("p1DamageTrailFill").objectReferenceValue = p1Trail;
            uiSO.FindProperty("p1BarBorder").objectReferenceValue = p1BarBorder;
            AssignObjectArray(uiSO.FindProperty("p1RoundPips"), p1Pips);

            uiSO.FindProperty("p2NameText").objectReferenceValue = p2NameText;
            uiSO.FindProperty("p2RoleBadgeText").objectReferenceValue = p2RoleBadgeText;
            uiSO.FindProperty("p2RoleBadgeBg").objectReferenceValue = p2RoleBadgeBg;
            uiSO.FindProperty("p2HealthValueText").objectReferenceValue = p2HpValText;
            uiSO.FindProperty("p2HealthFill").objectReferenceValue = p2Fill;
            uiSO.FindProperty("p2DamageTrailFill").objectReferenceValue = p2Trail;
            uiSO.FindProperty("p2BarBorder").objectReferenceValue = p2BarBorder;
            AssignObjectArray(uiSO.FindProperty("p2RoundPips"), p2Pips);

            uiSO.FindProperty("timerText").objectReferenceValue = timerText;
            uiSO.FindProperty("roundInfoText").objectReferenceValue = roundInfoText;
            uiSO.FindProperty("announcementContainer").objectReferenceValue = announcementBox;
            uiSO.FindProperty("announcementCanvasGroup").objectReferenceValue = annCanvasGroup;
            uiSO.FindProperty("announcementTextRect").objectReferenceValue = announcementText.rectTransform;
            uiSO.FindProperty("announcementText").objectReferenceValue = announcementText;
            uiSO.FindProperty("hudControlsStripText").objectReferenceValue = hudControlsText;

            uiSO.FindProperty("victoryTopAccent").objectReferenceValue = vTopAccent;
            uiSO.FindProperty("victoryTitleText").objectReferenceValue = victoryTitle;
            uiSO.FindProperty("victorySubtitleText").objectReferenceValue = victorySub;
            uiSO.FindProperty("rematchButton").objectReferenceValue = rematchBtn;
            uiSO.FindProperty("backToMenuButton").objectReferenceValue = backMenuBtn;
            uiSO.ApplyModifiedPropertiesWithoutUndo();

            hudRoot.SetActive(false);
            return uiManager;
        }

        private static MenuManager BuildMainMenu(Transform canvasTransform, UISpriteBundle sprites, TMP_FontAsset font, GameManager gameManager)
        {
            Color goldAccent = new Color(0.98f, 0.74f, 0.22f, 1f);
            Color tanjiroColor = MatchSettings.GetAccentColor(FighterId.Tanjiro);
            Color madaraColor = MatchSettings.GetAccentColor(FighterId.Madara);
            Color mutedLabel = new Color(0.64f, 0.70f, 0.82f, 1f);
            Color cardIdleBg = new Color(0.11f, 0.13f, 0.19f, 0.96f);

            // Fullscreen cinematic vignette backdrop
            GameObject menuRoot = CreatePanel(canvasTransform, "MainMenuPanel", sprites.vignette, new Color(0.03f, 0.04f, 0.08f, 0.92f), true);
            StretchFull(menuRoot.GetComponent<RectTransform>());

            // Twin subtle anime aura accents along screen edges
            GameObject leftAura = CreatePanel(menuRoot.transform, "TanjiroAmbientGlow", sprites.white, new Color(tanjiroColor.r, tanjiroColor.g, tanjiroColor.b, 0.12f), false);
            RectTransform laRect = leftAura.GetComponent<RectTransform>();
            laRect.anchorMin = new Vector2(0f, 0f);
            laRect.anchorMax = new Vector2(0f, 1f);
            laRect.pivot = new Vector2(0f, 0.5f);
            laRect.sizeDelta = new Vector2(10f, 0f);

            GameObject rightAura = CreatePanel(menuRoot.transform, "MadaraAmbientGlow", sprites.white, new Color(madaraColor.r, madaraColor.g, madaraColor.b, 0.12f), false);
            RectTransform raRect = rightAura.GetComponent<RectTransform>();
            raRect.anchorMin = new Vector2(1f, 0f);
            raRect.anchorMax = new Vector2(1f, 1f);
            raRect.pivot = new Vector2(1f, 0.5f);
            raRect.sizeDelta = new Vector2(10f, 0f);

            MenuManager menuManager = canvasTransform.gameObject.AddComponent<MenuManager>();

            // Central Obsidian Glass Panel
            GameObject card = CreateSlicedPanel(menuRoot.transform, "SelectionCard", sprites.roundedCard, new Color(0.07f, 0.09f, 0.14f, 0.95f), false);
            PlaceAnchored(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1440f, 920f));
            CreateOverlayBorder(card.transform, "CardOuterBorder", sprites.roundedBorder, new Color(0.98f, 0.74f, 0.22f, 0.45f));

            // Top Gold Hairline
            GameObject topHairline = CreatePanel(card.transform, "TopGoldLine", sprites.horizontalSlash, goldAccent, false);
            SetTopStretch(topHairline.GetComponent<RectTransform>(), -4f, 3f);
            topHairline.GetComponent<RectTransform>().sizeDelta = new Vector2(-120f, 3f);

            // === Header ===
            TextMeshProUGUI eyebrow = CreateTMPText(card.transform, "EyebrowText", "ANIME ARENA FIGHTER  •  1V1", 18, TextAlignmentOptions.Center, goldAccent, font);
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.characterSpacing = 8f;
            PlaceAnchored(eyebrow.rectTransform, new Vector2(0f, 412f), new Vector2(900f, 30f));

            TextMeshProUGUI titleText = CreateTMPText(
                card.transform, "TitleText",
                "<color=#1AC78E>TANJIRO</color>   <size=72%><color=#F9BE38>VS</color></size>   <color=#E62B3A>MADARA</color>",
                62, TextAlignmentOptions.Center, Color.white, font);
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 4f;
            AddTextShadow(titleText.gameObject, new Vector2(3f, -3f));
            PlaceAnchored(titleText.rectTransform, new Vector2(0f, 356f), new Vector2(1200f, 76f));

            GameObject titleDivider = CreatePanel(card.transform, "TitleDivider", sprites.horizontalSlash, new Color(1f, 1f, 1f, 0.20f), false);
            PlaceAnchored(titleDivider.GetComponent<RectTransform>(), new Vector2(0f, 308f), new Vector2(680f, 2f));

            // === 1. Mode Switcher ===
            TextMeshProUGUI modeLabel = CreateTMPText(card.transform, "ModeLabel", "MODO DE JUEGO", 16, TextAlignmentOptions.Center, mutedLabel, font);
            modeLabel.fontStyle = FontStyles.Bold;
            modeLabel.characterSpacing = 5f;
            PlaceAnchored(modeLabel.rectTransform, new Vector2(0f, 280f), new Vector2(500f, 24f));

            GameObject modePill = CreateSlicedPanel(card.transform, "ModeSwitcherPill", sprites.roundedCard, new Color(0.04f, 0.05f, 0.09f, 0.92f), false);
            PlaceAnchored(modePill.GetComponent<RectTransform>(), new Vector2(0f, 230f), new Vector2(736f, 68f));

            UnityEngine.UI.Button modeCpuBtn = CreateStyledButton(
                modePill.transform, "ModeVsCPUButton", "1P   VS   CPU",
                sprites, font, cardIdleBg, goldAccent, 24,
                new Vector2(-180f, 0f), new Vector2(350f, 54f), out UnityEngine.UI.Image modeCpuBorder, false);

            UnityEngine.UI.Button mode2PBtn = CreateStyledButton(
                modePill.transform, "ModeLocal2PButton", "1P   VS   2P   (LOCAL)",
                sprites, font, cardIdleBg, new Color(0.24f, 0.28f, 0.38f, 0.55f), 24,
                new Vector2(180f, 0f), new Vector2(350f, 54f), out UnityEngine.UI.Image mode2PBorder, false);

            // === 2. Fighter Selection Cards (Left P1 | VS Emblem | Right P2/CPU) ===
            // Left P1 Header + Role Badge
            GameObject p1HeaderRow = new GameObject("P1HeaderRow", typeof(RectTransform));
            p1HeaderRow.transform.SetParent(card.transform, false);
            PlaceAnchored(p1HeaderRow.GetComponent<RectTransform>(), new Vector2(-360f, 152f), new Vector2(560f, 36f));

            GameObject p1TagBg = CreateSlicedPanel(p1HeaderRow.transform, "P1TagBg", sprites.roundedCard, tanjiroColor, false);
            PlaceAnchored(p1TagBg.GetComponent<RectTransform>(), new Vector2(-110f, 0f), new Vector2(56f, 28f));
            TextMeshProUGUI p1TagTxt = CreateTMPText(p1TagBg.transform, "P1TagText", "P1", 16, TextAlignmentOptions.Center, Color.white, font);
            p1TagTxt.fontStyle = FontStyles.Bold;
            StretchFull(p1TagTxt.rectTransform);

            TextMeshProUGUI p1Header = CreateTMPText(p1HeaderRow.transform, "P1SelectHeader", "JUGADOR 1", 22, TextAlignmentOptions.Left, Color.white, font);
            p1Header.fontStyle = FontStyles.Bold;
            p1Header.characterSpacing = 3f;
            PlaceAnchored(p1Header.rectTransform, new Vector2(30f, 0f), new Vector2(200f, 34f));

            UnityEngine.UI.Button p1TanjiroBtn = CreateFighterCardButton(
                card.transform, "P1TanjiroButton", "TANJIRO", "KAMADO  •  AGUA",
                sprites, font, tanjiroColor, new Vector2(-505f, 60f), new Vector2(272f, 134f), out UnityEngine.UI.Image p1TanjiroBorder);

            UnityEngine.UI.Button p1MadaraBtn = CreateFighterCardButton(
                card.transform, "P1MadaraButton", "MADARA", "UCHIHA  •  SHARINGAN",
                sprites, font, madaraColor, new Vector2(-215f, 60f), new Vector2(272f, 134f), out UnityEngine.UI.Image p1MadaraBorder);

            // Center VS Diamond Emblem
            GameObject vsContainer = new GameObject("VSEmblem", typeof(RectTransform));
            vsContainer.transform.SetParent(card.transform, false);
            PlaceAnchored(vsContainer.GetComponent<RectTransform>(), new Vector2(0f, 60f), new Vector2(96f, 96f));

            GameObject vsDiamond = CreatePanel(vsContainer.transform, "VSDiamond", sprites.diamondPip, new Color(0.11f, 0.13f, 0.20f, 1f), false);
            PlaceAnchored(vsDiamond.GetComponent<RectTransform>(), Vector2.zero, new Vector2(88f, 88f));

            TextMeshProUGUI vsText = CreateTMPText(vsContainer.transform, "VSText", "VS", 34, TextAlignmentOptions.Center, goldAccent, font);
            vsText.fontStyle = FontStyles.Bold;
            AddTextShadow(vsText.gameObject);
            StretchFull(vsText.rectTransform);

            // Right P2/CPU Header + Role Badge
            GameObject p2HeaderRow = new GameObject("P2HeaderRow", typeof(RectTransform));
            p2HeaderRow.transform.SetParent(card.transform, false);
            PlaceAnchored(p2HeaderRow.GetComponent<RectTransform>(), new Vector2(360f, 152f), new Vector2(560f, 36f));

            TextMeshProUGUI p2Header = CreateTMPText(p2HeaderRow.transform, "P2SelectHeader", "OPONENTE CPU", 22, TextAlignmentOptions.Right, Color.white, font);
            p2Header.fontStyle = FontStyles.Bold;
            p2Header.characterSpacing = 3f;
            PlaceAnchored(p2Header.rectTransform, new Vector2(-42f, 0f), new Vector2(260f, 34f));

            GameObject p2TagBg = CreateSlicedPanel(p2HeaderRow.transform, "P2TagBg", sprites.roundedCard, madaraColor, false);
            PlaceAnchored(p2TagBg.GetComponent<RectTransform>(), new Vector2(125f, 0f), new Vector2(64f, 28f));
            TextMeshProUGUI p2RoleBadgeTxt = CreateTMPText(p2TagBg.transform, "P2RoleBadgeText", "CPU", 16, TextAlignmentOptions.Center, Color.white, font);
            p2RoleBadgeTxt.fontStyle = FontStyles.Bold;
            StretchFull(p2RoleBadgeTxt.rectTransform);

            UnityEngine.UI.Button p2TanjiroBtn = CreateFighterCardButton(
                card.transform, "P2TanjiroButton", "TANJIRO", "KAMADO  •  AGUA",
                sprites, font, tanjiroColor, new Vector2(215f, 60f), new Vector2(272f, 134f), out UnityEngine.UI.Image p2TanjiroBorder);

            UnityEngine.UI.Button p2MadaraBtn = CreateFighterCardButton(
                card.transform, "P2MadaraButton", "MADARA", "UCHIHA  •  SHARINGAN",
                sprites, font, madaraColor, new Vector2(505f, 60f), new Vector2(272f, 134f), out UnityEngine.UI.Image p2MadaraBorder);

            // === 3. AI Difficulty Selector ===
            GameObject diffSection = new GameObject("DifficultySection", typeof(RectTransform));
            diffSection.transform.SetParent(card.transform, false);
            PlaceAnchored(diffSection.GetComponent<RectTransform>(), new Vector2(0f, -84f), new Vector2(820f, 102f));

            TextMeshProUGUI diffLabel = CreateTMPText(diffSection.transform, "DifficultyLabel", "DIFICULTAD DE LA CPU", 16, TextAlignmentOptions.Center, mutedLabel, font);
            diffLabel.fontStyle = FontStyles.Bold;
            diffLabel.characterSpacing = 5f;
            PlaceAnchored(diffLabel.rectTransform, new Vector2(0f, 34f), new Vector2(500f, 24f));

            GameObject diffPill = CreateSlicedPanel(diffSection.transform, "DifficultyPill", sprites.roundedCard, new Color(0.04f, 0.05f, 0.09f, 0.92f), false);
            PlaceAnchored(diffPill.GetComponent<RectTransform>(), new Vector2(0f, -14f), new Vector2(720f, 62f));

            UnityEngine.UI.Button diffEasyBtn = CreateStyledButton(
                diffPill.transform, "DiffEasyButton", "FÁCIL",
                sprites, font, cardIdleBg, new Color(0.24f, 0.28f, 0.38f, 0.55f), 22,
                new Vector2(-232f, 0f), new Vector2(220f, 48f), out UnityEngine.UI.Image diffEasyBorder, false);

            UnityEngine.UI.Button diffNormalBtn = CreateStyledButton(
                diffPill.transform, "DiffNormalButton", "NORMAL",
                sprites, font, cardIdleBg, goldAccent, 22,
                new Vector2(0f, 0f), new Vector2(220f, 48f), out UnityEngine.UI.Image diffNormalBorder, false);

            UnityEngine.UI.Button diffHardBtn = CreateStyledButton(
                diffPill.transform, "DiffHardButton", "DIFÍCIL",
                sprites, font, cardIdleBg, new Color(0.24f, 0.28f, 0.38f, 0.55f), 22,
                new Vector2(232f, 0f), new Vector2(220f, 48f), out UnityEngine.UI.Image diffHardBorder, false);

            // === 4. Matchup Preview Strip & Pulsing Fight CTA ===
            GameObject matchupStrip = CreatePanel(card.transform, "MatchupPreviewStrip", sprites.horizontalSlash, new Color(0.04f, 0.05f, 0.09f, 0.85f), false);
            PlaceAnchored(matchupStrip.GetComponent<RectTransform>(), new Vector2(0f, -178f), new Vector2(1060f, 46f));

            TextMeshProUGUI matchupP1 = CreateTMPText(matchupStrip.transform, "MatchupP1Text", "TANJIRO  [P1]", 24, TextAlignmentOptions.Right, tanjiroColor, font);
            matchupP1.fontStyle = FontStyles.Bold;
            PlaceAnchored(matchupP1.rectTransform, new Vector2(-245f, 0f), new Vector2(420f, 38f));

            TextMeshProUGUI matchupMid = CreateTMPText(matchupStrip.transform, "MatchupMidVS", "VS", 22, TextAlignmentOptions.Center, goldAccent, font);
            matchupMid.fontStyle = FontStyles.Bold;
            PlaceAnchored(matchupMid.rectTransform, Vector2.zero, new Vector2(70f, 38f));

            TextMeshProUGUI matchupP2 = CreateTMPText(matchupStrip.transform, "MatchupP2Text", "[CPU · NORMAL]  MADARA", 24, TextAlignmentOptions.Left, madaraColor, font);
            matchupP2.fontStyle = FontStyles.Bold;
            PlaceAnchored(matchupP2.rectTransform, new Vector2(245f, 0f), new Vector2(420f, 38f));

            UnityEngine.UI.Button fightBtn = CreateStyledButton(
                card.transform, "FightButton", "¡PELEAR!",
                sprites, font, new Color(0.84f, 0.16f, 0.16f, 1f), goldAccent, 40,
                new Vector2(0f, -272f), new Vector2(440f, 86f), out _, pulseIdle: true);

            // === 5. Footer Controls Pill ===
            GameObject controlsBg = CreateSlicedPanel(card.transform, "ControlsFooterBg", sprites.roundedCard, new Color(0.04f, 0.05f, 0.09f, 0.88f), false);
            PlaceAnchored(controlsBg.GetComponent<RectTransform>(), new Vector2(0f, -388f), new Vector2(1320f, 54f));
            CreateOverlayBorder(controlsBg.transform, "ControlsFooterBorder", sprites.roundedBorder, new Color(0.32f, 0.38f, 0.50f, 0.35f));

            TextMeshProUGUI controlsText = CreateTMPText(
                controlsBg.transform, "ControlsHintText",
                "CONTROLES P1:   [A / D] Mover   •   [S] Bloqueo   •   [F] Jab Cross   •   [G] Patada Fuerte (Derribo)",
                18, TextAlignmentOptions.Center, new Color(0.76f, 0.82f, 0.92f, 1f), font);
            StretchFull(controlsText.rectTransform);

            // Wire MenuManager serialized fields
            SerializedObject menuSO = new SerializedObject(menuManager);
            menuSO.FindProperty("menuRoot").objectReferenceValue = menuRoot;
            menuSO.FindProperty("gameManager").objectReferenceValue = gameManager;

            menuSO.FindProperty("modeVsCpuButton").objectReferenceValue = modeCpuBtn;
            menuSO.FindProperty("modeVsCpuBorder").objectReferenceValue = modeCpuBorder;
            menuSO.FindProperty("modeLocal2PButton").objectReferenceValue = mode2PBtn;
            menuSO.FindProperty("modeLocal2PBorder").objectReferenceValue = mode2PBorder;

            menuSO.FindProperty("p1TanjiroButton").objectReferenceValue = p1TanjiroBtn;
            menuSO.FindProperty("p1TanjiroBorder").objectReferenceValue = p1TanjiroBorder;
            menuSO.FindProperty("p1MadaraButton").objectReferenceValue = p1MadaraBtn;
            menuSO.FindProperty("p1MadaraBorder").objectReferenceValue = p1MadaraBorder;

            menuSO.FindProperty("p2SectionTitleText").objectReferenceValue = p2Header;
            menuSO.FindProperty("p2RoleBadgeText").objectReferenceValue = p2RoleBadgeTxt;
            menuSO.FindProperty("p2TanjiroButton").objectReferenceValue = p2TanjiroBtn;
            menuSO.FindProperty("p2TanjiroBorder").objectReferenceValue = p2TanjiroBorder;
            menuSO.FindProperty("p2MadaraButton").objectReferenceValue = p2MadaraBtn;
            menuSO.FindProperty("p2MadaraBorder").objectReferenceValue = p2MadaraBorder;

            menuSO.FindProperty("difficultySection").objectReferenceValue = diffSection;
            menuSO.FindProperty("diffEasyButton").objectReferenceValue = diffEasyBtn;
            menuSO.FindProperty("diffEasyBorder").objectReferenceValue = diffEasyBorder;
            menuSO.FindProperty("diffNormalButton").objectReferenceValue = diffNormalBtn;
            menuSO.FindProperty("diffNormalBorder").objectReferenceValue = diffNormalBorder;
            menuSO.FindProperty("diffHardButton").objectReferenceValue = diffHardBtn;
            menuSO.FindProperty("diffHardBorder").objectReferenceValue = diffHardBorder;

            menuSO.FindProperty("matchupP1Text").objectReferenceValue = matchupP1;
            menuSO.FindProperty("matchupP2Text").objectReferenceValue = matchupP2;
            menuSO.FindProperty("controlsHintText").objectReferenceValue = controlsText;
            menuSO.FindProperty("fightButton").objectReferenceValue = fightBtn;
            menuSO.ApplyModifiedPropertiesWithoutUndo();

            return menuManager;
        }

        // =====================================================================
        // UI & PROCEDURAL SPRITE HELPERS
        // =====================================================================

        private static GameObject CreatePanel(Transform parent, string name, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            UnityEngine.UI.Image img = obj.AddComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycastTarget;
            return obj;
        }

        private static GameObject CreateSlicedPanel(Transform parent, string name, Sprite slicedSprite, Color color, bool raycastTarget)
        {
            GameObject obj = CreatePanel(parent, name, slicedSprite, color, raycastTarget);
            UnityEngine.UI.Image img = obj.GetComponent<UnityEngine.UI.Image>();
            img.type = UnityEngine.UI.Image.Type.Sliced;
            return obj;
        }

        private static UnityEngine.UI.Image CreateOverlayBorder(Transform parent, string name, Sprite borderSprite, Color color)
        {
            GameObject obj = CreateSlicedPanel(parent, name, borderSprite, color, false);
            StretchFull(obj.GetComponent<RectTransform>());
            return obj.GetComponent<UnityEngine.UI.Image>();
        }

        private static UnityEngine.UI.Image CreateFilledBar(
            Transform parent,
            string name,
            Sprite barSprite,
            Color color,
            UnityEngine.UI.Image.OriginHorizontal origin)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(5f, 5f);
            rt.offsetMax = new Vector2(-5f, -5f);

            UnityEngine.UI.Image img = obj.AddComponent<UnityEngine.UI.Image>();
            img.sprite = barSprite;
            img.type = UnityEngine.UI.Image.Type.Filled;
            img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            img.fillOrigin = (int)origin;
            img.fillAmount = 1f;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static UnityEngine.UI.Image[] CreateRoundPips(Transform parent, string containerName, Sprite diamondSprite, bool alignRight)
        {
            GameObject container = new GameObject(containerName, typeof(RectTransform));
            container.transform.SetParent(parent, false);
            RectTransform cRect = container.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(alignRight ? 1f : 0f, 1f);
            cRect.anchorMax = new Vector2(alignRight ? 1f : 0f, 1f);
            cRect.pivot = new Vector2(alignRight ? 1f : 0f, 1f);
            cRect.anchoredPosition = new Vector2(alignRight ? -4f : 4f, -94f);
            cRect.sizeDelta = new Vector2(120f, 34f);

            var pips = new UnityEngine.UI.Image[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject pipObj = CreatePanel(container.transform, $"RoundPip_{i + 1}", diamondSprite, new Color(0.18f, 0.20f, 0.28f, 0.90f), false);
                RectTransform pRect = pipObj.GetComponent<RectTransform>();
                pRect.anchorMin = new Vector2(alignRight ? 1f : 0f, 0.5f);
                pRect.anchorMax = new Vector2(alignRight ? 1f : 0f, 0.5f);
                pRect.pivot = new Vector2(alignRight ? 1f : 0f, 0.5f);
                float offset = i * 38f;
                pRect.anchoredPosition = new Vector2(alignRight ? -offset : offset, 0f);
                pRect.sizeDelta = new Vector2(30f, 30f);
                pips[i] = pipObj.GetComponent<UnityEngine.UI.Image>();
            }
            return pips;
        }

        private static UnityEngine.UI.Button CreateStyledButton(
            Transform parent,
            string name,
            string label,
            UISpriteBundle sprites,
            TMP_FontAsset font,
            Color bgColor,
            Color borderColor,
            int fontSize,
            Vector2 anchoredPos,
            Vector2 size,
            out UnityEngine.UI.Image borderImage,
            bool pulseIdle)
        {
            GameObject obj = CreateSlicedPanel(parent, name, sprites.roundedCard, bgColor, true);
            PlaceAnchored(obj.GetComponent<RectTransform>(), anchoredPos, size);

            borderImage = CreateOverlayBorder(obj.transform, "Border", sprites.roundedBorder, borderColor);

            UnityEngine.UI.Button btn = obj.AddComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f, 1f);
            colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            btn.colors = colors;

            UIButtonAnimator anim = obj.AddComponent<UIButtonAnimator>();
            anim.SetIdlePulse(pulseIdle);

            TextMeshProUGUI txt = CreateTMPText(obj.transform, "Label", label, fontSize, TextAlignmentOptions.Center, Color.white, font);
            txt.fontStyle = FontStyles.Bold;
            txt.characterSpacing = 2.5f;
            AddTextShadow(txt.gameObject);
            StretchFull(txt.rectTransform);
            return btn;
        }

        private static UnityEngine.UI.Button CreateFighterCardButton(
            Transform parent,
            string name,
            string fighterName,
            string subtitle,
            UISpriteBundle sprites,
            TMP_FontAsset font,
            Color accentColor,
            Vector2 anchoredPos,
            Vector2 size,
            out UnityEngine.UI.Image borderImage)
        {
            GameObject obj = CreateSlicedPanel(parent, name, sprites.roundedCard, new Color(0.11f, 0.13f, 0.19f, 0.96f), true);
            PlaceAnchored(obj.GetComponent<RectTransform>(), anchoredPos, size);

            // Colored left vertical fighter emblem stripe
            GameObject stripe = CreatePanel(obj.transform, "AccentStripe", sprites.white, accentColor, false);
            RectTransform sRect = stripe.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(0f, 1f);
            sRect.pivot = new Vector2(0f, 0.5f);
            sRect.anchoredPosition = new Vector2(10f, 0f);
            sRect.sizeDelta = new Vector2(6f, -26f);

            borderImage = CreateOverlayBorder(obj.transform, "SelectionBorder", sprites.roundedBorder, accentColor);

            UnityEngine.UI.Button btn = obj.AddComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            btn.colors = colors;

            obj.AddComponent<UIButtonAnimator>();

            TextMeshProUGUI titleTmp = CreateTMPText(obj.transform, "FighterName", fighterName, 30, TextAlignmentOptions.Left, Color.white, font);
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.characterSpacing = 3f;
            AddTextShadow(titleTmp.gameObject);
            PlaceAnchored(titleTmp.rectTransform, new Vector2(14f, 16f), new Vector2(size.x - 44f, 40f));

            TextMeshProUGUI subTmp = CreateTMPText(obj.transform, "FighterSubtitle", subtitle, 14, TextAlignmentOptions.Left, Color.Lerp(accentColor, Color.white, 0.45f), font);
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.characterSpacing = 1.5f;
            PlaceAnchored(subTmp.rectTransform, new Vector2(14f, -22f), new Vector2(size.x - 44f, 26f));

            return btn;
        }

        private static void AddTextShadow(GameObject target, Vector2? distance = null)
        {
            // TextMeshPro uses SDF rendering; keep clean without legacy UI.Shadow overhead.
        }

        private static TextMeshProUGUI CreateTMPText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            TMP_FontAsset font)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            if (font != null)
            {
                tmp.font = font;
            }
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetTopStretch(RectTransform rt, float topOffsetY, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topOffsetY);
            rt.sizeDelta = new Vector2(0f, height);
        }

        private static void PlaceAnchored(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void AssignObjectArray(SerializedProperty arrayProp, UnityEngine.Object[] items)
        {
            arrayProp.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static void EnsureTMPResources()
        {
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                Debug.Log("[FightingGameSetup] Importando TMP Essential Resources silenciosamente...");
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                AssetDatabase.Refresh();
            }
        }

        private static TMP_FontAsset FindDefaultTMPFont()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                string[] localGuids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/TextMesh Pro" });
                if (localGuids.Length > 0)
                {
                    TMP_FontAsset localFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(localGuids[0]));
                    if (localFont != null) return localFont;
                }
            }

            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return null;
        }

        private static UISpriteBundle EnsureUISprites()
        {
            return new UISpriteBundle
            {
                white = EnsureGeneratedSprite(WhiteSpritePath, 4, 4, Vector4.zero, (x, y, w, h) => Color.white),
                roundedCard = EnsureGeneratedSprite(RoundedCardSpritePath, 64, 64, new Vector4(16, 16, 16, 16), GenerateRoundedRectPixel),
                roundedBorder = EnsureGeneratedSprite(RoundedBorderSpritePath, 64, 64, new Vector4(16, 16, 16, 16), GenerateRoundedBorderPixel),
                healthBarGloss = EnsureGeneratedSprite(HealthBarGlossSpritePath, 16, 64, Vector4.zero, GenerateGlossBarPixel),
                diamondPip = EnsureGeneratedSprite(DiamondPipSpritePath, 64, 64, Vector4.zero, GenerateDiamondPixel),
                horizontalSlash = EnsureGeneratedSprite(HorizontalSlashSpritePath, 256, 16, Vector4.zero, GenerateHorizontalSlashPixel),
                vignette = EnsureGeneratedSprite(VignetteSpritePath, 128, 128, Vector4.zero, GenerateVignettePixel)
            };
        }

        private static Sprite EnsureGeneratedSprite(
            string assetPath,
            int width,
            int height,
            Vector4 border,
            Func<int, int, int, int, Color> pixelFunc)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null) return existing;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = pixelFunc(x, y, width, height);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] pngData = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string fullDiskPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullDiskPath));
            File.WriteAllBytes(fullDiskPath, pngData);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static float RoundedBoxSignedDistance(float px, float py, float halfW, float halfH, float radius)
        {
            float qx = Mathf.Abs(px) - (halfW - radius);
            float qy = Mathf.Abs(py) - (halfH - radius);
            float outDist = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            float inDist = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outDist + inDist - radius;
        }

        private static Color GenerateRoundedRectPixel(int x, int y, int w, int h)
        {
            float px = x + 0.5f - w * 0.5f;
            float py = y + 0.5f - h * 0.5f;
            float d = RoundedBoxSignedDistance(px, py, w * 0.5f - 1f, h * 0.5f - 1f, 12f);
            float alpha = Mathf.Clamp01(0.5f - d);
            return new Color(1f, 1f, 1f, alpha);
        }

        private static Color GenerateRoundedBorderPixel(int x, int y, int w, int h)
        {
            float px = x + 0.5f - w * 0.5f;
            float py = y + 0.5f - h * 0.5f;
            float d = RoundedBoxSignedDistance(px, py, w * 0.5f - 1.5f, h * 0.5f - 1.5f, 12f);
            float thickness = 2.6f;
            float edgeDist = Mathf.Abs(d + thickness * 0.5f) - thickness * 0.5f;
            float alpha = Mathf.Clamp01(0.5f - edgeDist);
            return new Color(1f, 1f, 1f, alpha);
        }

        private static Color GenerateGlossBarPixel(int x, int y, int w, int h)
        {
            float ny = (float)y / (h - 1);
            // Glossy arcade highlight on upper half, rich shaded lower half
            float shade = ny > 0.54f
                ? Mathf.Lerp(1.06f, 1.18f, (ny - 0.54f) / 0.46f)
                : Mathf.Lerp(0.76f, 0.98f, ny / 0.54f);
            shade = Mathf.Clamp01(shade);
            return new Color(shade, shade, shade, 1f);
        }

        private static Color GenerateDiamondPixel(int x, int y, int w, int h)
        {
            float nx = Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f;
            float ny = Mathf.Abs((y + 0.5f) / h - 0.5f) * 2f;
            float dist = (nx + ny) - 0.90f;
            float alpha = Mathf.Clamp01(0.5f - dist * (w * 0.35f));
            float bevel = Mathf.Lerp(1.0f, 0.80f, Mathf.Clamp01(nx + ny));
            return new Color(bevel, bevel, bevel, alpha);
        }

        private static Color GenerateHorizontalSlashPixel(int x, int y, int w, int h)
        {
            float nx = Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f;
            float fade = 1f - Mathf.SmoothStep(0.65f, 1f, nx);
            return new Color(1f, 1f, 1f, fade);
        }

        private static Color GenerateVignettePixel(int x, int y, int w, int h)
        {
            float dx = ((x + 0.5f) / w - 0.5f) * 2f;
            float dy = ((y + 0.5f) / h - 0.5f) * 2f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Lerp(0.76f, 1f, Mathf.SmoothStep(0.15f, 1.25f, r));
            return new Color(1f, 1f, 1f, alpha);
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => string.Equals(s.path, scenePath, StringComparison.OrdinalIgnoreCase)))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static string[] GetFbxAssetPaths(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p)
                .ToArray();
        }

        private static string FindBaseModelFbxPath(string folderPath)
        {
            string[] fbxPaths = GetFbxAssetPaths(folderPath);
            string idlePath = fbxPaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains("fighting idle"))
                ?? fbxPaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains("idle"))
                ?? fbxPaths.FirstOrDefault();

            if (string.IsNullOrEmpty(idlePath))
            {
                throw new FileNotFoundException($"No se encontró un modelo FBX base en {folderPath}");
            }

            return idlePath;
        }

        private static AnimationClip FindClipByKeywords(string[] fbxPaths, params string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                string matchPath = fbxPaths.FirstOrDefault(p =>
                    Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains(keyword.ToLowerInvariant()));

                if (!string.IsNullOrEmpty(matchPath))
                {
                    AnimationClip clip = LoadFirstAnimationClip(matchPath);
                    if (clip != null) return clip;
                }
            }
            return null;
        }

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureDirectory(string assetDir)
        {
            if (AssetDatabase.IsValidFolder(assetDir)) return;

            string[] parts = assetDir.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
