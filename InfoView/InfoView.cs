using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ICities;
using UnityEngine;
using UnityScript;
using ColossalFramework;
using System.Reflection;

namespace InfoView
{


	public class Mod : IUserMod
	{
		public string Name
		{
			get { return "InfoView"; }
		}

		public string Description
		{
			get { return "Show Info in a separate view"; }
		}
	}

	public class InfoView : MonoBehaviour
	{
		private CameraController controller;
		public static InfoView instance;
        private Camera newCamera;
        public static InfoManager.InfoMode currentInfoMode;
        public static InfoManager.SubInfoMode currentSubInfoMode;
        private Light mainLight;
		public static void Initialize()
		{
            try
            {
                var ml = GameObject.FindGameObjectWithTag("MainLight");
                var mc = GameObject.FindGameObjectWithTag("MainCamera");
                if (mc != null)
                {
                    var controller = mc.GetComponent<CameraController>();
                    instance = controller.gameObject.AddComponent<InfoView>();
                    instance.controller = controller;
                    InfoViewDebugger.Debug("got controller");
                }
                if (ml != null)
                {
                    instance.mainLight = ml.GetComponent<Light>();
                    InfoViewDebugger.Debug("got mainlight");
                }

                if (instance == null)
                {
                    InfoViewDebugger.Debug("instance is null!");
                }
                if (instance.controller == null)
                {
                    InfoViewDebugger.Debug("controller is null");
                }

                instance.newCamera = Camera.Instantiate<Camera>(Camera.main);
                instance.newCamera.backgroundColor = Color.cyan;
                instance.newCamera.cullingMask = Camera.main.cullingMask;
            }
            catch (Exception e)
            {
                InfoViewDebugger.Debug(e.Message);
                InfoViewDebugger.Debug(e.StackTrace);
            }
            if (instance == null)
            {
                InfoViewDebugger.Debug("instance is null after catch!");
            }
            instance.enabled = true;
			instance.InvokeRepeating("GetScreenshot", 1, 5);



		}

        public void GetScreenshot()
           {
           try
            {
            currentInfoMode = InfoManager.instance.CurrentMode;
            currentSubInfoMode = InfoManager.instance.CurrentSubMode;
            //InfoManager.instance.SetCurrentMode(InfoManager.InfoMode.Traffic, InfoManager.SubInfoMode.Default);
             /*   mainLight.color = InfoManager.instance.m_properties.m_lightColor;
                mainLight.intensity = InfoManager.instance.m_properties.m_lightIntensity;
                controller.SetViewMode(CameraController.ViewMode.Info);
                RenderSettings.ambientSkyColor = InfoManager.instance.m_properties.m_ambientColor;
                Shader.SetGlobalColor("_InfoCurrentColor", InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.Traffic].m_activeColor.linear);
                Shader.SetGlobalColor("_InfoCurrentColorB", InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.Traffic].m_activeColorB.linear);
                Shader.EnableKeyword("INFOMODE_ON");
                Shader.DisableKeyword("INFOMODE_OFF");
                Singleton<CoverageManager>.instance.SetMode(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Level.None, 300f, false);
                Singleton<ImmaterialResourceManager>.instance.ResourceMapVisible = ImmaterialResourceManager.Resource.None;
                Singleton<ElectricityManager>.instance.ElectricityMapVisible = false;
                Singleton<WaterManager>.instance.WaterMapVisible = false;
                Singleton<DistrictManager>.instance.DistrictsInfoVisible = false;
                Singleton<TransportManager>.instance.LinesVisible = false;
                Singleton<WindManager>.instance.WindMapVisible = false;
                Singleton<TerrainManager>.instance.TransparentWater = false;
                Singleton<BuildingManager>.instance.UpdateBuildingColors();
                Singleton<NetManager>.instance.UpdateSegmentColors();
                Singleton<NetManager>.instance.UpdateNodeColors();
                RenderManager.Managers_RenderOverlay(Singleton<RenderManager>.instance.CurrentCameraInfo);*/
            }
            catch (Exception e)
            {
                InfoViewDebugger.Debug("error in getscreenshot");
                InfoViewDebugger.Debug(e.Message);
                InfoViewDebugger.Debug(e.StackTrace);
            }
           InfoViewDebugger.Debug("calling render");
			StartCoroutine(SaveScreenshot_RenderToTexAsynch("c:\\temp\\render.png"));
		}
	

