using MassSpringLab.Apparatus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Wires Phase 4 Hooke's Law physics (SpringPhysicsController) onto SpringApparatus
    /// and verifies progressive extension and tabletop safety across all five masses.
    /// </summary>
    public static class Phase4PhysicsBuilder
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";

        [MenuItem("MassSpringLab/Apply Phase 4 Spring Physics")]
        public static void ApplyPhase4Physics()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject apparatus = GameObject.Find("SpringApparatus");
            if (apparatus == null)
            {
                Debug.LogError("[Phase4PhysicsBuilder] SpringApparatus not found in scene!");
                return;
            }

            Transform topAnchor = apparatus.transform.Find("SpringTopAnchor");
            Transform bottomHook = apparatus.transform.Find("SpringBottomHook");

            SpringPhysicsController physics = apparatus.GetComponent<SpringPhysicsController>();
            if (physics == null)
            {
                physics = apparatus.AddComponent<SpringPhysicsController>();
            }

            SerializedObject physicsSo = new SerializedObject(physics);
            physicsSo.FindProperty("springConstant").floatValue = 20.0f;
            physicsSo.FindProperty("gravity").floatValue = 9.81f;
            physicsSo.FindProperty("naturalLength").floatValue = 0.12f;
            physicsSo.FindProperty("dampingRatio").floatValue = 0.24f;
            physicsSo.FindProperty("tableTopY").floatValue = 0.85f;
            physicsSo.FindProperty("minTableClearance").floatValue = 0.05f;
            physicsSo.FindProperty("maxAllowedExtension").floatValue = 0.22f;
            physicsSo.FindProperty("assumedMassDropBelowHook").floatValue = 0.09f;
            physicsSo.FindProperty("springTopAnchor").objectReferenceValue = topAnchor;
            physicsSo.FindProperty("springBottomHook").objectReferenceValue = bottomHook;
            physicsSo.ApplyModifiedPropertiesWithoutUndo();

            SpringAttachmentPoint socket = Object.FindFirstObjectByType<SpringAttachmentPoint>();
            if (socket != null)
            {
                SerializedObject socketSo = new SerializedObject(socket);
                socketSo.FindProperty("springPhysics").objectReferenceValue = physics;
                socketSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(socket.gameObject);
            }

            VerifyHookesLawAndSafety(physics, topAnchor);

            EditorUtility.SetDirty(apparatus);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase4PhysicsBuilder] Phase 4 Hooke's Law spring physics applied and saved.");
        }

        private static void VerifyHookesLawAndSafety(SpringPhysicsController physics, Transform topAnchor)
        {
            int[] masses = { 50, 100, 150, 200, 250 };
            float prevExt = -1f;
            float topY = topAnchor != null ? topAnchor.position.y : 1.43f;
            float safeMax = physics.GetSafeMaxExtension();

            foreach (int g in masses)
            {
                float kg = g / 1000f;
                float force = kg * physics.Gravity;
                float ext = physics.ComputeSafeExtension(kg);
                float totalL = physics.NaturalLengthMeters + ext;
                float hookY = topY - totalL;
                float massBottomY = hookY - 0.09f; // hook offset + max mass height
                bool monotonic = ext > prevExt;
                bool aboveTable = massBottomY > 0.85f + 0.05f;
                prevExt = ext;

                Debug.Log(
                    $"[Phase4Verify] {g}g: F={force:F4}N, x={ext * 100f:F2}cm, L={totalL * 100f:F2}cm, " +
                    $"hookY={hookY:F3}m, estMassBottomY={massBottomY:F3}m, monotonic={monotonic}, aboveTable={aboveTable}");
            }

            Debug.Log($"[Phase4Verify] SafeMaxExtension={safeMax * 100f:F2}cm, SpringConstant k={physics.SpringConstant:F1} N/m");
        }
    }
}
