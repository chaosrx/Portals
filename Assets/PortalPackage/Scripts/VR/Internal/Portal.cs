using System.Collections;
using UnityEngine;

using UnityEngine.Rendering;
using System.Linq;
using Eppy;
using SKS.PortalLib;

namespace SKSStudios.Portals.VR {
    /// <summary>
    /// Class handling the majority of the portal logic
    /// </summary>
    public class Portal : MonoBehaviour {
        //Keeps the portal effect 100% seamless even at high head speeds
        private readonly float fudgeFactor = 0.02f;
        [HideInInspector]
        public bool inverted = false;
        [HideInInspector]
        public bool enterable = true;
        [HideInInspector]
        public PortalCamera portalCamera;
        private Material _portalMaterial;
        [HideInInspector]
        public Texture2D mask;
        [HideInInspector]
        public Transform origin;
        [HideInInspector]
        public Transform destination;
        [HideInInspector]
        public Transform portalRoot;
        [HideInInspector]
        public Collider headCollider;
        [HideInInspector]
        public Portal targetPortal;
        [HideInInspector]
        public Teleportable playerTeleportable;
        [HideInInspector]
        public bool VR;
        [HideInInspector]
        public bool nonObliqueOverride = false;
        [HideInInspector]
        public bool optimize;
        [HideInInspector]
        public bool is3d;
        [HideInInspector]
        public PhysicsPassthrough physicsPassthrough;
        [HideInInspector]
        public GameObject placeholder;

        private Camera headCamera;

        //Physical collision fixes
        public GameObject bufferWallObj;
        public ArrayList bufferWall;

        private bool _headInPortalTrigger = false;
        //The portals are not actually seamless, as no such thing is possible. However, they APPEAR to be, as once the camera gets within a 
        //certain distance the back wall begins stretching. This variable activates that effect.
        private bool _CheeseActivated = false;
        private bool _rendered = false;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshRenderer _targetRenderer;
        private Collider _collider;
        //Portal effects
        private ArrayList _nearTeleportables;
        //Objects near enough to trigger passthrough (controlled by their respective scripts)
        //Passthrough lights will come in an update fairly soon
        public ArrayList passthroughLights;
        public ArrayList passthroughColliders;
        //Camera tracking information
        private Vector3[] _nearClipVertsLocal;
        private Vector3[] _nearClipVertsGlobal;
        //Ignored colliders behind portal
        public Collider[] rearColliders;

        void Awake() {
            this.enabled = false;
        }

        /// <summary>
        /// Setup for the portal
        /// </summary>
        void OnEnable() {
            _meshRenderer = gameObject.GetComponent<MeshRenderer>();
            _meshFilter = gameObject.GetComponent<MeshFilter>();
            _portalMaterial = _meshRenderer.material;
            _collider = gameObject.GetComponent<Collider>();

            _nearTeleportables = new ArrayList();
            passthroughLights = new ArrayList();
            passthroughColliders = new ArrayList();
            SetupCamera();

            //Invert the shader if required
            if (inverted)
                _meshRenderer.material.SetFloat("_Inverted", 1);

            //Add the buffer colliders to the collection
            bufferWall = new ArrayList();
            foreach (Transform tran in bufferWallObj.transform) {
                bufferWall.Add(tran.GetComponent<Collider>());
            }

            physicsPassthrough.Initialize(this);
        }

        void SetupCamera() {
            headCamera = Camera.main;
            headCollider = headCamera.GetComponent<Collider>();
            _nearClipVertsLocal = EyeNearPlaneDimensions(headCamera);
            _nearClipVertsGlobal = new Vector3[_nearClipVertsLocal.Length];
            //portalCamera.headCamera = headCamera;
            portalCamera.Initialize(VR);
        }

        /// <summary>
        /// All portal updates are done after everything else has updated
        /// </summary>
        private void LateUpdate() {
            //Prevents collision from reporting false before we check the next frame
            UpdateBackPosition();

            //Setup camera again if the main camera has changed
            if (headCamera != Camera.main) {
                SetupCamera();
            }

            //Update the effects, disabling masks/ztesting for seamless transitions
            UpdateEffects();

            _meshRenderer.material.SetTexture("_AlphaTexture", mask);
            //Resets the rendered state before the next frame
            _rendered = false;

        }

