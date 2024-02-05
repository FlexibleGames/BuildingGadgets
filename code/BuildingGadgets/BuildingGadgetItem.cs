using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Cairo;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using Vintagestory;

namespace BuildingGadgets
{
    public class BuildingGadgetItem : Item
    {

        /// <summary>
        /// Mod Item to access Config Values.
        /// </summary>
        public BuildingGadgetMod BGMod
        {
            get
            {
                return this.api.ModLoader.GetModSystem<BuildingGadgetMod>();
            }
            set
            {
                BGMod = value;
            }
        }

        /// <summary>
        /// All Tool Modes; 11 items
        /// </summary>
        public SkillItem[] toolModes;
        
        ICoreServerAPI sapi;
        ICoreClientAPI capi;

        /// <summary>
        /// Build, Destroy, Exchange; 0-2
        /// </summary>
        public int cur_toolMode = 0;
        /// <summary>
        /// ToMe, HLine, VLine, Area, Volume; 3-7
        /// </summary>
        public int cur_funcMode = 1;
        /// <summary>
        /// OnTop, Inside, Under; 8-10
        /// </summary>
        public int cur_placeMode = 0;
        /// <summary>
        /// Current Block to build with
        /// </summary>
        public Block block_build;
        /// <summary>
        /// Attempt to build with chisel blocks... No guarantees here...
        /// </summary>
        public BlockEntityChisel block_chisel_build;
        /// <summary>
        /// What block (Pos) of storage to pull from, if any. Can be null;
        /// </summary>
        public BlockPos block_Storage;
        /// <summary>
        /// Anchors the build highlight 
        /// </summary>
        public BlockPos gadget_Anchor;
        /// <summary>
        /// Current BlockPos selected (used for rendering highlight)
        /// </summary>
        public BlockPos cur_blockSelection;
        /// <summary>
        /// What Block Face were we looking at.
        /// </summary>
        public BlockFacing cur_blockFaceSelection;
        /// <summary>
        /// What position the player was last at. For highlighting Build2Me
        /// </summary>
        public BlockPos cur_playerPosition;
        /// <summary>
        /// Range or Radius of block placement
        /// Not used for Build ToMe
        /// </summary>
        public int cur_range = 0;
        /// <summary>
        /// Item ID of Construction Paste
        /// </summary>
        public int pasteID = 0;

        /// <summary>
        /// A (hopefully) unique id for my high lighting
        /// </summary>
        private int highlightID = 79;
        /// <summary>
        /// Current Highlighted blocks
        /// </summary>
        private List<BlockPos> highlightedBlocks;
        /// <summary>
        /// Loading the item, client side generates toolModes
        /// </summary>
        /// <param name="api"></param>
        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            base.OnLoaded(api);
            pasteID = api.World.GetItem(new AssetLocation("buildinggadgets:constpaste")).Id;
            capi = api as ICoreClientAPI;
            if (capi != null)
            {
                // we're client side
                this.toolModes = ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "bgToolModes", () => new SkillItem[]
                {
                    new SkillItem
                    {
                        Code = new AssetLocation("build"),
                        Name = "Build",                        
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/build.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("destroy"),
                        Name = "Destroy (Voids Blocks)"
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/destroy.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("exchange"),
                        Name = "Exchange",                        
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/exchange.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("tome"),
                        Name = "To Me",
                        Linebreak = true
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/build2me.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("hline"),
                        Name = "Horizontal Line"
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/buildhline.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("vline"),
                        Name = "Vertical Line"
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/buildvline.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("area"),
                        Name = "Area"
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/buildarea.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("volume"),
                        Name = "Volume"                        
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/buildvolume.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("ontop"),
                        Name = "Build On Top",
                        Linebreak = true
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/buildontop.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("inside"),
                        Name = "Build Inside"
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/buildinside.svg"), 48, 48, 5, new int?(-1))),
                    new SkillItem
                    {
                        Code = new AssetLocation("under"),
                        Name = "Build Under"
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("buildinggadgets:textures/icon/buildunder.svg"), 48, 48, 5, new int?(-1)))
                });
                for (int i = 0; i < toolModes.Length; i++) { toolModes[i].TexturePremultipliedAlpha = false; }                
            }

