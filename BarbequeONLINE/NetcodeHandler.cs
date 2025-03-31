using JoelG.ENA4;
using JoelG.ENA4.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BarbequeONLINE
{
    public class MannequinGuy : MonoBehaviour
    {
        public Vector3 TargetPos;
        public float TargetRot;
        public float currentrot;

        public void Update()
        {
            transform.position += (TargetPos - transform.position) * Time.deltaTime;
            currentrot += (TargetRot - currentrot) * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, currentrot, 0);
        }
    }
    class NetcodeHandler
    {
        public static bool InServer = false;
        public static Steamworks.CSteamID CurrentLobbyID;
        public static Steamworks.CallResult<Steamworks.LobbyCreated_t> m_LobbyCreated;
        public static Steamworks.CallResult<Steamworks.LobbyEnter_t> m_LobbyJoined;
        public static Steamworks.CallResult<Steamworks.LobbyMatchList_t> m_LobbyMatchlist;
        static PropertyInfo SetThirdPersonActive;
        static float curr_time;
        static bool was_key;
        public static bool was_val;
        static float lastrotation;
        static Steamworks.CSteamID SteamId;
        static GameObject CurrentThirdPerson;

        static Dictionary<Steamworks.CSteamID, MannequinGuy> Mannequins = new Dictionary<Steamworks.CSteamID, MannequinGuy>();

        private static void OnLobbyCreated(Steamworks.LobbyCreated_t callback, bool bIOFailure)
        {
            Debug.LogError("OnLobbyCreated callback triggered.");

            Debug.LogError("Lobby created successfully.");
            Debug.LogError($"Lobby ID: {callback.m_ulSteamIDLobby}");
            InServer = true;
            CurrentLobbyID = new Steamworks.CSteamID(callback.m_ulSteamIDLobby);
            Steamworks.SteamMatchmaking.SetLobbyData(CurrentLobbyID, "OwnerName", Steamworks.SteamFriends.GetPersonaName());
            UIHandler.UpdateInServer();
        }
        public static void MakeServer(int MaxPlayers)
        {
            Steamworks.SteamAPICall_t LobbyCreatedCall = Steamworks.SteamMatchmaking.CreateLobby(Steamworks.ELobbyType.k_ELobbyTypePublic, MaxPlayers);
            m_LobbyCreated.Set(LobbyCreatedCall);
        }
        private static void MakeThirdPerson()
        {
            if (CurrentThirdPerson)
            {
                UnityEngine.Object.Destroy(CurrentThirdPerson);
            }
            CurrentThirdPerson = UnityEngine.Object.Instantiate(CharacterHandler.CharacterModels[UIHandler.SelectedCharacter]);
            CurrentThirdPerson.transform.GetChild(0).gameObject.AddComponent<SpriteBillboard>();
        }
        private static void OnLobbyJoined(Steamworks.LobbyEnter_t callback, bool bIOFailure)
        {
            Debug.LogError("OnLobbyJoined callback triggered.");

            Debug.LogError("Lobby joined successfully.");
            Debug.LogError($"Lobby ID: {callback.m_ulSteamIDLobby}");
            InServer = true;
            CurrentLobbyID = new Steamworks.CSteamID(callback.m_ulSteamIDLobby);
            UIHandler.UpdateInServer();
        }
        private static float bParse(string input)
        {
            return (float.Parse(input, CultureInfo.InvariantCulture));
        }
        public static void Start()
        {
            m_LobbyCreated = Steamworks.CallResult<Steamworks.LobbyCreated_t>.Create(OnLobbyCreated);
            m_LobbyJoined = Steamworks.CallResult<Steamworks.LobbyEnter_t>.Create(OnLobbyJoined);
            SetThirdPersonActive = (new PlayerTransformation()).GetType().GetProperty("Active");
        }

        public static void Update()
        {
            curr_time += Time.deltaTime;
            GameObject Player = GameObject.Find("Player/Player");
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (!was_key)
                {
                    was_val = !was_val;
                    if (was_val)
                    {
                        if (!CurrentThirdPerson)
                        {
                            MakeThirdPerson();
                        }

                    }

                    CurrentThirdPerson.SetActive(was_val);
                    SetThirdPersonActive.SetValue(Player.GetComponent<PlayerTransformation>(), was_val);
                }
                was_key = true;

            }
            else
            {
                was_key = false;
            }
            if (CurrentThirdPerson)
            {
                CurrentThirdPerson.transform.SetParent(Player.transform.parent);
                CurrentThirdPerson.transform.position = Player.transform.position;
                if (Player.GetComponent<Rigidbody>().velocity.magnitude > 0.1f)
                {
                    lastrotation = Player.GetComponent<PlayerViewer>().PlayerRotation;
                }
                CurrentThirdPerson.transform.rotation = Quaternion.Euler(0, lastrotation + 30, 0);
            }
            if (curr_time > 0.1)
            {
                curr_time = 0;
                if (InServer)
                {
                    if (Player)
                    {
                        Steamworks.SteamMatchmaking.SetLobbyMemberData(CurrentLobbyID, "scene_name", Loader.scene_name);
                        Steamworks.SteamMatchmaking.SetLobbyMemberData(CurrentLobbyID, "chosen_char", UIHandler.SelectedCharacter);
                        Steamworks.SteamMatchmaking.SetLobbyMemberData(CurrentLobbyID, "pos_x", (Math.Max(Math.Min(Player.transform.position.x, 999999), -999999)).ToString(CultureInfo.InvariantCulture));
                        Steamworks.SteamMatchmaking.SetLobbyMemberData(CurrentLobbyID, "pos_y", (Math.Max(Math.Min(Player.transform.position.y, 999999), -999999)).ToString(CultureInfo.InvariantCulture));
                        Steamworks.SteamMatchmaking.SetLobbyMemberData(CurrentLobbyID, "pos_z", (Math.Max(Math.Min(Player.transform.position.z, 999999), -999999)).ToString(CultureInfo.InvariantCulture));
                        Steamworks.SteamMatchmaking.SetLobbyMemberData(CurrentLobbyID, "rot", (Math.Floor(Player.GetComponent<PlayerViewer>().PlayerRotation * 20) / 20).ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        return;
                    }
                    List<Steamworks.CSteamID> OutIds = new List<Steamworks.CSteamID>();
                    SteamId = Steamworks.SteamUser.GetSteamID();
                    for (int i = 0; i < Steamworks.SteamMatchmaking.GetNumLobbyMembers(CurrentLobbyID); i++)
                    {
                        Steamworks.CSteamID cSteamID = Steamworks.SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobbyID, i);
                        if (cSteamID == SteamId)
                        {
                            continue;
                        }
                        string t_scene_name = Steamworks.SteamMatchmaking.GetLobbyMemberData(CurrentLobbyID, cSteamID, "scene_name");
                        string chosen_char = Steamworks.SteamMatchmaking.GetLobbyMemberData(CurrentLobbyID, cSteamID, "chosen_char");
                        float pos_x = bParse(Steamworks.SteamMatchmaking.GetLobbyMemberData(CurrentLobbyID, cSteamID, "pos_x"));
                        float pos_y = bParse(Steamworks.SteamMatchmaking.GetLobbyMemberData(CurrentLobbyID, cSteamID, "pos_y"));
                        float pos_z = bParse(Steamworks.SteamMatchmaking.GetLobbyMemberData(CurrentLobbyID, cSteamID, "pos_z"));
                        float rot = bParse(Steamworks.SteamMatchmaking.GetLobbyMemberData(CurrentLobbyID, cSteamID, "rot"));
                        if (!Mannequins.ContainsKey(cSteamID))
                        {
                            if (t_scene_name != null && t_scene_name != "")
                            {
                                GameObject NewThing = UnityEngine.Object.Instantiate(CharacterHandler.CharacterModels.ContainsKey(chosen_char) ? CharacterHandler.CharacterModels[chosen_char] : CharacterHandler.CharacterModels["Taski"]);
                                NewThing.transform.GetChild(0).gameObject.AddComponent<SpriteBillboard>();
                                UnityEngine.Object.DontDestroyOnLoad(NewThing);
                                UnityEngine.Object.Destroy(NewThing.GetComponent<NPCInteractable>());
                                UnityEngine.Object.Destroy(NewThing.GetComponent<CapsuleCollider>());
                                NewThing.SetActive(true);
                                NewThing.transform.position = new Vector3(pos_x, pos_y, pos_z);
                                Mannequins.Add(cSteamID, NewThing.AddComponent<MannequinGuy>());
                            }
                        }
                        else
                        {
                            Mannequins[cSteamID].gameObject.transform.SetParent(Player.transform.parent);
                            Mannequins[cSteamID].TargetPos = new Vector3(pos_x, pos_y, pos_z);
                            Mannequins[cSteamID].TargetRot = rot;
                            Mannequins[cSteamID].gameObject.SetActive(t_scene_name == Loader.scene_name);
                        }

                        OutIds.Add(cSteamID);
                    }
                    List<Steamworks.CSteamID> DelIDS = new List<Steamworks.CSteamID>();

                    foreach (KeyValuePair<Steamworks.CSteamID, MannequinGuy> keyValuePair in Mannequins)
                    {
                        if (!OutIds.Contains(keyValuePair.Key))
                        {
                            UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
                            DelIDS.Add(keyValuePair.Key);
                        }
                    }
                }
            }
        }

    }
}
