using Eppy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SKSStudios.Portals.NonVR {
    public class PhysicsPassthrough : MonoBehaviour {
        private Portal portal;
        private HashSet<Tuple<int, int>> ignoredCollisions;
        private HashSet<int> colliders;
        //Remove this 
        public List<GameObject> collidersPub;
        private Bounds bounds;
        new private Collider collider;
        private bool initialized = false;
        void Start() {
            ignoredCollisions = new HashSet<Tuple<int, int>>();
            colliders = new HashSet<int>();
            collidersPub = new List<GameObject>();
            collider = gameObject.GetComponent<Collider>();
            bounds.size = Vector3.Scale(bounds.size, transform.lossyScale);
        }

        public void Initialize(Portal portal) {
            this.portal = portal;
            initialized = true;
        }

        public void Update() {

            if (initialized)
                transform.position = portal.destination.position + (portal.targetPortal.transform.forward * bounds.extents.x);
            bounds = collider.bounds;
        }

        public void OnTriggerEnter(Collider col) {
            if (GlobalPortalSettings.physicsPassthrough && initialized) {
                if (col && col.gameObject.isStatic && !col.gameObject.tag.Equals("PhysicsPassthroughDuplicate") && !colliders.Contains(col.GetInstanceID())) {
                    GameObject newCollider = new GameObject();

                    Transform newTransform = newCollider.transform;
                    newTransform = newTransform.GetCopyOf(col.transform);
                    //newTransform.localScale = col.transform.lossyScale;
                    //newCollider.transform.SetParent(portal.destination);
                    //newCollider.transform.SetParent(portal.targetPortal.targetPortal.origin);
                    newCollider.transform.SetParent(null);
                    newCollider.isStatic = false;
                    Collider newColliderComponent = new Collider();

                    //Unfortunately no better way to do this
                    if (col is BoxCollider) {
                        newColliderComponent = newCollider.AddComponent<BoxCollider>();
                        newColliderComponent = newColliderComponent.GetCopyOf((BoxCollider)col);
                    } else if (col is MeshCollider) {
                        newColliderComponent = newCollider.AddComponent<MeshCollider>();
                        newColliderComponent = newColliderComponent.GetCopyOf((MeshCollider)col);

                    } else if (col is CapsuleCollider) {
                        newColliderComponent = newCollider.AddComponent<CapsuleCollider>();
                        newColliderComponent = newColliderComponent.GetCopyOf((CapsuleCollider)col);

                    } else if (col is SphereCollider) {
                        newColliderComponent = newCollider.AddComponent<SphereCollider>();
                        newColliderComponent = newColliderComponent.GetCopyOf((SphereCollider)col);

                    } else if (col is TerrainCollider) {
                        newColliderComponent = newCollider.AddComponent<TerrainCollider>();
                        newColliderComponent = newColliderComponent.GetCopyOf((TerrainCollider)col);
                        ((TerrainCollider)newColliderComponent).terrainData = ((TerrainCollider)col).terrainData;
                    }

                    //newColliderComponent.isTrigger = true;
                    newCollider.layer = 2;
                    newCollider.tag = "PhysicsPassthroughDuplicate";
                    newCollider.name = "Duplicate Collider";
                    Tuple<Collider, Collider> newEntry = new Tuple<Collider, Collider>((Collider)col, newCollider.GetComponent<Collider>());
                    portal.passthroughColliders.Add(newEntry);
                    colliders.Add(col.GetInstanceID());
                    collidersPub.Add(newColliderComponent.gameObject);
                }
            }
        }

        public void OnDrawGizmosSelected() {
            if (GlobalPortalSettings.physicsPassthrough && initialized) {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(bounds.center, bounds.extents * 2f);
            }
        }
        public void OnTriggerExit(Collider col) {
            if (GlobalPortalSettings.physicsPassthrough && initialized) {
                for (int i = 0; i < portal.passthroughColliders.Count; i++) {
                    var tup = (Tuple<Collider, Collider>)portal.passthroughColliders[i];
                    if (tup.Item1.GetInstanceID() == col.GetInstanceID()) {
                        Destroy(tup.Item2.gameObject);
                        tup.Item2.enabled = false;
                        portal.passthroughColliders.RemoveAt(i);
                        colliders.Remove(col.GetInstanceID());
                        i--;
                        break;
                    }
                }
            }
        }

        public void UpdatePhysics() {
            if (initialized) {
                Bounds exclBounds = new Bounds(transform.position, bounds.extents * 2f);
                //Grow bounds to encapsulate all duped colliders
                foreach (Tuple<Collider, Collider> c in portal.passthroughColliders) {
                    exclBounds.Encapsulate(c.Item2.bounds);
                }

                ArrayList nearDynamicColliders = new ArrayList(Physics.OverlapBox(transform.position, exclBounds.extents * 2.2f, transform.rotation, ~0, QueryTriggerInteraction.Ignore));

                foreach (Collider n in nearDynamicColliders) {
                    if (!n || n.gameObject.isStatic)
                        continue;

                    foreach (Tuple<Collider, Collider> c in portal.passthroughColliders) {
                        if (!c.Item2)
                            continue;
                        if (!ignoredCollisions.Contains(new Tuple<int, int>(n.GetInstanceID(), c.Item2.GetInstanceID()))) {
                            Physics.IgnoreCollision(c.Item2, n, true);
                            ignoredCollisions.Add(new Tuple<int, int>(n.GetInstanceID(), c.Item2.GetInstanceID()));
                        }
                    }

                    foreach (Collider c in portal.bufferWall) {
                        if (!ignoredCollisions.Contains(new Tuple<int, int>(n.GetInstanceID(), c.GetInstanceID()))) {
                            Physics.IgnoreCollision(c, n, true);
                            ignoredCollisions.Add(new Tuple<int, int>(n.GetInstanceID(), c.GetInstanceID()));
                        }
                    }
                }

                if (GlobalPortalSettings.physicsPassthrough && initialized) {
                    for (int i = 0; i < portal.passthroughColliders.Count; i++) {
                        var tup = (Tuple<Collider, Collider>)portal.passthroughColliders[i];

                        //tup.Item2.transform.localPosition = portal.destination.InverseTransformPoint(tup.Item1.transform.position);
                        PortalUtils.TeleportObject(tup.Item2.gameObject, portal.destination, portal.origin, tup.Item2.gameObject.transform, tup.Item1.transform);
                        // tup.Item2.transform.rotation = portal.destination.rotation * Quaternion.Inverse(portal.origin.rotation) * tup.Item1.transform.rotation;
                        /*
                        tup.Item2.transform.localScale = new Vector3(
                           tup.Item1.transform.lossyScale.x / portal.destination.localScale.x,
                           tup.Item1.transform.lossyScale.y / portal.destination.localScale.y,
                           tup.Item1.transform.lossyScale.z / portal.destination.localScale.z);*/
                        tup.Item2.transform.localScale = new Vector3(
                       tup.Item1.transform.lossyScale.x * (portal.origin.lossyScale.x / portal.destination.lossyScale.x),
                       tup.Item1.transform.lossyScale.y * (portal.origin.lossyScale.y / portal.destination.lossyScale.y),
                       tup.Item1.transform.lossyScale.z * (portal.origin.lossyScale.z / portal.destination.lossyScale.z));
                    }
                }
            }
        }

        /*
        public void UpdatePhysics() {
            if (portal.passthroughColliders != null) {
                ArrayList colliders = new ArrayList();
                ArrayList nearColliders = new ArrayList(Physics.OverlapBox(transform.position + (transform.rotation * new Vector3(0, 0, (maxDistance / 2f) + 0.01f)), Vector3.one * maxDistance / 2f, transform.rotation, ~0, QueryTriggerInteraction.Ignore));
                for (int i = 0; i < portal.passthroughColliders.Count; i++) {
                    var tup = (Tuple<Collider, Collider>)portal.passthroughColliders[i];

                    tup.Item2.transform.localPosition = portal.origin.InverseTransformPoint(tup.Item1.transform.position);
                    tup.Item2.transform.rotation = portal.destination.rotation * Quaternion.Inverse(portal.origin.rotation) * tup.Item1.transform.rotation;
                    tup.Item2.transform.localScale = new Vector3(
                        tup.Item1.transform.lossyScale.x / portal.destination.localScale.x,
                        tup.Item1.transform.lossyScale.y / portal.destination.localScale.y,
                        tup.Item1.transform.lossyScale.z / portal.destination.localScale.z);

                    if (!nearColliders.Contains(tup.Item1)) {
                        Destroy(tup.Item2);
                        portal.passthroughColliders.Remove(tup);
                        i--;
                        continue;
                    }
                    colliders.Add(tup.Item1);
                }


                foreach (Collider col in nearColliders) {
                    if (col && !col.gameObject.tag.Equals("PhysicsPassthroughDuplicate") && !colliders.Contains(col)) {
                        GameObject newCollider = new GameObject();

                        Transform newTransform = newCollider.transform;
                        newTransform = newTransform.GetCopyOf(col.transform);
                        //newTransform.localScale = col.transform.lossyScale;
                        newCollider.transform.SetParent(portal.destination);
                        newCollider.isStatic = false;
                        Collider newColliderComponent = new Collider();

                        //Unfortunately no better way to do this
                        if (col is BoxCollider) {
                            newColliderComponent = newCollider.AddComponent<BoxCollider>();
                            newColliderComponent = newColliderComponent.GetCopyOf((BoxCollider)col);
                        } else if (col is MeshCollider) {
                            newColliderComponent = newCollider.AddComponent<MeshCollider>();
                            newColliderComponent = newColliderComponent.GetCopyOf((MeshCollider)col);

                        } else if (col is CapsuleCollider) {
                            newColliderComponent = newCollider.AddComponent<CapsuleCollider>();
                            newColliderComponent = newColliderComponent.GetCopyOf((CapsuleCollider)col);

                        } else if (col is SphereCollider) {
                            newColliderComponent = newCollider.AddComponent<SphereCollider>();
                            newColliderComponent = newColliderComponent.GetCopyOf((SphereCollider)col);

                        } else if (col is TerrainCollider) {
                            newColliderComponent = newCollider.AddComponent<TerrainCollider>();
                            newColliderComponent = newColliderComponent.GetCopyOf((TerrainCollider)col);
                        }

                        //newColliderComponent.isTrigger = true;
                        newCollider.layer = 2;
                        newCollider.tag = "PhysicsPassthroughDuplicate";
                        newCollider.name = "Duplicate Collider";
                        Tuple<Collider, Collider> newEntry = new Tuple<Collider, Collider>((Collider)col, newCollider.GetComponent<Collider>());
                        portal.passthroughColliders.Add(newEntry);
                    }
                }

                ArrayList nearDynamicColliders = new ArrayList(Physics.OverlapBox(transform.position, Vector3.one * maxDistance * 2, transform.rotation, ~0, QueryTriggerInteraction.Ignore));

                foreach (Collider n in nearDynamicColliders) {
                    if (!n || n.gameObject.isStatic)
                        continue;

                    foreach (Tuple<Collider, Collider> c in portal.targetPortal.passthroughColliders) {
                        if (!c.Item2)
                            continue;
                        if (!ignoredCollisions.ContainsKey(new Tuple<int, int>(n.GetInstanceID(), c.Item2.GetInstanceID()))) {
                            Physics.IgnoreCollision(c.Item2, n, true);
                            ignoredCollisions.Add(new Tuple<int, int>(n.GetInstanceID(), c.Item2.GetInstanceID()), true);
                        }
                    }

                    foreach (Collider c in portal.targetPortal.bufferWall) {
                        if (!ignoredCollisions.ContainsKey(new Tuple<int, int>(n.GetInstanceID(), c.GetInstanceID()))) {
                            Physics.IgnoreCollision(c, n, true);
                            ignoredCollisions.Add(new Tuple<int, int>(n.GetInstanceID(), c.GetInstanceID()), true);
                        }
                    }
                }
            }
        }*/

    }
}
