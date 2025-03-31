using JoelG.ENA4.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BarbequeONLINE
{
    class CharacterHandler
    {
        static FieldInfo DirectionSpriteFrames;
        static FieldInfo DirectionalSpriteFrames;
        static FieldInfo DirectionalSpriteFramesFin;
        static FieldInfo SpriteRenderer;
        static FieldInfo FrameRate;
        public static Dictionary<String, GameObject> CharacterModels = new Dictionary<String, GameObject>() { };
        public static Dictionary<String, String> DirectionalCharacters = new Dictionary<String, String>() { ["Taski"] = "sp_Walks_Taskimaiden_", ["Rantarou"] = "sp_Rantarou_", ["Doomguy"] = "Doomguy_" };
        public static void MakeDirectional(GameObject Target, String Prefix, AssetBundle bundle)
        {
            DirectionalSpriteFrames.DirectionSpriteFrames[] Out_Frames = new DirectionalSpriteFrames.DirectionSpriteFrames[8];
            for (int i = 0; i < 8; i += 1)
            {
                int i3 = 7 - i;
                int i2 = -i3;
                if (i2 < 0)
                {
                    i2 += 8;
                }
                i2 *= 4;
                Sprite[] tmp2 = { bundle.LoadAsset<Sprite>(Prefix + i2), bundle.LoadAsset<Sprite>(Prefix + (i2 + 1)), bundle.LoadAsset<Sprite>(Prefix + (i2 + 2)), bundle.LoadAsset<Sprite>(Prefix + (i2 + 3)) };//hae hae composer reference
                Out_Frames[i] = new DirectionalSpriteFrames.DirectionSpriteFrames() { };
                DirectionSpriteFrames.SetValue(Out_Frames[i], tmp2);
            }
            DirectionalSpriteFrames DFrames = ScriptableObject.CreateInstance<DirectionalSpriteFrames>();
            DirectionalSpriteFrames.SetValue(DFrames, Out_Frames);
            DirectionalSprite dirSprite = Target.AddComponent<DirectionalSprite>();
            DirectionalSpriteFramesFin.SetValue(dirSprite, DFrames);
            SpriteRenderer.SetValue(dirSprite, Target.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>());
            FrameRate.SetValue(dirSprite, 4);
        }
        public static void Start()
        {
            DirectionSpriteFrames = (new DirectionalSpriteFrames.DirectionSpriteFrames() { }).GetType().GetField("frames", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
            DirectionalSpriteFrames = (ScriptableObject.CreateInstance<DirectionalSpriteFrames>()).GetType().GetField("directionFrames", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
            DirectionalSpriteFramesFin = (new DirectionalSprite() { }).GetType().GetField("frames", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
            SpriteRenderer = (new DirectionalSprite() { }).GetType().GetField("spriteRenderer", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
            FrameRate = (new DirectionalSprite() { }).GetType().GetField("frameRate", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
            Assembly _assembly = Assembly.GetExecutingAssembly();
            Stream _fileStream = _assembly.GetManifestResourceStream("BarbequeONLINE.barbequeonlinecharacters");
            byte[] imageData = new byte[_fileStream.Length];
            _fileStream.Read(imageData, 0, (int)_fileStream.Length);
            AssetBundle BarbequeONLINECharacters = AssetBundle.LoadFromMemory(imageData);
            GameObject DirectionalBase = BarbequeONLINECharacters.LoadAsset("DirectionalBase") as GameObject;
            DirectionalBase.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(DirectionalBase);
            foreach (KeyValuePair<String, String> Entry in DirectionalCharacters)
            {
                Debug.Log(Entry.Key);
                GameObject newer = UnityEngine.Object.Instantiate(DirectionalBase);
                newer.SetActive(false);
                Debug.Log("pass1");
                MakeDirectional(newer, Entry.Value, BarbequeONLINECharacters);
                Debug.Log("pass2");
                UnityEngine.Object.DontDestroyOnLoad(newer);
                CharacterModels.Add(Entry.Key, newer);
            }
        }
    }
}
