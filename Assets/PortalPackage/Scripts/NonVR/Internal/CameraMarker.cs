using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SKSStudios.Portals.NonVR {
    public class CameraMarker : MonoBehaviour {
        public PortalCamera owner;
        public void Initialize(PortalCamera owner) {
            this.owner = owner;
        }
    }
}
