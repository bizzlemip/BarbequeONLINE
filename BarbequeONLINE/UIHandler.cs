using JoelG.ENA4;
using JoelG.ENA4.UI;
using LMirman.Utilities.UI;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BarbequeONLINE
{
    public class SwagInventoryOverlay : OverlayInterface
    {
        public InventoryTabGroup tabGroup;
        public InventoryOverlayPreview inventoryPreview;
        public InventoryOverlayAnimations inventoryAnimations;
        protected override void Awake()
        {

        }

        public Player player;

        public LetterboxFilter.FitParameters fitParameters;

        public static event Action InventoryOpened;

        public void new_awake()
        {
            base.Awake();
            player = ReInput.players.GetPlayer(0);
            fitParameters = new LetterboxFilter.FitParameters(16, 9, LetterboxFilter.FitMode.UseEither, Color.black, 0.1f, 0.2f, this);
            tabGroup.ItemChanged += TabGroupOnItemChanged;
        }

        public override void Open()
        {
            base.Open();
            LetterboxFilter.SetFit(fitParameters);
            if ((bool)PlayerCore.Instance)
            {
                PlayerCore.Instance.Equipment.SetDisarm(this);
            }
            GameOverlays.Self.Visuals.SetInventoryPauseVisible(value: true);
            inventoryAnimations.PlayIntro();
            inventoryPreview.EnablePreview();
            InventoryOpened();
        }

        public override void Close()
        {
            base.Close();
            LetterboxFilter.ClearFit(this);
            if ((bool)PlayerCore.Instance)
            {
                PlayerCore.Instance.Equipment.UnsetDisarm(this);
            }
            GameOverlays.Self.Visuals.SetInventoryPauseVisible(value: false);
            inventoryPreview.DisablePreview();
        }

        public override bool RequestClose()
        {
            if (inventoryAnimations.IsBusy)
            {
                return false;
            }
            inventoryAnimations.PlayClose();
            LetterboxFilter.ClearFit(this);
            return false;
        }

        private void Update()
        {
            Debug.Log("Running!");
            if (base.IsOpen)
            {
                Debug.Log("Passed!");
                inventoryPreview.UpdateInput();
                if (player.GetButtonDown(38))
                {
                    inventoryAnimations.GoToNextTabIfNotBusy();
                }
                else if (player.GetButtonDown(39))
                {
                    inventoryAnimations.GoToPrevTabIfNotBusy();
                }
                else if (player.GetButtonDown(37))
                {
                    RequestClose();
                }
            }
        }
        private void TabGroupOnItemChanged(Tab newTab)
        {
            InventoryOverlay.PanelName panelName;
            if (tabGroup.CurrentIndex == -1)
            {
                panelName = InventoryOverlay.PanelName.Null;
            }
            else
            {
                int current_ind = tabGroup.CurrentIndex;
                if (current_ind == 3)
                {
                    current_ind = 2;
                }
                panelName = (InventoryOverlay.PanelName)current_ind;
            }
            if (panelName == InventoryOverlay.PanelName.OutOfRange)
            {
                panelName = InventoryOverlay.PanelName.DialogueHistory;
                //Debug.LogWarning($"Unhandled tab index on inventory overlay {tabGroup.CurrentIndex}"); no idea tbh
            }
            inventoryPreview.SetTabInfo(panelName);
        }
    }
    class UIHandler
    {

        static SwagButton ServerListTab;
        static SwagButton MakeNewServerTab;
        static SwagButton CurrentServerTab;
        static SwagButton ConfigurationTab;
        static GameObject ServerButtonBase;
        static GameObject PlayerButtonBase;
        static CustomSlider MaxPlayersSlider;
        static CustomDropdown CharacterDropdown;
        static FieldInfo TextMesh;
        static OptionsMenu OptionsMenu;
        static GameObject Tab_Buttons;
        static GameObject BBQO_Ver_Text;
        static GameObject Version_Text;
        static GameObject ServerListTitleContainer;
        static GameObject Tabs;
        static GameObject ServerListTabs;
        public static string SelectedCharacter = "Taski";
        public static int MaxPlayers = 2;
        static bool in_serv_menu = false;
        static private Steamworks.CallResult<Steamworks.LobbyMatchList_t> m_LobbyMatchlist;

        private static void PlayerCallback(Steamworks.CSteamID cSteamId)
        {
            if (UIFunctions.MostRecentConfirmation)
            {
                UIFunctions.MostRecentConfirmation.Close();
            }
            Steamworks.SteamFriends.ActivateGameOverlayToUser("steamid", cSteamId);
        }
        public static void UpdateInServer()
        {
            if (NetcodeHandler.InServer)
            {
                MakeNewServerTab.gameObject.SetActive(false);
                ServerListTab.gameObject.SetActive(false);
                CurrentServerTab.gameObject.SetActive(true);
                MakeNewServerTab.CurrentTab.SetActive(false);
                ServerListTab.CurrentTab.SetActive(false);
                CurrentServerTab.CurrentTab.SetActive(true);
            }
            else
            {
                MakeNewServerTab.gameObject.SetActive(true);
                ServerListTab.gameObject.SetActive(true);
                CurrentServerTab.gameObject.SetActive(false);
                MakeNewServerTab.CurrentTab.SetActive(MakeNewServerTab.IsActive);
                ServerListTab.CurrentTab.SetActive(ServerListTab.IsActive);
                CurrentServerTab.CurrentTab.SetActive(false);
            }
        }
        public static void JoinServer(Steamworks.CSteamID ServerID)
        {
            if (UIFunctions.MostRecentConfirmation)
            {
                UIFunctions.MostRecentConfirmation.Close();
            }
            Steamworks.SteamAPICall_t LobbyJoinedCall = Steamworks.SteamMatchmaking.JoinLobby(ServerID);
            NetcodeHandler.m_LobbyJoined.Set(LobbyJoinedCall);
        }
        private static void RenderServer(int Current_Players, int Max_Players, String OwnerName, Steamworks.CSteamID ServerID)
        {
            GameObject NewServer = UnityEngine.Object.Instantiate(ServerButtonBase, ServerButtonBase.transform.parent);
            NewServer.name = "yeagw server";
            NewServer.GetComponent<OptionsSettingData>().SetInformation(OwnerName + "'s Server - " + Current_Players.ToString() + "/" + MaxPlayers.ToString(), "A server! That you can join! And theres like. Other people there! Wowza!");
            NewServer.SetActive(true);
            NewServer.GetComponent<CustomButton>().onClick.AddListener(() => { JoinServer(ServerID); });
        }
        public static void RenderServers(Steamworks.LobbyMatchList_t MatchList, bool bIOFailure)
        {
            for (int i = 0; i < ServerButtonBase.transform.parent.childCount; i++)
            {
                GameObject Child = ServerButtonBase.transform.parent.GetChild(i).gameObject;
                if (Child.name != "Refresh" && Child != ServerButtonBase)
                {
                    UnityEngine.Object.Destroy(Child);
                }
            }
            Debug.Log("RENDER SERVERS CALLED!");
            if (bIOFailure)
            {
                Debug.Log("FAILED");
            }
            else
            {
                for (int i = 0; i < MatchList.m_nLobbiesMatching; i++)
                {
                    Steamworks.CSteamID cSteamID = Steamworks.SteamMatchmaking.GetLobbyByIndex(i);
                    int max_players = Steamworks.SteamMatchmaking.GetLobbyMemberLimit(cSteamID);
                    int current_players = Steamworks.SteamMatchmaking.GetNumLobbyMembers(cSteamID);
                    string OwnerName = Steamworks.SteamMatchmaking.GetLobbyData(cSteamID, "OwnerName");
                    try
                    {
                        RenderServer(current_players, max_players, OwnerName, cSteamID);
                    }
                    catch (Exception ex) { };
                }
            }
        }

        private static void RenderPlayer(Steamworks.CSteamID Steam_ID, String PlayerName)
        {
            GameObject NewServer = UnityEngine.Object.Instantiate(PlayerButtonBase, PlayerButtonBase.transform.parent);
            NewServer.name = "yeagw player";
            NewServer.GetComponent<OptionsSettingData>().SetInformation(PlayerName, "Wow!!!! Its: " + PlayerName + "! I cannot believe that it is infact " + PlayerName + ". Unbelievable.");
            NewServer.GetComponent<CustomButton>().onClick.AddListener(() => { PlayerCallback(Steam_ID); });
            NewServer.SetActive(true);
        }

        private static void RefreshPlayers()
        {
            if (UIFunctions.MostRecentConfirmation)
            {
                UIFunctions.MostRecentConfirmation.Close();
            }
            for (int i = 0; i < PlayerButtonBase.transform.parent.childCount; i++)
            {
                GameObject Child = PlayerButtonBase.transform.parent.GetChild(i).gameObject;
                if (Child.name != "Refresh" && Child != PlayerButtonBase && Child.name != "Leave")
                {
                    UnityEngine.Object.Destroy(Child);
                }
            }
            for (int i = 0; i < Steamworks.SteamMatchmaking.GetNumLobbyMembers(NetcodeHandler.CurrentLobbyID); i++)
            {
                Steamworks.CSteamID cSteamID = Steamworks.SteamMatchmaking.GetLobbyMemberByIndex(NetcodeHandler.CurrentLobbyID, i);                try
                {
                    RenderPlayer(cSteamID, Steamworks.SteamFriends.GetFriendPersonaName(cSteamID));
                }
                catch (Exception ex) { RenderPlayer(cSteamID, cSteamID.ToString()); };
            }
        }
        private static void LeaveServer()
        {
            if (UIFunctions.MostRecentConfirmation)
            {
                UIFunctions.MostRecentConfirmation.Close();
            }
            Steamworks.SteamMatchmaking.LeaveLobby(NetcodeHandler.CurrentLobbyID);
            UpdateInServer();
        }
        public class SwagButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
        {

            private ComponentVisuals componentVisuals;
            public string Key;
            public SwagButton[] OtherButtons;
            public GameObject[] OtherTabs;
            public GameObject CurrentTab;

            public bool IsActive;

            private void Awake()
            {
                componentVisuals = GetComponent<ComponentVisuals>();
            }

            private void Update()
            {
                if (componentVisuals != null)
                {
                    componentVisuals.UpdateVisuals(IsActive, isHighlighted: false, IsActive);
                }
            }

            private void OnDestroy()
            {
            }
            private void SetIsActive(bool value)
            {
                IsActive = value;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (OtherButtons.Length == 0)
                {
                    return;
                }
                int ind = 0;
                foreach (SwagButton OtherButton in OtherButtons)
                {
                    Debug.Log(ind);
                    ind += 1;
                    OtherButton.IsActive = false;
                }
                Debug.Log("Passed");
                IsActive = true;
                CurrentTab.SetActive(true);
                foreach (GameObject OtherTab in OtherTabs)
                {
                    OtherTab.SetActive(false);
                }
            }
        }
        private static void RefreshServers()
        {
            Debug.Log("hi!!!!!!! refresh called!!!!");
            if (UIFunctions.MostRecentConfirmation)
            {
                UIFunctions.MostRecentConfirmation.Close();
            }
            Steamworks.SteamMatchmaking.AddRequestLobbyListDistanceFilter(Steamworks.ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            Steamworks.SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
            Steamworks.SteamAPICall_t try_getList = Steamworks.SteamMatchmaking.RequestLobbyList();
            Debug.Log("Setting result");
            m_LobbyMatchlist.Set(try_getList);
        }
        private static void UpdateCharacter(int ind)
        {
            SelectedCharacter = CharacterDropdown.options[CharacterDropdown.value].text;
        }
        private static void PanelUpdate(MainMenuPanel Panel)
        {
            if (Tab_Buttons == null)
            {
                Tab_Buttons = GameObject.Find("Tab Buttons");
            }
            if (Tab_Buttons != null)
            {
                if (!ServerListTitleContainer)
                {
                    ServerListTitleContainer = UnityEngine.Object.Instantiate(Tab_Buttons, Tab_Buttons.transform.parent);
                    for (int i = 0; i < ServerListTitleContainer.transform.childCount; i++)
                    {
                        GameObject child = ServerListTitleContainer.transform.GetChild(i).gameObject;
                        if (child.name == "Tab Button - Gameplay")
                        {
                            child.name = "Server List Title";
                            child.transform.Find("Label").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Server List";
                            UnityEngine.Object.Destroy(child.GetComponent<TabButton>());
                            ServerListTab = child.AddComponent<SwagButton>();
                            ServerListTab.IsActive = true;
                        }
                        else if (child.name == "Tab Button - Input")
                        {
                            child.name = "Current Server";
                            child.transform.Find("Label").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Current Server";
                            UnityEngine.Object.Destroy(child.GetComponent<TabButton>());
                            CurrentServerTab = child.AddComponent<SwagButton>();
                            CurrentServerTab.IsActive = true;
                            child.SetActive(false);
                        }
                        else if (child.name == "Tab Button - Video")
                        {
                            child.name = "Make New Server";
                            child.transform.Find("Label").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "New Server";
                            UnityEngine.Object.Destroy(child.GetComponent<TabButton>());
                            MakeNewServerTab = child.AddComponent<SwagButton>();
                        }
                        else if (child.name == "Tab Button - Audio")
                        {
                            child.name = "Server Config";
                            child.transform.Find("Label").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Configuration";
                            UnityEngine.Object.Destroy(child.GetComponent<TabButton>());
                            ConfigurationTab = child.AddComponent<SwagButton>();
                        }
                        else if (child.name != "PrevTab" && child.name != "NextTab")
                        {
                            UnityEngine.Object.Destroy(child);
                        }
                    }
                }

                if (!Tabs)
                {
                    ServerListTab.OtherButtons = new SwagButton[2] { MakeNewServerTab, ConfigurationTab };
                    MakeNewServerTab.OtherButtons = new SwagButton[2] { ServerListTab, ConfigurationTab };
                    ConfigurationTab.OtherButtons = new SwagButton[3] { ServerListTab, MakeNewServerTab, CurrentServerTab };
                    CurrentServerTab.OtherButtons = new SwagButton[1] { ConfigurationTab };
                    ServerListTab.OtherTabs = new GameObject[2];
                    MakeNewServerTab.OtherTabs = new GameObject[2];
                    ConfigurationTab.OtherTabs = new GameObject[3];
                    CurrentServerTab.OtherTabs = new GameObject[1];
                    Tabs = GameObject.Find("Tabs");
                    Debug.Log("Starting tabs!");
                    GameObject Options_Menu = GameObject.Find("Options");
                    if (Tabs && !ServerListTabs)
                    {
                        OptionsMenu = Options_Menu.GetComponent<OptionsMenu>();
                        if (OptionsMenu)
                        {
                            ServerListTabs = UnityEngine.Object.Instantiate(Tabs, Tabs.transform.parent);
                            for (int i = 0; i < ServerListTabs.transform.childCount; i++)
                            {
                                GameObject child = ServerListTabs.transform.GetChild(i).gameObject;
                                if (child.name == "Tab (1) - Gameplay")
                                {
                                    child.name = "Server List";
                                    Transform Container = child.transform.Find("Viewport/VBox");
                                    for (int i2 = 0; i2 < Container.childCount; i2++)
                                    {
                                        GameObject child2 = Container.GetChild(i2).gameObject;
                                        if (child2.name == "Button - Reset Gameplay")
                                        {
                                            OptionsSettingData NewData = child2.AddComponent<OptionsSettingData>();
                                            TextMesh.SetValue(NewData, child2.transform.Find("Label").gameObject.GetComponent<TMPro.TextMeshProUGUI>());
                                            NewData.SetInformation("Refresh Server List", "Refreshes the displayed serverlist.");
                                            ServerButtonBase = child2;
                                            GameObject child3 = UnityEngine.Object.Instantiate(child2, child2.transform.parent);
                                            child2.SetActive(false);
                                            child3.SetActive(true);
                                            child3.name = "Refresh";
                                            child3.GetComponent<CustomButton>().onClick.AddListener(RefreshServers);
                                        }
                                        else if (child2.name != "Refresh")
                                        {
                                            UnityEngine.Object.Destroy(child2);
                                        }
                                    }
                                    child.SetActive(true);
                                    ServerListTab.CurrentTab = child;
                                    MakeNewServerTab.OtherTabs[0] = child;
                                    ConfigurationTab.OtherTabs[0] = child;

                                }
                                else if (child.name == "Tab (2) - Input")
                                {
                                    child.name = "Current Server";
                                    child.SetActive(false);
                                    Transform Container = child.transform.Find("Viewport/VBox");
                                    for (int i2 = 0; i2 < Container.childCount; i2++)
                                    {
                                        GameObject child2 = Container.GetChild(i2).gameObject;
                                        if (child2.name == "Button - Reset Input")
                                        {
                                            OptionsSettingData NewData = child2.AddComponent<OptionsSettingData>();
                                            TextMesh.SetValue(NewData, child2.transform.Find("Label").gameObject.GetComponent<TMPro.TextMeshProUGUI>());
                                            NewData.SetInformation("Leave Server", "Leaves the current server.");
                                            PlayerButtonBase = child2;
                                            GameObject child3 = UnityEngine.Object.Instantiate(child2, Container);
                                            child2.SetActive(false);
                                            child3.name = "Leave";
                                            child3.GetComponent<CustomButton>().onClick.AddListener(LeaveServer);
                                            child3.SetActive(true);
                                            GameObject child4 = UnityEngine.Object.Instantiate(child2, Container);
                                            child4.GetComponent<OptionsSettingData>().SetInformation("Refresh Players", "Refreshes the list of players currently in your server.");
                                            child4.name = "Refresh";
                                            child4.GetComponent<CustomButton>().onClick.AddListener(RefreshPlayers);
                                            child4.SetActive(true);
                                        }
                                        else if (child2.name != "Leave" && child2.name != "Refresh")
                                        {
                                            UnityEngine.Object.Destroy(child2);
                                        }
                                    }
                                    CurrentServerTab.CurrentTab = child;
                                    ConfigurationTab.OtherTabs[1] = child;

                                }
                                else if (child.name == "Tab (3) - Video")
                                {
                                    child.name = "Make Server";
                                    Transform Container = child.transform.Find("Viewport/VBox");
                                    for (int i2 = 0; i2 < Container.childCount; i2++)
                                    {
                                        GameObject child2 = Container.GetChild(i2).gameObject;
                                        if (child2.name == "Slider - FPS Limit")
                                        {
                                            child2.GetComponent<OptionsSettingData>().SetInformation("Max Players", "The maximum amount of players in the created server.");
                                            child2.GetComponent<CustomSlider>().minValue = 2;
                                            child2.GetComponent<CustomSlider>().maxValue = 200;
                                            child2.GetComponent<CustomSlider>().wholeNumbers = true;
                                            child2.GetComponent<CustomSlider>().onValueChanged.AddListener(update_max_players);
                                            UnityEngine.Object.Destroy(child2.GetComponent<SliderSetting>());
                                            MaxPlayersSlider = child2.GetComponent<CustomSlider>();
                                        }
                                        else if (child2.name == "Button - Reset Video")
                                        {
                                            OptionsSettingData NewData = child2.AddComponent<OptionsSettingData>();
                                            TextMesh.SetValue(NewData, child2.transform.Find("Label").gameObject.GetComponent<TMPro.TextMeshProUGUI>());
                                            NewData.SetInformation("Create Server", "Creates a BarbequeONLINE server with the set amount of max players! :3");
                                            child2.GetComponent<CustomButton>().onClick.AddListener(MakeServerCreationConfirmation);
                                        }
                                        else
                                        {
                                            UnityEngine.Object.Destroy(child2);
                                        }
                                    }
                                    MakeNewServerTab.CurrentTab = child;
                                    ServerListTab.OtherTabs[0] = child;
                                    ConfigurationTab.OtherTabs[2] = child;
                                }
                                else if (child.name == "Tab (6) - Accessibility")
                                {
                                    Debug.Log("YEAH FOUND!!!");
                                    child.name = "Configuration";
                                    child.SetActive(false);
                                    Transform Container = child.transform.Find("Viewport/VBox");
                                    for (int i2 = 0; i2 < Container.childCount; i2++)
                                    {
                                        GameObject child2 = Container.GetChild(i2).gameObject;
                                        if (child2.name == "Dropdown - Accessibility Font")
                                        {
                                            Debug.Log("YEAH FOUND2!!!");
                                            UnityEngine.Object.Destroy(child2.GetComponent<DropdownSetting>());
                                            child2.GetComponent<OptionsSettingData>().SetInformation("Selected Character", "The character which other players will see you as. TIP: Press T to toggle Third Person Mode!");
                                            child2.GetComponent<CustomDropdown>().options.Clear();
                                            Debug.Log("YEAH FOUND3!!!");
                                            CharacterDropdown = child2.GetComponent<CustomDropdown>();
                                            foreach (KeyValuePair<String, GameObject> Entry in CharacterHandler.CharacterModels)
                                            {
                                                child2.GetComponent<CustomDropdown>().options.Add(new TMPro.TMP_Dropdown.OptionData(Entry.Key));
                                            }
                                            child2.GetComponent<CustomDropdown>().onValueChanged.AddListener(UpdateCharacter);
                                            Debug.Log("YEAH FOUND4!!!");
                                        }
                                        else
                                        {
                                            UnityEngine.Object.Destroy(child2);
                                        }
                                    }
                                    ConfigurationTab.CurrentTab = child;
                                    MakeNewServerTab.OtherTabs[1] = child;
                                    ServerListTab.OtherTabs[1] = child;
                                    CurrentServerTab.OtherTabs[0] = child;
                                }
                                else if (child.name != "PrevTab" && child.name != "NextTab")
                                {
                                    UnityEngine.Object.Destroy(child);
                                }
                            }
                        }
                    }
                }
                
                if (!BBQO_Ver_Text)
                {
                    Version_Text = GameObject.Find("Version Text");
                    if (Version_Text)
                    {
                        BBQO_Ver_Text = UnityEngine.Object.Instantiate(Version_Text, Version_Text.transform.parent);
                        UnityEngine.Object.Destroy(BBQO_Ver_Text.GetComponent<VersionText>());
                        BBQO_Ver_Text.GetComponent<TMPro.TextMeshProUGUI>().text = "BarbequeONLINE Ver - 0.0.3 by bizzlemip";
                    }
                }
                Tab_Buttons.SetActive(!in_serv_menu);
            }
            if (ServerListTabs)
            {
                ServerListTabs.SetActive(in_serv_menu);
            }
            if (Tabs)
            {
                Tabs.SetActive(!in_serv_menu);
            }
            if (BBQO_Ver_Text)
            {
                BBQO_Ver_Text.SetActive(in_serv_menu);
            }
            if (Version_Text)
            {
                Version_Text.SetActive(!in_serv_menu);
            }
            if (ServerListTitleContainer)
            {
                Debug.Log("ServerListTitleContainer FOUND!");

                ServerListTitleContainer.SetActive(in_serv_menu);
            }
        }
        private static void OptionsClicked()
        {
            GameObject Tab_Buttons = GameObject.Find("Tab Buttons");
            in_serv_menu = false;
        }
        private static void ServersClicked()
        {
            in_serv_menu = true;
        }
        private static void MakeServerCreationConfirmation()
        {
            UIFunctions.CreateConfirmationWindow(new ENAConfirmationWindow.CustomRequest(()=> {NetcodeHandler.MakeServer(MaxPlayers);
            }, delegate
            {
            }, "Start a BarbequeONLINE Server?", "You'll be starting a server that ANYONE with the mod can join!", "Yeag", "Nah", 1f), Resources.Load<GameObject>("UI/Confirmation Window"), GameObject.Find("Main Menu").GetComponent<Canvas>());
        }
        private static void update_max_players(float val)
        {
            MaxPlayers = (int)MaxPlayersSlider.value;
        }
        public class UIInjector
        {
            public static bool InjectedIntoMainMenu = false;
            public static bool InjectedIntoMainGame = false;
            public static bool OverlayModded = false;
            static FieldInfo inventoryAnimationsOverlay;
            static FieldInfo inventoryPreviewOverlay;
            static FieldInfo tabGroupOverlay;
            static FieldInfo playerOverlay;
            static FieldInfo fitParametersOverlay;
            static FieldInfo TabGroupItems;
            static FieldInfo TabGroupLookup;
            static FieldInfo TabKey;
            static FieldInfo TabMaskSprites;
            static FieldInfo TabImages;
            public static void Start()
            {
                TextMesh = (new OptionsSettingData()).GetType().GetField("labelTextMesh", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                m_LobbyMatchlist = Steamworks.CallResult < Steamworks.LobbyMatchList_t >.Create(RenderServers);
                TabGroupItems = (new TabGroup()).GetType().GetField("items", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                TabGroupLookup = (new TabGroup()).GetType().GetField("itemLookup", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                TabMaskSprites = (new InventoryTabGroup()).GetType().GetField("maskSprites", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                TabImages = (new InventoryTabGroup()).GetType().GetField("tabImages", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                TabKey = (new Tab()).GetType().GetField("key", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                inventoryAnimationsOverlay = (new InventoryOverlay()).GetType().GetField("inventoryAnimations", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                inventoryPreviewOverlay = (new InventoryOverlay()).GetType().GetField("inventoryPreview", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                tabGroupOverlay = (new InventoryOverlay()).GetType().GetField("tabGroup", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                playerOverlay = (new InventoryOverlay()).GetType().GetField("player", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);
                fitParametersOverlay = (new InventoryOverlay()).GetType().GetField("fitParameters", System.Reflection.BindingFlags.NonPublic
| System.Reflection.BindingFlags.Instance);

            }
            public static void AttemptInjectMainGame()
            {
                if (InjectedIntoMainGame)
                {
                    return;
                }
                InventoryOverlay TargetOverlay = UnityEngine.Object.FindObjectOfType<InventoryOverlay>();
                InventoryTabGroup TargetGroup = UnityEngine.Object.FindObjectOfType<InventoryTabGroup>();
                if (TargetOverlay && !OverlayModded)
                {
                    InventoryOverlayAnimations inventoryAnimations = inventoryAnimationsOverlay.GetValue(TargetOverlay) as InventoryOverlayAnimations;
                    InventoryOverlayPreview inventoryPreview = inventoryPreviewOverlay.GetValue(TargetOverlay) as InventoryOverlayPreview;
                    InventoryTabGroup tabGroup = tabGroupOverlay.GetValue(TargetOverlay) as InventoryTabGroup;
                    Player player = playerOverlay.GetValue(TargetOverlay) as Player;
                    LetterboxFilter.FitParameters fitParameters = fitParametersOverlay.GetValue(TargetOverlay) as LetterboxFilter.FitParameters;
                    SwagInventoryOverlay swagoverlay = TargetOverlay.gameObject.AddComponent<SwagInventoryOverlay>();
                    swagoverlay.inventoryAnimations = inventoryAnimations;
                    swagoverlay.inventoryPreview = inventoryPreview;
                    swagoverlay.tabGroup = tabGroup;
                    swagoverlay.player = player;//TO: FUTURE ME !!!! ! use reflections to clone InventoryOverlayAnimations! Youre gonna have to make a swag ver again. sorry
                    swagoverlay.fitParameters = fitParameters;
                    UnityEngine.Object.Destroy(TargetOverlay);
                    swagoverlay.new_awake();
                    OverlayModded = true;
                    Debug.Log("Overlay modded");
                }
                if (TargetGroup)
                {
                    Debug.Log("Modding ingame menu with chat");

                    List<Tab> MainTabs = (TabGroupItems.GetValue(TargetGroup) as List<Tab>);
                    Dictionary<string, int> MainTabsLookup = (TabGroupLookup.GetValue(TargetGroup) as Dictionary<string, int>);

                    Sprite[] maskSprites = (TabMaskSprites.GetValue(TargetGroup) as Sprite[]);
                    Sprite[] tabImages = (TabImages.GetValue(TargetGroup) as Sprite[]);
                    Array.Resize(ref maskSprites, 4);
                    Array.Resize(ref tabImages, 4);
                    maskSprites[3] = maskSprites[0];
                    tabImages[3] = tabImages[0];
                    TabMaskSprites.SetValue(TargetGroup, maskSprites);
                    TabImages.SetValue(TargetGroup, tabImages);
                    GameObject ServerTab = UnityEngine.Object.Instantiate(MainTabs[0].gameObject, MainTabs[0].transform.parent);
                    ServerTab.name = "ServerTab";
                    TabKey.SetValue(ServerTab.GetComponent<Tab>(), "inventory overlay 3");
                    MainTabs.Add(ServerTab.GetComponent<Tab>());
                    MainTabsLookup.Add("inventory overlay 3", 3);
                    InjectedIntoMainGame = true;
                }
            }
            public static void AttemptInjectMainMenu()
            {
                if (InjectedIntoMainMenu)
                {
                    return;
                }
                else
                {
                    if (GameObject.Find("Options Button"))
                    {
                        GameObject main_menu = GameObject.Find("Main Menu");
                        MainMenuPanelGroup panel_group = main_menu.GetComponent<MainMenuPanelGroup>();
                        panel_group.ItemChanged += PanelUpdate;
                        in_serv_menu = false;
                        GameObject Quit_Button = GameObject.Find("Exit Game");
                        Quit_Button.GetComponent<RectTransform>().localPosition -= new Vector3(0, 96, 0);
                        GameObject Options_Button = GameObject.Find("Options Button");
                        GameObject Server_Button = UnityEngine.Object.Instantiate(Options_Button, Options_Button.transform.parent) as GameObject;
                        Server_Button.name = "Server Button";
                        Server_Button.transform.Find("Label").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Servers";
                        Debug.Log("Passed text change!");
                        Server_Button.GetComponent<RectTransform>().localPosition -= new Vector3(0, 192, 0);
                        Options_Button.GetComponent<CustomButton>().onClick.AddListener(OptionsClicked);
                        Server_Button.GetComponent<CustomButton>().onClick.AddListener(ServersClicked);
                        InjectedIntoMainMenu = true;
                    }
                }
                
            }
        }
    }
}