            if (api.Side == EnumAppSide.Server) sapi = api as ICoreServerAPI;
        }

        /// <summary>
        /// Dispose of skill items, cleans up memory
        /// </summary>
        /// <param name="api"></param>
        public override void OnUnloaded(ICoreAPI api)
        {
            int num = 0;
            while (this.toolModes != null && num < this.toolModes.Length)
            {
                SkillItem skillItem = this.toolModes[num];
                if (skillItem != null) skillItem.Dispose();

                num++;
            }
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return this.toolModes;
        }

        /// <summary>
        /// Called from the Mod OnKey events to signal a change in range.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns>True if range was altered</returns>
        public bool UpdateRange(ItemSlot slot, bool increase, IServerPlayer serverPlayer)
        {
            if (slot != null)
            {
                int prevRange = slot.Itemstack.Attributes.GetInt("range", 0);
                int curRange = prevRange;
                if (increase)
                {
                    curRange++;
                }
                else curRange--;

                if (curRange > BGMod.BGConfig.rangeMax) curRange = BGMod.BGConfig.rangeMax; // clamp the value
                if (curRange < 0) curRange = 0; // no negative ranges
                slot.Itemstack.Attributes.SetInt("range", curRange);
                slot.MarkDirty();
                this.cur_range = curRange;
                if (prevRange != curRange)
                {
                    serverPlayer.BroadcastPlayerData(true);
                    return true;                    
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Gets the Tool Mode 0-2; Build, Destroy, Exchange
        /// </summary>
        /// <param name="slot">Slot Selected</param>
        /// <param name="byPlayer">The Player</param>
        /// <param name="blockSelection">Block Player is Aiming At</param>
        /// <returns>int 0-2 or 0 if missing</returns>
        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
        {
            int mode = slot.Itemstack.Attributes.GetInt("toolMode", -1);
            if (mode == -1)
            {
                mode = 0;
                slot.Itemstack.Attributes.SetInt("toolMode", mode);
            }
            return mode;
        }

        /// <summary>
        /// Gets the Tool Function Mode 0-4; ToMe, HLine, VLine, Area, Volume
        /// </summary>
        /// <param name="slot">Slot Selected</param>
        /// <param name="byPlayer">The Player</param>
        /// <returns>int 0-4 or -1 if missing</returns>
        public int GetToolFunction(ItemSlot slot, IPlayer byPlayer)
        {
            return slot.Itemstack.Attributes.GetInt("toolFunction", -1);
        }

        /// <summary>
        /// Gets the Tool Placement Mode 0-2; OnTop, Inside, Under
        /// </summary>
        /// <param name="slot">Slot Selected</param>
        /// <param name="byPlayer">The Player</param>
        /// <returns>int 0-2 or -1 if missing</returns>
        public int GetToolPlaceMode(ItemSlot slot, IPlayer byPlayer)
        {
            return slot.Itemstack.Attributes.GetInt("toolPlacement", -1);
        }

        /// <summary>
        /// Gets the range (radius) of the tool, ignored for Build2Me mode
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byPlayer"></param>
        /// <returns>Range (Radius) of tool or -1 if missing</returns>
        public int GetToolRange(ItemSlot slot, IPlayer byPlayer)
        {
            return slot.Itemstack.Attributes.GetInt("range", -1);
        }

        /// <summary>
        /// Returns the Block ID set when selecting a block to build with. Saved in the
        /// item attributes.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byPlayer"></param>
        /// <returns>BlockID or -1 if missing</returns>
        public int GetToolBuildBlock(ItemSlot slot, IPlayer byPlayer)
        {
            return slot.Itemstack.Attributes.GetInt("blockid", -1);
        }

        /// <summary>
        /// Gets the saved Anchor set by Sneak+Left Clicking a block
        /// If not set, returns int.MinValue for X,Y, and Z (roughly -2 billion)
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byPlayer"></param>
        /// <returns>BlockPos</returns>
        public BlockPos GetAnchor(ItemSlot slot, IPlayer byPlayer)
        {
            return new BlockPos(
                slot.Itemstack.Attributes.GetInt("anchoredx", int.MinValue),
                slot.Itemstack.Attributes.GetInt("anchoredy", int.MinValue),
                slot.Itemstack.Attributes.GetInt("anchoredz", int.MinValue));
        }

        /// <summary>
        /// Gets the linked Storage block for the gadget. Will pull blocks or paste from here.
        /// If not set, returns int.MinValue for X, Y, and Z
        /// </summary>
        /// <param name="slot"></param>
        /// <returns>BlockPos </returns>
        public BlockPos GetStorageBlock(ItemSlot slot)
        {
            return new BlockPos(
                slot.Itemstack.Attributes.GetInt("storagex", int.MinValue),
                slot.Itemstack.Attributes.GetInt("storagey", int.MinValue),
                slot.Itemstack.Attributes.GetInt("storagez", int.MinValue));
        }

        /// <summary>
        /// A call to alert the player to select a block to build with.
        /// Typically called outside of the Item object (by the ModSystem)
        /// </summary>
        /// <param name="slot"></param>
        public void DoCheckBuildBlock(ItemSlot slot)
        {
            if (GetToolBuildBlock(slot, null) < 1)
            {
                if (capi != null) capi.ShowChatMessage("Please Sneak+Right Click to select a block to build with.");
            }
        }

        /// <summary>
        /// Check the players inventory and hotbar for Construction Paste
        /// Will also check the linked Inventory block if it exists.
        /// </summary>
        /// <param name="player">IPlayer to check</param>
        /// <returns>Total Paste in Player Inventory or is linked to</returns>
        public int PlayerHasPaste(IPlayer player, ItemSlot gadget)
        {
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative) return int.MaxValue; // Creative costs nothing.
            int totalPaste = 0;
            IInventory hotbar = player.InventoryManager.GetHotbarInventory();
            IInventory inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName); 
            if (hotbar != null && !hotbar.Empty)
            {
                foreach (ItemSlot itemSlot in hotbar)
                {
                    if (itemSlot.Empty || itemSlot.Itemstack.Item == null) continue; // must be an item
                    if (itemSlot.Itemstack.Id == pasteID)
                    {
                        totalPaste += itemSlot.Itemstack.StackSize;
                    }
                }
            }
            if (inventory != null && !inventory.Empty)
            {
                foreach (ItemSlot itemSlot in inventory)
                {
                    if (itemSlot.Empty || itemSlot.Itemstack.Item == null) continue; // must be an item
                    if (itemSlot.Itemstack.Id == pasteID)
                    {
                        totalPaste += itemSlot.Itemstack.StackSize;
                    }
                }
            }
            BlockPos storage = GetStorageBlock(gadget);
            if (storage.X != int.MinValue)
            {
                // storage block is set and valid... well maybe valid...
                BlockEntity storageEntity = player.Entity.World.BlockAccessor.GetBlockEntity(storage);
                if (storageEntity != null && storageEntity is IBlockEntityContainer)
                {
                    // we're bound to a storage of some kind... 
                    foreach (ItemSlot itemSlot in ((IBlockEntityContainer)storageEntity).Inventory)
                    {
                        if (!itemSlot.Empty && itemSlot.Itemstack.Item != null)
                        {
                            // slot exists and has an item...
                            if (itemSlot.Itemstack.Id == pasteID)
                            {
                                totalPaste += itemSlot.Itemstack.StackSize;
                            }
                        }
                    }
                }
            }
            return totalPaste;
        }

        /// <summary>
        /// Checks the players inventory for How many blocks they have of the Building Block
        /// Will also check the linked Inventory block if it exists.
        /// </summary>
        /// <param name="player">Player to Check</param>
        /// <returns>Total number of blocks of type player holds or is linked to</returns>
        public int PlayerHasBlocks(IPlayer player, ItemSlot gadget)
        {
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative) return int.MaxValue; // Creative costs nothing.
            int totalBlocks = 0;
            IInventory hotbar = player.InventoryManager.GetHotbarInventory();
            IInventory inventory = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            if (hotbar != null && !hotbar.Empty)
            {
                foreach (ItemSlot itemSlot in hotbar)
                {
                    if (itemSlot.Empty || itemSlot.Itemstack.Block == null) continue; // must be a block
                    if (itemSlot.Itemstack.Id == block_build.Id)
                    {
                        totalBlocks += itemSlot.Itemstack.StackSize;
                    }
                }
            }
            if (inventory != null && !inventory.Empty)
            {
                foreach (ItemSlot itemSlot in inventory)
                {
                    if (itemSlot.Empty || itemSlot.Itemstack.Block == null) continue; // must be a block
                    if (itemSlot.Itemstack.Id == block_build.Id)
                    {
                        totalBlocks += itemSlot.Itemstack.StackSize;
                    }
                }
            }
            BlockPos storage = GetStorageBlock(gadget);
            if (storage.X != int.MinValue)
            {
                // storage block is set and valid... well maybe valid...
                BlockEntity storageEntity = player.Entity.World.BlockAccessor.GetBlockEntity(storage);
                if (storageEntity != null && storageEntity is IBlockEntityContainer)
                {
                    // we're bound to a storage of some kind... 
                    foreach (ItemSlot itemSlot in ((IBlockEntityContainer)storageEntity).Inventory)
                    {
                        if (itemSlot != null && itemSlot.Itemstack != null && itemSlot.Itemstack.Block != null)
                        {
                            // slot exists and has an item...
                            if (itemSlot.Itemstack.Id == block_build.Id)
                            {
                                totalBlocks += itemSlot.Itemstack.StackSize;
                            }
                        }
                    }
                }
            }
            return totalBlocks;
        }

        /// <summary>
        /// Sets the Tool mode from GUI dialog
        /// </summary>
        /// <param name="slot">Slot Selected</param>
        /// <param name="byPlayer">The Player</param>
        /// <param name="blockSelection">Block Aimed At</param>
        /// <param name="toolMode">Tool Mode selected</param>
        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            // make sure these are set to default values
            //if (GetToolMode(slot, byPlayer, null) < 0) slot.Itemstack.Attributes.SetInt("toolMode", 0);
            if (GetToolFunction(slot, byPlayer) < 0) slot.Itemstack.Attributes.SetInt("toolFunction", 1);
            if (GetToolPlaceMode(slot, byPlayer) < 0) slot.Itemstack.Attributes.SetInt("toolPlacement", 0);
            if (GetToolRange(slot, byPlayer) < 0) slot.Itemstack.Attributes.SetInt("range", 0);

            // Tool mode 0-2 = cur_ToolMode
            if (toolMode <= 2)
            {
                this.cur_toolMode = toolMode;
                slot.Itemstack.Attributes.SetInt("toolMode", cur_toolMode);
            }
            // tool mode 3-7 = cur_funcMode 0-4
            else if (toolMode > 2 && toolMode <= 7)
            {
                this.cur_funcMode = toolMode - 3;
                slot.Itemstack.Attributes.SetInt("toolFunction", cur_funcMode);
                // Build To Me check
                if (cur_funcMode == 0)
                {
                    // need to set player picking radius to 64...                    
                    byPlayer.WorldData.PickingRange = 64;                    
                }
                else
                {
                    byPlayer.WorldData.PickingRange = 16;
                }
            }
            // tool mode 8-10 = cur_placeMode 0-2
            else if (toolMode > 7)
            {
                this.cur_placeMode = toolMode - 8;
                slot.Itemstack.Attributes.SetInt("toolPlacement", cur_placeMode);
            }
            ClearAnchor(slot);
            ClearHighlight(byPlayer.Entity.World, byPlayer, slot, true); // clear the highlight so it can be redrawn with new settings
        }

        /// <summary>
        /// Set whether to display durability based on Config value
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public override bool ShouldDisplayItemDamage(ItemStack itemstack)
        {
            return BGMod.BGConfig.useDurability;
        }

        /// <summary>
        /// Custom Damage Item, do not void the item when durability = 0;
        /// Ignored if mod config value useDurability = false
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byEntity"></param>
        /// <param name="itemslot"></param>
        /// <param name="amount">Amount to damage</param>
        public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
        {
            if (ShouldDisplayItemDamage(itemslot.Itemstack))
            {
                if (((EntityPlayer)byEntity).Player.WorldData.CurrentGameMode == EnumGameMode.Creative) return; // Creative costs nothing.
                ItemStack itemStack = itemslot.Itemstack;
                int num = itemStack.Attributes.GetInt("durability", this.GetMaxDurability(itemStack));
                num -= amount;                
                
                if (num <= 0)
                {
                    // item is broken and needs repairs
                    num = 0;
                    EntityPlayer entityPlayer = byEntity as EntityPlayer;
                    IPlayer player = entityPlayer?.Player;                    
                    if (player != null)
                    {                        
                        world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player, null, true, 32f, 1f);
                    }
                }
                itemStack.Attributes.SetInt("durability", num);
            }            
        }

        /// <summary>
        /// Called when the player Right-Clicks with the gadget
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="firstEvent"></param>
        /// <param name="handling"></param>
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            /*if (!firstEvent)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }*/
            if (!(byEntity is EntityPlayer player)) return; // a non-player is trying to use the gadget? O,o
            if (byEntity.Controls.Sneak)
            {
                // player was sneaking when they right clicked...
                CheckUpdateBlockOrStorage(slot, byEntity, blockSel, entitySel);
            }
            else
            {
                // player is trying to use the gadget!! \o/
                
                int blockid = GetToolBuildBlock(slot, player.Player); // lets see if we have one saved
                if (blockid > 0) block_build = player.World.Blocks[blockid]; // grab it if it is.
                if (block_build != null && block_build.Code != null)
                {
                    if (highlightedBlocks != null && highlightedBlocks.Count > 0)
                    {
                        // we have a build code, lets build!
                        int toolMode = GetToolMode(slot, player.Player, null);

                        if (toolMode == 0) // building
                        {
                            int playerpaste = PlayerHasPaste(player.Player, slot);
                            int pastecost = GetPasteBuildCost(block_build);
                            int totalpastecost = 0;
                            int playerblocks = 0;
                            if (BGMod.BGConfig.usePaste)
                            {
                                totalpastecost = (pastecost * highlightedBlocks.Count);
                                if (playerpaste < totalpastecost)
                                {
                                    if (capi != null)
                                    {
                                        capi.ShowChatMessage("You need more Construction Paste. Try linking a storage! (Shift+Right Click a chest)");
                                    }
                                    return;
                                }
                            }
                            else
                            {
                                // they do NOT want to use paste :(
                                playerblocks = PlayerHasBlocks(player.Player, slot);
                                if (playerblocks < highlightedBlocks.Count)
                                {
                                    if (capi != null) capi.ShowChatMessage("You Need More Blocks to build with. Try linking a storage! (Shift+Right Click a chest)");
                                    return;
                                }
                            }

                            int actualpastecost = 0; // while it says paste, could also be block cost.
                            foreach (BlockPos blockPos in highlightedBlocks)
                            {
                                bool blockoverwrite = player.World.BlockAccessor.GetBlock(blockPos, BlockLayersAccess.SolidBlocks).IsReplacableBy(block_build);
                                
                                if (player.World.BlockAccessor.GetBlock(blockPos, BlockLayersAccess.SolidBlocks).Id == 0 || blockoverwrite) // ONLY air blocks or grass-like blocks
                                {
                                    actualpastecost += pastecost;                                    
                                    player.World.BlockAccessor.SetBlock(block_build.Id, blockPos);
                                    player.World.BlockAccessor.MarkBlockDirty(blockPos);
                                    api.World.BlockAccessor.TriggerNeighbourBlockUpdate(blockPos);
                                }
                            }
                            if (BGMod.BGConfig.usePaste) ConsumePasteOrBlocks(player.Player, slot, actualpastecost); // amount of paste
                            else ConsumePasteOrBlocks(player.Player, slot, actualpastecost / pastecost); // number of blocks

                            if (BGMod.BGConfig.useDurability) DamageItem(player.World, player, slot, actualpastecost / pastecost);                                                      
                        }
                        else if (toolMode == 1) // destroy; a very dangerous mode..
                        {
                            // no paste used, voids blocks and uses durability, ignores blocks with block entities
                            int blocksset = 0;

                            int durleft = slot.Itemstack.Attributes.GetInt("durability", this.GetMaxDurability(slot.Itemstack));
                            if (durleft == 0 && BGMod.BGConfig.useDurability)
                            {
                                if (capi != null) capi.ShowChatMessage($"Gadget needs repairing/recharging.");
                                return;
                            }

                            foreach (BlockPos blockPos in highlightedBlocks)
                            {
                                if (player.World.BlockAccessor.GetBlock(blockPos).Id != 0) // block at Pos isn't air
                                {                                    
                                    if (player.World.BlockAccessor.GetBlockEntity(blockPos) == null &&
                                        blocksset < durleft)
                                    {
                                        // ignores blocks with an entity for now, unless someone wants to do this
                                        blocksset++;
                                        player.World.BlockAccessor.SetBlock(0, blockPos);
                                        player.World.BlockAccessor.MarkBlockDirty(blockPos);
                                        api.World.BlockAccessor.TriggerNeighbourBlockUpdate(blockPos);
                                    }
                                }
                            }
                            if (BGMod.BGConfig.useDurability) DamageItem(player.World, player, slot, blocksset);                                                  
                        }
                        else if (toolMode == 2) // exchange, actually gives the drop back to the player (only in survival mode)
                        {
                            int blocksset = 0;
                            foreach (BlockPos blockPos in highlightedBlocks)
                            {
                                blocksset++;
                                ItemStack[] drops = player.World.BlockAccessor.GetBlock(blockPos).GetDrops(player.World, blockPos, player.Player, 1f);
                                api.World.BlockAccessor.SetBlock(block_build.Id, blockPos);
                                api.World.BlockAccessor.MarkBlockDirty(blockPos);
                                api.World.BlockAccessor.TriggerNeighbourBlockUpdate(blockPos);
                                if (drops != null && drops.Length > 0 && player.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
                                {
                                    foreach (ItemStack itemStack in drops)
                                    {
                                        if (!player.Player.InventoryManager.TryGiveItemstack(itemStack, true))
                                        {
                                            player.World.SpawnItemEntity(itemStack, player.Pos.AsBlockPos.ToVec3d());
                                        }
                                    }
                                }                                                               
                            }
                            if (BGMod.BGConfig.useDurability) DamageItem(player.World, player, slot, blocksset);
                        }
                        ClearAnchor(slot);
                        ClearHighlight(player.World, player.Player, slot, true);                        
                    }
                }
                else
                {
                    capi.ShowChatMessage("Please Sneak+Right Click a block to set what block to build with.");
                }
            }
            handling = EnumHandHandling.PreventDefaultAction;
        }

        /// <summary>
        /// When Shift+Left click is pressed, set the anchor for the gadget.
        /// Allows players to walk around before commiting.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="handling"></param>
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (byEntity.Controls.Sneak)
            {
                if (blockSel != null)
                {
                    bool dirty = false;
                    // player was sneaking and left clicked (set anchor)                    
                    if (GetAnchor(slot, (byEntity as EntityPlayer).Player).X != int.MinValue)
                    {
                        // they have an anchor set, but they set a new anchor, we have to redraw the highlight.
                        if (blockSel.Position != GetAnchor(slot, (byEntity as EntityPlayer).Player))
                        {
                            // but only redraw if the old anchor and new anchor are different.                            
                            ClearHighlight(byEntity.World, (byEntity as EntityPlayer).Player, slot, true);
                            cur_blockFaceSelection = blockSel.Face;
                            cur_blockSelection = blockSel.Position.Copy();
                            cur_playerPosition = byEntity.Pos.AsBlockPos.Copy();
                            dirty = true;
                        }
                    }
                    switch (GetToolPlaceMode(slot, null))
                    {
                        case 0: this.gadget_Anchor = blockSel.Position.AddCopy(blockSel.Face, 1); break;
                        case 1: this.gadget_Anchor = blockSel.Position.Copy(); break;
                        case 2: this.gadget_Anchor = blockSel.Position.AddCopy(blockSel.Face.Opposite, 1); break;
                        case -1: this.gadget_Anchor = blockSel.Position.Copy(); break;
                    }

                    slot.Itemstack.Attributes.SetInt("anchoredx", gadget_Anchor.X);
                    slot.Itemstack.Attributes.SetInt("anchoredy", gadget_Anchor.Y);
                    slot.Itemstack.Attributes.SetInt("anchoredz", gadget_Anchor.Z);

                    BlockPos tempPos = blockSel.Position.Copy();
                    tempPos.Sub(api.World.DefaultSpawnPosition.AsBlockPos);
                    tempPos.Y = gadget_Anchor.Y;
                    if (dirty)
                    {
                        HighlightToolBlocks(slot, (byEntity as EntityPlayer), gadget_Anchor, byEntity.Pos.AsBlockPos, blockSel.Face);
                    }
                    if (capi != null) capi.ShowChatMessage($"Anchored to {tempPos}");
                }
                else
                {
                    // sneak left clicking in the air clears the anchor
                    ClearAnchor(slot);
                    if (capi != null) capi.ShowChatMessage("Gadget Anchor Removed");
                }
                handling = EnumHandHandling.PreventDefaultAction;
            }
            else
            {
                base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
            }
        }

        /// <summary>
        /// Clears the Anchor... Obvious?
        /// </summary>
        /// <param name="slot">Gadget</param>
        public void ClearAnchor(ItemSlot slot)
        {            
            if (gadget_Anchor != null || GetAnchor(slot, null).X != int.MinValue)
            {
                this.gadget_Anchor = null;
                slot.Itemstack.Attributes.RemoveAttribute("anchoredx");
                slot.Itemstack.Attributes.RemoveAttribute("anchoredy");
                slot.Itemstack.Attributes.RemoveAttribute("anchoredz");                
            }
        }

        /// <summary>
        /// Called every frame while holding this item.
        /// Needs a LOT of code
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {            
            EntityPlayer entityPlayer = byEntity as EntityPlayer;
            if (entityPlayer != null)
            {
                BlockPos curAnchor = GetAnchor(slot, entityPlayer.Player);
                BlockPos blockSelection = null;
                BlockPos playerPosition = null;
                bool dirty = false; // if dirty == true, we need to redraw the highlight
                // if they haven't selected a block to build with yet, default to crappy soil. Used in IsReplacableBy
                if (block_build == null)
                {
                    int buildID = GetToolBuildBlock(slot, entityPlayer.Player);
                    if (buildID > 0)
                    {
                        block_build = entityPlayer.World.GetBlock(buildID);
                    }
                    else
                    {
                        block_build = entityPlayer.World.GetBlock(new AssetLocation("game:soil-low-none"));
                    }
                }
                if (curAnchor.X != int.MinValue)
                {
                    // anchor is set, let the player move around
                    // Build2Me should still update based on player position, other functions will not
                    if (entityPlayer.Pos.AsBlockPos != cur_playerPosition)
                    {
                        if (GetToolFunction(slot, entityPlayer.Player) == 0) // Build2Me?
                        {
                            if (highlightedBlocks != null && highlightedBlocks.Count > 0)
                            {
                                ClearHighlight(entityPlayer.World, entityPlayer.Player, slot, false);
                            }
                            // we have an anchor, lets restore the block selection
                            if (cur_blockSelection == null) cur_blockSelection = GetAnchor(slot, entityPlayer.Player);
                            if (cur_blockFaceSelection == null)
                            {
                                // if this is null, take the opposite face from where the player is looking
                                cur_blockFaceSelection = BlockFacing.HorizontalFromAngle(GameMath.Mod(entityPlayer.Pos.Yaw, 6.28318548f)).Opposite;
                            }                            
                            blockSelection = cur_blockSelection; // Keep selection
                            // change position
                            cur_playerPosition = entityPlayer.Pos.AsBlockPos.Copy();
                            playerPosition = cur_playerPosition.AddCopy(cur_blockFaceSelection.Opposite);
                            dirty = true;
                        }
                    }
                }
                else // no anchor set
                {
                    // anchor is not set, let the selection change
                    if (entityPlayer.BlockSelection == null)
                    {
                        if (highlightedBlocks != null && highlightedBlocks.Count > 0)
                        {
                            ClearHighlight(entityPlayer.World, entityPlayer.Player, slot, true);
                        }
                        return;
                    }
                    if (this.cur_blockSelection != entityPlayer.BlockSelection.Position || this.cur_blockFaceSelection != entityPlayer.BlockSelection.Face || entityPlayer.Pos.AsBlockPos != cur_playerPosition)
                    {
                        cur_blockSelection = entityPlayer.BlockSelection.Position.Copy();
                        cur_blockFaceSelection = entityPlayer.BlockSelection.Face;
                        cur_playerPosition = entityPlayer.Pos.AsBlockPos.Copy();
                        int toolMode = GetToolMode(slot, entityPlayer.Player, null);
                        playerPosition = entityPlayer.Pos.AsBlockPos.AddCopy(cur_blockFaceSelection.Opposite);
                        // Normalize block selection based on face being aimed at. According to PlaceMode
                        // First is 'on top' as in toward the player.
                        if (GetToolPlaceMode(slot, entityPlayer.Player) == 0 && toolMode == 0) // only build mode can use placement mode
                        {
                            if (entityPlayer.World.BlockAccessor.GetBlock(cur_blockSelection, BlockLayersAccess.SolidBlocks).IsReplacableBy(block_build))
                                blockSelection = cur_blockSelection.Copy();
                            else
                                blockSelection = cur_blockSelection.AddCopy(entityPlayer.BlockSelection.Face);
                        }
                        // Next is 'under' as in away from the player
                        else if (GetToolPlaceMode(slot, entityPlayer.Player) == 2 && toolMode == 0) // only build mode can use placement mode
                        {
                            if (entityPlayer.World.BlockAccessor.GetBlock(cur_blockSelection, BlockLayersAccess.SolidBlocks).IsReplacableBy(block_build))
                                blockSelection = cur_blockSelection.Copy();
                            else
                                blockSelection = cur_blockSelection.AddCopy(entityPlayer.BlockSelection.Face.Opposite);
                        }
                        else
                        {
                            // inside doesn't alter the coords at all. Exchange mode ONLY uses this option.
                            blockSelection = cur_blockSelection.Copy();
                        }
                        dirty = true;
                    }
                }
                if (blockSelection != null && playerPosition != null && dirty)
                {
                    HighlightToolBlocks(slot, entityPlayer, blockSelection, playerPosition, cur_blockFaceSelection);
                }
            }
        }

        /// <summary>
        /// Highlight the blocks the tool will cover for the given player. 
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="player"></param>
        /// <param name="blocks"></param>
        /// <param name="selectedFace"></param>
        public void HighlightToolBlocks(ItemSlot slot, EntityPlayer player, BlockPos blockSel, BlockPos playerPos, BlockFacing selectedFace)
        {            
            int toolMode = GetToolMode(slot, player.Player, null);
            int toolFunc = GetToolFunction(slot, player.Player);
            int toolPlac = GetToolPlaceMode(slot, player.Player);
            int toolrange = GetToolRange(slot, player.Player);
            if (toolrange == -1) toolrange = 0; // if no range is saved, default of 0
            BlockPos toolAnchor = GetAnchor(slot, player.Player); // can be used for everything
            BlockPos startPoint = null; // start of highlight
            BlockPos endPoint = null;   // end of highlight
            // Tool Mode ... Largely ignored as all modes affect in the same way.
            // Function Mode ... Build2Me ignores Range ... Others based on player view direction :(
            // Placement mode ... taken care of in OnHeldIdle

            if (toolFunc <= 0) // selection for Build2Me specifically, the only one that uses playerPos
            {
                // The start is obvious, the selected block
                if (toolAnchor.X != int.MinValue) startPoint = toolAnchor.Copy();
                else startPoint = blockSel.Copy();
                // the end, however isn't obvious...                 
                switch (selectedFace.Code)
                {
                    case "north": endPoint = blockSel.NorthCopy(Math.Abs(blockSel.Z - playerPos.Z)); break;
                    case "east": endPoint = blockSel.EastCopy(Math.Abs(blockSel.X - playerPos.X)); break;
                    case "south": endPoint = blockSel.SouthCopy(Math.Abs(blockSel.Z - playerPos.Z)); break;
                    case "west": endPoint = blockSel.WestCopy(Math.Abs(blockSel.X - playerPos.X)); break;
                    case "up": endPoint = blockSel.UpCopy(Math.Abs(blockSel.Y - playerPos.Y)); break;
                    case "down": endPoint = blockSel.DownCopy(Math.Abs(blockSel.Y - playerPos.Y)); break;
                    default: endPoint = blockSel.AddCopy(BlockFacing.UP, 64); break;
                }
                if (blockSel.Y == playerPos.Y) endPoint.Y = blockSel.Y;
            }
            else
            {
                // other tool functions!
                if (toolrange == 0) startPoint = endPoint = blockSel;
                else
                {
                    string playerfacecode = BlockFacing.HorizontalFromAngle(GameMath.Mod(player.Pos.Yaw, 6.28318548f)).Code;
                    switch (toolFunc)
                    {
                        case 1: // Horizontal Line
                            {
                                switch (selectedFace.Code)
                                {
                                    case "north":
                                    case "south":
                                        {
                                            startPoint = blockSel.EastCopy(toolrange);
                                            endPoint = blockSel.WestCopy(toolrange);
                                            break;
                                        }
                                    case "east":
                                    case "west":
                                        {
                                            startPoint = blockSel.NorthCopy(toolrange);
                                            endPoint = blockSel.SouthCopy(toolrange);
                                            break;
                                        }
                                    case "up":
                                    case "down":
                                        {
                                            if (playerfacecode == "north" || playerfacecode == "south")
                                            {
                                                startPoint = blockSel.EastCopy(toolrange);
                                                endPoint = blockSel.WestCopy(toolrange);
                                            }
                                            else
                                            {
                                                startPoint = blockSel.NorthCopy(toolrange);
                                                endPoint = blockSel.SouthCopy(toolrange);
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                        case 2: // Vertical Line
                            {
                                switch (selectedFace.Code)
                                {
                                    case "north":
                                    case "south":
                                        {
                                            startPoint = blockSel.UpCopy(toolrange);
                                            endPoint = blockSel.DownCopy(toolrange);
                                            break;
                                        }
                                    case "east":
                                    case "west":
                                        {
                                            startPoint = blockSel.UpCopy(toolrange);
                                            endPoint = blockSel.DownCopy(toolrange);
                                            break;
                                        }
                                    case "up":
                                        {
                                            startPoint = blockSel.Copy();
                                            endPoint = blockSel.UpCopy(toolrange * 2);
                                            break;
                                        }
                                    case "down":
                                        {
                                            startPoint = blockSel.Copy();
                                            endPoint = blockSel.DownCopy(toolrange * 2);
                                            break;
                                        }
                                }
                                break;
                            }
                        case 3: // Area - 2 dimensional area of blocks
                            {
                                switch (selectedFace.Code)
                                {
                                    case "north":
                                    case "south":
                                        {
                                            startPoint = blockSel.EastCopy(toolrange).Up(toolrange);
                                            endPoint = blockSel.WestCopy(toolrange).Down(toolrange);
                                            break;
                                        }
                                    case "east":
                                    case "west":
                                        {
                                            startPoint = blockSel.NorthCopy(toolrange).Up(toolrange);
                                            endPoint = blockSel.SouthCopy(toolrange).Down(toolrange);
                                            break;
                                        }
                                    case "up":
                                    case "down":
                                        {
                                            startPoint = blockSel.AddCopy(toolrange, 0, toolrange);
                                            endPoint = blockSel.AddCopy(-toolrange, 0, -toolrange);
                                            break;
                                        }
                                }
                                break;
                            }
                        case 4: // Volume ... Around the player? Or away from player? Using Anchor will be key
                            {
                                startPoint = blockSel.AddCopy(toolrange, toolrange, toolrange);
                                endPoint = blockSel.AddCopy(-toolrange, -toolrange, -toolrange);
                                break;
                            }
                    }
                }
            }

            List<int> usecolors = new List<int>
            {
                ColorUtil.ColorFromRgba(BGMod.BGConfig.buildColors) // Blue for build
            };
            // if in destroy mode, highlight goes RED
            if (toolMode == 1) usecolors[0] = ColorUtil.ColorFromRgba(BGMod.BGConfig.destroyColors); // Red for Destroy
            if (toolMode == 2) usecolors[0] = ColorUtil.ColorFromRgba(BGMod.BGConfig.exchangeColors); // Purple for Exchange

            // Build the Highlight and list of blocks for this selection
            if (startPoint != null && endPoint != null)
            {
                if (highlightedBlocks != null && highlightedBlocks.Count > 0)
                {
                    ClearHighlight(player.World, player.Player, slot, false); // clears the items in the array AND the highlight object in the world...
                }
                highlightedBlocks = GetToolBlocks(slot, startPoint, endPoint, selectedFace, blockSel);
                try
                {
                    api.World.HighlightBlocks(player.Player, highlightID, highlightedBlocks,
                            usecolors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);
                }
                catch (Exception e)
                {
                    if (capi != null) capi.ShowChatMessage($"Exception : {e}");
                }
            }
            
        }

        /// <summary>
        /// Clears the block highlight for a given player.
        /// Resets the gadget internals, called after build.
        /// </summary>
        /// <param name="world">IWorldAccessor</param>
        /// <param name="player">IPlayer</param>
        /// <param name="slot">Gadget</param>
        /// <param name="resetGadget">If true, resets stored BlockPos values</param>
        public void ClearHighlight(IWorldAccessor world, IPlayer player, ItemSlot slot, bool resetGadget)
        {            
            if (highlightedBlocks != null && highlightedBlocks.Count > 0)
            {
                highlightedBlocks.Clear();                
                if (resetGadget)
                {
                    cur_blockSelection = null;
                    cur_playerPosition = null;
                    cur_blockFaceSelection = null;
                }
            }
            api.World.HighlightBlocks(player, highlightID, new List<BlockPos>(), new List<int>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);
        }

        /// <summary>
        /// Gets a List of BlockPos to highlight/edit
        /// </summary>
        /// <param name="slot">Slot holding the gadget</param>
        /// <param name="startBlock">Start Block, typically the block right at the selected block.</param>
        /// <param name="endBlock">End Block, depends on toolMode</param>
        /// <param name="blockFacing">Block Face player is looking at</param>
        /// <param name="originalPos">Original Block Selected, before the offsets</param>
        /// <returns>List of valid matching Blocks for tool mode selections.</returns>
        public List<BlockPos> GetToolBlocks(ItemSlot slot, BlockPos startBlock, BlockPos endBlock, BlockFacing blockFacing, BlockPos originalPos)
        {
            int toolMode = GetToolMode(slot, null, null);

            if (startBlock.dimension != endBlock.dimension)
            {
                api.Logger.Debug($"BuildingGadget: GetToolBlocks crosses from dimension {startBlock.dimension} to {endBlock.dimension}");
            }
            int dim = startBlock.dimension;

            List<BlockPos> blocksForTool = new List<BlockPos>();                        
            BlockPos minBlock = new BlockPos(Math.Min(startBlock.X, endBlock.X), Math.Min(startBlock.Y, endBlock.Y),
                Math.Min(startBlock.Z, endBlock.Z), dim );
            BlockPos maxBlock = new BlockPos(Math.Max(startBlock.X, endBlock.X), Math.Max(startBlock.Y, endBlock.Y),
                Math.Max(startBlock.Z, endBlock.Z), dim );            

            if (toolMode != 2)
            {
                for (int x = minBlock.X; x <= maxBlock.X; x++)
                {
                    for (int y = minBlock.Y; y <= maxBlock.Y; y++)
                    {
                        for (int z = minBlock.Z; z <= maxBlock.Z; z++)
                        {
                            blocksForTool.Add(new BlockPos(x, y, z, dim));
                        }
                    }
                }
            }
            else
            {
                Block originalBlock = api.World.BlockAccessor.GetBlock(originalPos);
                for (int x = minBlock.X; x <= maxBlock.X; x++)
                {
                    for (int y = minBlock.Y; y <= maxBlock.Y; y++)
                    {
                        for (int z = minBlock.Z; z <= maxBlock.Z; z++)
                        {
                            Block blockCheck = api.World.BlockAccessor.GetBlock(new BlockPos(x, y, z, dim));
                            if (blockCheck.Id == 0) continue; // skip air
                            // ignore blocks with an entity
                            if (api.World.BlockAccessor.GetBlockEntity(new BlockPos(x, y, z, dim)) == null) 
                            {
                                if (blockCheck.FirstCodePart() == block_build.FirstCodePart())
                                {
                                    // they are both soil, or rock, or whatever                                
                                    if (blockCheck.CodeWithoutParts(1) != block_build.CodeWithoutParts(1))
                                    {
                                        //checked the code without the end part to see if they are the same.
                                        // if they are NOT the same, add to list. 
                                        // IOW soil-medium-none != soil-medium-grassy
                                        blocksForTool.Add(new BlockPos(x, y, z, dim));
                                    }
                                }
                                else
                                {
                                    blocksForTool.Add(new BlockPos(x, y, z, dim));
                                }
                            }
                        }
                    }
                }
            }
            return blocksForTool;
        }

        /// <summary>
        /// Called when Shift-Clicking a block to either update the block selected or set the tool to pull items from a chest.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <returns>True if CurBlock or Storage was changed.</returns>
        public bool CheckUpdateBlockOrStorage(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel != null)
            {
                Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
                
                if (block == null) return false;
                EntityPlayer player = byEntity as EntityPlayer;
                if (player == null) return false;
                if (player.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
                {
                    // probably very prone to crashes
                    if (block_build.Id != block.Id)
                    {
                        block_build = block; // in creative, they can just build with anything...                        
                        slot.Itemstack.Attributes.SetInt("blockid", block_build.Id);
                        if (api.Side == EnumAppSide.Client) capi.ShowChatMessage($"Block selected: {this.block_build.Code}");                        
                    }
                    return true;
                }
                BlockEntity blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                if (blockEntity == null || BGMod.BGConfig.whitelist.ContainsKey(block.FirstCodePart()))
                {
                    // block has no entity and is a normal block or is whitelisted which ignores BE's.
                    string blockcode = block.Code.ToString();                    
                    this.block_build = player.World.SearchBlocks(new AssetLocation(NormalizeBlockCode(block)))[0];
                    
                    slot.Itemstack.Attributes.SetInt("blockid", block_build.Id);
                    if (api.Side == EnumAppSide.Client) capi.ShowChatMessage($"Block selected: {this.block_build.Code}");
                    return true;
                }
                else
                {
                    // a block entity exists, lets see if it has inventory                    
                    if (blockEntity is IBlockEntityContainer)
                    {
                        // the block, at the very least, is a container. I leave it to users to be smart about it.
                        if (((IBlockEntityContainer)blockEntity).Inventory.Count > 4)
                        {
                            // container has to have at least 5 inventory slots to count... weeds out querns and the like.
                            this.block_Storage = blockSel.Position;
                            slot.Itemstack.Attributes.SetInt("storagex", block_Storage.X);
                            slot.Itemstack.Attributes.SetInt("storagey", block_Storage.Y);
                            slot.Itemstack.Attributes.SetInt("storagez", block_Storage.Z);
                            if (capi != null) capi.ShowChatMessage("Gadget Storage Set");
                            return true;
                        }
                    }
                    else if (blockEntity is BlockEntityChisel)
                    {
                        if (capi != null) capi.ShowChatMessage("You cannot use this gadget to build Chiseled Blocks. Patreon support would help bring a Copy/Paste Gadget.");
                    }
                    else
                    {
                        if (capi != null) capi.ShowChatMessage("Invalid block, not a storage block.");
                    }
                    // one block entity I know exists are berry bushes... should we build with those?
                }
            }    
            return false;
        }

        /// <summary>
        /// Tries to reduce crashing and odd behavior by limiting what can and cannot be used for building.
        /// Typically only full non-ore standard solid blocks can be used, with the possible exception of axles.
        /// </summary>
        /// <param name="block">The block to check against</param>
        /// <returns>Block Code String of approved block.</returns>
        public string NormalizeBlockCode(Block block)
        {
            // default in case the block type is bonkers.
            string defaultblock = "game:soil-low-none";
            string firstcodepart = block.FirstCodePart();
            if (BGMod.BGConfig.whitelist.ContainsKey(block.FirstCodePart()))
            {
                // the whitelist contains the first block-code part, assume block selected is valid.
                // test with berry bushes
                return block.Code.ToString();
            }
            if (firstcodepart == "air") return defaultblock; // something bad happened or they tried to pick a chiseled block...
            if (block.FirstCodePart(1).Contains("resin"))  return "game:log-placed-" + block.LastCodePart(1) + "-ud";
            switch(firstcodepart)
            {
                case "soil": return "game:" + block.CodeWithoutParts(1) + "-none";
                case "forestfloor": return "game:soil-medium-none";
                case "ore": return "game:rock-" + block.LastCodePart();
                case "woodenaxle": return block.Code.ToString(); // the one odd block exception
                case "log": return "game:log-placed-" + block.LastCodePart(1) + "-ud";
                case "looseflints":
                case "loosestones":
                case "looseores":
                case "saltpeter":
                case "stalagsection":
                case "crystal":
                case "stonecoffinsection":
                case "stonecoffinlid": return defaultblock;
            }
            for (int i = 0; i < 6; i++)
            {
                // if not all sides are 'solid' then return default dirt.
                // this acts as a 'catch all' to odd blocks and hopefully prevent crashes
                if (!block.SideSolid[i]) return defaultblock;
            }
            return block.Code.ToString();
        }

        /// <summary>
        /// Returns the Construction Paste cost of building a block of the block parameter.
        /// Should only return 0 if block parameter is invalid.
        /// </summary>
        /// <param name="block">Block type to check</param>
        /// <returns>INT cost of building a single block of type block parameter.</returns>
        public int GetPasteBuildCost(Block block)
        {
            int cost = 10;
            if (block == null) return 0;
            string firstcodepart = block.FirstCodePart(0);
            string secondcodepart = block.FirstCodePart(1);
            if (firstcodepart == "soil")
            {
                cost = 1;
                if (secondcodepart == "high")
                {
                    cost = 50;
                }
            }
            if (firstcodepart == "rock")
            {
                cost = 5;
            }
            if (BGMod.BGConfig.whitelist.ContainsKey(block.FirstCodePart()))
            {
                cost = BGMod.BGConfig.whitelist[block.FirstCodePart()];
            }
            return (cost == 0) ? 1 : cost; // Cost can NEVER equal 0, ever, that would throw so many errors.
        }

        /// <summary>
        /// Consume items (Paste or Blocks) for building, called once after blocks are placed and we know the final cost.
        /// </summary>
        /// <param name="player">Player</param>
        /// <param name="gadget">Gadget Used</param>
        /// <param name="amount">Amount to consume</param>
        public void ConsumePasteOrBlocks(IPlayer player, ItemSlot gadget, int amount)
        {
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative) return; // Creative costs nothing.

            IInventory hotbar = player.InventoryManager.GetHotbarInventory();
            IInventory backpack = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            BlockPos storage = GetStorageBlock(gadget);
            IInventory invstorage;
            if (storage.X != int.MinValue) { invstorage = ((IBlockEntityContainer)player.Entity.World.BlockAccessor.GetBlockEntity(storage)).Inventory; }
            else { invstorage = null; }                       

            if (BGMod.BGConfig.usePaste)
            {
                if (hotbar != null)
                {
                    foreach (ItemSlot itemSlot in hotbar)
                    {
                        if (amount == 0) return;
                        if (!itemSlot.Empty && itemSlot.Itemstack.Item != null && itemSlot.Itemstack.Item.Id == pasteID)
                        {
                            // we found paste!
                            if (itemSlot.StackSize <= amount)
                            {
                                amount -= itemSlot.StackSize;
                                itemSlot.TakeOutWhole();
                            }
                            else
                            {
                                itemSlot.TakeOut(amount);
                                amount = 0;
                            }
                        }
                    }
                }
                if (backpack != null)
                {
                    foreach (ItemSlot itemSlot in backpack)
                    {
                        if (amount == 0) return;
                        if (!itemSlot.Empty && itemSlot.Itemstack.Item != null && itemSlot.Itemstack.Item.Id == pasteID)
                        {
                            // we found paste!
                            if (itemSlot.StackSize <= amount)
                            {
                                amount -= itemSlot.StackSize;
                                itemSlot.TakeOutWhole();
                            }
                            else
                            {
                                itemSlot.TakeOut(amount);
                                amount = 0;
                            }
                        }
                    }
                }
                if (invstorage != null)
                {
                    foreach (ItemSlot itemSlot in invstorage)
                    {
                        if (amount == 0) return;
                        if (!itemSlot.Empty && itemSlot.Itemstack.Item != null && itemSlot.Itemstack.Item.Id == pasteID)
                        {
                            // we found paste!
                            if (itemSlot.StackSize <= amount)
                            {
                                amount -= itemSlot.StackSize;
                                itemSlot.TakeOutWhole();
                            }
                            else
                            {
                                itemSlot.TakeOut(amount);
                                amount = 0;
                            }
                        }
                    }
                }
            }
            else
            {
                if (hotbar != null)
                {
                    foreach (ItemSlot itemSlot in hotbar)
                    {
                        if (amount == 0) return;
                        if (!itemSlot.Empty && itemSlot.Itemstack.Block != null && itemSlot.Itemstack.Block.Id == block_build.Id)
                        {
                            // we found paste!
                            if (itemSlot.StackSize <= amount)
                            {
                                amount -= itemSlot.StackSize;
                                itemSlot.TakeOutWhole();
                            }
                            else
                            {
                                itemSlot.TakeOut(amount);
                                amount = 0;
                            }
                        }
                    }
                }
                if (backpack != null)
                {
                    foreach (ItemSlot itemSlot in backpack)
                    {
                        if (amount == 0) return;
                        if (!itemSlot.Empty && itemSlot.Itemstack.Block != null && itemSlot.Itemstack.Block.Id == block_build.Id)
                        {
                            // we found paste!
                            if (itemSlot.StackSize <= amount)
                            {
                                amount -= itemSlot.StackSize;
                                itemSlot.TakeOutWhole();
                            }
                            else
                            {
                                itemSlot.TakeOut(amount);
                                amount = 0;
                            }
                        }
                    }
                }
                if (invstorage != null)
                {
                    foreach (ItemSlot itemSlot in invstorage)
                    {
                        if (amount == 0) return;
                        if (!itemSlot.Empty && itemSlot.Itemstack.Block != null && itemSlot.Itemstack.Block.Id == block_build.Id)
                        {
                            // we found paste!
                            if (itemSlot.StackSize <= amount)
                            {
                                amount -= itemSlot.StackSize;
                                itemSlot.TakeOutWhole();
                            }
                            else
                            {
                                itemSlot.TakeOut(amount);
                                amount = 0;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when user drops gadget from inventory, resets Picking Range if applicable.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byPlayer"></param>
        /// <param name="slot"></param>
        /// <param name="quantity"></param>
        /// <param name="handling"></param>
        public override void OnHeldDropped(IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)
        {
            byPlayer.WorldData.PickingRange = GlobalConstants.DefaultPickingRange;
            if (highlightedBlocks != null && highlightedBlocks.Count > 0)
            {
                ClearHighlight(world, byPlayer, slot, true);
            }

            base.OnHeldDropped(world, byPlayer, slot, quantity, ref handling);
        }

    }
}
