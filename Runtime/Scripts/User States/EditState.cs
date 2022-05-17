using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IVLab.VisTools;

namespace IVLab.VRDolly
{
    /// <summary>
    /// The editing state allows users to adjust or remove individual track pairs from their current configuration. This is where
    /// users can adjust control points, checkpoints, and speeds.
    /// </summary>
    public class EditState : BaseState
    {
        public EditState()
        {
            dominantSelected = false;
            recessiveSelected = false;
            adjustmentSensitivity = 0.3f;
            selectionRadius = 0.075f;
        }


        /// <summary>
        /// The edit state checks for selections on points when the user attempts to grab and edit a point. Once selected, the 
        /// dominant controller is used to add points while the recessive controller is used to remove them.
        /// </summary>
        /// <param name="dominantInput"></param>
        /// <param name="recessiveInput"></param>
        /// <param name="data"></param>
        /// <param name="menu"></param>
        /// <returns></returns>
        public override State UpdateState(InputData dominantInput, InputData recessiveInput, VRDollyData data, RadialMenu menu)
        {
            // If the dominant controller is selecting a point, determine what to do with it.
            if (dominantSelected)
            {
                if (dominantCheckpoint)
                {
                    if (dominantInput.gripButton)
                    {
                        bool changeMade = false;

                        // New experimental code that moves checkpoints based on the position of the controller.
                        float newT = dominantTrack.SampleClosestPoint(dominantInput.probe.transform.position);
                        dominantTrack.moveCheckpoint(dominantCheckpointIndex, newT);
                        dominantCheckpointIndex = data.lookTracks[data.positionTracks.IndexOf(dominantTrack)].moveCheckpoint(dominantCheckpointIndex, newT);
                        changeMade = true;
                        
                        if (dominantInput.primary2DAxis.y != 0)
                        {
                            float newSpeed = dominantTrack.GetCheckpointSpeed(dominantCheckpointIndex);
                            newSpeed += dominantInput.primary2DAxis.y * adjustmentSensitivity * Time.deltaTime;
                            dominantTrack.SetCheckpointSpeed(dominantCheckpointIndex, newSpeed);
                            data.lookTracks[data.positionTracks.IndexOf(dominantTrack)].SetCheckpointSpeed(dominantCheckpointIndex, newSpeed);
                            changeMade = true;
                        }

                        // Only update the selected point's track if a change was made.
                        if (changeMade)
                        {
                            dominantTrack.UpdateTrack();
                            data.lookTracks[data.positionTracks.IndexOf(dominantTrack)].UpdateTrack();
                        }
                    }
                    else
                    {
                        dominantSelected = false;
                    }
                }
                else
                {
                    if (dominantInput.triggerButton)
                    {
                        dominantPoint.transform.position = dominantInput.probe.transform.position;
                        dominantTrack.UpdateTrack();
                    }
                    else
                    {
                        dominantSelected = false;
                    }
                }

                // Add new points to the selected track if requested.
                if (dominantInput.primaryButtonDown)
                {
                    // Create a new control point halfway between the last two control points on the selected track.
                    List<GameObject> controlPoints = dominantTrack.GetControlPoints();
                    Vector3 halfway = (controlPoints[controlPoints.Count - 1].transform.position - controlPoints[controlPoints.Count - 2].transform.position) * 0.5f;
                    Vector3 newPosition = controlPoints[0].transform.position + halfway;
                    dominantTrack.AddControlPoint(newPosition);
                    dominantTrack.UpdateTrack();
                }
                else if (dominantInput.secondaryButtonDown && dominantPositionTrackSelected)
                {
                    // Add a checkpoint in the middle of the curve.
                    dominantTrack.AddCheckpoint(0.5f);
                    dominantTrack.UpdateTrack();
                    data.lookTracks[data.positionTracks.IndexOf(dominantTrack)].AddCheckpoint(0.5f);
                    data.lookTracks[data.positionTracks.IndexOf(dominantTrack)].UpdateTrack();
                }
            }
            else
            {
                // Test the position tracks before the look tracks.
                if (dominantInput.triggerButton)
                {
                    dominantPositionTrackSelected = TestPoints(data.positionTracks, dominantInput.probe, true, false);
                    if (!dominantPositionTrackSelected)
                    {
                        TestPoints(data.lookTracks, dominantInput.probe, true, false);
                    }
                }
                else if (dominantInput.gripButton)
                {
                    dominantPositionTrackSelected = TestPoints(data.positionTracks, dominantInput.probe, true, true);
                }
            }

            // If the recessive controller is selecting a point, determine what to do with it.
            if (recessiveSelected)
            {
                if (recessiveCheckpoint)
                {
                    if (recessiveInput.gripButton)
                    {
                        bool changeMade = false;

                        // New experimental code that moves checkpoints based on the position of the controller.
                        float newT = recessiveTrack.SampleClosestPoint(recessiveInput.probe.transform.position);
                        recessiveTrack.moveCheckpoint(recessiveCheckpointIndex, newT);
                        recessiveCheckpointIndex = data.lookTracks[data.positionTracks.IndexOf(recessiveTrack)].moveCheckpoint(recessiveCheckpointIndex, newT);
                        changeMade = true;

                        if (recessiveInput.primary2DAxis.y != 0)
                        {
                            float newSpeed = recessiveTrack.GetCheckpointSpeed(recessiveCheckpointIndex);
                            newSpeed += recessiveInput.primary2DAxis.y * adjustmentSensitivity * Time.deltaTime;
                            recessiveTrack.SetCheckpointSpeed(recessiveCheckpointIndex, newSpeed);
                            data.lookTracks[data.positionTracks.IndexOf(recessiveTrack)].SetCheckpointSpeed(recessiveCheckpointIndex, newSpeed);
                            changeMade = true;
                        }

                        // Only update the selected point's track if a change was made.
                        if (changeMade)
                        {
                            recessiveTrack.UpdateTrack();
                            data.lookTracks[data.positionTracks.IndexOf(recessiveTrack)].UpdateTrack();
                        }
                    }
                    else
                    {
                        recessiveSelected = false;
                    }
                }
                else
                {
                    if (recessiveInput.triggerButton)
                    {
                        recessivePoint.transform.position = recessiveInput.probe.transform.position;
                        recessiveTrack.UpdateTrack();
                    }
                    else
                    {
                        recessiveSelected = false;
                    }
                }

                // Pressing the primary button on the recessive controller will remove the currently selected checkpoint or intermediate control point.
                if (recessiveInput.primaryButtonDown)
                {
                    if (recessiveCheckpoint && recessivePositionTrackSelected)
                    {
                        BezierTrack lookTrack = data.lookTracks[recessiveTrackIndex];
                        recessiveTrack.DeleteCheckpoint(recessiveCheckpointIndex);
                        lookTrack.DeleteCheckpoint(recessiveCheckpointIndex);
                        recessiveTrack.UpdateTrack();
                        lookTrack.UpdateTrack();
                    }
                    else
                    {
                        recessiveTrack.DeleteControlPoint(recessiveControlPointIndex);
                        recessiveTrack.UpdateTrack();
                    }

                    recessiveSelected = false;
                }
                // Pressing the secondary button on the recessive controller will delete the entire track pair.
                else if (recessiveInput.secondaryButtonDown)
                {
                    BezierTrack positionTrack;
                    BezierTrack lookTrack;

                    // Find the other track in the pair that will be deleted.
                    if (recessivePositionTrackSelected)
                    {
                        positionTrack = recessiveTrack;
                        lookTrack = data.lookTracks[recessiveTrackIndex];
                    }
                    else
                    {
                        lookTrack = recessiveTrack;
                        positionTrack = data.positionTracks[recessiveTrackIndex];
                    }

                    // If the dominant controller is working with the deleted pair, release its selection. 
                    if (dominantSelected && dominantTrackIndex == recessiveTrackIndex)
                    {
                        dominantSelected = false;
                    } 

                    // Destroy the tracks and remove them from the track lists.
                    positionTrack.DisableTrack();
                    lookTrack.DisableTrack();
                    data.positionTracks.RemoveAt(recessiveTrackIndex);
                    data.lookTracks.RemoveAt(recessiveTrackIndex);
                    recessiveSelected = false;

                    // Delete the corresponding frustum and ray as well.
                    GameObject.Destroy(data.frustums[recessiveTrackIndex]);
                    GameObject.Destroy(data.rays[recessiveTrackIndex]);
                    data.frustums.RemoveAt(recessiveTrackIndex);
                    data.rays.RemoveAt(recessiveTrackIndex);
                    data.frustumLocations.RemoveAt(recessiveTrackIndex);
                }
            }
            else
            {
                // Test the position tracks before the look tracks.
                if (recessiveInput.triggerButton)
                {
                    recessivePositionTrackSelected = TestPoints(data.positionTracks, recessiveInput.probe, false, false);
                    if (!recessivePositionTrackSelected)
                    {
                        TestPoints(data.lookTracks, recessiveInput.probe, false, false);
                    }
                }
                else if (recessiveInput.gripButton)
                {
                    recessivePositionTrackSelected = TestPoints(data.positionTracks, recessiveInput.probe, false, true);
                }
            }

            // Determine which state to return for next frame.
            if (recessiveInput.primary2DAxisDown && !recessiveInput.gripButton)
            {
                int nextStateIndex = menu.GetSectorSelection(recessiveInput.primary2DAxis);

                if (nextStateIndex != (int)State.EDIT)
                {
                    // Release the selections if necessary.
                    dominantSelected = false;
                    recessiveSelected = false;
                }

                return (State)nextStateIndex;
            }
            else
            {
                return State.EDIT;
            }
        }

