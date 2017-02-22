using Eppy;
using SKS.PortalLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SKSStudios.Portals.NonVR {
    /*
    public class LightPassthrough : MonoBehaviour {
        public Portal portal;
        public float maxDistance = 3f;

        void LateUpdate() {
            ArrayList lights = new ArrayList();
            ArrayList nearLights = new ArrayList(Physics.OverlapSphere(transform.position, maxDistance, ~0, QueryTriggerInteraction.Ignore));
            //Catalogues all cached nearby lights and removes those that are too far away
            if (GlobalPortalSettings.lightPassthrough) {
                for (int i = 0; i < portal.passthroughLights.Count; i++) {
                    var tup = (Tuple<Light, GameObject>)portal.passthroughLights[i];
                    if (Vector3.Distance(tup.Item1.transform.position, transform.position) > maxDistance) {
                        Destroy(tup.Item2);
                        portal.passthroughLights.Remove(tup);
                        i--;
                        continue;
                    }
                    lights.Add(tup.Item1);
                }
            }
            foreach (Collider col in nearLights) {
                Light light = col.GetComponent<Light>();
                if (light && !lights.Contains(light) && !portal.targetPortal.passthroughLights.Contains(light)) {
                    GameObject newLight = new GameObject();
                    newLight.name = "Duplicate Light";
                    newLight.transform.parent = portal.destination;
                    newLight.transform.localPosition = transform.InverseTransformPoint(newLight.transform.position);
                    Light newLightComponent = newLight.AddComponent<Light>();
                    newLightComponent.GetCopyOf(light);
                    Tuple<Light, GameObject> newEntry = new Tuple<Light, GameObject>(light, newLight);
                    portal.passthroughLights.Add(newEntry);
                }
            }
        }
    }*/
}
