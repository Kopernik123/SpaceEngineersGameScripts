using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        class OreOnGrid
        {
            public string SubTypeId;
            public MyFixedPoint amount;
            public OreOnGrid(string SubTypeId, MyFixedPoint amount)
            {
                this.SubTypeId = SubTypeId;
                this.amount = amount;
            }
            public OreOnGrid(string SubTypeId)
            {
                this.SubTypeId = SubTypeId;
                this.amount = 0;
            }
            public static bool FindOreById(List<OreOnGrid> oresOnGrid, string SubTypeId)
            {
                bool found = false;
                foreach (OreOnGrid ore in oresOnGrid)
                {
                    if (ore.SubTypeId == SubTypeId)
                    {
                        found = true;
                        break;
                    }
                }
                return found;
            }
            public static void AddOreAmountById(List<OreOnGrid> oresOnGrid, string SubTypeId, MyFixedPoint amount)
            {
                foreach (OreOnGrid ore in oresOnGrid)
                {
                    if (ore.SubTypeId == SubTypeId)
                    {
                        ore.amount += amount;
                        break;
                    }
                }
            }
            public string DisplayOreInfo()
            {
                return $"{SubTypeId}: {amount} kg\n";
            }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        public void Main(string argument, UpdateType updateSource)
        {
            //1 - Finding and assigning all blocks on the grid into a list
            List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> myTerminalBlocks = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(myTerminalBlocks);
            Echo($"{myTerminalBlocks.Count}");

            //2 - Assigning blocks with inventories to a specific list
            List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> myTerminalBlocksWithInventory = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
            foreach (Sandbox.ModAPI.Ingame.IMyTerminalBlock block in myTerminalBlocks)
            {
                if (block.HasInventory && block.IsFunctional)
                    myTerminalBlocksWithInventory.Add(block);
            }
            Echo($"{myTerminalBlocksWithInventory.Count}");

            //3 - Counting the amount of ores in grid inventories
            List<OreOnGrid> oresOnGrid = new List<OreOnGrid>();
            MyFixedPoint maxVolumeOfOreContainers = 0;
            MyFixedPoint filledVolumeOfOreContainers = 0;

            foreach (var block in myTerminalBlocksWithInventory)
            {
                bool oreFound = false;
                VRage.Game.ModAPI.Ingame.IMyInventory myInventory = block.GetInventory();

                List<MyInventoryItem> myInventoryItems = new List<MyInventoryItem>();
                myInventory.GetItems(myInventoryItems);

                //Calculating the maxVolume of containers that can hold ore and their current filled volume
                List<MyItemType> itemTypes = new List<MyItemType>();
                myInventory.GetAcceptedItems(itemTypes);

                foreach (MyItemType itemType in itemTypes)
                {
                    if (itemType.TypeId.Equals("MyObjectBuilder_Ore"))
                    {
                        maxVolumeOfOreContainers += myInventory.MaxVolume;
                        filledVolumeOfOreContainers += myInventory.CurrentVolume;
                        Echo($"Type {itemType.TypeId} has been found.");
                        oreFound = true;
                        break;
                    }
                }
                //Finding and recording the SubTypeId's of existing ores on the grid
                if (oreFound)
                {
                    foreach (MyInventoryItem item in myInventoryItems)
                        if (item.Type.TypeId.Equals("MyObjectBuilder_Ore") && !OreOnGrid.FindOreById(oresOnGrid, item.Type.SubtypeId))
                            oresOnGrid.Add(new OreOnGrid(item.Type.SubtypeId));

                    foreach (OreOnGrid ore in oresOnGrid)
                        OreOnGrid.AddOreAmountById(oresOnGrid, ore.SubTypeId, myInventory.GetItemAmount(MyItemType.MakeOre(ore.SubTypeId)));
                }
            }
            //Counting the separate amounts of ores in block inventories
            /*foreach (var block in myTerminalBlocksWithInventory)
            {
                VRage.Game.ModAPI.Ingame.IMyInventory myInventory = block.GetInventory();
                foreach (OreOnGrid ore in oresOnGrid)
                    OreOnGrid.AddOreAmountById(oresOnGrid, ore.SubTypeId, myInventory.GetItemAmount(MyItemType.MakeOre(ore.SubTypeId)));
            }*/
            MyFixedPoint currentVolumeOfOreContainersPercentage = (MyFixedPoint)((float)filledVolumeOfOreContainers / (float)maxVolumeOfOreContainers) * 100;
            //5 - Finding and assigning a text panel based on its name, and displaying inventory info on it
            string textToDisplay = $"Current ore container volume: {currentVolumeOfOreContainersPercentage}%\n";
            foreach (OreOnGrid ore in oresOnGrid)
            {
                textToDisplay += ore.DisplayOreInfo();
            }
            foreach (var block in myTerminalBlocks)
            {
                Sandbox.ModAPI.Ingame.IMyTextSurface myTextSurface;
                if (block.DisplayNameText.Equals("OreInfo") && block.IsSameConstructAs(Me))
                {
                    Echo("FOUND PANEL");
                    myTextSurface = block as Sandbox.ModAPI.Ingame.IMyTextSurface;
                    myTextSurface.WriteText(textToDisplay);
                }
            }
        }
    }
}