		private IEnumerator SaveScreenshot_RenderToTexAsynch(string filePath)
		{
			//Wait for graphics to render
			yield return new WaitForEndOfFrame();

            try
            {
                RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
                Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, true);

                InfoViewDebugger.Debug("creating new camera");
                 
                if (newCamera == null)
                {
                    InfoViewDebugger.Debug("newCamera is null");
                }
                InfoViewDebugger.Debug("getting caminfo");
                var gameCamInfo = Singleton<RenderManager>.instance.CurrentCameraInfo;
                
                InfoViewDebugger.Debug("copying camera");
                //newCamera.CopyFrom(gameCamInfo.m_camera);

                InfoViewDebugger.Debug("Cloning caminfo");
                var camInfo = CloneCamInfo(gameCamInfo);
                camInfo.m_camera = newCamera;

                InfoViewDebugger.Debug("rendering with newCamera");
                newCamera.targetTexture = rt;
                Singleton<RenderManager>.instance.BeginRendering(camInfo);
                Singleton<TerrainManager>.instance.BeginRendering(camInfo);
                Singleton<NetManager>.instance.BeginRendering(camInfo);
                Singleton<NetManager>.instance.BeginOverlay(camInfo);
                newCamera.Render();
                Singleton<NetManager>.instance.EndRendering(camInfo);
                Singleton<TerrainManager>.instance.EndRendering(camInfo);
                Singleton<NetManager>.instance.EndOverlay(camInfo);
                Singleton<RenderManager>.instance.EndRendering(camInfo);
                newCamera.targetTexture = null;

                /*InfoViewDebugger.Debug("loopingcameras");
                InfoViewDebugger.Debug("There are " + Camera.allCamerasCount + " cameras");
                foreach (Camera cam in Camera.allCameras)
                {
                    if (cam.name.ToLower().Contains("ui")) continue;
                    camInfo.m_camera = cam;
                    cam.targetTexture = rt;

                    cam.Render();

                    cam.targetTexture = null;
                }
                */
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                Camera.main.targetTexture = null;
                RenderTexture.active = null;
                Destroy(rt);


           // InfoManager.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
			byte[] bytes = screenShot.EncodeToPNG();
            FileWriter f = new FileWriter(bytes,filePath);
            Thread t = new Thread(new ThreadStart(f.WriteFile));
            t.Start();
            }
            catch (Exception e)
            {
                InfoViewDebugger.Debug("error in render");
                InfoViewDebugger.Debug(e.Message);
                InfoViewDebugger.Debug(e.StackTrace);
            }

            yield return 0;

		}
        private RenderManager.CameraInfo CloneCamInfo(RenderManager.CameraInfo from)
        {
            var newCamInfo = new RenderManager.CameraInfo()
            {
                m_camera = null,
                m_bounds = from.m_bounds,
                m_directionA = from.m_directionA,
                m_directionB = from.m_directionB,
                m_directionC = from.m_directionC,
                m_directionD = from.m_directionD,
                m_far = from.m_far,
                m_forward = from.m_forward,
                m_height = from.m_height,
                m_layerMask = from.m_layerMask,
                m_near = from.m_near,
                m_nearBounds = from.m_nearBounds,
                m_planeA = from.m_planeA,
                m_planeB = from.m_planeB,
                m_planeC = from.m_planeC,
                m_planeD = from.m_planeD,
                m_planeE = from.m_planeE,
                m_planeF = from.m_planeF,
                m_position = from.m_position,
                m_right = from.m_right,
                m_rotation = from.m_rotation,
                m_shadowRotation = from.m_shadowRotation,
                m_up = from.m_up
            };
            return newCamInfo;

        }

	}

    public class FileWriter{
        private byte[] bytes;
        private string path;
        public FileWriter(byte[] bytes, string path)
        {
            this.path = path;
            this.bytes = bytes;
        }

        public void WriteFile()
        {
            File.WriteAllBytes(path, bytes);
        }
    }

	public class ModLoad : LoadingExtensionBase
	{
		public override void OnLevelLoaded(LoadMode mode)
		{
            InfoViewDebugger.Debug("There are " + Camera.allCamerasCount + " cameras");
			InfoView.Initialize();

		}
        public override void OnLevelUnloading()
        {
            InfoView.instance.enabled = false;
            base.OnLevelUnloading();
        }
        public override void OnReleased()
        {
            base.OnReleased();
            InfoView.instance.enabled = false;
        }
	}

	public static class InfoViewDebugger
	{
		public static void Debug(string message)
		{
			DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, message);
		}
	}

}