        /// <summary>
        /// This function is used to perform grip checks on track points. Once a point meets proximity requirements
        /// for a grip, the necessary state variables are adjusted and a boolean true is returned. Arguments can be
        /// supplied in order to test control points or checkpoints, as well as which hand is attempting the grip.
        /// Currently there are no spatial hierarchies in place to ease performance.
        /// </summary>
        /// <param name="tracks"></param>
        /// <param name="probe"></param>
        /// <param name="dominantController"></param>
        /// <param name="testCheckpoints"></param>
        /// <returns></returns>
        private bool TestPoints(List<BezierTrack> tracks, GameObject probe, bool dominantController, bool testCheckpoints)
        {
            if (dominantController)
            {
                dominantSelected = false;
                dominantCheckpoint = false;

                for (int i = 0; i < tracks.Count; i++)
                {
                    List<GameObject> points;
                    if (testCheckpoints)
                    {
                        points = tracks[i].GetCheckpoints();
                    }
                    else
                    {
                        points = tracks[i].GetControlPoints();
                    }

                    for (int j = 0; j < points.Count; j++)
                    {
                        float pointDistance = (points[j].transform.position - probe.transform.position).magnitude;
                        if (pointDistance <= selectionRadius)
                        {
                            dominantSelected = true;
                            dominantPoint = points[j];
                            dominantTrack = tracks[i];
                            dominantTrackIndex = i;

                            if (testCheckpoints)
                            {
                                dominantCheckpoint = true;
                                dominantCheckpointIndex = j;
                            }

                            break;
                        }
                    }

                    // Swap hands if the point was already selected.
                    if (dominantSelected)
                    {
                        if (recessiveSelected && dominantPoint == recessivePoint)
                        {
                            recessiveSelected = false;
                        }

                        return true;
                    }
                }
            }
            else
            {
                recessiveSelected = false;
                recessiveCheckpoint = false;

                for (int i = 0; i < tracks.Count; i++)
                {
                    List<GameObject> points;
                    if (testCheckpoints)
                    {
                        points = tracks[i].GetCheckpoints();
                    }
                    else
                    {
                        points = tracks[i].GetControlPoints();
                    }

                    for (int j = 0; j < points.Count; j++)
                    {
                        float pointDistance = (points[j].transform.position - probe.transform.position).magnitude;
                        if (pointDistance <= selectionRadius)
                        {
                            recessiveSelected = true;
                            recessivePoint = points[j];
                            recessiveTrack = tracks[i];
                            recessiveTrackIndex = i;

                            if (testCheckpoints)
                            {
                                recessiveCheckpoint = true;
                                recessiveCheckpointIndex = j;
                            }
                            else
                            {
                                // The recessive controller needs to access control point indices as well.
                                recessiveControlPointIndex = j;
                            }

                            break;
                        }
                    }

                    // Swap hands if the point was already selected.
                    if (recessiveSelected)
                    {
                        if (dominantSelected && dominantPoint == recessivePoint)
                        {
                            dominantSelected = false;
                        }

                        return true;
                    }
                }
            }

            // Return false if no point was found.
            probe.GetComponent<Renderer>().enabled = true;
            return false;
        }


        // Member data used for updating and maintaining selections.
        private bool dominantSelected;
        private bool recessiveSelected;
        private bool dominantPositionTrackSelected;
        private bool recessivePositionTrackSelected;
        private bool dominantCheckpoint;
        private bool recessiveCheckpoint;
        private GameObject dominantPoint;
        private GameObject recessivePoint;
        private BezierTrack dominantTrack;
        private BezierTrack recessiveTrack;

        private int dominantCheckpointIndex;
        private int recessiveControlPointIndex;
        private int recessiveCheckpointIndex;
        private int dominantTrackIndex;
        private int recessiveTrackIndex;

        // Float values for travel
        private float selectionRadius;
        private float adjustmentSensitivity;

    }
}
