using System.Collections;
using MassSpringLab.Apparatus;
using MassSpringLab.Core;
using MassSpringLab.Interaction;
using MassSpringLab.Robot;
using MassSpringLab.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MassSpringLab.Tests
{
    public class MassSpringPlayModeTests
    {
        [UnityTest]
        public IEnumerator Atom8Instructor_FaceStudentPointAndNarrate_CompleteSequence()
        {
            LogAssert.ignoreFailingMessages = true;
            AsyncOperation loadOp = SceneManager.LoadSceneAsync("Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity");
            while (!loadOp.isDone) yield return null;
            yield return null;

            Atom8VoiceNarrator narrator = Object.FindFirstObjectByType<Atom8VoiceNarrator>();
            Atom8GuidanceController atom8 = Object.FindFirstObjectByType<Atom8GuidanceController>();
            VoiceGuidanceCoordinator vgc = Object.FindFirstObjectByType<VoiceGuidanceCoordinator>();
            SpringPhysicsController physics = Object.FindFirstObjectByType<SpringPhysicsController>();
            SpringAttachmentPoint socket = Object.FindFirstObjectByType<SpringAttachmentPoint>();
            SpringOscillationController oscCtrl = Object.FindFirstObjectByType<SpringOscillationController>();
            StopwatchController stopwatch = Object.FindFirstObjectByType<StopwatchController>();
            CalculationSpotlightController spotlightCtrl = Object.FindFirstObjectByType<CalculationSpotlightController>();

            LabActionButton measureBtn = GameObject.Find("MeasureButtonStation/MeasureLengthButton")?.GetComponent<LabActionButton>();
            LabActionButton proceedBtn = GameObject.Find("ObservationConsoleStation")?.transform.Find("ProceedButton")?.GetComponent<LabActionButton>();
            LabActionButton replayBtn = GameObject.Find("ObservationConsoleStation/ReplayVoiceButton")?.GetComponent<LabActionButton>();

            Assert.IsNotNull(narrator);
            Assert.IsNotNull(atom8);
            Assert.IsNotNull(vgc);
            Assert.IsNotNull(replayBtn);
            spotlightCtrl.SetTimingForTesting(0.15f, 0.08f);

            // Verify AudioListener, AudioSource, and all 25 real WAV AudioClips
            AudioListener listener = Object.FindFirstObjectByType<AudioListener>();
            AudioSource audioSrc = atom8.GetComponent<AudioSource>();
            Assert.IsNotNull(listener);
            Assert.IsNotNull(audioSrc);
            Assert.AreEqual(25, narrator.CueCount, "All 25 instructor narration cues must be loaded");
            Assert.IsTrue(narrator.HasValidClip("01_welcome"));
            Assert.IsTrue(narrator.HasValidClip("02a_look_ruler"));
            Assert.IsTrue(narrator.HasValidClip("03b_attach_point"));
            Assert.IsTrue(narrator.HasValidClip("04a_click_measure"));
            Assert.IsTrue(narrator.HasValidClip("05a_osc_controller"));
            Assert.IsTrue(narrator.HasValidClip("05b_osc_hands"));
            Assert.IsTrue(narrator.HasValidClip("10_complete"));

            // Verify ATOM-8 is beside workstation (x ~ -1.24, z ~ 0.82)
            Assert.Less(atom8.transform.position.x, -1.10f);
            Assert.Greater(atom8.transform.position.z, 0.50f);

            // 1. Welcome facing student + audible playback
            Assert.AreEqual("01_welcome", narrator.CurrentCueId);
            Assert.IsTrue(audioSrc.isPlaying, "Welcome AudioClip must actually play");
            Assert.IsNull(atom8.CurrentPointTarget, "ATOM-8 faces student during Welcome");

            // 2. Natural length measured -> 02b_natural_recorded
            measureBtn.TriggerClick();
            yield return null;
            Assert.AreEqual("02b_natural_recorded", narrator.CurrentCueId);

            // Test 50g (Controller) and 100g (Hand Tracking)
            int[] testMasses = { 50, 100 };
            for (int i = 0; i < testMasses.Length; i++)
            {
                int g = testMasses[i];
                bool handTracking = (i == 1);
                vgc.SetInteractionModeOverrideForTest(handTracking);

                MassObject mass = GameObject.Find($"Mass_{g}g").GetComponent<MassObject>();
                mass.BeginDesktopGrab();
                mass.transform.position = socket.AttachWorldPosition;
                Physics.SyncTransforms();
                yield return null;
                mass.EndDesktopGrab();

                float wait = 0f;
                while (physics.IsSettling && wait < 6.0f)
                {
                    wait += Time.deltaTime;
                    yield return null;
                }
                yield return null;

                // Extended Length -> points at ruler with hand-originating pointer ray
                Assert.AreEqual("04a_extended_length", narrator.CurrentCueId);
                Assert.AreEqual("MeasurementRuler", atom8.CurrentPointTarget?.name);
                yield return new WaitForSeconds(0.6f);
                LineRenderer handRay = GameObject.Find("ATOM-8/Torso/RightArm/Elbow/PointerTip")?.GetComponent<LineRenderer>();
                Assert.IsNotNull(handRay, "Hand pointer ray must be on PointerTip (robot's hand), not head");
                Assert.IsTrue(atom8.IsHandRayActive, "ATOM-8 hand pointer ray should be active when pointing at ruler");

                measureBtn.TriggerClick();
                yield return null;
                Assert.AreEqual("04b_extension_recorded", narrator.CurrentCueId);

                // Wait for 04b clip to finish -> 05a vs 05b
                float cueWait = 0f;
                while (narrator.CurrentCueId == "04b_extension_recorded" && cueWait < 6.0f)
                {
                    cueWait += Time.deltaTime;
                    yield return null;
                }
                Assert.AreEqual(handTracking ? "05b_osc_hands" : "05a_osc_controller", narrator.CurrentCueId);
                Assert.AreEqual("SpringBottomHook", atom8.CurrentPointTarget?.name);

                // Oscillate -> Stopwatch -> Calculations -> Observation Table -> Proceed
                oscCtrl.ReleaseToOscillate(0.038f);
                yield return null;
                Assert.AreEqual("06a_oscillating", narrator.CurrentCueId);

                float oscWait = 0f;
                while (stopwatch.IsRunning && oscWait < 15.0f)
                {
                    oscWait += Time.deltaTime;
                    yield return null;
                }
                Assert.AreEqual("06b_osc_done", narrator.CurrentCueId);

                float presWait = 0f;
                while (spotlightCtrl.IsPresenting && presWait < 3.0f)
                {
                    presWait += Time.deltaTime;
                    yield return null;
                }

                Assert.AreEqual("09_ready_proceed", narrator.CurrentCueId);
                Assert.AreEqual("ProceedButton", atom8.CurrentPointTarget?.name);

                proceedBtn.TriggerClick();
                yield return null;
                Assert.AreEqual($"09_next_{(g == 50 ? 100 : 150)}g", narrator.CurrentCueId);
            }
        }
    }
}
