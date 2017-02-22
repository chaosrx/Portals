using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SKS.PortalLib;

namespace SKSStudios.Portals.NonVR {
    [RequireComponent(typeof(Collider))]
    public class Teleportable : MonoBehaviour {
        //The root of the game object to be copied
        public Transform root;

        public Material replacementMaterial;
        [HideInInspector]
        public GameObject doppleganger;
        //Gameobjects going through a portal may have one skinned renderer and one animator
        [HideInInspector]
        public Renderer[] oTeleportRends;
        [HideInInspector]
        public Renderer[] dTeleportRends;
        [HideInInspector]
        public bool movementOverride = false;

        public bool VisOnly = false;
        public bool isActive = true;

        private Transform[] originalTransforms;
        private Transform[] duplicateTransforms;
        private SkinnedMeshRenderer[] dSkinnedRenderers;
        private TeleportableScript[] teleportableScripts;

        private ArrayList oTransformList;
        private ArrayList dTransformList;
        private ArrayList oRendererList;
        private ArrayList dRendererList;
        private ArrayList dSkinnedRendList;
        private ArrayList teleportableScriptList;

        //Collider collections for the purpose of physics through portals
        private List<Collider> ignoredColliders;
        private List<Collider> addedColliders;

        private ArrayList oColliders;

        private TeleportableLib teleLib;
        Animator test;
        [HideInInspector]
        public bool initialized = false;
        // Use this for initialization
        void Start() {
            teleLib = gameObject.AddComponent<TeleportableLib>();
            oTransformList = new ArrayList();
            dTransformList = new ArrayList();
            oRendererList = new ArrayList();
            dRendererList = new ArrayList();
            dSkinnedRendList = new ArrayList();
            ignoredColliders = new List<Collider>();
            addedColliders = new List<Collider>();
            oColliders = new ArrayList();
            teleportableScriptList = new ArrayList();
            if (!root)
                root = transform;
            //StartCoroutine(SpawnDoppleganger());
            SpawnDoppleganger();   
        }

        // IEnumerator
        void SpawnDoppleganger() {
            //Wait for any initializing behaviors
            //yield return new WaitForSeconds(2);
            //Spawns the doppleganger game object
            doppleganger = new GameObject("doppleganger");
            //yield return new WaitForEndOfFrame();
            doppleganger = CloneNonVisuals(doppleganger, root.gameObject, null);
            //doppleganger.transform.parent = transform.parent;       
            //yield return new WaitForEndOfFrame();
            //Transfers bone references to doppleganger bones
            if (dSkinnedRendList.Count > 0) {
                dSkinnedRenderers = (SkinnedMeshRenderer[])dSkinnedRendList.ToArray(typeof(SkinnedMeshRenderer));
                //Copies over correct bones and sets the root bone
                for (int i = 0; i < dSkinnedRenderers.Length; i++) {
                    SkinnedMeshRenderer rend = dSkinnedRenderers[i];
                    Transform oRootBone = rend.rootBone;
                    Transform dRootBone = PortalUtils.FindAnalogousTransform(oRootBone, root, doppleganger.transform);
                    Transform[] originalBones;
                    Transform[] duplicateBones;

                    originalBones = rend.bones;
                    duplicateBones = new Transform[rend.bones.Length];

                    for (int u = 0; u < rend.bones.Length; u++) {
                        duplicateBones[u] = PortalUtils.FindAnalogousTransform(rend.bones[u], root, doppleganger.transform);
                    }

                    rend.rootBone = dRootBone;
                    rend.bones = duplicateBones;
                }
            }

            originalTransforms = (Transform[])oTransformList.ToArray(typeof(Transform));
            duplicateTransforms = (Transform[])dTransformList.ToArray(typeof(Transform));
            //Exchanges material for cullable
            oTeleportRends = (Renderer[])oRendererList.ToArray(typeof(Renderer));
            dTeleportRends = (Renderer[])dRendererList.ToArray(typeof(Renderer));
            if (replacementMaterial) {
                //yield return new WaitForEndOfFrame();
                foreach (Renderer renderer in oTeleportRends) {
                    if (renderer.GetType() == typeof(ParticleSystemRenderer))
                        continue;
                    Material[] newMats = new Material[renderer.materials.Length];
                    for (int i = 0; i < renderer.materials.Length; i++) {
                        Material oNewMat = new Material(replacementMaterial);
                        Material m = renderer.materials[i];
                        oNewMat.CopyPropertiesFromMaterial(m);
                        newMats[i] = oNewMat;
                    }
                    renderer.materials = newMats;
                }

                //yield return new WaitForEndOfFrame();

                foreach (Renderer renderer in dTeleportRends) {
                    Material[] newMats = new Material[renderer.materials.Length];
                    if (renderer.GetType() == typeof(ParticleSystemRenderer))
                        continue;
                    for (int i = 0; i < renderer.materials.Length; i++) {
                        Material dNewMat = new Material(replacementMaterial);
                        Material m = renderer.materials[i];
                        dNewMat.CopyPropertiesFromMaterial(m);
                        newMats[i] = dNewMat;
                    }
                    renderer.materials = newMats;
                }
            }


            //Saves teleportable scripts to array
            teleportableScripts = (TeleportableScript[])teleportableScriptList.ToArray(typeof(TeleportableScript));

            //Initializes teleportable scripts
            foreach (TeleportableScript t in teleportableScripts)
                t.Initialize(this);

            ResetDoppleganger();
            initialized = true;
            doppleganger.name = doppleganger.name + " (Doppleganger)";

        }

