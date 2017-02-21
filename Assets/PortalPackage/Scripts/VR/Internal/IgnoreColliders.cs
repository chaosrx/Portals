using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SKSStudios.Portals.VR {
    public class IgnoreColliders : MonoBehaviour {
        public Portal portal;
        private ArrayList ignoredColliders;
        //private 
        public void Start() {
            ignoredColliders = new ArrayList();
            ignoredColliders.Add(gameObject.GetComponent<Collider>());
            gameObject.GetComponent<Collider>().enabled = true;
        }
        //Mantains list of ignored colliders
        public void OnTriggerEnter(Collider col) {
            if (col.gameObject.layer != LayerMask.NameToLayer("Portal")) {
                ignoredColliders.Add(col);
            }
        }
        public void OnTriggerExit(Collider col) {
            ignoredColliders.Remove(col);
        }
        //Sends the list to the portal controller
        public void Update() {
            //Ensures that walls/the floor aren't being ignored from portal entry

            portal.rearColliders = (Collider[])ignoredColliders.ToArray(typeof(Collider));
        }
    }
}
