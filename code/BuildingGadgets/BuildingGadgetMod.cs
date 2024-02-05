using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;

namespace BuildingGadgets
{
    public class BuildingGadgetMod : ModSystem
    {
        ICoreAPI api;
        ICoreServerAPI sapi;
        ICoreClientAPI capi;

        public GlKeys range_increase = GlKeys.BracketRight;
        public GlKeys range_decrease = GlKeys.BracketLeft;

        private IClientNetworkChannel clientChannel;
        private IServerNetworkChannel serverChannel;

        public BuildingGadgetsConfig BGConfig
        {
            get
            {
                return (BuildingGadgetsConfig)this.api.ObjectCache["bg_config.json"];
            }
            set
            {
                this.api.ObjectCache.Add("bg_config.json", value);
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.api = api;

            // load the config 
            BuildingGadgetsConfig bgconfig = null;
            try
            {
                 bgconfig = this.api.LoadModConfig<BuildingGadgetsConfig>("bg_config.json");
            }
            catch (Exception e)
            {
                base.Mod.Logger.Warning("Error loading Building Gadget Config: " + Environment.NewLine + e.Message + " : " + e.StackTrace);
            }
            if (bgconfig == null)
            {
                base.Mod.Logger.Warning("Regenerating default config for Building Gadgets as it was missing or broken.");                
                api.StoreModConfig<BuildingGadgetsConfig>(new BuildingGadgetsConfig(), "bg_config.json");
                bgconfig = api.LoadModConfig<BuildingGadgetsConfig>("bg_config.json");
            }
            this.BGConfig = bgconfig;

            // Register items
            api.RegisterItemClass("BuildingGadgetItem", typeof(BuildingGadgetItem));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;

            // Server things, like event subscription, I hope this doesn't cause to much lag...
            serverChannel = sapi.Network.RegisterChannel("buildinggadget").RegisterMessageType(typeof
                (RangeSync)).RegisterMessageType(typeof(RangeSyncResponse)).SetMessageHandler<RangeSync>(new
                NetworkClientMessageHandler<RangeSync>(OnClientSent));
        }

        private void OnClientSent(IServerPlayer fromPlayer, RangeSync networkMessage)
        {
            if (fromPlayer == null || networkMessage == null) return;
            ItemSlot slot = fromPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot != null 
                && slot.Itemstack != null 
                && slot.Itemstack.Item != null 
                && slot.Itemstack.Item.FirstCodePart().Contains("buildinggadget"))
            {
                if (!(slot.Itemstack.Item is BuildingGadgetItem gadget)) return;
                bool changed = gadget.UpdateRange(slot, networkMessage.increase, fromPlayer);
                if (changed) gadget.ClearHighlight(fromPlayer.Entity.World, fromPlayer.Entity.Player, slot, true);
                slot.MarkDirty();
                RangeSyncResponse rangeResponse = new RangeSyncResponse()
                {
                    response = changed
                };                
                serverChannel.SendPacket<RangeSyncResponse>(rangeResponse, fromPlayer);
            }
        }

        private void Event_AfterActiveSlotChanged(IServerPlayer serverplayer, ActiveSlotChangeEventArgs eventArgs)
        {
            int fromslot = eventArgs.FromSlot;
            int toslot = eventArgs.ToSlot;
            //float playerpicking = serverplayer.WorldData.PickingRange;
            IInventory playerinventory = serverplayer.InventoryManager.GetInventory("character");            
            if (playerinventory == null) return;
            ItemStack fromstack = playerinventory[fromslot].Itemstack;
            if (fromstack == null) return;
            if (fromstack.Item.FirstCodePart() == "buildinggadget")
            {
                serverplayer.WorldData.PickingRange = GlobalConstants.DefaultPickingRange;
                serverplayer.BroadcastPlayerData();
            }
            ItemStack tostack = playerinventory[toslot].Itemstack;
            if (tostack == null) return;
            if (tostack.Item.FirstCodePart() == "buildinggadget")
            {
                if (tostack.Attributes.GetInt("toolFunction", 0) == 0)
                {
                    // item going TO is a gadget AND it's function is set to BuildToMe
                    serverplayer.WorldData.PickingRange = 64;
                    serverplayer.BroadcastPlayerData();
                    return;
                }
                serverplayer.WorldData.PickingRange = GlobalConstants.DefaultPickingRange;
                serverplayer.BroadcastPlayerData();
            } 
            
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;
            RegisterGadgetKeys();
            // client things, like visual & rendering things and client events
            capi.Event.AfterActiveSlotChanged += Event_ClientAfterActiveSlotChanged;
            clientChannel = api.Network.RegisterChannel("buildinggadget").RegisterMessageType(typeof
                (RangeSync)).RegisterMessageType(typeof(RangeSyncResponse)).SetMessageHandler<RangeSyncResponse>(new
                NetworkServerMessageHandler<RangeSyncResponse>(OnClientReceived));
        }

