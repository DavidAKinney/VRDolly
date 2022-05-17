using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.SpatialTracking;
using UnityEngine.XR;

namespace IVLab.VRDolly
{
    /// <summary>
    /// This enum helps the Input Mapper differentiate the user's dominant hand.
    /// </summary>
    public enum Hand
    {
        Left,
        Right
    }

    /// <summary>
    /// This class offers base functionality necessary for VRDolly to attain input from the user.
    /// Classes that derive from Input Mapper should be based off of a specific input device(s)
    /// such as mouse/keyboard or a brand of VR controllers.
    /// </summary>
    public class InputMapper : MonoBehaviour
    {
        /// <summary>
        /// On startup, Input Mapper locates all the necessary GameObjects needed for managing user input and stores them for later use.
        /// </summary>
        public virtual void Start()
        {
            // Initialize the dominant data.
            dominantInput = new InputData();
            dominantInput.initialize();

            // Initialize the recessive data.
            recessiveInput = new InputData();
            recessiveInput.initialize();

            // No snap turns occur at the start.
            snapTurned = false;

            // Determine which GameObjects corresponds to which hand.
            if (dominantHand == Hand.Right)
            {
                dominantObject = GameObject.Find("VR Tracking Rig/Height Offset/Right Controller");
                dominantInput.probe = GameObject.Find("VR Tracking Rig/Height Offset/Right Controller/Probe");
                dominantInput.viewScreen = GameObject.Find("VR Tracking Rig/Height Offset/Right Controller/View Screen");
                dominantInput.textDisplay = GameObject.Find("VR Tracking Rig/Height Offset/Right Controller/Text Display").GetComponent<TextMesh>();

                recessiveObject = GameObject.Find("VR Tracking Rig/Height Offset/Left Controller");
                recessiveInput.probe = GameObject.Find("VR Tracking Rig/Height Offset/Left Controller/Probe");
                recessiveInput.viewScreen = GameObject.Find("VR Tracking Rig/Height Offset/Left Controller/View Screen");
                recessiveInput.textDisplay = GameObject.Find("VR Tracking Rig/Height Offset/Left Controller/Text Display").GetComponent<TextMesh>();
            }
            else
            {
                dominantObject = GameObject.Find("VR Tracking Rig/Height Offset/Left Controller");
                dominantInput.probe = GameObject.Find("VR Tracking Rig/Height Offset/Left Controller/Probe");
                dominantInput.viewScreen = GameObject.Find("VR Tracking Rig/Height Offset/Left Controller/View Screen");
                dominantInput.textDisplay = GameObject.Find("VR Tracking Rig/Height Offset/Left Controller/Text Display").GetComponent<TextMesh>();

                recessiveObject = GameObject.Find("VR Tracking Rig/Height Offset/Right Controller");
                recessiveInput.probe = GameObject.Find("VR Tracking Rig/Height Offset/Right Controller/Probe");
                recessiveInput.viewScreen = GameObject.Find("VR Tracking Rig/Height Offset/Right Controller/View Screen");
                recessiveInput.textDisplay = GameObject.Find("VR Tracking Rig/Height Offset/Right Controller/Text Display").GetComponent<TextMesh>();
            }

            // Disable the controller view screens on startup.
            dominantInput.viewScreen.GetComponent<Renderer>().enabled = false;
            recessiveInput.viewScreen.GetComponent<Renderer>().enabled = false;

            // Find the headset object and height offset.
            headsetObject = GameObject.Find("VR Tracking Rig/Height Offset/Headset Camera");
            heightOffset = GameObject.Find("VR Tracking Rig/Height Offset");
        }

