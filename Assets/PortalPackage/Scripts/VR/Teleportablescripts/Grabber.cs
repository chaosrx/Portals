﻿
using SKSStudios.Portals.VR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SKSStudios.Portals.VR {
    public class Grabber : TeleportableScript {
        public SteamVR_TrackedObject trackedObj;
        public SteamVR_Controller.Device device;
        public GameObject currentObject;
        private Vector3 lastPosition;
        private Vector3 velocity;
        private readonly int sampleframes = 25;
        private readonly int sampleframesExcluded = 0;
        //Average velocity of the last 60 frames
        private Queue<Vector3> avgVelocitySamples = new Queue<Vector3>();
        private bool objWasKinematic = false;

        private Vector3 _avgVelocity {
            get {
                Vector3 totalMovement = Vector3.zero;
                for (int i = 0; i < avgVelocitySamples.Count - sampleframesExcluded; i++)
                    totalMovement += avgVelocitySamples.ToArray()[i + sampleframesExcluded];
                return totalMovement / (avgVelocitySamples.Count) * 5;
            }
        }


        // Update is called once per frame
        new void Update() {

            try {
                device = SteamVR_Controller.Input((int)trackedObj.index);
            } catch (System.IndexOutOfRangeException) {
                return;
            }
            velocity = (transform.position - lastPosition) / Time.deltaTime;
            lastPosition = transform.position;
            //Adds velocity to the queue
            avgVelocitySamples.Enqueue(velocity);
            if (avgVelocitySamples.Count > sampleframes)
                avgVelocitySamples.Dequeue();



            if (currentObject != null) {
                Teleportable teleportable2 = currentObject.GetComponent<Teleportable>();
                //Initially use other teleporter for early detection, then switch once grabber is through portal

                //teleportable2.enabled = !throughPortal;
                if (!device.GetPress(SteamVR_Controller.ButtonMask.Grip) || currentObject.tag != "Grabbable") {
                    //Resumes teleportable
                    if (teleportable2) {
                        teleportable2.VisOnly = false;
                        teleportable2.isActive = true;
                    }
                    //Removes rigidbody from player and doppleganger


                    if (!throughPortal) {

                    }
                    //teleportable.RemoveChild(currentObject);
                    else {
                        Vector3 oldPosition = currentObject.transform.position;
                        Transform oldParent = currentObject.transform.parent;
                        if (teleportable2) {
                            teleportable2.root.SetParent(transform);
                        }
                        //Reparents the doppleganger component to where it should be
                        //PortalUtils.FindAnalogousTransform(currentObject.transform, teleportable.doppleganger.transform, teleportable.root, false).SetParent(oldParent);
                        //teleportable.RemoveChild(currentObject);
                        currentObject.transform.position = oldPosition;
                    }
                    //Resumes Rigidbody
                    if (teleportable2) {
                        if (teleportable2.root.parent == transform) {
                            teleportable2.root.SetParent(null);
                        }
                    } else {
                        if (currentObject.transform.parent == transform) {
                            currentObject.transform.SetParent(null);
                        }
                    }
                    Rigidbody currentRigid = currentObject.GetComponent<Collider>().attachedRigidbody;
                    currentRigid.isKinematic = objWasKinematic;
                    currentRigid.velocity = velocity;
                    currentRigid.angularVelocity = device.angularVelocity;
                    teleportable.AddCollision(currentObject.GetComponent<Collider>());
                    currentObject = null;
                    return;
                } else {
                    teleportable.IgnoreCollision(currentObject.GetComponent<Collider>());
                }
            }
            base.Update();
        }

        void OnTriggerStay(Collider col) {
            if (device != null && device.GetPressDown(SteamVR_Controller.ButtonMask.Grip) && currentObject == null) {
                if (col.CompareTag("Grabbable")) {
                    objWasKinematic = col.attachedRigidbody.isKinematic;
                    col.attachedRigidbody.isKinematic = true;
                    //FixedJoint grabberJoint = col.gameObject.AddComponent<FixedJoint>();
                    //grabberJoint.connectedBody = gameObject.GetComponent<Rigidbody>();
                    currentObject = col.gameObject;

                    Teleportable teleportable2 = col.GetComponent<Teleportable>();


                    if (teleportable2) {
                        teleportable2.root.SetParent(transform);
                        teleportable2.VisOnly = true;
                        teleportable2.IgnoreCollision(col);
                    } else {
                        currentObject.transform.SetParent(transform);
                        teleportable.IgnoreCollision(col);
                    }


                    //teleportable2.isActive = false;
                    //teleportable.AddChild(currentObject);
                }
            }
        }

        protected override void OnPassthrough() {
            if (currentObject) {
                Teleportable teleportable2 = currentObject.GetComponent<Teleportable>();
                if (teleportable2) {
                    teleportable2.UpdateDoppleganger();
                    /*
                    Vector3 oldPos = currentObject.transform.localPosition;
                    Rigidbody rigidbody = currentObject.GetComponent<Rigidbody>();
                    rigidbody.position = transform.TransformPoint(oldPos);*/
                }
            }

        }
    }
}
