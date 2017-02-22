

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace SKSStudios.Portals.NonVR {
    public class Movement : TeleportableScript {

        public GameObject eyeParent;
        public GameObject headset;
        public Camera headCam;
        public Collider playerPhysBounds;
        private Quaternion targetRotation;
        public float moveDistance = 0.2f;
        public float moveTime = 0.2f;


        private bool moving = false;
        float totalTime = 0f;
        private float distanceMoved;
        private Vector3 moveStartPosition;
        private Vector3 touchpadPosition = Vector3.zero;
        private Vector3 lastHeadPos;
        private Quaternion lastBodyRot;

        public Vector2 clampInDegrees = new Vector2(360, 180);
        public bool lockCursor;
        public Vector2 sensitivity = new Vector2(2, 2);
        public Vector2 smoothing = new Vector2(3, 3);
        public Vector2 targetDirection;
        public Vector2 targetCharacterDirection;

        Vector2 _mouseAbsolute;
        Vector2 _smoothMouse;
        // Assign this if there's a parent object controlling motion, such as a Character Controller.
        // Yaw rotation will affect this object instead of the camera if set.
        public GameObject characterBody;

        void Start() {
            // Set target direction to the camera's initial orientation.
            targetDirection = transform.localRotation.eulerAngles;
            // Set target direction for the character body to its inital state.
            if (characterBody) targetCharacterDirection = characterBody.transform.localRotation.eulerAngles;
            targetRotation = playerPhysBounds.transform.rotation;
        }
        void OnEnable() {
            //playerPhysBounds.transform.position = headCam.transform.position;
            lastHeadPos = headCam.transform.localPosition;
            PositionalUpdate();
            playerPhysBounds.enabled = true;
            teleportScriptIndependantly = false;
            playerPhysBounds.transform.localPosition += new Vector3(0, playerPhysBounds.GetComponent<Collider>().bounds.extents.y, 0);
        }



        new void Update() {
            base.Update();
        }


        public override void OnTeleport() {
            //VR-Relevant code
            /*
            Quaternion oldRootRot = transform.rotation;
            Vector3 oldRootEuler = transform.rotation.eulerAngles;
            Vector3 oldRootPos = transform.position;
            Quaternion oldPhysRot = playerPhysBounds.transform.rotation;
            Vector3 oldPhysEuler = oldPhysRot.eulerAngles;
            Vector3 oldPhysPos = playerPhysBounds.transform.position;
            Vector3 oldPhysEulerLocal = playerPhysBounds.transform.localEulerAngles;

            targetRotation = transform.rotation * Quaternion.Euler(0, lastBodyRot.eulerAngles.y, 0);
            targetRotation = Quaternion.Euler(new Vector3(0, targetRotation.eulerAngles.y, 0));
            Transform oldParent = transform.parent;
            transform.SetParent(playerPhysBounds.transform);
            playerPhysBounds.transform.position = oldPhysPos;
            //playerPhysBounds.transform.rotation = oldPhysRot;
            transform.rotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, 0);
            //playerPhysBounds.transform.rotation = oldPhysRot;

            Vector3 oldCameraPos = eyeParent.transform.position;
            playerPhysBounds.transform.localRotation = Quaternion.Euler(oldPhysEuler.x, lastBodyRot.eulerAngles.y, oldPhysEuler.z);
            eyeParent.transform.position = oldCameraPos;
            lastHeadPos = headCam.transform.localPosition;
            transform.position = oldRootPos - (playerPhysBounds.transform.position - oldPhysPos);
            playerPhysBounds.transform.position = oldPhysPos;*/
            
        }

        // Update is called once per frame
        void LateUpdate() {

            // Ensure the cursor is always locked when set
            Screen.lockCursor = lockCursor;

            // Allow the script to clamp based on a desired target value.
            var targetOrientation = Quaternion.Euler(targetDirection);
            var targetCharacterOrientation = Quaternion.Euler(targetCharacterDirection);

            // Get raw mouse input for a cleaner reading on more sensitive mice.
            var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            // Scale input against the sensitivity setting and multiply that against the smoothing value.
            mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));

            // Interpolate mouse movement over time to apply smoothing delta.
            _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
            _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);

            // Find the absolute mouse movement value from point zero.
            _mouseAbsolute += _smoothMouse;

            // Clamp and apply the local x value first, so as not to be affected by world transforms.
            if (clampInDegrees.x < 360)
                _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);

            var xRotation = Quaternion.AngleAxis(-_mouseAbsolute.y, targetOrientation * Vector3.right);
            headset.transform.localRotation = xRotation;

            // Then clamp and apply the global y value.
            if (clampInDegrees.y < 360)
                _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);

            headset.transform.localRotation *= targetOrientation;

            // If there's a character body that acts as a parent to the camera
            if (characterBody) {
                var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, characterBody.transform.up);
                characterBody.transform.localRotation = yRotation;
                characterBody.transform.localRotation *= targetCharacterOrientation;
            } else {
                var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, headset.transform.InverseTransformDirection(Vector3.up));
                headset.transform.localRotation *= yRotation;
            }

            //Keyboard input
            if (Input.GetKey(KeyCode.W) && !moving) {
                touchpadPosition = Vector3.forward;
                distanceMoved = 0f;
                moving = true;
                moveStartPosition = transform.position;
            }
            if (Input.GetKey(KeyCode.A) && !moving) {
                touchpadPosition = Vector3.left;
                distanceMoved = 0f;
                moving = true;
                moveStartPosition = transform.position;
            }
            if (Input.GetKey(KeyCode.S) && !moving) {
                touchpadPosition = Vector3.back;
                distanceMoved = 0f;
                moving = true;
                moveStartPosition = transform.position;
            }
            if (Input.GetKey(KeyCode.D) && !moving) {
                touchpadPosition = Vector3.right;
                distanceMoved = 0f;
                moving = true;
                moveStartPosition = transform.position;
            }

            if (moving && totalTime < moveTime) {
                moveByDelta(moveStartPosition, new Vector3(touchpadPosition.x * moveDistance, 0, touchpadPosition.z * moveDistance) * transform.lossyScale.magnitude, moveTime);
            }

            if (totalTime >= moveTime) {
                moving = false;
                totalTime = 0f;
            }
            PositionalUpdate();
        }

        private void PositionalUpdate() {

            Vector3 diffPos = headCam.transform.localPosition - lastHeadPos;
            playerPhysBounds.transform.localPosition += new Vector3(diffPos.x, 0, diffPos.z);
            /*
            Vector3 physEuler = playerPhysBounds.transform.localRotation.eulerAngles;
            playerPhysBounds.transform.localRotation = Quaternion.LookRotation(headCam.transform.forward);
            playerPhysBounds.transform.localRotation = Quaternion.Euler(physEuler.x, playerPhysBounds.transform.localRotation.eulerAngles.y, physEuler.z);*/
            //playerPhysBounds.transform.localPosition += new Vector3(diffPos.x, 0, diffPos.z);
            //Vertical movement
            eyeParent.transform.localPosition = new Vector3(eyeParent.transform.localPosition.x, diffPos.y, eyeParent.transform.localPosition.z);

            //Camera movement /w physical control override
            Vector3 oldEuler = playerPhysBounds.transform.localRotation.eulerAngles;
            Transform oldParent = eyeParent.transform.parent;
            eyeParent.transform.SetParent(playerPhysBounds.transform);
            eyeParent.transform.localPosition = (-headCam.transform.localPosition);

            eyeParent.transform.localPosition = new Vector3(eyeParent.transform.localPosition.x,
                playerPhysBounds.bounds.extents.y - (playerPhysBounds.bounds.extents.y * 2),
                eyeParent.transform.localPosition.z);

            //Cache new position
            lastHeadPos = headCam.transform.localPosition;
            lastBodyRot = playerPhysBounds.transform.localRotation;
        }

        void FixedUpdate() {
            //playerPhysBounds.transform.rotation = Quaternion.Slerp(playerPhysBounds.transform.rotation, targetRotation, Time.deltaTime * 3f);

            //Vector3 oldPos = playerPhysBounds.transform.localPosition;
            //transform.position = new Vector3(transform.position.x, playerPhysBounds.transform.position.y, transform.position.z);
            //playerPhysBounds.transform.localPosition = oldPos;

            //eyeParent.transform.position = playerPhysBounds.transform.position;
            //eyeParent.transform.position = playerPhysBounds.transform.TransformPoint(-headCam.transform.localPosition);
            //eyeParent.transform.rotation = playerPhysBounds.transform.rotation;


        }

        void moveByDelta(Vector3 start, Vector3 delta, float moveTime) {
            float distDelta = Mathfx.Hermite(0, 1, totalTime / moveTime) - distanceMoved;
            Vector3 deltaPos = Quaternion.AngleAxis(headset.gameObject.transform.localEulerAngles.y, Vector3.up) * (delta * distDelta);
            transform.position += transform.rotation * deltaPos;
            totalTime += Time.deltaTime;
            distanceMoved += distDelta;
        }
    }
}
