using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using IVLab.VisTools;

namespace IVLab.VRDolly
{
    /// <summary>
    /// This enum helps the state manager determine which VR platform
    /// the user is currently running VRDolly with.
    /// </summary>
    public enum Platform
    {
        DESKTOP,
        OCULUS_QUEST,
        OCULUS_RIFT,
        HTC_VIVE,
        VALVE_INDEX
    }

    /// <summary>
    /// This class acts as the backbone of VRDolly. The manager creates instances
    /// of the different sub-states and handles context switch requests between
    /// all of them.
    /// </summary>
    public class StateManager : MonoBehaviour
    {
        /// <summary>
        /// Upon startup, instances of StateManager create instances of all the 
        /// VRDolly sub-states along with a Radial Menu that the user can use to
        /// switch between them.
        /// </summary>
        void Start()
        {
            // If no scene was assigned to the VRDolly prefab, create a default GameObject to prevent errors.
            if (scene == null) {
                scene = new GameObject("Temp Scene");
            }

            // Initialize the private data used to track changes in the scene's transform.
            prevPosition = new Vector3();
            prevRotation = new Quaternion();
            prevScale = new Vector3();

            // Initialize the tracks structure.
            data = new VRDollyData();
            data.anchor = GameObject.Find("Tracks Anchor");
            data.camera = GameObject.Find("Track Camera");
            data.positionTracks = new List<BezierTrack>();
            data.lookTracks = new List<BezierTrack>();
            data.frustums = new List<GameObject>();
            data.rays = new List<GameObject>();
            data.frustumLocations = new List<float>();

            // Set the tracks anchor and camera as children of the user's scene.
            data.anchor.transform.SetParent(scene.transform);
            data.camera.transform.SetParent(scene.transform);

            // Initialize the states data.
            states = new BaseState[5];
            states[0] = new LocomotionState();
            states[1] = new CreationState();
            states[2] = new EditState();
            states[3] = new ViewState();
            states[4] = new FileState();
            currentState = State.LOCOMOTION;

            // Initialize the state messages.
            stateMessages = new string[5];
            stateMessages[0] = "Teleport";
            stateMessages[1] = "Create";
            stateMessages[2] = "Edit";
            stateMessages[3] = "Flythrough";
            stateMessages[4] = "Save/Load";

            probeSensitivity = 0.5f;
            dominantProbeLocked = false;
            recessiveProbeLocked = false;

            // Instantiate the radial state menu and attach it to the recessive controller.
            stateMenu = new RadialMenu(stateMessages, new Color(0.5f, 0.5f, 0.5f), Color.black, new Color(0, 1, 1), 0.08f, "State Menu");
            if (platform == Platform.DESKTOP)
            {
                stateMenu.setMenuParent(GetComponent<MKInput>().GetRecessiveTransform());
            }
            else if (platform == Platform.OCULUS_QUEST)
            {
                stateMenu.setMenuParent(GetComponent<OculusInput>().GetRecessiveTransform());
            }
            stateMenu.setLocalPosition(new Vector3(0, 0.06f, 0.1f));

            // Keep the scale of the tracks constant by negating the scale of the scene on the tracks anchor.
            // NOTE: This is only done once, meaning the tracks will likely break if the scene is rescaled during runtime!
            data.anchor.transform.localScale = new Vector3(1/scene.transform.localScale.x, 1/scene.transform.localScale.y, 1/scene.transform.localScale.z);
        }

