using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using JoelG.ENA4;
using JoelG.ENA4.UI;
using JoelG.ENA4.Rendering;

using LMirman.Utilities.UI;
using System;
using System.Reflection;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine.UIElements;
using System.IO;
using System.Globalization;

namespace BarbequeONLINE
{


    
    public class Loader : MonoBehaviour
    {
        public static string scene_name;        
        private void ChangedActiveScene(Scene current, Scene next)
        {
            string currentName = current.name;
            scene_name = currentName;
            if (currentName != "Menu")
            {
                scene_name = next.name;
            }
            if (scene_name == "Menu")
            {
                UIHandler.UIInjector.InjectedIntoMainMenu = false;
            }
        }
        public void Start()
        {
            Debug.Log("hook start");
            SceneManager.activeSceneChanged += ChangedActiveScene;
            ChangedActiveScene(SceneManager.GetActiveScene(), SceneManager.GetActiveScene());
            Debug.Log("netcode start");
            NetcodeHandler.Start();
            Debug.Log("uihandler start");
            UIHandler.UIInjector.Start();
            Debug.Log("characterhandler start");
            CharacterHandler.Start();
            Debug.Log("end");
        }

        public void Update()
        {
            if (scene_name == "Menu")
            {
                UIHandler.UIInjector.AttemptInjectMainMenu();
            }else if (scene_name != "Boot")
            {
                NetcodeHandler.Update();
                UIHandler.UIInjector.AttemptInjectMainGame();
            }
        }
        
    }
}

namespace Doorstop
{
    public class Entrypoint
    {
        public static void Start()
        {
            new Thread(() =>
            {
                Debug.Log("Start!");
                Thread.Sleep(17500);
                GameObject MenuEditor_Holder = new GameObject();
                MenuEditor_Holder.AddComponent<BarbequeONLINE.Loader>();
                UnityEngine.Object.DontDestroyOnLoad(MenuEditor_Holder);
            }).Start();

        }
    }
}
