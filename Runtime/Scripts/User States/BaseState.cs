using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IVLab.VisTools;

namespace IVLab.VRDolly
{
    /// <summary>
    /// This base class acts as a starting point for each user state in VRDolly. Every state must overwrite UpdateState() in order
    /// to function correctly.
    /// </summary>
    public class BaseState
    {
        /// <summary>
        /// This function holds the core functionality of each state. It takes the users input, a list of current track configurations,
        /// and the user's radial menu in order to perform a specific set of operations related to the current state. Each call returns
        /// a State enum value that indicates the next state to set active and update.
        /// </summary>
        /// <param name="dominantInput"></param>
        /// <param name="recessiveInput"></param>
        /// <param name="tracks"></param>
        /// <param name="menu"></param>
        /// <returns></returns>
        public virtual State UpdateState(InputData dominantInput, InputData recessiveInput, VRDollyData tracks, RadialMenu menu)
        {
            return State.BASE;
        }
    }


    /// <summary>
    /// This enum is used by the state manager and its substates to manage transfer-of-control requests
    /// between each other. Each value corresponds to a specific derived substate class.
    /// </summary>
    public enum State
    {
        BASE = -1,           // The "parent" state, which is not normally used.
        LOCOMOTION = 0,      // Allows the user to freely move around their scene.
        CREATION = 1,        // Allows the user to initialize a new track pair.
        EDIT = 2,            // Allows the user to select and adjust track components.
        VIEW = 3,            // Allows the user to test and view their track setups.
        FILE = 4             // Allow the user to save and load track sets from memory.
    }
}