        public void Teleport() {
            //ResetDoppleganger();
            foreach (TeleportableScript t in teleportableScriptList)
                t.OnTeleport();
            //Close out collisions to prevent ignoring of important collisions post-teleport
        }

        //Resets doppleganger, making it inactive 
        public void ResetDoppleganger() {
            doppleganger.transform.SetParent(transform.parent);
            doppleganger.transform.localPosition = Vector3.zero;
            doppleganger.transform.localRotation = Quaternion.identity;
            doppleganger.SetActive(false);
        }

        // Update is called once per frame
        void LateUpdate() {
            //if (initialized && doppleganger.activeSelf)
            //if(initialized)
            //    UpdateDoppleGanger();
            if (!isActive) {
                doppleganger.SetActive(false);
            }
        }

        public IEnumerator UpdateDoppleganger() {
            doppleganger.transform.SetParent(root.parent);
            if (!movementOverride) {
                for (int i = 0; i < originalTransforms.Length; i++) {

                    if (originalTransforms[i] != root) {
                        duplicateTransforms[i].localPosition = originalTransforms[i].localPosition;
                        duplicateTransforms[i].localRotation = originalTransforms[i].localRotation;
                    }
                }

                yield return new WaitForEndOfFrame();
                //Prevents renderers from being enabled mid-frame

                for (int i = 0; i < originalTransforms.Length - 1; i++)
                    duplicateTransforms[i].gameObject.SetActive(originalTransforms[i].gameObject.activeSelf);


                doppleganger.transform.localScale = root.lossyScale;
            }
            yield return null;

        }

