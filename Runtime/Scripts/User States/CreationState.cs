using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IVLab.VisTools;

namespace IVLab.VRDolly
{
    /// <summary>
    /// The Creation state handles the instantiation of new track pairs. The user is able to set four points
    /// that later define the start and end points for the new pair.
    /// </summary>
    public class CreationState : BaseState
    {
        public CreationState()
        {
            Shader unlitWithColor = Shader.Find("Unlit/Color");

            points = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.layer = 5;
                point.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                point.GetComponent<Renderer>().material.shader = unlitWithColor;
                point.GetComponent<Renderer>().material.color = Color.Lerp(Color.yellow, Color.red, 0.5f);
                point.GetComponent<Renderer>().enabled = false;
                point.name = "Creation Marker";
                points[i] = point;
            }

            placementIndex = 0;
        }

        
        /// <summary>
        /// The creation state detects whether the user is attempting to set down points. Once four points have been set, a new
        /// track pair is instantiated and added to the scene.
        /// </summary>
        /// <param name="dominantInput"></param>
        /// <param name="recessiveInput"></param>
        /// <param name="data"></param>
        /// <param name="menu"></param>
        /// <returns></returns>
        public override State UpdateState(InputData dominantInput, InputData recessiveInput, VRDollyData data, RadialMenu menu)
        {
            // Create a new track pair once enough points have been specified.
            if (placementIndex >= 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    points[i].GetComponent<Renderer>().enabled = false;
                }

                // Instantiate the new track data.
                BezierTrack positionTrack = new BezierTrack(points[0].transform.localPosition, points[1].transform.localPosition);
                BezierTrack lookTrack = new BezierTrack(points[2].transform.localPosition, points[3].transform.localPosition);
                positionTrack.GetTrackObject().transform.SetParent(data.anchor.transform);
                lookTrack.GetTrackObject().transform.SetParent(data.anchor.transform);
                data.positionTracks.Add(positionTrack);
                data.lookTracks.Add(lookTrack);
                positionTrack.UpdateTrack();
                lookTrack.UpdateTrack();

                // Create a new frustum and add it to the track data.
                Mesh frustumMesh = Resources.Load<Mesh>("frustum");
                GameObject frustum = new GameObject();
                frustum.transform.SetParent(positionTrack.GetTrackObject().transform);
                frustum.name = "position frustum";
                frustum.layer = 5;
                frustum.AddComponent(typeof(MeshFilter));
                frustum.AddComponent(typeof(MeshRenderer));
                frustum.GetComponent<MeshFilter>().mesh = frustumMesh;
                frustum.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color"));
                frustum.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                data.frustums.Add(frustum);
                data.frustumLocations.Add(0);

                // Create a new ray and add it to the track data.
                Mesh rayMesh = Resources.Load<Mesh>("ray");
                GameObject ray = new GameObject();
                ray.transform.SetParent(lookTrack.GetTrackObject().transform);
                ray.name = "look ray";
                ray.layer = 5;
                ray.AddComponent(typeof(MeshFilter));
                ray.AddComponent(typeof(MeshRenderer));
                ray.GetComponent<MeshFilter>().mesh = rayMesh;
                ray.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color"));
                ray.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                data.rays.Add(ray);

                // Allow the user to set down another track pair if they want to.
                placementIndex = 0;
            }
            // Otherwise, check for new placements.
            else if (dominantInput.triggerButtonDown)
            {
                points[placementIndex].transform.position = dominantInput.probe.transform.position;
                points[placementIndex].GetComponent<Renderer>().enabled = true;
                placementIndex++;
            }
            else if (recessiveInput.triggerButtonDown)
            {
                points[placementIndex].transform.position = recessiveInput.probe.transform.position;
                points[placementIndex].GetComponent<Renderer>().enabled = true;
                placementIndex++;
            }

            // Reset the state and quit early if the user selects a different state.
            if (recessiveInput.primary2DAxisDown && !recessiveInput.gripButton)
            {
                int nextStateIndex = menu.GetSectorSelection(recessiveInput.primary2DAxis);

                if (nextStateIndex != (int)State.CREATION)
                {
                    for (int i = 0; i < placementIndex; i++)
                    {
                        points[i].GetComponent<Renderer>().enabled = false;
                    }

                    placementIndex = 0;
                }

                return (State)nextStateIndex;
            }

            return State.CREATION;
        }
        

        // Member data
        private GameObject[] points;
        int placementIndex;
    }
}