        private void OnClientReceived(RangeSyncResponse networkMessage)
        {            
            if (networkMessage.response)
            {
                if (capi != null)
                {
                    ItemSlot itemSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
                    int range = itemSlot.Itemstack.Attributes.GetInt("range", 0);
                    capi.ShowChatMessage($"Gadget Radius is now {range}");
                    BuildingGadgetItem buildingGadgetItem = itemSlot.Itemstack.Item as BuildingGadgetItem;
                    buildingGadgetItem.ClearHighlight(capi.World, capi.World.Player, capi.World.Player.InventoryManager.ActiveHotbarSlot, true);
                }
            }            
        }

        private void Event_ClientAfterActiveSlotChanged(ActiveSlotChangeEventArgs slotevenargs)
        {
            int fromslot = slotevenargs.FromSlot;
            int toslot = slotevenargs.ToSlot;
            IPlayer player = capi.World.Player;
            if (player == null) return;
            IInventory playerinventory = player.InventoryManager.GetHotbarInventory(); //.GetInventory("hotbar");
            if (playerinventory == null) return;
            ItemStack fromstack = playerinventory[fromslot].Itemstack;
            if (fromstack != null && fromstack.Item != null)
            {
                if (fromstack.Item.FirstCodePart() == "buildinggadget")
                {
                    player.WorldData.PickingRange = GlobalConstants.DefaultPickingRange;
                    BuildingGadgetItem bgitem = fromstack.Item as BuildingGadgetItem;
                    if (bgitem != null) bgitem.ClearHighlight(capi.World, capi.World.Player, null, true);
                }
            }
            ItemStack tostack = playerinventory[toslot].Itemstack;
            if (tostack == null || tostack.Item == null) return; // to slot is either empty or not an item.
            if (tostack.Item.FirstCodePart() == "buildinggadget")
            {
                ((BuildingGadgetItem)tostack.Item).DoCheckBuildBlock(playerinventory[toslot]);
                if (tostack.Attributes.GetInt("toolFunction", 1) == 0)
                {
                    // item going TO is a gadget AND it's function is set to BuildToMe                                        
                    player.WorldData.PickingRange = 64;                    
                    return;
                }
                else
                {
                    player.WorldData.PickingRange = 16;
                    return;
                }                
            }
        }



        private void RegisterGadgetKeys()
        {
            base.Mod.Logger.VerboseDebug("Building Gadget: Registering Keys");
            this.capi.Input.RegisterHotKey("bgrangeinc", "Gadget Range Increase", GlKeys.BracketRight, HotkeyType.CharacterControls);
            this.capi.Input.SetHotKeyHandler("bgrangeinc", OnRangeIncrease);

            this.capi.Input.RegisterHotKey("bgrangedec", "Gadget Range Decrease", GlKeys.BracketLeft, HotkeyType.CharacterControls);
            this.capi.Input.SetHotKeyHandler("bgrangedec", OnRangeDecrease);            
        }

        /// <summary>
        /// Need to directly update the server as the event is only fired on the client.
        /// </summary>
        /// <param name="comb"></param>
        /// <returns></returns>
        private bool OnRangeIncrease(KeyCombination comb)
        {
            if (CheckActiveHotbarItem())
            {
                RangeSync rangeSync = new RangeSync()
                {
                    increase = true
                };
                clientChannel.SendPacket<RangeSync>(rangeSync);                
                return true;
            }
            return false;
        }
        /// <summary>
        /// Need to directly update the server as the event is only fired on the client.
        /// </summary>
        /// <param name="comb"></param>
        /// <returns></returns>
        private bool OnRangeDecrease(KeyCombination comb)
        {
            if (CheckActiveHotbarItem())
            {
                RangeSync rangeSync = new RangeSync()
                {
                    increase = false
                };
                clientChannel.SendPacket<RangeSync>(rangeSync);
                return true;
            }
            return false;
        }

        private bool CheckActiveHotbarItem()
        {
            if (capi != null && capi.World.Player != null)
            {
                if (capi.World.Player.InventoryManager.ActiveHotbarSlot != null)
                {
                    if (capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack != null)
                    {
                        if (capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack.Item != null)
                        {
                            if (capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack.Item.FirstCodePart().Contains("buildinggadget"))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
