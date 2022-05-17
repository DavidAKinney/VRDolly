using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace IVLab.VRDolly
{
    /// <summary>
    /// This input class should be active when using VRDolly with a mouse and keyboard.
    /// </summary>
    public class MKInput : InputMapper
    {
        /// <summary>
        /// Start() is used primarily to initialize sensitivity data as well as call the base class' Start() function.
        /// </summary>
        public override void Start()
        {
            translationSensitivity = 0.5f;
            rotationSensitivity = 500f;
            controllerSensitivity = 0.5f;
            dominantSelected = false;
            recessiveSelected = false;
            headsetSelected = true;
            yaw = 0f;
            pitch = 0f;

            base.Start();
        }

        /// <summary>
        /// Update() is used to detect input for the controllers as well as allow the user to move the controllers/headset
        /// individually with their mouse and keyboard.
        /// </summary>
        public override void Update()
        {
            // Call the base update function for general updates. 
            base.Update();

            // Update the current yaw angle when a snap-turn occurs.
            if (snapTurned)
            {
                yaw += snapYaw;   
            }

            // Determine which VR device is being manipulated.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                dominantSelected = true;
                recessiveSelected = false;
                headsetSelected = false;
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                dominantSelected = false;
                recessiveSelected = true;
                headsetSelected = false;
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                dominantSelected = false;
                recessiveSelected = false;
                headsetSelected = true;
            }

            // Get the headset's axes so that they can be used to update transforms.
            Vector3 headsetForward = headsetObject.transform.forward;
            Vector3 headsetUp = headsetObject.transform.up;
            Vector3 headsetRight = Vector3.Cross(headsetUp, headsetForward).normalized;

            if (dominantSelected)
            {
                Vector3 directionChange = headsetObject.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition).direction;
                dominantObject.transform.forward = directionChange;

                if (Input.GetKey(KeyCode.A)) { dominantObject.transform.position -= headsetRight * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.D)) { dominantObject.transform.position += headsetRight * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.W)) { dominantObject.transform.position += headsetForward * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.S)) { dominantObject.transform.position -= headsetForward * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.Space)) { dominantObject.transform.position += headsetUp * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.LeftShift)) { dominantObject.transform.position -= headsetUp * controllerSensitivity * Time.deltaTime; }

                dominantInput.triggerButton = Input.GetMouseButton(0);
                dominantInput.gripButton = Input.GetMouseButton(1);
                dominantInput.primaryButton = Input.GetKey(KeyCode.Alpha1);
                dominantInput.secondaryButton = Input.GetKey(KeyCode.Alpha2);
                dominantInput.primary2DAxisClick = Input.GetMouseButton(2);
                dominantInput.menuButton = Input.GetKey(KeyCode.Alpha4);

                Vector2 joystickInput = new Vector2();
                if (Input.GetKey(KeyCode.LeftArrow)) { joystickInput.x -= 1; }
                if (Input.GetKey(KeyCode.RightArrow)) { joystickInput.x += 1; }
                if (Input.GetKey(KeyCode.DownArrow)) { joystickInput.y -= 1; }
                if (Input.GetKey(KeyCode.UpArrow)) { joystickInput.y += 1; }
                dominantInput.primary2DAxis = joystickInput;
            }
            if (recessiveSelected)
            {
                Vector3 directionChange = headsetObject.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition).direction;
                recessiveObject.transform.forward = directionChange;

                if (Input.GetKey(KeyCode.A)) { recessiveObject.transform.position -= headsetRight * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.D)) { recessiveObject.transform.position += headsetRight * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.W)) { recessiveObject.transform.position += headsetForward * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.S)) { recessiveObject.transform.position -= headsetForward * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.Space)) { recessiveObject.transform.position += headsetUp * controllerSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.LeftShift)) { recessiveObject.transform.position -= headsetUp * controllerSensitivity * Time.deltaTime; }

                recessiveInput.triggerButton = Input.GetMouseButton(0);
                recessiveInput.gripButton = Input.GetMouseButton(1);
                recessiveInput.primaryButton = Input.GetKey(KeyCode.Alpha1);
                recessiveInput.secondaryButton = Input.GetKey(KeyCode.Alpha2);
                recessiveInput.primary2DAxisClick = Input.GetMouseButton(2);
                recessiveInput.menuButton = Input.GetKey(KeyCode.Alpha4);

                Vector2 joystickInput = new Vector2();
                if (Input.GetKey(KeyCode.LeftArrow)) { joystickInput.x -= 1; }
                if (Input.GetKey(KeyCode.RightArrow)) { joystickInput.x += 1; }
                if (Input.GetKey(KeyCode.DownArrow)) { joystickInput.y -= 1; }
                if (Input.GetKey(KeyCode.UpArrow)) { joystickInput.y += 1; }
                recessiveInput.primary2DAxis = joystickInput;
            }
            if (headsetSelected)
            {
                // Holding the middle mouse button allows camera movement.
                if (Input.GetMouseButton(2))
                {
                    // Lock the controller transforms to the headset so they stay on screen.
                    dominantObject.transform.SetParent(headsetObject.transform);
                    recessiveObject.transform.SetParent(headsetObject.transform);

                    // Update the yaw and pitch values with movement from the mouse.
                    yaw += Time.deltaTime * rotationSensitivity * Input.GetAxis("Mouse X");
                    pitch -= Time.deltaTime * rotationSensitivity * Input.GetAxis("Mouse Y");

                    // Set the headset's rotation with the yaw and pitch values.
                    headsetObject.transform.localEulerAngles = new Vector3(pitch, yaw, 0.0f);

                    // Set the parent of the controllers back to the height offset.
                    dominantObject.transform.SetParent(heightOffset.transform);
                    recessiveObject.transform.SetParent(heightOffset.transform);
                }

                Vector3 dominantTranslate = dominantObject.transform.position - headsetObject.transform.position;
                Vector3 recessiveTranslate = recessiveObject.transform.position - headsetObject.transform.position;

                if (Input.GetKey(KeyCode.A)) { headsetObject.transform.position -= headsetRight * translationSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.D)) { headsetObject.transform.position += headsetRight * translationSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.S)) { headsetObject.transform.position -= headsetForward * translationSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.W)) { headsetObject.transform.position += headsetForward * translationSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.Space)) { headsetObject.transform.position += headsetUp * translationSensitivity * Time.deltaTime; }
                if (Input.GetKey(KeyCode.LeftShift)) { headsetObject.transform.position -= headsetUp * translationSensitivity * Time.deltaTime; }

                // Move the controllers along with the headset so they don't get lost.
                dominantObject.transform.position = headsetObject.transform.position + dominantTranslate;
                recessiveObject.transform.position = headsetObject.transform.position + recessiveTranslate;
            }
        }


        // Member data
        private float translationSensitivity;
        private float rotationSensitivity;
        private float controllerSensitivity;
        private bool dominantSelected;
        private bool recessiveSelected;
        private bool headsetSelected;
        private float yaw;
        private float pitch;
    }
}
