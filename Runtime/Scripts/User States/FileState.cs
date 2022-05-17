using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IVLab.VisTools;

namespace IVLab.VRDolly
{
    /// <summary>
    /// The file state handles loading and saving track configurations for later use. Tracks are stored as JSON data
    /// in a project's persistent data folder.
    /// </summary>
    public class FileState : BaseState
    {
        // This constructor finds the current number of available track configurations and reports it to the debug log.
        public FileState()
        {
            // Get all the json track files currently stored in the persistant data path.
            string[] JSONFilePaths = Directory.GetFiles(Application.persistentDataPath, "track_state_*.json", SearchOption.TopDirectoryOnly);
            trackFiles = new List<string>(JSONFilePaths);
            Debug.Log("There are currently (" + trackFiles.Count + ") track states present on your system.");

            trackFiles.Add("<save to new file>");
            fileIndex = trackFiles.Count - 1;
        }

        /// <summary>
        /// The file state handles requests from the user to overwrite, save, or load track states from memory.
        /// </summary>
        /// <param name="dominantInput"></param>
        /// <param name="recessiveInput"></param>
        /// <param name="data"></param>
        /// <param name="menu"></param>
        /// <returns></returns>
        public override State UpdateState(InputData dominantInput, InputData recessiveInput, VRDollyData data, RadialMenu menu)
        {
            // If the user presses the primary button on their dominant controller, save the current track states to JSON.
            if (dominantInput.primaryButtonDown)
            {
                // Create a new save state struct and store the current track data inside it.
                SaveData savedTrackData = new SaveData();
                savedTrackData.positionTracks = new List<TrackSaveState>();
                savedTrackData.lookTracks = new List<TrackSaveState>();
                for (int i = 0; i < data.positionTracks.Count; i++)
                {
                    savedTrackData.positionTracks.Add(data.positionTracks[i].MakeSaveState());
                    savedTrackData.lookTracks.Add(data.lookTracks[i].MakeSaveState());
                }

                // Either write to a new file or overwrite an existing one.
                if (fileIndex == trackFiles.Count - 1)
                {
                    SaveJSONFile(savedTrackData);
                }
                else
                {
                    SaveJSONFile(savedTrackData, trackFiles[fileIndex]);
                }

                // Set the file index back to the 'new save' setting.
                fileIndex = trackFiles.Count - 1;
            }

            // If the user presses the secondary button down on their dominant controller, load the currently selected state file.
            if (dominantInput.secondaryButtonDown && fileIndex != trackFiles.Count - 1)
            {
                // Only change the current track state if the selected file still exists.
                if (File.Exists(trackFiles[fileIndex]))
                {
                    // Destroy all the track GUI that is currently present.
                    for (int i = 0; i < data.positionTracks.Count; i++)
                    {
                        data.positionTracks[i].DisableTrack();
                        data.lookTracks[i].DisableTrack();
                        GameObject.Destroy(data.frustums[i]);
                        GameObject.Destroy(data.rays[i]);
                    }

                    // Reset the tracks data before loading.
                    data.positionTracks.Clear();
                    data.lookTracks.Clear();
                    data.frustums.Clear();
                    data.rays.Clear();
                    data.frustumLocations.Clear();

                    // Retrieve the data from the JSON file as a string.
                    string JSONData = File.ReadAllText(trackFiles[fileIndex]);

                    // Deserialize the selected JSON state.
                    SaveData loadedTrackData = Newtonsoft.Json.JsonConvert.DeserializeObject<SaveData>(JSONData);

                    for (int i = 0; i < loadedTrackData.positionTracks.Count; i++)
                    {
                        data.positionTracks.Add(new BezierTrack(saveState: loadedTrackData.positionTracks[i]));
                        data.lookTracks.Add(new BezierTrack(saveState: loadedTrackData.lookTracks[i]));

                        // Create a new frustum and add it to the track data.
                        Mesh frustumMesh = Resources.Load<Mesh>("frustum");
                        GameObject frustum = new GameObject();
                        frustum.name = "position frustum";
                        frustum.AddComponent(typeof(MeshFilter));
                        frustum.AddComponent(typeof(MeshRenderer));
                        frustum.GetComponent<MeshFilter>().mesh = frustumMesh;
                        frustum.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color"));
                        data.frustums.Add(frustum);
                        data.frustumLocations.Add(0);

                        // Create a new ray and add it to the track data.
                        Mesh rayMesh = Resources.Load<Mesh>("ray");
                        GameObject ray = new GameObject();
                        ray.name = "look ray";
                        ray.AddComponent(typeof(MeshFilter));
                        ray.AddComponent(typeof(MeshRenderer));
                        ray.GetComponent<MeshFilter>().mesh = rayMesh;
                        ray.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color"));
                        data.rays.Add(ray);

                        Debug.Log("Loaded " + trackFiles[fileIndex] + " as the current state.");
                    }
                } 
            }

            // Delete the currently selected file with the secondary button on the recessive controller.
            if (recessiveInput.secondaryButtonDown && fileIndex != trackFiles.Count - 1)
            {
                File.Delete(trackFiles[fileIndex]);
                Debug.Log("You deleted the track state at: " + trackFiles[fileIndex]);
                trackFiles.RemoveAt(fileIndex);
                fileIndex = trackFiles.Count - 1;
            }

            // Cycle through which file to load/overwrite/delete with the dominant thumbstick.
            if (dominantInput.primary2DAxisDown && dominantInput.primary2DAxis.y != 0)
            {
                if (dominantInput.primary2DAxis.y > 0)
                {
                    fileIndex++;
                    if (fileIndex > trackFiles.Count - 1)
                    {
                        fileIndex = 0;
                    }
                }
                else
                {
                    fileIndex--;
                    if (fileIndex < 0)
                    {
                        fileIndex = trackFiles.Count - 1;
                    }
                }
            }

            // Determine which state to return for next frame.
            if (recessiveInput.primary2DAxisDown && !recessiveInput.gripButton)
            {
                int nextStateIndex = menu.GetSectorSelection(recessiveInput.primary2DAxis);

                if (nextStateIndex != (int)State.FILE)
                {
                    dominantInput.textDisplay.text = "";
                    fileIndex = trackFiles.Count - 1;
                }

                return (State)nextStateIndex;
            }
            else
            {
                dominantInput.textDisplay.text = trackFiles[fileIndex];
                return State.FILE;
            }
        }

