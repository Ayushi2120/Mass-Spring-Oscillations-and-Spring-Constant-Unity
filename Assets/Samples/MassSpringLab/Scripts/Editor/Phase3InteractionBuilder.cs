using MassSpringLab.Apparatus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Configures Phase 3 VR/Hand-Tracking/Desktop grabbing on all five existing MassObjects
    /// and SpringAttachmentPoint. Ensures no Phase 4+ workflow/physics scripts block grabbing.
    /// </summary>
    public static class Phase3InteractionBuilder
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";

        [MenuItem("MassSpringLab/Apply Phase 3 Mass Grabbing & Attachment")]
        public static void ApplyPhase3Interaction()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Ensure Phase 4+ workflow/physics do not lock masses or stretch spring in Phase 3
            GameObject expGo = GameObject.Find("ExperimentController");
            if (expGo != null) Object.DestroyImmediate(expGo);

            GameObject apparatus = GameObject.Find("SpringApparatus");
            if (apparatus != null)
            {
                Component physicsComp = apparatus.GetComponent("SpringPhysicsController");
                if (physicsComp != null) Object.DestroyImmediate(physicsComp);
            }

            // 1. Configure all 5 MassObjects with XRGrabInteractable (VR controllers + hand tracking)
            MassObject[] masses = Object.FindObjectsByType<MassObject>(FindObjectsSortMode.None);
            foreach (MassObject mass in masses)
            {
                ConfigureMassObject(mass);
            }

            // 2. Configure SpringAttachmentPoint
            SpringAttachmentPoint socket = Object.FindFirstObjectByType<SpringAttachmentPoint>();
            if (socket != null)
            {
                ConfigureSpringSocket(socket);
            }
            else
            {
                Debug.LogError("[Phase3InteractionBuilder] SpringAttachmentPoint not found in scene!");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase3InteractionBuilder] Phase 3 mass grabbing and attachment configured and saved.");
        }

        private static void ConfigureMassObject(MassObject mass)
        {
            GameObject go = mass.gameObject;

            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = mass.MassKg;
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.None;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            CapsuleCollider col = go.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                // Ergonomic collider size for VR controller ray, direct grab, and hand-tracking pinch
                col.radius = Mathf.Max(0.032f, col.radius);
                col.height = Mathf.Max(0.085f, col.height);
                col.isTrigger = false;
            }

            XRGrabInteractable grab = go.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                grab = go.AddComponent<XRGrabInteractable>();
            }

            grab.enabled = true;
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.useDynamicAttach = true;
            grab.throwOnDetach = false;
            grab.retainTransformParent = false;
            grab.interactionLayers = InteractionLayerMask.GetMask("Default");

            if (col != null)
            {
                grab.colliders.Clear();
                grab.colliders.Add(col);
            }

            mass.RecordHomeTransform();
            mass.SetGrabbable(true);

            Transform body = go.transform.Find("Body");
            SerializedObject so = new SerializedObject(mass);
            if (body != null)
            {
                so.FindProperty("bodyRenderer").objectReferenceValue = body.GetComponent<Renderer>();
            }
            so.FindProperty("minWorldY").floatValue = 0.875f;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(go);
        }

        private static void ConfigureSpringSocket(SpringAttachmentPoint socket)
        {
            Transform oldHalo = socket.transform.Find("AttachmentHalo");
            if (oldHalo != null) Object.DestroyImmediate(oldHalo.gameObject);

            SphereCollider triggerCol = socket.GetComponent<SphereCollider>();
            if (triggerCol == null)
            {
                triggerCol = socket.gameObject.AddComponent<SphereCollider>();
            }
            triggerCol.isTrigger = true;
            triggerCol.radius = 0.16f;
            triggerCol.center = Vector3.zero;

            GameObject hookCollar = GameObject.Find("SpringApparatus/SpringBottomHook/HookCollar");
            Renderer collarRenderer = hookCollar != null ? hookCollar.GetComponent<Renderer>() : null;

            SerializedObject so = new SerializedObject(socket);
            so.FindProperty("springPhysics").objectReferenceValue = null;
            if (collarRenderer != null)
            {
                so.FindProperty("haloRenderer").objectReferenceValue = collarRenderer;
            }
            so.FindProperty("snapRadius").floatValue = 0.16f;
            so.FindProperty("highlightRadius").floatValue = 0.28f;
            so.FindProperty("autoSnapWhenGrabbedNearHook").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(socket.gameObject);
        }
    }
}
