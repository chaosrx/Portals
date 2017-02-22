using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//[ExecuteInEditMode]
namespace SKSStudios.Portals.NonVR {
    public class PortalController : MonoBehaviour {
        //The target of this portal
        public PortalController targetInitializer;
        //The scale of this portal, for resizing
        public float PortalScale = 1;
        //The actual unit size of the portal opening
        public Vector3 PortalOpeningSize = Vector3.one;
        //The portal prefab.
        public GameObject portal;
        //VR capability of portals. Remember to disable for non-vr applications
        [HideInInspector]
        public bool VR_Enabled = false;
        //mask texture for portal
        public Texture2D mask;
        //Inverted- for some rendering engines
        public bool inverted = false;
        //Oblique override (for webgl builds with skinned renderers)
        public bool NonObliqueOverride = false;
        //Is the portal enterable?
        public bool enterable = true;
        //How many times can this portal render through other portals? (total).
        public int recursionNumber = 1;
        //Should the portals be optimized for maximum frames? Warning: Due to a Unity bug, this can break dynamic lights through portals. No reason to turn this off.
        [HideInInspector]
        public bool optimize = true;

        public bool is3d = false;

        private PortalCamera portalCameraScript;
        private Portal portalScript;
        private PortalTrigger portalTrigger;
        private Teleportable playerTeleportable;
        private GameObject portalRenderer;
        private GameObject portalPlaceholder;
        private Transform portalOrigin;
        private Transform portalDestination;
        private bool setup = false;
        private Color color = Color.white;

        void Start() {
            //Load the portal prefab
            portal = Instantiate(portal, transform);
            portal.transform.position = transform.position;
            portal.transform.localRotation = Quaternion.Euler(0, 180, 0);
            portal.transform.localScale = Vector3.one;
            portal.name = "Portal";
            color = Random.ColorHSV(0, 1, 1, 1, 0, 1, 1, 1);
            StartCoroutine(Setup());
        }

        IEnumerator Setup() {
            yield return new WaitForEndOfFrame();
            while (targetInitializer == null) {
                yield return new WaitForEndOfFrame();
            }
            //Removes the impostor, if there was one
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            portal.SetActive(true);
            //Sets up portal camera script
            portalCameraScript = transform.FindChild("Portal/PortalCameraParent/PortalCamera").GetComponent<PortalCamera>();
            portalScript = transform.FindChild("Portal/PortalRenderer/BackWall").GetComponent<Portal>();
            portalTrigger = transform.FindChild("Portal/PortalRenderer/PortalTrigger").GetComponent<PortalTrigger>();
            
                try {
                    playerTeleportable = GameObject.Find("PlayerExtents").GetComponent<Teleportable>();
                    portalScript.playerTeleportable = playerTeleportable;
                } catch (System.NullReferenceException) {
                    Debug.LogWarning("No player collider found! Player will not be able to walk into portals. Remember to have your camera parented to a gameobject named PlayerExtents!");
                }

            portalCameraScript.PortalCameraParent = portalCameraScript.gameObject.transform.parent.gameObject.transform;
            portalCameraScript.portalTransform = transform.FindChild("Portal/PortalSource").transform;
            portalCameraScript.portalDestination = targetInitializer.transform.FindChild("Portal/PortalTarget");
            //portalCameraScript.Initialize(VR_Enabled);
            portalPlaceholder = transform.FindChild("Portal/PortalRenderer/PortalPlaceholder").gameObject;

            portalScript.origin = transform.FindChild("Portal/PortalSource");
            portalScript.destination = targetInitializer.transform.FindChild("Portal/PortalTarget");
            portalScript.targetPortal = targetInitializer.transform.FindChild("Portal/PortalRenderer/BackWall").GetComponent<Portal>();
            portalScript.portalRoot = transform;
            portalScript.VR = VR_Enabled;
            portalScript.is3d = is3d;
            portalScript.mask = mask;
            portalScript.nonObliqueOverride = NonObliqueOverride;
            portalScript.inverted = inverted;
            portalScript.optimize = optimize;
            portalScript.portalCamera = portalCameraScript;
            portalScript.physicsPassthrough = transform.FindChild("Portal/PortalRenderer/PortalPhysicsPassthrough").GetComponent<PhysicsPassthrough>();
            portalScript.enterable = enterable;
            portalScript.placeholder = portalPlaceholder;
            portalScript.maxRenderCount = recursionNumber;

            portalTrigger.portal = portalScript.gameObject;

            //Readies the portal for scaling and transformations
            portalRenderer = transform.FindChild("Portal/PortalRenderer/").gameObject;
           
            portalPlaceholder.GetComponent<Renderer>().material.SetTexture("_AlphaTexture", mask);
            portalOrigin = transform.FindChild("Portal/PortalSource");
            portalDestination = transform.FindChild("Portal/PortalTarget");

            //Enable scripts
            portalScript.enabled = true;
            portalCameraScript.enabled = true;
            portalTrigger.enabled = true;
            setup = true;

            //Transfer transform values to modifiable var
            PortalOpeningSize = transform.localScale;
            transform.localScale = Vector3.one;

            targetInitializer.GetComponent<PortalController>().color = color;
        }

        void Update() {
            if (setup) {
                portalRenderer.transform.localScale = new Vector3(PortalOpeningSize.x, PortalOpeningSize.y, PortalOpeningSize.z);
                portalOrigin.localScale = Vector3.one * PortalScale;
                portalDestination.localScale = Vector3.one * PortalScale;
                transform.localScale = Vector3.one;
                portalScript.mask = mask;
                Debug.DrawLine(transform.position, targetInitializer.transform.position, color);
            }
        }

    }
}