        /// <summary>
        /// Updates the "dopplegangers", the visual counterparts, to all teleportable objects near portals.
        /// </summary>
        private void UpdateDopplegangers() {
            //Updates dopplegangers
            foreach (Teleportable tp in _nearTeleportables) {
                PortalUtils.TeleportObject(tp.doppleganger, origin, destination, tp.doppleganger.transform, tp.root);
                //PortalUtils.TeleportObject(tp.doppleganger, origin, destination, tp.doppleganger.transform);
                StartCoroutine(tp.UpdateDoppleganger());
            }

            //Updates duplicate lights
            for (int i = 0; i < passthroughLights.Count; i++) {
                ((Tuple<Light, GameObject>)passthroughLights[i]).Item2.GetComponent<Light>().GetCopyOf(((Tuple<Light, GameObject>)passthroughLights[i]).Item1);
                (((Tuple<Light, GameObject>)passthroughLights[i])).Item2.transform.localPosition = origin.InverseTransformPoint(((Tuple<Light, GameObject>)passthroughLights[i]).Item1.gameObject.transform.position);
            }
        }

        /// <summary>
        /// Updates the back position of the portal wall for visual seamlessness
        /// </summary>
        private void UpdateBackPosition() {
            _CheeseActivated = false;
            //Gets the near clipping plane verts in global space
            for (int i = 0; i < _nearClipVertsGlobal.Length; i++) {
                // nearClipVertsGlobal[i] = headCamera.transform.TransformPoint(nearClipVertsLocal[i]);
                _nearClipVertsGlobal[i] = headCamera.transform.position + (headCamera.transform.rotation * _nearClipVertsLocal[i]);
            }

            //Moves the drawn "plane" back if the camera gets too close
            if (_headInPortalTrigger) {
                if (!is3d)
                    transform.localScale = new Vector3(1, 1, 0);
                float deepestVert = 0f;

                for (int i = 0; i < _nearClipVertsGlobal.Length; i++) {
                    Vector3 currentVert = _nearClipVertsGlobal[i];
                    Debug.DrawLine(headCamera.transform.position, currentVert, Color.green);
                    Vector3 planePoint = transform.position;
                    Vector3 heading = currentVert - planePoint;
                    float dotProduct = Vector3.Dot(heading, -portalRoot.forward);
                    if (dotProduct < 0f) {
                        _CheeseActivated = true;
                        _portalMaterial.renderQueue = (int)RenderQueue.Overlay;
                        if (dotProduct <= deepestVert)
                            deepestVert = dotProduct;
                    }
                }
                if (_CheeseActivated && !is3d) {
                    transform.localScale = new Vector3(1f, 1f, ((-deepestVert + fudgeFactor) / portalRoot.localScale.x));
                    placeholder.SetActive(false);
                } else if (!_CheeseActivated) 
                {
                    placeholder.SetActive(true);
                    _portalMaterial.renderQueue = (int)RenderQueue.Transparent;
                }
                   
               
            }

        }

