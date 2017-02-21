using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Eppy;
using SKSStudios.Portals.VR;
namespace SKSStudios.Portals.VR {
    public class PortalRaycastGun : TeleportableScript {
        public SteamVR_TrackedObject trackedObj;
        public SteamVR_Controller.Device device;

        // Update is called once per frame
        new void Update() {
            base.Update();
            try {
                device = SteamVR_Controller.Input((int)trackedObj.index);
            } catch (System.IndexOutOfRangeException) {
                return;
            }

            if (device.GetPress(SteamVR_Controller.ButtonMask.Trigger)) {
                Ray ray = new Ray(transform.position, transform.forward);
                Tuple<Ray, RaycastHit> hit = PortalUtils.TeleportableRaycast(ray, 100, ~0, QueryTriggerInteraction.Ignore);
                Debug.Log(hit);
            }
        }
    }
}
