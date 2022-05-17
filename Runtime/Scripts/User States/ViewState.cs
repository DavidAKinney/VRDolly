using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IVLab.VisTools;

namespace IVLab.VRDolly
{
    /// <summary>
    /// The view state allows the user to test their track configuration by enabling the track camera and projecting the 
    /// results onto the user's handheld screen.
    /// </summary>
    public class ViewState : BaseState
    {
        public ViewState()
        {
            // Find the seconday camera used for viewing tracks.
            trackIndex = 0;
            travelTime = 0;
            action = true;
        }

        /// <summary>
        /// The view state allows the user to select which track pair they are currently viewing. Until a change is made
        /// the current track pair is run on a loop in order to make recording specific shots easier.
        /// </summary>
        /// <param name="dominantInput"></param>
        /// <param name="recessiveInput"></param>
        /// <param name="data"></param>
        /// <param name="menu"></param>
        /// <returns></returns>
        public override State UpdateState(InputData dominantInput, InputData recessiveInput, VRDollyData data, RadialMenu menu)
        {
            // Exit early if there are no tracks to view.
            if (data.positionTracks.Count == 0)
            {
                Debug.Log("There are no tracks to view!");
                action = true;
                menu.SetSectorState((int)State.LOCOMOTION);
                return State.LOCOMOTION;
            }

            // If the state has just started, do some setup.
            if (action)
            {
                // Reset the viewing data.
                currentPTrack = data.positionTracks[0];
                currentLTrack = data.lookTracks[0];
                trackIndex = 0;
                travelTime = data.frustumLocations[trackIndex];

                // Show the view screen.
                viewScreen = dominantInput.viewScreen;
                viewScreen.GetComponent<Renderer>().enabled = true;
                action = false;
            }
            else
            {
                travelTime += currentPTrack.GetCurrentSpeed(travelTime) * Time.deltaTime;
            }

            // Continue along the current track or restart once its end is reached.
            if (travelTime > 1)
            {
                travelTime = 0;
            }
            else
            {
                Vector3 nextPosition = currentPTrack.GetLocationOnCurve(travelTime);
                Vector3 nextLook = currentLTrack.GetLocationOnCurve(travelTime);

                data.camera.transform.position = nextPosition;
                data.camera.transform.forward = (nextLook - nextPosition).normalized;
            }

            // Change the track pair that is being shown when the user moves one of their thumbsticks left or right.
            if (dominantInput.primary2DAxisDown || recessiveInput.primary2DAxisDown)
            {
                if (dominantInput.primary2DAxis.y > 0 || recessiveInput.primary2DAxis.y > 0)
                {
                    trackIndex++;
                    if (trackIndex >= data.positionTracks.Count)
                    {
                        trackIndex = 0;
                    }
                }
                else if (dominantInput.primary2DAxis.y < 0 || recessiveInput.primary2DAxis.y < 0)
                {
                    trackIndex--;
                    if (trackIndex < 0)
                    {
                        trackIndex = data.positionTracks.Count - 1;
                    }
                }

                travelTime = data.frustumLocations[trackIndex];
                currentPTrack = data.positionTracks[trackIndex];
                currentLTrack = data.lookTracks[trackIndex];
            }

            // Determine which state should be returned to on the next frame.
            if (recessiveInput.primary2DAxisDown && !recessiveInput.gripButton)
            {
                int nextStateIndex = menu.GetSectorSelection(recessiveInput.primary2DAxis);

                if (nextStateIndex != (int)State.VIEW)
                {
                    // Hide the viewing screen.
                    viewScreen.GetComponent<Renderer>().enabled = false;

                    action = true;
                    dominantInput.textDisplay.text = "";
                }

                return (State)nextStateIndex;
            }
            else
            {
                // Show the number of the current track on the user's dominant text display.
                if (dominantInput.textDisplay.text != "Track " + (trackIndex + 1))
                {
                    dominantInput.textDisplay.text = "Track " + (trackIndex + 1);
                }

                return State.VIEW;
            }
        }


        // Member data
        private bool action;
        private GameObject viewScreen;
        private BezierTrack currentPTrack;
        private BezierTrack currentLTrack;
        private int trackIndex;
        private float travelTime;
    }
}