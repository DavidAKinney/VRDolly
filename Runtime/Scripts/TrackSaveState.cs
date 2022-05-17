using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IVLab.VRDolly
{
    /// <summary>
    /// This class is used to store data from a Bezier track instance
    /// for use with VRDolly's JSON save/load functionality.
    /// </summary>
    public class TrackSaveState
    {
        /// <summary>
        /// The TrackSaveState constructor takes in all necessary data
        /// for rebuilding a customized Bezier Track from scratch. Specifically:
        /// 1. The list of control point objects that define the track's shape
        /// 2. The list of t-values that correspond to the checkpoint positions
        /// 3. A list of floats representing the travel speeds of each checkpoint
        /// </summary>
        public TrackSaveState(List<GameObject> controlPoints, List<float> checkpointTValues, List<float> checkpointSpeeds)
        {
            // Initialize the data lists.
            controlPointPositions = new List<List<float>>();
            this.checkpointTValues = new List<float>();
            this.checkpointSpeeds = new List<float>();

            // Save the control point positions.
            for (int i = 0; i < controlPoints.Count; i++)
            {
                Vector3 pointPosition = controlPoints[i].transform.position;
                controlPointPositions.Add(new List<float>(new float[] {pointPosition.x, pointPosition.y, pointPosition.z}));
            }

            // Save the checkpoint t-values.
            for (int i = 0; i < checkpointTValues.Count; i++)
            {
                this.checkpointTValues.Add(checkpointTValues[i]);
            }

            // Save the checkpoint speeds.
            for (int i = 0; i < checkpointSpeeds.Count; i++)
            {
                this.checkpointSpeeds.Add(checkpointSpeeds[i]);
            }
        }

        /// <summary>
        /// In order to comply with Newtonsoft JSON, TrackSaveState requires a 
        /// default constructor. This allows the package to instantiate 
        /// TrackSaveState instances before copying over the actual data.
        /// Otherwise, this should not be called directly!
        /// </summary>
        public TrackSaveState()
        {
            
        }

        /// <summary>
        /// A nested list that holds xyz-coordinates for control points.
        /// </summary>
        public List<List<float>> controlPointPositions;

        /// <summary>
        /// A list that holds t-values for checkpoint positions.
        /// </summary>
        public List<float> checkpointTValues;

        /// <summary>
        /// A list that holds the speeds of each checkpoint.
        /// </summary>
        public List<float> checkpointSpeeds;
    }
}