        /// <summary>
        /// Update() is mostly left in the control of derived classes. This function should
        /// maps user input from a mouse/keyboard or VR controllers onto the input data
        /// structures stored in this base class.
        /// </summary>
        public virtual void Update()
        {
            // Update the input "down" states.
            dominantInput.updateDownStates();
            recessiveInput.updateDownStates();

            // Reset the snapturn state if used previously.
            snapTurned = false;

            // Perform snap-turning when the grips, triggers on the dominant controller aren't pressed.
            if (!dominantInput.triggerButton && !dominantInput.gripButton)
            {
                if (dominantInput.primary2DAxisDown && dominantInput.primary2DAxis.x != 0)
                {
                    // Rotate to the right.
                    if (dominantInput.primary2DAxis.x > 0)
                    {
                        heightOffset.transform.Rotate(new Vector3(0, 45, 0), Space.World);
                        snapYaw = 45;
                    }
                    // Rotate to the left.
                    else
                    {
                        heightOffset.transform.Rotate(new Vector3(0, -45, 0), Space.World);
                        snapYaw = -45;
                    }

                    // Indicate that a snap-turn occured.
                    snapTurned = true;
                }
            }

            // Get updated transform data from the dominant "controller" object.
            dominantInput.controllerPosition = dominantObject.transform.position;
            dominantInput.controllerPointer = dominantObject.transform.forward;

            // Get updated transform data from the recessive "controller" object.
            recessiveInput.controllerPosition = recessiveObject.transform.position;
            recessiveInput.controllerPointer = recessiveObject.transform.forward;
        }

        /// <summary>
        /// This function returns the most recently stored data from the user's dominant controller.
        /// </summary>
        /// <returns></returns>
        public InputData GetDominantInput() { return dominantInput; }

        /// <summary>
        /// This function returns the most recently stored data from the user's recessive controller.
        /// </summary>
        /// <returns></returns>
        public InputData GetRecessiveInput() { return recessiveInput; }

        /// <summary>
        /// This function returns the transform of the user's dominant controller.
        /// </summary>
        /// <returns></returns>
        public Transform GetDominantTransform() { return dominantObject.transform; }

        /// <summary>
        /// This function returns the transform of the user's recessive controller.
        /// </summary>
        /// <returns></returns>
        public Transform GetRecessiveTransform() { return recessiveObject.transform; }

        // These gameobjects represent the controllers and headset in world space... 
        protected GameObject dominantObject;
        protected GameObject recessiveObject;
        protected GameObject headsetObject;
        protected GameObject heightOffset;

        protected InputData dominantInput;
        protected InputData recessiveInput;

        // Determines whether the user is right-handed.
        public Hand dominantHand;

        // Useful data for handling snap-turns.
        protected bool snapTurned;
        protected float snapYaw;
    }


    /// <summary>
    /// This struct is used to store input data for one of the user's controllers. The currently available fields
    /// were included specifically for Oculus Quest controllers but they are generic and extensible enough to 
    /// support different specs if necessary.
    /// </summary>
    public struct InputData
    {
        public Vector3 controllerPosition;
        public Vector3 controllerPointer;
        public Vector2 primary2DAxis;
        public float trigger;
        public float grip;
        public bool primaryButton;
        public bool secondaryButton;
        public bool primaryTouch;
        public bool secondaryTouch;
        public bool gripButton;
        public bool triggerButton;
        public bool menuButton;
        public bool primary2DAxisClick;
        public bool primary2DAxisTouch;
        public float batteryLevel;
        public bool userPresence;

        // These boolean values should only be true during the first 
        // frame their corresponding input is used.
        public bool primaryButtonDown;
        public bool secondaryButtonDown;
        public bool primaryTouchDown;
        public bool secondaryTouchDown;
        public bool gripButtonDown;
        public bool triggerButtonDown;
        public bool menuButtonDown;
        public bool primary2DAxisClickDown;
        public bool primary2DAxisTouchDown;
        public bool primary2DAxisDown;

        // These GameObjects are components of the controller's GUI.
        public GameObject probe;
        public GameObject viewScreen;
        public TextMesh textDisplay;

        // These boolean values help determine when the "down" states
        // should be set to true.
        private bool primaryButtonPrev;
        private bool secondaryButtonPrev;
        private bool primaryTouchPrev;
        private bool secondaryTouchPrev;
        private bool gripButtonPrev;
        private bool triggerButtonPrev;
        private bool menuButtonPrev;
        private bool primary2DAxisClickPrev;
        private bool primary2DAxisTouchPrev;
        private Vector2 primary2DAxisPrev;

