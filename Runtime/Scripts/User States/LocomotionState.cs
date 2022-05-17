using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using IVLab.VisTools;

namespace IVLab.VRDolly
{
    /// <summary>
    /// The locomotion state allows the user to teleport around the environment. This is especially
    /// useful for larger scenes.
    /// </summary>
    public class LocomotionState : BaseState
    {
        // Constructor
        public LocomotionState()
        {
            // Get the unlit/color shader.
            Shader unlitWithColor = Shader.Find("Unlit/Color");

            // Create the teleport marker.
            teleportMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            teleportMarker.transform.localScale = new Vector3(2, 0.1f, 2);
            teleportMarker.GetComponent<Renderer>().material.shader = unlitWithColor;
            teleportMarker.GetComponent<Renderer>().material.color = Color.cyan;
            teleportMarker.GetComponent<Renderer>().enabled = false;
            teleportMarker.name = "Teleport Marker";

            // Initialize data
            trackingRig = GameObject.Find("VR Tracking Rig");
            dominantCast = false;
            recessiveCast = false;
            castDistance = 0;
            castSensitivity = 2;
            heightOffset = GameObject.Find("VR Tracking Rig/Height Offset").transform.position;
        }

        /// <summary>
        /// The locomotion state checks for input from the user and casts a disk into the scene while the input is held.
        /// Once released, the user is teleported to latest position of the disk.
        /// </summary>
        /// <param name="dominantInput"></param>
        /// <param name="recessiveInput"></param>
        /// <param name="data"></param>
        /// <param name="menu"></param>
        /// <returns></returns>
        public override State UpdateState(InputData dominantInput, InputData recessiveInput, VRDollyData data, RadialMenu menu)
        {
            if (dominantCast)
            {
                if (dominantInput.triggerButton)
                {
                    castDistance += castSensitivity * Time.deltaTime;
                    teleportMarker.transform.position = dominantInput.controllerPosition + (dominantInput.controllerPointer * castDistance) - heightOffset;
                }
                else
                {
                    trackingRig.transform.position = teleportMarker.transform.position;
                    teleportMarker.GetComponent<Renderer>().enabled = false;
                    castDistance = 0;
                    dominantCast = false;
                }
            }
            else if (recessiveCast)
            {
                if (recessiveInput.triggerButton)
                {
                    castDistance += castSensitivity * Time.deltaTime;
                    teleportMarker.transform.position = recessiveInput.controllerPosition + (recessiveInput.controllerPointer * castDistance) - heightOffset;
                }
                else
                {
                    trackingRig.transform.position = teleportMarker.transform.position;
                    teleportMarker.GetComponent<Renderer>().enabled = false;
                    castDistance = 0;
                    recessiveCast = false;
                }
            }
            else
            {
                if (dominantInput.triggerButton)
                {
                    dominantCast = true;
                    teleportMarker.GetComponent<Renderer>().enabled = true;
                }
                else if (recessiveInput.triggerButton)
                {
                    recessiveCast = true;
                    teleportMarker.GetComponent<Renderer>().enabled = true;
                }
            }

            // Determine which state to return based on user input.
            if (recessiveInput.primary2DAxisDown && !recessiveInput.gripButton)
            {
                int nextStateIndex = menu.GetSectorSelection(recessiveInput.primary2DAxis);

                if (nextStateIndex != (int)State.LOCOMOTION)
                {
                    dominantCast = false;
                    recessiveCast = false;
                }
                
                return (State)nextStateIndex;
            }
            else
            {
                return State.LOCOMOTION;
            }
        }
        

        // Teleport data
        private GameObject teleportMarker;
        private bool dominantCast;
        private bool recessiveCast;
        private float castSensitivity;
        private float castDistance;
        private Vector3 heightOffset;

        GameObject trackingRig;
    }
}
