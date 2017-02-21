using UnityEngine;
using System.Collections;

using UnityEngine.Rendering;
using UnityEngine.Assertions;
using SKS.PortalLib;
namespace SKSStudios.Portals.VR {
    /// <summary>
    /// Class handling portal rendering
    /// </summary>
    public class PortalCamera : MonoBehaviour {
        //Render lib
        PortalCameraLib cameraLib;

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
        private RenderTexture leftEyeRenderTexture;
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

            _blitMat = new Material(Shader.Find("Unlit/Texture"));

        }

        /// <summary>
        /// Further initialization call
        /// </summary>
        /// <param name="VR"></param>
        public void Initialize(bool VR) {
            if (VR) {
                baseResolution = new Vector2((int)SteamVR.instance.sceneWidth, (int)SteamVR.instance.sceneHeight);
                this.VR = true;
            } else {
                baseResolution = new Vector2(Screen.width, Screen.height);
                this.VR = false;
            }

            cameraForPortal = GetComponent<Camera>();
            if (!leftEyeRenderTexture)
                leftEyeRenderTexture = new RenderTexture((int)baseResolution.x, (int)baseResolution.y, 24, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Default);
            if (!rightEyeRenderTexture)
                rightEyeRenderTexture = new RenderTexture((int)baseResolution.x, (int)baseResolution.y, 24, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Default);

            cameraForPortal.enabled = false;
            cameraLib = gameObject.AddComponent<PortalCameraLib>();
            cameraLib.Initialize(headCamera, cameraForPortal, portalTransform, portalDestination, baseResolution, _blitMat, VR);
        }

        /// <summary>
        /// Converts the HDM matrix to a 4x4 matrix (Credit to Railboy)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        protected Matrix4x4 HMDMatrix4x4ToMatrix4x4(Valve.VR.HmdMatrix44_t input) {
            var m = Matrix4x4.identity;
            m[0, 0] = input.m0;
            m[0, 1] = input.m1;
            m[0, 2] = input.m2;
            m[0, 3] = input.m3;

            m[1, 0] = input.m4;
            m[1, 1] = input.m5;
            m[1, 2] = input.m6;
            m[1, 3] = input.m7;

            m[2, 0] = input.m8;
            m[2, 1] = input.m9;
            m[2, 2] = input.m10;
            m[2, 3] = input.m11;

            m[3, 0] = input.m12;
            m[3, 1] = input.m13;
            m[3, 2] = input.m14;
            m[3, 3] = input.m15;

            return m;
        }
        /// <summary>
        /// Renders the portal view of a given portal
        /// </summary>
        /// <param name="material">The portal material</param>
        /// <param name="viewBounds">The bounds of the renderer on the screen</param>
        /// <param name="obliquePlane">Should the camera render with an oblique near clipping plane?</param>
        //public RenderTexture tempTex ;
        public void RenderIntoMaterial(Material material, MeshRenderer sourceRenderer, MeshRenderer targetRenderer, Mesh mesh, bool obliquePlane = true, bool optimize = true, bool is3d = false) {
            //Renders the portal itself to the rendertexture
            transform.parent = PortalCameraParent;
            PortalCameraParent.rotation = portalDestination.rotation * (Quaternion.Inverse(portalTransform.rotation) * (headCamera.transform.rotation));
            if (VR) {
                //Left eye
                //Older version
                //Matrix4x4 viewMatrix = HMDMatrix4x4ToMatrix4x4(SteamVR.instance.hmd.GetProjectionMatrix(Valve.VR.EVREye.Eye_Left, headCamera.nearClipPlane,
                //headCamera.farClipPlane, Valve.VR.EGraphicsAPIConvention.API_DirectX));
                Matrix4x4 viewMatrix = HMDMatrix4x4ToMatrix4x4(SteamVR.instance.hmd.GetProjectionMatrix(Valve.VR.EVREye.Eye_Left, headCamera.nearClipPlane,
                    headCamera.farClipPlane));

                cameraLib.RenderCamera(SteamVR.instance.eyes[0].pos, "_LeftEyeTexture", obliquePlane, optimize, material, viewMatrix, leftEyeRenderTexture, sourceRenderer, targetRenderer, mesh, is3d);
                //Right eye
                viewMatrix = HMDMatrix4x4ToMatrix4x4(SteamVR.instance.hmd.GetProjectionMatrix(Valve.VR.EVREye.Eye_Right, headCamera.nearClipPlane,
                    headCamera.farClipPlane));
                cameraLib.RenderCamera(SteamVR.instance.eyes[1].pos, "_RightEyeTexture", obliquePlane, optimize, material, viewMatrix, rightEyeRenderTexture, sourceRenderer, targetRenderer, mesh, is3d);
            } else {
                //Screen projection
                cameraLib.RenderCamera(headCamera.transform.position, "_RightEyeTexture", obliquePlane, optimize, material, headCamera.projectionMatrix, rightEyeRenderTexture, sourceRenderer, targetRenderer, mesh, is3d);
            }
        }
    }
}