        /// <summary>
        /// Calls the rendering of a portal frame
        /// </summary>
        private void OnWillRenderObject() {
            //Is the mesh renderer in the camera frustrum?
            if (_meshRenderer.isVisible && !_rendered) {
                //Is the camera looking at the portal the head camera?
                UpdateDopplegangers();
                targetPortal.UpdateDopplegangers();
                //if (Camera.current == headCamera) {
                    //Update the doppleganger positions

                    TryRenderPortal(Camera.current, _nearClipVertsGlobal);
                //}
            }
        }
        /// <summary>
        /// Updates information needed for the "Scissor" test, to only render necessary fragments.
        /// </summary>
        private void UpdateScissor() {
            _targetRenderer = targetPortal.GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// Sets the z rendering order to make through-wall rendering seamless, as well as disabling masks while traversing portals
        /// </summary>
        private void UpdateEffects() {
            if (_CheeseActivated) {
                _meshRenderer.material.SetFloat("_ZTest", (int)CompareFunction.Always);
                _meshRenderer.material.SetFloat("_Mask", 0);
            } else {
                _meshRenderer.material.SetFloat("_ZTest", (int)CompareFunction.Less);
                _meshRenderer.material.SetFloat("_Mask", 1);
            }
        }

        /// <summary>
        /// Render a portal frame, assuming that the camera is in front of the portal and all conditions are met.
        /// </summary>
        /// <param name="camera">The camera rendering the portal</param>
        /// <param name="nearClipVerts">The vertices of the camera's near clip plane</param>
        private void TryRenderPortal(Camera camera, Vector3[] nearClipVerts) {
            UpdateScissor();
            //Tests object occlusion
            _meshFilter = gameObject.GetComponent<MeshFilter>();
            //bool isVisible = false;
            bool isVisible = true;
            //Check if the camera itself is behind the portal, even if the frustum isn't.

            if (!PortalUtils.IsBehind(camera.gameObject.transform.position, origin.position, origin.forward)) {
                isVisible = true;
            } else {
                //Checks to see if any part of the camera is in front of the portal
                for (int i = 0; i < nearClipVerts.Length; i++) {
                    if (!PortalUtils.IsBehind(nearClipVerts[i], origin.position, origin.forward)) {
                        isVisible = true;
                        break;
                    }
                }
            }

            //Early return if no part of the camera is in front of the camera

            if (!isVisible)
                return;

            if ((isVisible || _CheeseActivated)) {
                portalCamera.RenderIntoMaterial(camera, _portalMaterial, gameObject.GetComponent<MeshRenderer>(), _targetRenderer, _meshFilter.mesh, !nonObliqueOverride ? !_CheeseActivated : false, optimize, is3d);
                _rendered = true;
            }
        }

        /// <summary>
        /// External class keeps track of the physics duplicates for clean, consistent physical passthrough
        /// </summary>
        public void FixedUpdate() {
            //if (GlobalPortalSettings.physicsPassthrough)
                physicsPassthrough.UpdatePhysics();
        }

        /// <summary>
        /// All collsision methods are externed to another script
        /// </summary>
        public void E_OnTriggerEnter(Collider col) {
            Teleportable teleportScript = col.GetComponent<Teleportable>();
            AddTeleportable(teleportScript);
        }


        /// <summary>
        /// Checks if objects are in portal, and teleports them if they are. Also handles player entry.
        /// </summary>
        /// <param name="col"></param>
        public void E_OnTriggerStay(Collider col) {
            Teleportable teleportScript = col.GetComponent<Teleportable>();
            AddTeleportable(teleportScript);
            //Detects when head enters portal area
            if (col == headCollider) {
                _headInPortalTrigger = true;
            }

            if (enterable && teleportScript) {
                //Updates clip planes for disappearing effect
                if (!nonObliqueOverride) {
                    teleportScript.SetClipPlane(origin.position, origin.forward, teleportScript.oTeleportRends);
                    teleportScript.SetClipPlane(destination.position, -destination.forward, teleportScript.dTeleportRends);
                }
                if (GlobalPortalSettings.physicsPassthrough) {
                    //Makes objects collide with objects on the other side of the portal
                    foreach (Tuple<Collider, Collider> c in passthroughColliders) {
                        teleportScript.AddCollision(c.Item2);
                    }

                    foreach (Collider c in bufferWall) {
                        teleportScript.AddCollision(c);
                    }
                }


                //Passes portal info to teleport script
                teleportScript.SetPortalInfo(this);

                //Teleports objects
                if (PortalUtils.IsBehind(col.transform.position, origin.position, origin.forward) && !teleportScript.VisOnly) {
                    //_nearTeleportables.Remove(teleportScript);
                    //teleportScript.ResumeAllCollision();
                    RemoveTeleportable(teleportScript);
                    PortalUtils.TeleportObject(teleportScript.root.gameObject, origin, destination, teleportScript.root);
                    targetPortal.FixedUpdate();
                    targetPortal.SendMessage("E_OnTriggerStay", col);
                    teleportScript.Teleport();
                    targetPortal.UpdateDopplegangers();
                    targetPortal.physicsPassthrough.SendMessage("UpdatePhysics");
                    //physicsPassthrough.SendMessage("UpdatePhysics");

                    if (teleportScript == playerTeleportable) {
                        targetPortal.UpdateDopplegangers();
                        targetPortal.IncomingCamera();
                        _CheeseActivated = false;
                        _headInPortalTrigger = false;
                        //Resets the vis depth of the portal volume
                        if (!is3d)
                            transform.localScale = new Vector3(1f, 1f, 0f);

                        //Flips the nearby light tables

                        ArrayList tempList = new ArrayList(passthroughLights);
                        passthroughLights = targetPortal.passthroughLights;
                        targetPortal.passthroughLights = tempList;
                        foreach (Tuple<Light, GameObject> tup in passthroughLights) {
                            tup.Item2.transform.parent = destination;
                        }
                        foreach (Tuple<Light, GameObject> tup in targetPortal.passthroughLights) {
                            tup.Item2.transform.parent = targetPortal.destination;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes primed objects from the queue if they move away from the portal
        /// </summary>
        public void E_OnTriggerExit(Collider col) {
            Teleportable teleportScript = col.GetComponent<Teleportable>();
            if (col == headCollider) {
                _headInPortalTrigger = false;
            }
            RemoveTeleportable(teleportScript);
        }

        /// <summary>
        /// Attempt to add a teleportableScript to the nearteleportables group
        /// </summary>
        /// <param name="teleportScript">the script to add</param>
        private void AddTeleportable(Teleportable teleportScript) {
            if (teleportScript && !_nearTeleportables.Contains(teleportScript)) {
                teleportScript.doppleganger.SetActive(true);

                _nearTeleportables.Add(teleportScript);

                //Ignores collision with rear objects- todo: add buffer around portal
                Vector3[] checkedVerts = PortalUtils.ReferenceVerts(_meshFilter.mesh);

                //Ignore collision with the portal itself
                teleportScript.IgnoreCollision(_collider);
                teleportScript.IgnoreCollision(targetPortal._collider);

                //Ignores rear-facing colliders
                Ray ray;
                RaycastHit[] hit;
                for (int i = 0; i < checkedVerts.Length; i++) {
                    ray = new Ray(transform.TransformPoint(checkedVerts[i]) + transform.forward * 0.01f, -transform.forward);
                    hit = Physics.RaycastAll(ray, 1 * transform.parent.localScale.z, ~0, QueryTriggerInteraction.Collide);
                    Debug.DrawRay(ray.origin, -transform.forward * transform.parent.localScale.z, Color.cyan, 10);
                    if (hit.Length > 0) {
                        foreach (RaycastHit h in hit) {
                            //Never ignore collisions with teleportables
                            //Never ignore collisions with teleportables
                            if (h.collider.gameObject.tag != "PhysicsPassthroughDuplicate" &&
                                h.collider.gameObject.GetComponent<Teleportable>() == null) {
                                if (h.collider.transform.parent && transform.parent && h.collider.transform.parent.parent && transform.parent.parent) {
                                    if (h.collider.transform.parent.parent != transform.parent.parent)
                                        teleportScript.IgnoreCollision(h.collider);
                                } else {
                                    teleportScript.IgnoreCollision(h.collider);
                                }

                            }
                        }
                    }
                }
                UpdateDopplegangers();
            }
        }

        private void RemoveTeleportable(Teleportable teleportScript) {
            if (teleportScript && _nearTeleportables.Contains(teleportScript)) {
                teleportScript.ResetDoppleganger();
                if (!nonObliqueOverride)
                    teleportScript.SetClipPlane(Vector3.zero, Vector3.zero, teleportScript.oTeleportRends);
                _nearTeleportables.Remove(teleportScript);
                teleportScript.LeavePortal();
                teleportScript.ResumeAllCollision();
            }
        }

        /// <summary>
        /// Returns the quad verts of the near clip plane
        /// </summary>
        /// <param name="headCam">The camera to return</param>
        /// <returns></returns>
        private Vector3[] EyeNearPlaneDimensions(Camera headCam) {
            float a = headCam.nearClipPlane;//get length
            float A = headCam.fieldOfView * 0.5f;//get angle
            A = A * Mathf.Deg2Rad;//convert tor radians
            float h = (Mathf.Tan(A) * a);//calc height
            float w = (h / headCam.pixelHeight) * headCam.pixelWidth;//deduct width
                                                                     //VR eye fudging
            if (VR) {
                w += Mathf.Abs(SteamVR.instance.eyes[0].pos.x - SteamVR.instance.eyes[1].pos.x) / 2;
                h += Mathf.Abs(SteamVR.instance.eyes[0].pos.y - SteamVR.instance.eyes[1].pos.y) / 2;
            }
            Vector3[] returnedVerts = new Vector3[4];
            //Upper left
            returnedVerts[0] = new Vector3(-w, h, headCam.nearClipPlane);
            //Upper right
            returnedVerts[1] = new Vector3(w, h, headCam.nearClipPlane);
            //Lower left
            returnedVerts[2] = new Vector3(-w, -h, headCam.nearClipPlane);
            //Lower right
            returnedVerts[3] = new Vector3(w, -h, headCam.nearClipPlane);

            return returnedVerts;
        }

        /// <summary>
        /// Called when another portal is sending a camera to this portal
        /// </summary>
        public void IncomingCamera() {
            _CheeseActivated = true;
            _headInPortalTrigger = true;
            UpdateBackPosition();
            UpdateDopplegangers();
            UpdateScissor();
            UpdateEffects();
            TryRenderPortal(headCamera, _nearClipVertsGlobal);
        }
    }
}