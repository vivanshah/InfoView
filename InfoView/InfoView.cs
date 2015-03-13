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
        public static InfoManager.InfoMode currentInfoMode;
        public static InfoManager.SubInfoMode currentSubInfoMode;

		public static void Initialize()
		{
			var controller = GameObject.FindObjectOfType<CameraController>();

			instance = controller.gameObject.AddComponent<InfoView>();
			instance.controller = controller;
			instance.enabled = true;

			instance.InvokeRepeating("GetScreenshot", 1, 5);



		}

        public void GetScreenshot()
        {
            currentInfoMode = InfoManager.instance.CurrentMode;
            currentSubInfoMode = InfoManager.instance.CurrentSubMode;
            InfoManager.instance.SetCurrentMode(InfoManager.InfoMode.Traffic, InfoManager.SubInfoMode.Default);
			StartCoroutine(SaveScreenshot_RenderToTexAsynch("c:\\temp\\render.png"));
		}
	

		private IEnumerator SaveScreenshot_RenderToTexAsynch(string filePath)
		{
			//Wait for graphics to render
			yield return new WaitForEndOfFrame();


			RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
			Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, true);

            foreach (Camera cam in Camera.allCameras)
			{
				if (cam.name.ToLower().Contains("ui")) continue;
				cam.targetTexture = rt;
                cam.Render();
				cam.targetTexture = null;
			}

			RenderTexture.active = rt;
			screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
			Camera.main.targetTexture = null;
			RenderTexture.active = null;
			Destroy(rt);
			
            yield return 0;

            InfoManager.instance.SetCurrentMode(currentInfoMode, currentSubInfoMode);
			byte[] bytes = screenShot.EncodeToPNG();
            FileWriter f = new FileWriter(bytes,filePath);
            Thread t = new Thread(new ThreadStart(f.WriteFile));
            t.Start();

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
	}

	public static class InfoViewDebugger
	{
		public static void Debug(string message)
		{
			DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, message);
		}
	}

}
