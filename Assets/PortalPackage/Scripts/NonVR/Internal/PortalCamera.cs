using UnityEngine;
using System.Collections;

using UnityEngine.Rendering;
using UnityEngine.Assertions;
using SKS.PortalLib;

/// <summary>
/// Class handling portal rendering
/// </summary>

namespace SKSStudios.Portals.NonVR {
    public class PortalCamera : MonoBehaviour {
        //Render lib
        PortalCameraLib cameraLib;

        private Portal portal;

        //Is the camera in VR mode?
        private bool VR;
        public Transform PortalCameraParent;
        public Transform portalTransform;
        public Transform portalDestination;
        public Camera headCamera;
        public bool distanceScaling = true;
        public Camera cameraForPortal;
        public Mesh blitMesh;

        private Vector2 baseResolution;
        private RenderTexture rightEyeRenderTexture;
        //Saves shadows in between eye renders
        //private RenderTexture shadowRenderTexture;
        private CommandBuffer shadowSaveBuffer;
        private CommandBuffer shadowWriteBuffer;
        private CommandBuffer portalBuffer;

        private Material _blitMat;

        private MeshRenderer _targetRenderer;
        private Rect _boundRect;

        public Rect displayRect;

        //Cameras for recursion
        private Camera[] recursionCams;
        private int recursionNumber = 0;
        /// <summary>
        /// Instantiation is done on awake
        /// </summary>
        void Awake() {
            this.enabled = false;
            //This feature has been deferred until Unity adds a hook for shadow rendering, but once they do shadows will only have to be rendered once per frame
            //Save shadow passes in between eyes 
            //RenderTargetIdentifier shadowTarget = new RenderTargetIdentifier("ShadowTarget");
            //shadowSaveBuffer.SetRenderTarget(shadowTarget);
            //shadowSaveBuffer.Blit(BuiltinRenderTextureType.Depth, shadowTarget);
            //Blit the shadows to the new eye
            //shadowWriteBuffer.SetRenderTarget(shadowTarget);
            //shadowSaveBuffer.Blit(shadowTarget, BuiltinRenderTextureType.Depth);
            _blitMat = new Material(Shader.Find("Custom/InverseDepthBlit"));

        }

        private void LateUpdate() {
            recursionNumber = 0;
            foreach (Camera camera in recursionCams) {
                //camera.transform.localPosition = Vector3.zero;
                //camera.transform.localRotation = Quaternion.identity;
            }
        }
        /// <summary>
        /// Further initialization call
        /// </summary>
        /// <param name="VR"></param>
        public void Initialize(bool VR, Portal portal) {
            baseResolution = new Vector2(Screen.width, Screen.height);
            cameraForPortal = GetComponent<Camera>();
            if (!rightEyeRenderTexture)
                rightEyeRenderTexture = new RenderTexture((int)baseResolution.x, (int)baseResolution.y, 24, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Default);

            cameraForPortal.enabled = false;
            cameraLib = gameObject.AddComponent<PortalCameraLib>();
            cameraLib.Initialize(portalTransform, portalDestination, baseResolution, _blitMat, VR);
            this.portal = portal;
        }

        public void InstantiateRecursion(int count) {
            count++;
            recursionCams = new Camera[count];
            string name = this.name + Random.value;
            CameraMarker mainMarker = gameObject.AddComponent<CameraMarker>();
            mainMarker.Initialize(this);

            for (int i = 0; i < count; i++) {
                GameObject cameraRecursor = new GameObject();
                cameraRecursor.transform.parent = PortalCameraParent;
                //cameraRecursor.transform.parent = cameraForPortal.transform;
                cameraRecursor.transform.localPosition = Vector3.zero;
                cameraRecursor.transform.localRotation = Quaternion.identity;
                CameraMarker marker = cameraRecursor.AddComponent<CameraMarker>();
                marker.Initialize(this);
                Camera camera = cameraRecursor.AddComponent<Camera>();

                //Don't use copyof here, unexpected behavior
                camera.cullingMask = cameraForPortal.cullingMask;
                camera.name = name + "Recursor " + i;
                camera.renderingPath = cameraForPortal.renderingPath;
                camera.stereoTargetEye = cameraForPortal.stereoTargetEye;
                camera.useOcclusionCulling = cameraForPortal.useOcclusionCulling;
                camera.enabled = false;
                
                camera.ResetProjectionMatrix();
                camera.ResetWorldToCameraMatrix();
                camera.ResetCullingMatrix();
                //camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, invertBuffer);

                recursionCams[i] = camera;

            }
        }

        /// <summary>
        /// Renders the portal view of a given portal
        /// </summary>
        /// <param name="material">The portal material</param>
        /// <param name="viewBounds">The bounds of the renderer on the screen</param>
        /// <param name="obliquePlane">Should the camera render with an oblique near clipping plane?</param>
        //public RenderTexture tempTex ;
        //Camera lastRenderedCamera;
        public void RenderIntoMaterial(Camera camera, Material material, MeshRenderer sourceRenderer, MeshRenderer targetRenderer, Mesh mesh, bool obliquePlane = true, bool optimize = true, bool is3d = false) {
            Camera renderingCamera = cameraForPortal;
            Camera currentCamera = camera;
            bool inverted = portal.inverted;
            CameraMarker marker = camera.GetComponent<CameraMarker>();
            //if (marker && marker.owner != this)
            //  return;
            //Unsafe recursive rendering
            /*
            if (marker && marker.owner == portal.targetPortal.portalCamera)
               return;*/
            //Safe recursive rendering
            if (marker && marker.owner != portal.portalCamera)
                return; 
            if (recursionNumber > portal.maxRenderCount)
                return;
            //if (recursionNumber == portal.maxRenderCount)
                //camera.cullingMask = camera.cullingMask | ~LayerMask.NameToLayer("PortalPlaceholder");
            //else if(camera.cullingMask == (camera.cullingMask & LayerMask.NameToLayer("PortalPlaceholder")))
             //       camera.cullingMask = ~(~camera.cullingMask | LayerMask.NameToLayer("PortalPlaceholder"));

            renderingCamera = recursionCams[recursionNumber];
            recursionNumber++;

            if (currentCamera != headCamera) {
                inverted = !inverted;
            }
            //Renders the portal itself to the rendertexture
            transform.parent = PortalCameraParent;
            renderingCamera.transform.rotation = portalDestination.rotation * (Quaternion.Inverse(portalTransform.rotation) * (camera.transform.rotation));
            cameraLib.RenderCamera(renderingCamera, currentCamera, currentCamera.transform.position, currentCamera.projectionMatrix, "_RightEyeTexture", obliquePlane, optimize, material, rightEyeRenderTexture, sourceRenderer, targetRenderer, mesh, is3d, inverted);
            material.SetTexture("_LeftEyeTexture", rightEyeRenderTexture);

            //camera.cullingMask = cachedLayers;
        }
    }
}