        /// <summary>
        /// This function takes a SaveData object and converts it to a JSON file in the project's
        /// persistent data path.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="filePathName"></param>
        private void SaveJSONFile(SaveData data, string filePathName = "") 
        {
            // Parse the data.
            string formattedData = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            // Decide whether to create a new file or overwrite an existing one.
            if (filePathName == "")
            {
                // Parse the save data into a JSON-formatted string and save it at the end of the list.
                string fileName = "track_state_" + Mathf.Max(0, trackFiles.Count + 1) + ".json";
                File.WriteAllText(Application.persistentDataPath + "/" + fileName, formattedData);
                trackFiles.Insert(trackFiles.Count - 1, Application.persistentDataPath + "/" + fileName);
                Debug.Log("Your tracks were saved in " + fileName + " at: " + Application.persistentDataPath);
            }
            else
            {
                File.WriteAllText(filePathName, formattedData);
                Debug.Log("You overwrote " + filePathName + " with the current track state.");
            }
        }

        private int fileIndex;
        private List<string> trackFiles;
    }

    /// <summary>
    /// This struct holds two lists of TrackSaveStates (for position and look) and is useful for writing into a JSON file.
    /// </summary>
    public struct SaveData
    {
        public List<TrackSaveState> positionTracks;
        public List<TrackSaveState> lookTracks;
    }
}