        /// <summary>
        /// During each update, StateManager takes new input from the user, determines 
        /// the correct state to enter/maintain, allows the user to adjust the positions
        /// of their probes, and updates the frustum/ray travel animations for each track 
        /// pair that has been added to the scene.
        /// </summary>
        void Update()
        {
            // Check if the scene transform changed since the last frame and update the tracks accordingly.
            if (!scene.transform.position.Equals(prevPosition) || !scene.transform.rotation.Equals(prevRotation) || !scene.transform.localScale.Equals(prevScale))
            {
                for (int i = 0; i < data.positionTracks.Count; i++)
                {
                    data.positionTracks[i].UpdateTrack();
                    data.lookTracks[i].UpdateTrack();
                }

                prevPosition = scene.transform.position;
                prevRotation = scene.transform.rotation;
                prevScale = scene.transform.localScale;
            }

            // Determine which platform the user wants to get input from.
            if (platform == Platform.DESKTOP)
            {
                // Get updated emulator input from a mouse and keyboard.
                dominantInput = GetComponent<MKInput>().GetDominantInput();
                recessiveInput = GetComponent<MKInput>().GetRecessiveInput();
            }
            else if (platform == Platform.OCULUS_QUEST)
            {
                // Get updated input from an Oculus Quest system.
                dominantInput = GetComponent<OculusInput>().GetDominantInput();
                recessiveInput = GetComponent<OculusInput>().GetRecessiveInput();
            }

            // Pass the input into the current state and update it.
            currentState = states[(int)currentState].UpdateState(dominantInput, recessiveInput, data, stateMenu);

            // Allow the user to lock their controller probes in place when they press down on joysticks.
            if (dominantInput.primary2DAxisClickDown && dominantInput.gripButton)
            {
                Debug.Log("Dominant probe lock toggled!");
                dominantProbeLocked = !dominantProbeLocked;
            }
            if (recessiveInput.primary2DAxisClickDown && recessiveInput.gripButton)
            {
                Debug.Log("Recessive probe lock toggled!");
                recessiveProbeLocked = !recessiveProbeLocked;
            }

            // Allow the user to adjust their controller probes with the controller joysticks.
            if ((dominantInput.primary2DAxis.y != 0 || dominantInput.primary2DAxis.x != 0) && dominantInput.gripButton && !dominantProbeLocked)
            {
                Vector3 joystickVector = new Vector3(dominantInput.primary2DAxis.x, 0, dominantInput.primary2DAxis.y).normalized;
                dominantInput.probe.transform.localPosition += joystickVector * probeSensitivity * Time.deltaTime;
            }
            if ((recessiveInput.primary2DAxis.y != 0 || recessiveInput.primary2DAxis.x != 0) && recessiveInput.gripButton && !recessiveProbeLocked)
            {
                Vector3 joystickVector = new Vector3(recessiveInput.primary2DAxis.x, 0, recessiveInput.primary2DAxis.y).normalized;
                recessiveInput.probe.transform.localPosition += joystickVector * probeSensitivity * Time.deltaTime;
            }

            // Update the state message shown on the recessive controller whenever the state changes.
            if (recessiveInput.textDisplay.text != stateMessages[(int)currentState])
            {
                recessiveInput.textDisplay.text = stateMessages[(int)currentState];
            }

            // Move the frustums and rays along their corresponding tracks.
            for (int i = 0; i < data.frustums.Count; i++)
            {
                data.frustumLocations[i] += data.positionTracks[i].GetCurrentSpeed(data.frustumLocations[i]) * Time.deltaTime;

                if (data.frustumLocations[i] > 1)
                {
                    data.frustumLocations[i] = 0;
                }
                else
                {
                    Vector3 nextPosition = data.positionTracks[i].GetLocationOnCurve(data.frustumLocations[i]);
                    Vector3 nextLook = data.lookTracks[i].GetLocationOnCurve(data.frustumLocations[i]);

                    data.frustums[i].transform.position = nextPosition;
                    data.frustums[i].transform.forward = (nextLook - nextPosition).normalized;

                    data.rays[i].transform.position = nextLook;
                    data.rays[i].transform.forward = (nextLook - nextPosition).normalized;
                }
            }
        }


        // Copies of the scene's previous transform data.
        public GameObject scene;
        private Vector3 prevPosition;
        private Quaternion prevRotation;
        private Vector3 prevScale;
        // Data used to handle user input.
        private VRDollyData data;
        private InputData dominantInput;
        private InputData recessiveInput;
        private bool dominantProbeLocked;
        private bool recessiveProbeLocked;
        // Data used to handle different states.
        public Platform platform;
        private BaseState[] states;
        private State currentState;
        private string[] stateMessages;
        private RadialMenu stateMenu;
        // Data used to adjust user sensitivities.
        private float probeSensitivity;
    }


    /// <summary>
    /// This struct is used to pass information between VRDolly states along
    /// with the track pairs for determi
    /// </summary>
    public struct VRDollyData
    {
        /// <summary>
        /// The game object that all the tracks are anchored to.
        /// </summary>
        public GameObject anchor;
        /// <summary>
        /// The camera that is used to show and record footage along the tracks.
        /// </summary>
        public GameObject camera;
        /// <summary>
        /// A list of all the tracks used to determine the position of the track camera.
        /// </summary>
        public List<BezierTrack> positionTracks;
        /// <summary>
        /// A list of all the tracks used to determine the direction of the track camera's look vector.
        /// </summary>
        public List<BezierTrack> lookTracks;
        /// <summary>
        /// A list of frustum mesh instances used to visualize the track camera along each position track.
        /// </summary>
        public List<GameObject> frustums;
        /// <summary>
        /// A list of ray mesh instances used to visualize the track camera's direction along each look track.
        /// </summary>
        public List<GameObject> rays;
        /// <summary>
        /// A list of t-values representing the positions of each frustum on their corresponding track.
        /// </summary>
        public List<float> frustumLocations;
    }
}