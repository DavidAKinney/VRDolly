using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace IVLab.VRDolly
{
    /// <summary>
    /// This input class should be active when using VRDolly with an Oculus Quest.
    /// </summary>
    public class OculusInput : InputMapper
    {
        /// <summary>
        /// Start() is used primarily to initialize sensitivity data as well as call the base class' Start() function.
        /// </summary>
        public override void Start()
        {
            base.Start();
        }

        /// <summary>
        /// Update continuously checks whether valid Oculus Quest controllers are connected and reads input from them.
        /// </summary>
        public override void Update()
        {
            // Check and update the reconnection state.
            if ((!dominantConnection || !recessiveConnection) && !reconnecting)
            {
                reconnecting = true;
                Debug.Log("Attempting to connect your Oculus controllers.");
            }
            else if ((dominantConnection && recessiveConnection) && reconnecting)
            {
                reconnecting = false;
                Debug.Log("Successfully conntected to your Oculus controllers!");
            }

            // Attempt to reconnect the controllers if necessary.
            if (reconnecting)
            {
                reconnectControllers();
            }

            // Try to get updated input from the dominant controller.
            dominantConnection = dominantController.TryGetFeatureValue(CommonUsages.primary2DAxis, out dominantInput.primary2DAxis) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.trigger, out dominantInput.trigger) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.grip, out dominantInput.grip) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.primaryButton, out dominantInput.primaryButton) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.secondaryButton, out dominantInput.secondaryButton) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.primaryTouch, out dominantInput.primaryTouch) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.secondaryTouch, out dominantInput.secondaryTouch) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.gripButton, out dominantInput.gripButton) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.triggerButton, out dominantInput.triggerButton) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out dominantInput.primary2DAxisClick) &&
                                 dominantController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out dominantInput.primary2DAxisTouch);

            // Try to get updated input from the recessive controller.
            recessiveConnection = recessiveController.TryGetFeatureValue(CommonUsages.primary2DAxis, out recessiveInput.primary2DAxis) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.trigger, out recessiveInput.trigger) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.grip, out recessiveInput.grip) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.primaryButton, out recessiveInput.primaryButton) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.secondaryButton, out recessiveInput.secondaryButton) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.primaryTouch, out recessiveInput.primaryTouch) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.secondaryTouch, out recessiveInput.secondaryTouch) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.gripButton, out recessiveInput.gripButton) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.triggerButton, out recessiveInput.triggerButton) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out recessiveInput.primary2DAxisClick) &&
                                  recessiveController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out recessiveInput.primary2DAxisTouch);

            // Call the base Update() function for general updates.
            base.Update();
        }

        /// <summary>
        /// This helper attempts to locate and connect to valid Oculus Quest controllers whenever a disconnect occurs.
        /// </summary>
        private void reconnectControllers()
        {
            // Search for Oculus Quest controllers.
            List<InputDevice> leftHandedControllers = new List<InputDevice>();
            List<InputDevice> rightHandedControllers = new List<InputDevice>();
            var leftCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left;
            var rightCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right;
            InputDevices.GetDevicesWithCharacteristics(leftCharacteristics, leftHandedControllers);
            InputDevices.GetDevicesWithCharacteristics(rightCharacteristics, rightHandedControllers);

            // Check to see if both controllers where detected.
            if (leftHandedControllers.Count == 0 || rightHandedControllers.Count == 0)
            {
                return;
            }

            // Determine which controller corresponds to which hand.
            if (dominantHand == Hand.Right)
            {
                dominantController = rightHandedControllers[0];
                recessiveController = leftHandedControllers[0];
            }
            else
            {
                dominantController = leftHandedControllers[0];
                recessiveController = rightHandedControllers[0];
            }
        }


        // XR input objects for the controllers
        private InputDevice dominantController;
        private InputDevice recessiveController;

        // Boolean values that monitor controller connections.
        private bool dominantConnection = false;
        private bool recessiveConnection = false;
        private bool reconnecting = false;
    }
}
