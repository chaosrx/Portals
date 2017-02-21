using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SKSStudios.Portals.VR {
    //Global settings for portals. For specific details, see PortalController.cs
    public class GlobalPortalSettings : MonoBehaviour {
        public bool ShouldOverrideScale = false;
        public float PortalScale = 1;
        public bool ShouldOverrideSize = false;
        public Vector2 PortalOpeningSize = Vector2.one;
        public bool ShouldOverrideMask = false;
        public Texture2D mask;
        [HideInInspector]
        public bool VR_Enabled = true;
        public bool shadowsEnabled = false;
        public bool inverted = false;
        [HideInInspector]
        public bool optimize = true;
        public static bool lightPassthrough;
        public static bool physicsPassthrough;
        public bool _lightPassthrough;
        public bool _physicsPassthrough;

        private PortalController[] portalInits;
        private static Light[] softShadowLights;
        private static Light[] hardShadowLights;
        // Use this for initialization
        void Start() {
            lightPassthrough = _lightPassthrough;
            physicsPassthrough = _physicsPassthrough;

            portalInits = FindObjectsOfType<PortalController>();
            foreach (PortalController p in portalInits) {
                //Sets global settings on portals
                if (ShouldOverrideScale)
                    p.PortalScale = PortalScale;
                if (ShouldOverrideSize)
                    p.transform.localScale = PortalOpeningSize;
                if (ShouldOverrideMask)
                    p.mask = mask;
                p.VR_Enabled = VR_Enabled;
                p.inverted = inverted;
                p.optimize = optimize;
                //Gets all lights
                Light[] lights = GameObject.FindObjectsOfType<Light>();
                ArrayList softLights = new ArrayList();
                ArrayList hardLights = new ArrayList();

                foreach (Light l in lights) {
                    if (l.shadows == LightShadows.Hard)
                        hardLights.Add(l);
                    if (l.shadows == LightShadows.Soft)
                        softLights.Add(l);
                }
                //Sets lights to arrays for enabling and disabling of shadows for camera passes
                softShadowLights = (Light[])softLights.ToArray(typeof(Light));
                hardShadowLights = (Light[])hardLights.ToArray(typeof(Light));
            }
        }

        void Update() {
            if (shadowsEnabled) {
                EnableShadows();
            } else {
                DisableShadows();
            }
        }
        //Enable and disable shadows
        public static void DisableShadows() {
            foreach (Light l in softShadowLights)
                l.shadows = LightShadows.None;
            foreach (Light l in hardShadowLights)
                l.shadows = LightShadows.None;
        }

        public static void EnableShadows() {
            foreach (Light l in softShadowLights)
                l.shadows = LightShadows.Soft;
            foreach (Light l in hardShadowLights)
                l.shadows = LightShadows.Hard;
        }
    }
}
