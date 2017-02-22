using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SKSStudios.Portals.NonVR {
    /// <summary>
    /// Class allowing for scripts on gameobjects to be teleported separately from the object proper.
    /// </summary>
    public abstract class TeleportableScript : MonoBehaviour {
        [HideInInspector]
        public Portal currentPortal;
        [HideInInspector]
        public Teleportable teleportable;
        [HideInInspector]
        public bool throughPortal = false;
        public bool teleportScriptIndependantly = true;

        private Transform originalParent;
        private Transform otherTransformParent;
        private Transform otherTransform;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;

        public void Initialize(Teleportable teleportable) {
            this.teleportable = teleportable;
            originalParent = transform.parent;
            try {
                otherTransform = PortalUtils.FindAnalogousTransform(transform, teleportable.root, teleportable.doppleganger.transform, true);
                otherTransformParent = otherTransform.parent;
            } catch (System.NullReferenceException e) {
                Debug.LogError("Teleportablescript on " + name + "had a problem:" + e.Message);
            }

            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
            originalScale = transform.localScale;
        }

        // Update is called once per frame
        protected void Update() {


            //Check if the gameobject this script is attached to is through a portal
            if (!throughPortal && currentPortal && PortalUtils.IsBehind(transform.position, currentPortal.origin.position, currentPortal.origin.forward)) {
                //If it is, move the gameobject to the doppleganger
                //Is this script going to teleport before its primary object?
                if (teleportScriptIndependantly) {
                    otherTransform.SetParent(transform.parent);
                    transform.SetParent(otherTransformParent);
                    throughPortal = true;
                    ActivateInheritance(gameObject);
                    resetTransform();
                }
                OnPassthrough();
            } else if (throughPortal && currentPortal && PortalUtils.IsBehind(transform.position, currentPortal.targetPortal.origin.position, currentPortal.targetPortal.origin.forward)) {
                if (teleportScriptIndependantly) {
                    otherTransform.SetParent(otherTransformParent);
                    transform.SetParent(originalParent);
                    throughPortal = false;

                    resetTransform();
                }
                OnPassthrough();
            }
        }

        //Hook for detecting script portal passthrough
        protected virtual void OnPassthrough() { }

        //Hook for detecting parent object teleport
        public virtual void OnTeleport() { }

        public void leavePortal() {
            if (teleportScriptIndependantly) {
                transform.SetParent(originalParent);
                throughPortal = false;

                resetTransform();
                currentPortal = null;
            }
            OnPassthrough();
        }

        private void ActivateInheritance(GameObject child) {
            child.SetActive(true);
            if (child.transform.parent != null)
                ActivateInheritance(child.transform.parent.gameObject);
        }
        private void resetTransform() {
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = originalScale;
        }


    }
}
