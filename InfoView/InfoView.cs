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
		private GameObject traffic { get; set; }
		private int x = 0;
		private CameraController controller;
		public static InfoView instance;

        private List<Camera> RenderCameras;
		public static void Initialize(GameObject traffic)
		{
			var controller = GameObject.FindObjectOfType<CameraController>();
			instance = controller.gameObject.AddComponent<InfoView>();
			instance.controller = controller;
			instance.enabled = true;
			instance.traffic = traffic;

			instance.InvokeRepeating("GetScreenshot", 1, 5);

		}

		public void GetScreenshot()
		{
			InfoManager.instance.SetCurrentMode(InfoManager.InfoMode.Traffic, InfoManager.SubInfoMode.Default);
			StartCoroutine(SaveScreenshot_RenderToTexAsynch("c:\\temp\\render.png"));

            try
            {
                var data = new FastList<IDataContainer>();
                InfoManager.instance.GetData(data);
                FileStream fileStream = new FileStream("c:\\temp\\file.txt", FileMode.Create);
                DataSerializer.SerializeArray<IDataContainer>(fileStream, DataSerializer.Mode.Full, 1, data.ToArray());
                fileStream.Flush();
                fileStream.Close();
                fileStream.Dispose();

            }
            catch (Exception e)
            {
                Debugger.Debug(e.Message);
                Debugger.Debug(e.StackTrace);
            }

		}
	
		private void PrintStuff()
		{
			try {
				var go = new GameObject();
				go.AddComponent<Camera>();
				var mainCamera = go.GetComponent<Camera>();
				mainCamera.enabled = false;
				mainCamera.CopyFrom(Camera.main);
				//Debugger.Debug("mainCamera cullingMask: " + Convert.ToString(mainCamera.cullingMask, 2));
				Debugger.Debug("Attempting screenshot");
				//InfoManager.instance.SetCurrentMode(InfoManager.InfoMode.Traffic,InfoManager.SubInfoMode.Default);

				//var infoCamera = new Camera();
				//infoCamera.CopyFrom(mainCamera);
				Application.CaptureScreenshot("c:\\temp\\screenshot.png");

				int sqr = 512;

				//infoCamera.aspect = 1.0f;
				// recall that the height is now the "actual" size from now on

				var tempRT = new RenderTexture(1366, 768, 24);
				// the 24 can be 0,16,24, formats like
				// RenderTextureFormat.Default, ARGB32 etc.

				mainCamera.targetTexture = tempRT;
				mainCamera.Render();
				Debugger.Debug("took picture");
				RenderTexture.active = tempRT;
				var virtualPhoto =
					new Texture2D(1366, 768, TextureFormat.RGB24, false);
				// false, meaning no need for mipmaps
				virtualPhoto.ReadPixels(new Rect(0, 0, 1366, 768), 0, 0);



				byte[] bytes;
				bytes = virtualPhoto.EncodeToPNG();
				Debugger.Debug("bytes: " + bytes.Length);
				System.IO.File.WriteAllBytes(
					"c:\\temp\\render2.png", bytes);
				RenderTexture.active = null; //can help avoid errors 
				mainCamera.targetTexture = null;
				// consider ... Destroy(tempRT);
			}
			catch (Exception e)
			{
				Debugger.Debug(e.Message);
				Debugger.Debug(e.StackTrace);
				Debugger.Debug(e.InnerException.Message);
				Debugger.Debug(e.InnerException.StackTrace);
			}
		//	InfoManager.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
		}


		private IEnumerator SaveScreenshot_RenderToTexAsynch(string filePath)
		{
			//Wait for graphics to render
			yield return new WaitForEndOfFrame();

			RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
			//Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
			Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, true);

			//Camera.main.targetTexture = rt;
			//Camera.main.Render();

			//Render from all!
			foreach (Camera cam in Camera.allCameras)
			{
				if (cam.name.ToLower().Contains("ui")) continue;
				cam.targetTexture = rt;
				cam.Render();
				cam.targetTexture = null;
			}
			InfoManager.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
			RenderTexture.active = rt;
			screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
			Camera.main.targetTexture = null;
			RenderTexture.active = null; //Added to avoid errors
			Destroy(rt);

			//Split the process up
			yield return 0;

			byte[] bytes = screenShot.EncodeToPNG();
			File.WriteAllBytes(filePath, bytes);


		}

		
	}

	public class ModLoad : LoadingExtensionBase
	{
		public override void OnLevelLoaded(LoadMode mode)
		{
			Debugger.Debug("There are " + Camera.allCamerasCount + " cameras");
			var mainCamera = Camera.allCameras.First();
			//Debugger.Debug("Main camera initial mask: " + Convert.ToString(mainCamera.cullingMask, 2));
			/*var trafficObjects = FindMatchingObjects("traffic");
			foreach(var t in trafficObjects)
			{
				Debugger.Debug("TrafficObject:" + t.name);
				Debugger.Debug("TrafficObject type:" + t.GetType().FullName);
			}*/
			var trafficCongestion = FindObjectByName("TrafficCongestion");
			InfoView.Initialize(trafficCongestion);
		}

		private static GameObject FindObjectByName(string name)
		{
			var gameObjects = GameObject.FindObjectsOfType<GameObject>();
			foreach (var gameObject in gameObjects) {
				if (gameObject.name == name) return gameObject;
			}

			return null;
		}

		private static List<GameObject> FindMatchingObjects(string name)
		{
			var gameObjects = GameObject.FindObjectsOfType<GameObject>();
			var results = new List<GameObject>();
			foreach (var gameObject in gameObjects) {
				if (gameObject.name.ToLower().Contains(name)) results.Add(gameObject);
			}

			return results;
		}
	}

	public static class Debugger
	{
		public static void Debug(string message)
		{

			DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, message);
		}
	}

}
