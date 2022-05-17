using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IVLab.VRDolly
{
    /// <summary>
    /// BezierTrack implements a traversable and adjustable Bezier curve.
    /// Each track is comprised of two or more control points which determine
    /// the shape of the curve using De Casteljau's Algorithm. New intermediate
    /// control points can be added or removed dynamically. Travel speed is
    /// adjusted by setting and moving checkpoints along the track that enforce
    /// linear interpolation between speeds.
    /// </summary>
    public class BezierTrack
    {
        /// <summary>
        /// BezierTrack's constructor takes in three optional arguments:
        /// 1. The position of the track's starting point (0-vector by default).
        /// 2. The position of the track's end point (0-vector by default).
        /// 3. A TrackSaveState object (NULL by default).
        /// If a TrackSaveState is provided, the values of the start and end positions
        /// are ignored in favor of the configuration described by the save data.
        /// </summary>
        public BezierTrack(Vector3 startPosition = new Vector3(), Vector3 endPosition = new Vector3(), TrackSaveState saveState = null)
        {
            // Initialize the track's primary game object and its parent.
            self = new GameObject("Track");
            // Future Note: The line below does not update the hierarchy!
            // self.transform.SetParent(GameObject.Find("Tracks Anchor").transform);

            // Initialize the track's data lists.
            controlPoints = new List<GameObject>();
            checkpoints = new List<GameObject>();
            checkpointTValues = new List<float>();
            checkpointSpeeds = new List<float>();

            // Adjust these values in order to change the speed range of the checkpoints.
            maxSpeed = 0.3f;
            minSpeed = 0.01f;

            // Find the shaders needed to render the track's GUI.
            unlitWithColor = Shader.Find("Unlit/Color");
            curveShader = Shader.Find("Sprites/Default");

            // Create the sphere GUI for the start point.
            GameObject startGUI = BuildTrackPoint(startPosition, calcSpeedColor(0.5f), "Start Point");

            // Create the sphere GUI for the end point.
            GameObject endGUI = BuildTrackPoint(endPosition, calcSpeedColor(0.5f), "End Point");

            // Add the spheres to the control points list.
            controlPoints.Add(startGUI);
            controlPoints.Add(endGUI);

            // The first and last control points are also treated as a permanent checkpoints.
            checkpoints.Add(startGUI);
            checkpointTValues.Add(0);
            checkpointSpeeds.Add((minSpeed + maxSpeed) / 2);

            checkpoints.Add(endGUI);
            checkpointTValues.Add(1);
            checkpointSpeeds.Add((minSpeed + maxSpeed) / 2);

            // Initialize the line renderer for the control point links.
            pointLines = new GameObject("Control Point Links");
            pointLines.transform.SetParent(self.transform, false);
            pointLines.layer = 5;
            LineRenderer pointLinks = pointLines.AddComponent<LineRenderer>();
            pointLinks.material.shader = unlitWithColor;
            pointLinks.material.color = Color.white;
            pointLinks.loop = false;
            pointLinks.startWidth = 0.01f;
            pointLinks.endWidth = 0.01f;
            Vector3[] startingLine = { startPosition, endPosition };
            pointLinks.GetComponent<LineRenderer>().positionCount = 2;
            pointLinks.GetComponent<LineRenderer>().SetPositions(startingLine);

            // Initialize the list of curve renderers.
            curveLines = new List<GameObject>();

            // If a valid save state was given as an argument, populate the track with its data.
            if (saveState != null && saveState.controlPointPositions.Count >= 2)
            {
                // Update the start and end control point positions based on the save state.
                List<float> startComponents = saveState.controlPointPositions[0];
                controlPoints[0].transform.position = new Vector3(startComponents[0], startComponents[1], startComponents[2]);
                List<float> endComponents = saveState.controlPointPositions[saveState.controlPointPositions.Count - 1];
                controlPoints[controlPoints.Count - 1].transform.position = new Vector3(endComponents[0], endComponents[1], endComponents[2]);

                // Add the intermediate control points based on the save state.
                for (int i = 1; i < saveState.controlPointPositions.Count - 1; i++)
                {
                    List<float> components = saveState.controlPointPositions[i];
                    AddControlPoint(new Vector3(components[0], components[1], components[2]));
                }

                // Add the intermediate checkpoints based on the save state.
                for (int i = 1; i < saveState.checkpointTValues.Count - 1; i++)
                {
                    AddCheckpoint(saveState.checkpointTValues[i]);
                }

                // Update the speeds of each checkpoint based on the save state.
                for (int i = 0; i < saveState.checkpointSpeeds.Count; i++)
                {
                    // FUTURE NOTE: The speed limits aren't enforced here!!
                    SetCheckpointSpeed(i, saveState.checkpointSpeeds[i]);
                }
            }

            // Finally, update the track's GUI to reflect the track's structure.
            UpdateTrack();
        }

        /// <summary>
        /// This function acts as an unnoficial destructor for a track. It destroys
        /// all the GameObjects used to build the track and leaves the rest for C#'s
        /// garbage collector.
        /// </summary>
        public void DisableTrack()
        {
            // Destroy the control points.
            foreach(GameObject controlPoint in controlPoints)
            {
                GameObject.Destroy(controlPoint);
            }

            // Destroy the intermediate checkpoints.
            for (int i = 1; i < checkpoints.Count - 1; i++)
            {
                GameObject.Destroy(checkpoints[i]);
            }

            // Destroy the line renderers.
            GameObject.Destroy(pointLines);
            foreach(GameObject line in curveLines)
            {
                GameObject.Destroy(line);
            }

            // Finally, destroy the GameObject that the visual elements were attached to.
            GameObject.Destroy(self);
        }

        /// <summary>
        /// This helper function runs De Casteljau's algorithm recursively in order to calculate a point on the curve.
        /// </summary>
        private Vector3 DeCasteljau(float t, int i, int j)
        {
            if (j == 0)
            {
                return controlPoints[i].transform.position;
            }
            else
            {
                Vector3 curvePosition = (1 - t) * DeCasteljau(t, i, j - 1) + t * DeCasteljau(t, i + 1, j - 1);
                return curvePosition;
            }
        }

        /// <summary>
        /// This helper function is the same as DeCasteljau(t, i, j) except it uses the local positions of the control points.
        /// </summary>
        private Vector3 DeCasteljauLocal(float t, int i, int j)
        {
            if (j == 0)
            {
                return controlPoints[i].transform.localPosition;
            }
            else
            {
                Vector3 curvePosition = (1 - t) * DeCasteljau(t, i, j - 1) + t * DeCasteljau(t, i + 1, j - 1);
                return curvePosition;
            }
        }

        /// <summary>
        /// This function returns a position vector along the track based on a t-value in the range
        /// [0-1]. An additional boolean argument can be supplied that returns a local position
        /// relative to the track's parent GameObject if set to true.
        /// </summary>
        public Vector3 GetLocationOnCurve(float t, bool local = false)
        {
            if (t < 0 || t > 1)
            {
                // Return the zero vector if t is out of range.
                return new Vector3(0, 0, 0);
            }
            else
            {
                // Otherwise, return the point on the curve that corresponds with t.
                if (local)
                {
                    return DeCasteljauLocal(t, 0, controlPoints.Count - 1);
                }
                else
                {
                    return DeCasteljau(t, 0, controlPoints.Count - 1);
                }
            }
        }

        /// <summary>
        /// This function returns a t-value [0-1] corresponding to the closest 
        /// point along the track relative to an arbitrary position supplied by 
        /// the user. The current approach simply iterates across the track and 
        /// compares vector distances.
        /// </summary>
        public float SampleClosestPoint(Vector3 targetPoint)
        {
            float increment = 0.01f;
            float bestTargetDist = float.PositiveInfinity;
            float bestT = 0; ;

            // Sample the curve for the closest point.
            for (float t = 0; t <= 1; t += increment)
            {
                Vector3 pointOnCurve = GetLocationOnCurve(t);
                float targetDist = Vector3.Distance(pointOnCurve, targetPoint);
                if (targetDist < bestTargetDist) 
                {
                    bestTargetDist = targetDist;
                    bestT = t;
                }
            }

            return bestT;
        }

        /// <summary>
        /// This function takes a positon vector from the user and creates a new control
        /// point for the track with it. This alters the shape of the track as a result.
        /// </summary>
        public void AddControlPoint(Vector3 newPosition)
        {
            // Create the sphere GUI for the new control point.
            GameObject newControlPoint = BuildTrackPoint(newPosition, Color.blue, "Control Point");

            // Find the point that is closest to the new point.
            int closestIndex = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < controlPoints.Count; i++)
            {
                float newDistance = (newPosition - controlPoints[i].transform.position).magnitude;
                if (newDistance < closestDistance)
                {
                    closestIndex = i;
                    closestDistance = newDistance;
                }
            }

            // Find the most "natural" place to insert the new control point
            // based on the closest point and its neighbors. 
            if (closestIndex == 0)
            {
                controlPoints.Insert(1, newControlPoint);
            }
            else if (closestIndex == controlPoints.Count - 1)
            {
                controlPoints.Insert(closestIndex, newControlPoint);
            }
            else
            {
                float leftDistance = (newPosition - controlPoints[closestIndex - 1].transform.position).magnitude;
                float rightDistance = (newPosition - controlPoints[closestIndex + 1].transform.position).magnitude;

                if (leftDistance < rightDistance)
                {
                    controlPoints.Insert(closestIndex, newControlPoint);
                }
                else
                {
                    controlPoints.Insert(closestIndex + 1, newControlPoint);
                }
            }
        }

        /// <summary>
        /// This function takes a list index and deletes the corresponding control point from the track.
        /// </summary>
        public void DeleteControlPoint(int index)
        {
            if (index > 0 && index < controlPoints.Count - 1)
            {
                GameObject removedPoint = controlPoints[index];
                controlPoints.RemoveAt(index);
                GameObject.Destroy(removedPoint);
            }
        }

        /// <summary>
        /// This function takes a t-value [0-1] and adds a checkpoint to the track at the corresponding
        /// location along its curve. The default speed is set halfway between the current min and max
        /// speed limits.
        /// </summary>
        public int AddCheckpoint(float t)
        {
            // Find the right index to insert the new checkpoint at.
            int index = 0;
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpointTValues[i] <= t)
                {
                    index++;
                }
                else
                {
                    break;
                }
            }

            // Create the sphere GUI for the new checkpoint.
            GameObject newCheckpoint = BuildTrackPoint(GetLocationOnCurve(t), calcSpeedColor(0.5f), "Checkpoint");

            // Store the data for the new point and update the curve.
            checkpoints.Insert(index, newCheckpoint);
            checkpointTValues.Insert(index, t);
            checkpointSpeeds.Insert(index, (minSpeed + maxSpeed) / 2);

            // Return the index that the new checkpoint is stored at.
            return index;
        }

        /// <summary>
        /// This function takes a list index and deletes the corresponding checkpoint from the track
        /// with the condition that it is not one of the track's endpoints.
        /// </summary>
        public void DeleteCheckpoint(int index)
        {
            if (index > 0 && index < checkpoints.Count - 1)
            {
                GameObject removedPoint = checkpoints[index];
                checkpoints.RemoveAt(index);
                checkpointTValues.RemoveAt(index);
                checkpointSpeeds.RemoveAt(index);
                GameObject.Destroy(removedPoint);
            }
        }

        /// <summary>
        /// This function is used to visually update the track based on its most recent control point and checkpoint data.
        /// This process currently involves deleting and reinstantiating the track's curve renderers and should only
        /// be called when changes are necessary. This function is automatically called when edits to the track's 
        /// points are made, but there are scenarios where a manual call may be necessary (such as when a track's 
        /// parent's transform has been altered since the curve renderers use world-space).
        /// </summary>
        public void UpdateTrack()
        {
            // Only draw links between the control points if more than two are present.
            if (controlPoints.Count == 2)
            {
                pointLines.GetComponent<LineRenderer>().enabled = false;
            }
            else
            {
                // Update the number of points for the links renderer.
                pointLines.GetComponent<LineRenderer>().positionCount = controlPoints.Count;

                // Update the point lines renderer.
                Vector3[] pointPositions = new Vector3[controlPoints.Count];

                for (int i = 0; i < controlPoints.Count; i++)
                {
                    pointPositions[i] = controlPoints[i].transform.position;
                }

                LineRenderer pointLinks = pointLines.GetComponent<LineRenderer>();
                pointLinks.startWidth = 0.2f * controlPoints[0].transform.lossyScale.x;
                pointLinks.endWidth = 0.2f * controlPoints[0].transform.lossyScale.x;
                pointLinks.SetPositions(pointPositions);
                pointLinks.GetComponent<LineRenderer>().enabled = true;
            }

            // Destroy and remove the current curve line renderers.
            for (int i = curveLines.Count - 1; i >= 0; i--)
            {
                GameObject removedRenderer = curveLines[i];
                curveLines.RemoveAt(i);
                GameObject.Destroy(removedRenderer);
            }

            // Start the speed at the first checkpoint.
            float currentSpeed = checkpointSpeeds[0];

            // Use these variables to parse the curve into 1-second intervals.
            float timeIncrement = 0;
            List<Vector3> newLine = new List<Vector3>();

            // The curve's lines will use color to indicate speed (yellow = minSpeed, red = maxSpeed).
            Color startColor = calcSpeedColor((currentSpeed - minSpeed) / (maxSpeed - minSpeed));
            Color endColor;

            // Iterate across the track and create GUI timesteps over 1-second intervals.
            for (float t = 0; t <= 1; t += currentSpeed * Time.deltaTime)
            {
                timeIncrement += Time.deltaTime;
                currentSpeed = GetCurrentSpeed(t);

                // If the time increment has reached a second, create a new line segment.
                if (timeIncrement > 1)
                {
                    // Reset the time increment.
                    timeIncrement = 0;

                    // Create and add the new line renderer.
                    endColor = calcSpeedColor((currentSpeed - minSpeed) / (maxSpeed - minSpeed));
                    GameObject curveSegment = new GameObject("Curve Segment");
                    curveSegment.layer = 5;
                    curveSegment.transform.SetParent(self.transform, false);
                    LineRenderer newRenderer = curveSegment.AddComponent<LineRenderer>();
                    newRenderer.material.shader = curveShader;
                    newRenderer.startColor = startColor;
                    newRenderer.endColor = endColor;
                    newRenderer.loop = false;
                    newRenderer.startWidth = 0.2f * controlPoints[0].transform.lossyScale.x;
                    newRenderer.endWidth = 0.1f * controlPoints[0].transform.lossyScale.x;
                    newRenderer.positionCount = newLine.Count;
                    newRenderer.SetPositions(newLine.ToArray());
                    curveLines.Add(curveSegment);

                    // Create a new list for more points to be added.
                    newLine = new List<Vector3>();

                    // Increment the starting color for the next line.
                    startColor = endColor;
                }

                // Otherwise, continue to sample points along the curve for the current line.
                else
                {
                    Vector3 newPoint = GetLocationOnCurve(t);
                    newLine.Add(newPoint);
                }
            }

            // Use the remaining points to add a final line renderer at the end of the curve.
            endColor = calcSpeedColor((currentSpeed - minSpeed) / (maxSpeed - minSpeed));
            GameObject finalSegment = new GameObject("Curve Segment");
            finalSegment.layer = 5;
            finalSegment.transform.SetParent(self.transform, false);
            LineRenderer finalRenderer = finalSegment.AddComponent<LineRenderer>();
            finalRenderer.material.shader = curveShader;
            finalRenderer.startColor = startColor;
            finalRenderer.endColor = endColor;
            finalRenderer.loop = false;
            finalRenderer.startWidth = 0.2f * controlPoints[0].transform.lossyScale.x;
            finalRenderer.endWidth = 0.1f * controlPoints[0].transform.lossyScale.x;
            finalRenderer.positionCount = newLine.Count;
            finalRenderer.SetPositions(newLine.ToArray());
            curveLines.Add(finalSegment);

            // Update the positions of the checkpoints so that they are still on the curve.
            for (int i = 0; i < checkpoints.Count; i++)
            {
                checkpoints[i].transform.position = GetLocationOnCurve(checkpointTValues[i]);
            }
        }

        /// <summary>
        /// This helper is used to calculate colors for the track's checkpoints based on their 
        /// speed settings. Colors are currently set to lerp from white to red as the speed 
        /// of a checkpoint increases.
        /// </summary>
        private Color calcSpeedColor(float i)
        {
            if (i < 0 || i > 1)
            {
                // Return black if the interval is out of range.
                Debug.Log("An out of speed color was requested.");
                return new Color(0, 0, 0);
            }
            else if (i < 0.33)
            {
                return Color.Lerp(Color.white, Color.yellow, i / 0.33f);
            }
            else if (i >= 0.33 && i < 0.66)
            {
                return Color.Lerp(Color.yellow, new Color(1, 0.647f, 0), (i - 0.33f) / 0.33f);
            }
            else
            {
                return Color.Lerp(new Color(1, 0.647f, 0), Color.red, (i - 0.66f) / 0.33f);
            }
        }

        /// <summary>
        /// This function takes in a t-value [0-1] and returns a speed value corresponding to its
        /// position along the track. The result of this function is determined based on the 
        /// track's current checkpoint arrangement.
        /// </summary>
        public float GetCurrentSpeed(float t)
        {
            int currentCheckpoint = 0;
            int nextCheckpoint = 1;

            // Find the two checkpoints that t is between.
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpointTValues[i] <= t)
                {
                    currentCheckpoint = i;

                    if (i != checkpoints.Count - 1)
                    {
                        nextCheckpoint = i + 1;
                    }
                }
                else
                {
                    break;
                }
            }

            // Return the correct speed.
            if (currentCheckpoint == checkpoints.Count - 1)
            {
                return checkpointSpeeds[currentCheckpoint];
            }
            else
            {
                // Lerp between checkpoints if necessary.
                float startSpeed = checkpointSpeeds[currentCheckpoint];
                float endSpeed = checkpointSpeeds[nextCheckpoint];
                float speedRatio = (t - checkpointTValues[currentCheckpoint]) / (checkpointTValues[nextCheckpoint] - checkpointTValues[currentCheckpoint]);
                return Mathf.Lerp(startSpeed, endSpeed, speedRatio);
            }
        }

        /// <summary>
        /// This function takes a boolean value that toggles whether the track's visual elements are currently being rendered.
        /// This is useful in situations where a track needs to he hidden without being outright deleted.
        /// </summary>
        public void toggleGUI(bool toggle)
        {
            // Toggle the control points.
            for (int i = 0; i < controlPoints.Count; i++)
            {
                controlPoints[i].GetComponent<Renderer>().enabled = toggle;
            }

            // Toggle the check points (skip the start and end points).
            for (int i = 1; i < checkpoints.Count - 1; i++)
            {
                checkpoints[i].GetComponent<Renderer>().enabled = toggle;
            }

            // Only toggle the control point links if the track is curved.
            if (controlPoints.Count > 2)
            {
                pointLines.GetComponent<LineRenderer>().enabled = toggle;
            }

            // Toggle the track's curve.
            for (int i = 0; i < curveLines.Count; i++)
            {
                curveLines[i].GetComponent<LineRenderer>().enabled = toggle;
            }
        }

        /// <summary>
        /// This function returns the list of control points for the track.
        /// </summary>
        public List<GameObject> GetControlPoints() { return controlPoints; }

        /// <summary>
        /// This function returns the list of checkpoints for the track.
        /// </summary>
        public List<GameObject> GetCheckpoints() { return checkpoints; }

        /// <summary>
        /// This function returns the list of checkpoint t-values for the track.
        /// </summary>
        public float GetCheckpointT(int index) { return checkpointTValues[index]; }

        /// <summary>
        /// This function returns the list of checkpoint speeds for the track.
        /// </summary>
        public float GetCheckpointSpeed(int index) { return checkpointSpeeds[index]; }

        /// <summary>
        /// This function takes a list index and a speed value in order to update the speed of the 
        /// corresponding checkpoint (after clamping if necessary).
        /// </summary>
        public void SetCheckpointSpeed(int index, float newSpeed)
        {
            if (index >= 0 && index < checkpoints.Count)
            {
                // Clamp the speed if necessary.
                if (newSpeed < minSpeed) newSpeed = minSpeed;
                if (newSpeed > maxSpeed) newSpeed = maxSpeed;

                // Set speed and color.
                checkpointSpeeds[index] = newSpeed;
                checkpoints[index].GetComponent<Renderer>().material.color = calcSpeedColor((newSpeed - minSpeed) / (maxSpeed - minSpeed));

                // FUTURE NOTE: UpdateTrack() should be called here if the speed was changed to a new value!!
            }
        }

        /// <summary>
        /// This function takes a list index and a t-value [0-1] in order to update the corresponding checkpoint's position
        /// along the track's curve.
        /// </summary>
        public int moveCheckpoint(int index, float t)
        {
            if (index >= 1 && index < checkpoints.Count - 1)
            {
                // Clamp t if necessary.
                if (t < 0.01) t = 0.01f;
                if (t > 0.99) t = 0.99f;

                // Retrieve the data for the checkpoint.
                GameObject checkpoint = checkpoints[index];
                float speed = checkpointSpeeds[index];

                // Remove the checkpoint from its original position.
                checkpoints.RemoveAt(index);
                checkpointTValues.RemoveAt(index);
                checkpointSpeeds.RemoveAt(index);

                // Find the right index to re-insert the checkpoint at.
                int newIndex = 0;
                for (int i = 0; i < checkpoints.Count; i++)
                {
                    if (checkpointTValues[i] <= t)
                    {
                        newIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                // Re-insert the checkpoint and update the track.
                checkpoints.Insert(newIndex, checkpoint);
                checkpointTValues.Insert(newIndex, t);
                checkpointSpeeds.Insert(newIndex, speed);
                checkpoint.GetComponent<Renderer>().material.color = calcSpeedColor((speed - minSpeed) / (maxSpeed - minSpeed));

                // Return the new index of the checkpoint.
                return newIndex;
            }

            // Return the bouding indices or -1 otherwise.
            if (index == 0) return 0;
            if (index == checkpoints.Count - 1) return checkpoints.Count - 1;
            return -1;
        }

        /// <summary>
        /// This helper function builds and returns sphere GameObjects to use for track points. This function 
        /// was implemented to help reduce code redundancy. 
        /// </summary>
        private GameObject BuildTrackPoint(Vector3 pointPosition = new Vector3(), Color pointColor = new Color(), string pointName = "point") {
            // Create a new point and return it.
            GameObject newPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newPoint.transform.SetParent(self.transform);
            newPoint.name = pointName;
            newPoint.layer = 5;
            newPoint.GetComponent<Renderer>().material.shader = unlitWithColor;
            newPoint.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            newPoint.GetComponent<Renderer>().material.color = pointColor;
            newPoint.transform.position = pointPosition;
            return newPoint;
        }

        /// <summary>
        /// This function creates and returns a TrackSaveState instance which can later 
        /// converted into a JSON format for saving/loading the track's current configuration.
        /// </summary>
        public TrackSaveState MakeSaveState()
        {
            return new TrackSaveState(controlPoints, checkpointTValues, checkpointSpeeds);
        }

        /// <summary>
        /// This function returns the 'Track' GameObject that the curve components are directly anchored to.
        /// </summary>
        public GameObject GetTrackObject()
        {
            return self;
        }

        /*
        Member Data Starts below: 
        */

        private GameObject self;

        /// <summary>
        /// The maximum speed that checkpoints allow.
        /// </summary>
        private float maxSpeed;
        /// <summary>
        /// The minimum speed that checkpoints allow.
        /// </summary>
        private float minSpeed;

        // Control and end points for the curve are represented as sphere primitives.
        private List<GameObject> controlPoints;

        // this list contains points along the curve that help segment it.
        private List<GameObject> checkpoints;

        // This list keeps the t-values for each checkpoint.
        private List<float> checkpointTValues;

        // This list keeps the track speeds past each checkpoint.
        private List<float> checkpointSpeeds;

        // This line renderer object draws links between the control and end points.
        private GameObject pointLines;

        // This adjustable list of line renderers is used to draw the curve over 1-second intervals.
        private List<GameObject> curveLines;

        // These shaders are used for the control points and curve respectively.
        private Shader unlitWithColor;
        private Shader curveShader;
    }
}