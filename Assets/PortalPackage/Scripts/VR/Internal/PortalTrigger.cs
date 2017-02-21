using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SKSStudios.Portals.VR {
    public class PortalTrigger : MonoBehaviour {
        //Trigger segregated from portal to prevent scaling interaction
        public GameObject portal;

        void Awake() {
            this.enabled = false;
            gameObject.GetComponent<Collider>().enabled = false;
        }

        void OnEnable() {
            gameObject.GetComponent<Collider>().enabled = true;
        }

        void OnTriggerEnter(Collider col) {
            portal.SendMessage("E_OnTriggerEnter", col);
        }

        void OnTriggerStay(Collider col) {
            portal.SendMessage("E_OnTriggerStay", col);
        }

        void OnTriggerExit(Collider col) {
            portal.SendMessage("E_OnTriggerExit", col);
        }
    }
}