        /// <summary>
        /// This function sets the initial states of all the input data.
        /// </summary>
        public void initialize()
        {
            primary2DAxis = new Vector2();
            trigger = 0;
            grip = 0;
            primaryButton = false;
            secondaryButton = false;
            primaryTouch = false;
            secondaryTouch = false;
            gripButton = false;
            triggerButton = false;
            menuButton = false;
            primary2DAxisClick = false;
            primary2DAxisTouch = false;
            batteryLevel = 0;
            userPresence = false;
            primaryButtonDown = false;
            secondaryButtonDown = false;
            primaryTouchDown = false;
            secondaryTouchDown = false;
            gripButtonDown = false;
            triggerButtonDown = false;
            menuButtonDown = false;
            primary2DAxisClickDown = false;
            primary2DAxisTouchDown = false;
            primary2DAxisDown = false;
            primary2DAxisPrev = primary2DAxis;
        }


        /// <summary>
        /// This function should be called each frame in order to correctly detect input "down" states, 
        /// i.e. whether an input was just pressed during the current frame. This sets corresponding
        /// public boolean values if so.
        /// </summary> 
        public void updateDownStates()
        {
            if (primaryButton)
            {
                if (primaryButtonPrev)
                {
                    primaryButtonDown = false;
                }
                else
                {
                    //Debug.Log("Primary button down!");
                    primaryButtonDown = true;
                }
            }
            if (secondaryButton)
            {
                if (secondaryButtonPrev)
                {
                    secondaryButtonDown = false;
                }
                else
                {
                    //Debug.Log("Secondary button down!");
                    secondaryButtonDown = true;
                }
            }
            if (primaryTouch)
            {
                if (primaryTouchPrev)
                {
                    primaryTouchDown = false;
                }
                else
                {
                    //Debug.Log("Primary Touch down!");
                    primaryTouchDown = true;
                }
            }
            if (secondaryTouch)
            {
                if (secondaryTouchPrev)
                {
                    secondaryTouchDown = false;
                }
                else
                {
                    //Debug.Log("Secondary Touch down!");
                    secondaryTouchDown = true;
                }
            }
            if (gripButton)
            {
                if (gripButtonPrev)
                {
                    gripButtonDown = false;
                }
                else
                {
                    //Debug.Log("Grip button down!");
                    gripButtonDown = true;
                }
            }
            if (triggerButton)
            {
                if (triggerButtonPrev)
                {
                    triggerButtonDown = false;
                }
                else
                {
                    //Debug.Log("Trigger button down!");
                    triggerButtonDown = true;
                }
            }
            if (menuButton)
            {
                if (menuButtonPrev)
                {
                    menuButtonDown = false;
                }
                else
                {
                    //Debug.Log("Menu button down!");
                    menuButtonDown = true;
                }
            }
            if (primary2DAxisClick)
            {
                if (primary2DAxisClickPrev)
                {
                    primary2DAxisClickDown = false;
                }
                else
                {
                    //Debug.Log("Primary 2D-Axis Click down!");
                    primary2DAxisClickDown = true;
                }
            }
            if (primary2DAxisTouch)
            {
                if (primary2DAxisTouchPrev)
                {
                    primary2DAxisTouchDown = false;
                }
                else
                {
                    //Debug.Log("Primary 2D-Axis Touch down!");
                    primary2DAxisTouchDown = true;
                }
            }
            if (primary2DAxis != Vector2.zero)
            {
                if (primary2DAxisPrev != Vector2.zero)
                {
                    primary2DAxisDown = false;
                }
                else
                {
                    //Debug.Log("Primary 2D-Axis down!");
                    primary2DAxisDown = true;
                }
            }

            // Update the previous states to the current ones.
            primaryButtonPrev = primaryButton;
            secondaryButtonPrev = secondaryButton;
            primaryTouchPrev = primaryTouch;
            secondaryTouchPrev = secondaryTouch;
            gripButtonPrev = gripButton;
            triggerButtonPrev = triggerButton;
            menuButtonPrev = menuButton;
            primary2DAxisClickPrev = primary2DAxisClick;
            primary2DAxisTouchPrev = primary2DAxisTouch;
            primary2DAxisPrev = primary2DAxis;
        }
    }
}
