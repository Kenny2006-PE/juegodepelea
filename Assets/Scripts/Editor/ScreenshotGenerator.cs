#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FightingGame
{
    public static class ScreenshotGenerator
    {
        private const string ScreenshotsDir = "Screenshots";

        [MenuItem("Tools/Generate Presentation Screenshots", false, 50)]
        public static void GenerateScreenshots()
        {
            Debug.Log("[ScreenshotGenerator] Iniciando generación de capturas HD para la presentación...");

            if (!Directory.Exists(ScreenshotsDir))
            {
                Directory.CreateDirectory(ScreenshotsDir);
            }

            string scenePath = "Assets/Scenes/FightingArena.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam == null)
            {
                Debug.LogError("[ScreenshotGenerator] No se encontró cámara principal.");
                return;
            }

            Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            Transform mainMenuPanel = null;
            Transform combatHUD = null;

            if (canvas != null)
            {
                mainMenuPanel = canvas.transform.Find("MainMenuPanel");
                combatHUD = canvas.transform.Find("CombatHUD");
            }

            RenderMode originalMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            Camera originalCam = canvas != null ? canvas.worldCamera : null;

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 2.4f;
            }

            Vector3 origCamPos = cam.transform.position;
            Quaternion origCamRot = cam.transform.rotation;
            float origFov = cam.fieldOfView;

            try
            {
                // =========================================================================
                // 1. CAPTURA: MENÚ PRINCIPAL PROFESIONAL (UI Completa)
                // =========================================================================
                if (canvas != null) canvas.gameObject.SetActive(true);
                if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(true);
                if (combatHUD != null) combatHUD.gameObject.SetActive(false);

                cam.transform.position = origCamPos;
                cam.transform.rotation = origCamRot;
                cam.fieldOfView = origFov;

                SaveCameraToPNG(cam, Path.Combine(ScreenshotsDir, "01_Menu_Principal.png"), 1920, 1080);
                Debug.Log("[ScreenshotGenerator] Captura 1 guardada: 01_Menu_Principal.png");

                // =========================================================================
                // PREPARACIÓN DE PERSONAJES
                // =========================================================================
                GameObject tanjiroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TanjiroFighter.prefab");
                GameObject madaraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MadaraFighter.prefab");

                GameObject tanjiroInstance = null;
                GameObject madaraInstance = null;

                if (tanjiroPrefab != null && madaraPrefab != null)
                {
                    tanjiroInstance = (GameObject)PrefabUtility.InstantiatePrefab(tanjiroPrefab);
                    tanjiroInstance.transform.position = new Vector3(-2.0f, 0f, 0f);
                    tanjiroInstance.transform.rotation = Quaternion.LookRotation(Vector3.right);

                    madaraInstance = (GameObject)PrefabUtility.InstantiatePrefab(madaraPrefab);
                    madaraInstance.transform.position = new Vector3(2.0f, 0f, 0f);
                    madaraInstance.transform.rotation = Quaternion.LookRotation(Vector3.left);
                }

                // Cargar clips de animación
                AnimationClip tanjiroIdle = GetClipFromFbx("Assets/Animations Characters/Tanjiro/Fighting Idle.fbx");
                AnimationClip madaraIdle = GetClipFromFbx("Assets/Animations Characters/Madara/Fighting Idle (1).fbx");
                AnimationClip tanjiroKick = GetClipFromFbx("Assets/Animations Characters/Tanjiro/Roundhouse Kick.fbx");
                AnimationClip madaraBlock = GetClipFromFbx("Assets/Animations Characters/Madara/Center Block (1).fbx");
                AnimationClip tanjiroJab = GetClipFromFbx("Assets/Animations Characters/Tanjiro/Jab Cross.fbx");
                AnimationClip madaraHit = GetClipFromFbx("Assets/Animations Characters/Madara/Head Hit (1).fbx");

                // Postura Idle
                SamplePose(tanjiroInstance, tanjiroIdle, 0.5f);
                SamplePose(madaraInstance, madaraIdle, 0.5f);

                // =========================================================================
                // 2. CAPTURA: ARENA DE COMBATE CON HUD ACTIVO (Cara a cara)
                // =========================================================================
                if (canvas != null) canvas.gameObject.SetActive(true);
                if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(false);
                if (combatHUD != null) combatHUD.gameObject.SetActive(true);

                cam.transform.position = new Vector3(0f, 2.3f, -5.8f);
                cam.transform.rotation = Quaternion.Euler(11f, 0f, 0f);
                cam.fieldOfView = 50f;

                SaveCameraToPNG(cam, Path.Combine(ScreenshotsDir, "02_Combate_Arena_HUD.png"), 1920, 1080);
                Debug.Log("[ScreenshotGenerator] Captura 2 guardada: 02_Combate_Arena_HUD.png");

                // =========================================================================
                // 3. CAPTURA: PRIMER PLANO CINEMATOGRÁFICO DE TANJIRO (Pose de Guardia)
                // =========================================================================
                if (canvas != null) canvas.gameObject.SetActive(false);
                cam.transform.position = new Vector3(-1.3f, 1.45f, -2.2f);
                cam.transform.rotation = Quaternion.Euler(6f, -28f, 0f);
                cam.fieldOfView = 35f;

                SaveCameraToPNG(cam, Path.Combine(ScreenshotsDir, "03_Tanjiro_CloseUp.png"), 1920, 1080);
                Debug.Log("[ScreenshotGenerator] Captura 3 guardada: 03_Tanjiro_CloseUp.png");

                // =========================================================================
                // 4. CAPTURA: PRIMER PLANO CINEMATOGRÁFICO DE MADARA (Pose de Guardia)
                // =========================================================================
                cam.transform.position = new Vector3(1.3f, 1.55f, -2.2f);
                cam.transform.rotation = Quaternion.Euler(6f, 28f, 0f);
                cam.fieldOfView = 35f;

                SaveCameraToPNG(cam, Path.Combine(ScreenshotsDir, "04_Madara_CloseUp.png"), 1920, 1080);
                Debug.Log("[ScreenshotGenerator] Captura 4 guardada: 04_Madara_CloseUp.png");

                // =========================================================================
                // 5. CAPTURA: DUELO ÉPICO - COMBATE DINÁMICO (Patada vs Bloqueo)
                // =========================================================================
                if (tanjiroInstance != null && madaraInstance != null)
                {
                    tanjiroInstance.transform.position = new Vector3(-0.95f, 0f, 0f);
                    madaraInstance.transform.position = new Vector3(0.95f, 0f, 0f);

                    SamplePose(tanjiroInstance, tanjiroKick, 0.55f);
                    SamplePose(madaraInstance, madaraBlock, 0.35f);
                }

                // Plano de combate medio-cercano dinámico
                cam.transform.position = new Vector3(0f, 1.4f, -3.3f);
                cam.transform.rotation = Quaternion.Euler(7f, 0f, 0f);
                cam.fieldOfView = 44f;

                SaveCameraToPNG(cam, Path.Combine(ScreenshotsDir, "05_Duelo_Epico_Kick_vs_Block.png"), 1920, 1080);
                Debug.Log("[ScreenshotGenerator] Captura 5 guardada: 05_Duelo_Epico_Kick_vs_Block.png");

                // =========================================================================
                // 6. CAPTURA: SISTEMA DE GOLPES (Jab Cross Impact vs Head Hit)
                // =========================================================================
                if (tanjiroInstance != null && madaraInstance != null)
                {
                    tanjiroInstance.transform.position = new Vector3(-0.75f, 0f, 0f);
                    madaraInstance.transform.position = new Vector3(0.75f, 0f, 0f);

                    SamplePose(tanjiroInstance, tanjiroJab, 0.40f);
                    SamplePose(madaraInstance, madaraHit, 0.20f);
                }

                cam.transform.position = new Vector3(0f, 1.45f, -2.8f);
                cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
                cam.fieldOfView = 40f;

                SaveCameraToPNG(cam, Path.Combine(ScreenshotsDir, "06_Impacto_Jab_Cross.png"), 1920, 1080);
                Debug.Log("[ScreenshotGenerator] Captura 6 guardada: 06_Impacto_Jab_Cross.png");

                // Limpieza de objetos instanciados
                if (tanjiroInstance != null) UnityEngine.Object.DestroyImmediate(tanjiroInstance);
                if (madaraInstance != null) UnityEngine.Object.DestroyImmediate(madaraInstance);
            }
            finally
            {
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(true);
                    canvas.renderMode = originalMode;
                    canvas.worldCamera = originalCam;
                }
                cam.transform.position = origCamPos;
                cam.transform.rotation = origCamRot;
                cam.fieldOfView = origFov;

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            Debug.Log("[ScreenshotGenerator] ¡6 Capturas HD generadas con éxito en la carpeta 'Screenshots'!");
        }

        private static AnimationClip GetClipFromFbx(string fbxPath)
        {
            if (string.IsNullOrEmpty(fbxPath) || !File.Exists(fbxPath)) return null;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static void SamplePose(GameObject target, AnimationClip clip, float time)
        {
            if (target == null || clip == null) return;
            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(target, clip, time);
                AnimationMode.StopAnimationMode();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ScreenshotGenerator] No se pudo muestrear clip {clip.name}: {ex.Message}");
            }
        }

        private static void SaveCameraToPNG(Camera cam, string outputPath, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture oldRt = cam.targetTexture;
            RenderTexture oldActive = RenderTexture.active;

            try
            {
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenShot.Apply();

                byte[] bytes = screenShot.EncodeToPNG();
                File.WriteAllBytes(outputPath, bytes);
                UnityEngine.Object.DestroyImmediate(screenShot);
            }
            finally
            {
                cam.targetTexture = oldRt;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
#endif