        //Removes all non-visual components on the gameobject
        GameObject CloneNonVisuals(GameObject clone, GameObject original, Transform parent) {
            //Disables components if they do not match type
            foreach (Component component in original.GetComponents<Component>()) {
                //Check if the component already exists (I.E. transforms)
                Component copy;
                //Copies transforms for later updating
                if (component is Transform) {
                    copy = teleLib.CopyToClone(component, original, clone);
                    oTransformList.Add(component);
                    dTransformList.Add(copy);
                } else
                //for later bone copying
                if (component is SkinnedMeshRenderer) {
                    copy = teleLib.CopyToClone(component, original, clone);
                    dSkinnedRendList.Add(copy as SkinnedMeshRenderer);
                } else
                //Adds materials to the material queue to be exchanged for culling material
                if (component is Renderer) {
                    copy = teleLib.CopyToClone(component, original, clone);
                    oRendererList.Add((component as Renderer));
                    dRendererList.Add((copy as Renderer));
                    //Adds colliders to list for collision ignoring upon portal entry
                } else if (component is Collider) {
                    oColliders.Add(component);
                } else if (component is MonoBehaviour) {
                    //Handling of teleportable scripts
                    if (component.GetType().IsSubclassOf(typeof(TeleportableScript))) {
                        teleportableScriptList.Add(component);
                    }
                    //Nonspecific setup copying
                } else if (component is MeshFilter || component is ParticleSystem) {
                    copy = teleLib.CopyToClone(component, original, clone);
                    //nothing to do here
                }
            }
            clone.SetActive(original.activeSelf);
            clone.transform.SetParent(parent);
            //Iterates over all children
            foreach (Transform t in original.transform) {
                GameObject newObject = new GameObject(t.gameObject.name);
                newObject.transform.SetParent(clone.transform);
                CloneNonVisuals(newObject, t.gameObject, clone.transform);
            }
            //Tags the gameobject as being part of a teleportable
            return clone;
        }

       

        public void SetClipPlane(Vector3 position, Vector3 vector, Renderer[] renderers) {
            foreach (Renderer renderer in renderers) {
                for (int i = 0; i < renderer.materials.Length; i++) {
                    renderer.materials[i].SetVector("_ClipPosition", position);
                    renderer.materials[i].SetVector("_ClipVector", vector);
                    if (!renderer.materials[i].HasProperty("_ClipPosition"))
                        Debug.LogWarning("Default material not properly edited for clipping! Objects will not disappear as they enter portals.");
                }
            }
        }

        //Add a child object to the doppleganger
        public void AddChild(GameObject gameObject) {
            GameObject newObject = new GameObject("NewDopplegangerPart");
            newObject.transform.parent = PortalUtils.FindAnalogousTransform(gameObject.transform.parent, root, doppleganger.transform, true);
            CloneNonVisuals(newObject, gameObject, newObject.transform.parent);
        }

        public void RemoveChild(GameObject gameObject) {
            Destroy(PortalUtils.FindAnalogousTransform(gameObject.transform, root, doppleganger.transform, false).gameObject);
        }

        public void IgnoreCollision(Collider ignoredCollider) {
            if (!ignoredColliders.Contains(ignoredCollider))
                ignoredColliders.Add(ignoredCollider);

            foreach (Collider col in oColliders)
                Physics.IgnoreCollision(col, ignoredCollider, true);
        }

        //Overrides ignore
        public void AddCollision(Collider addCollider) {
            if (!addedColliders.Contains(addCollider))
                addedColliders.Add(addCollider);

            foreach (Collider col in oColliders)
                Physics.IgnoreCollision(col, addCollider, false);
        }

        public void ResumeAllCollision() {
            foreach (Collider col in oColliders) {

                foreach (Collider aCol in addedColliders) {
                    if (aCol)
                        Physics.IgnoreCollision(col, aCol, true);
                }

                foreach (Collider iCol in ignoredColliders) {
                    if (iCol)
                        Physics.IgnoreCollision(col, iCol, false);
                }

            }
            ignoredColliders.Clear();
            addedColliders.Clear();
        }

        public void SetPortalInfo(Portal portal) {
            foreach (TeleportableScript ts in teleportableScripts) {
                ts.currentPortal = portal;
            }
        }

        public void LeavePortal() {
            foreach (TeleportableScript ts in teleportableScripts) {
                ts.leavePortal();
            }
            StartCoroutine(DisableDoppleganger());
        }

        IEnumerator DisableDoppleganger() {
            yield return new WaitForEndOfFrame();
            doppleganger.SetActive(false);
        }
    }
}