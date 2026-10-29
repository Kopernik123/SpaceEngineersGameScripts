using Sandbox.Game;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        int rotationCycle;
        class ItemOnGrid
        {
            public MyItemType type;
            public MyFixedPoint amount;
            public ItemOnGrid(MyItemType type, MyFixedPoint amount)
            {
                this.type = type;
                this.amount = amount;
            }
            public ItemOnGrid(MyItemType type)
            {
                this.type = type;
                this.amount = 0;
            }
            public static bool FindItemByType(List<ItemOnGrid> itemsOnGrid, MyItemType type)
            {
                bool found = false;
                foreach (ItemOnGrid item in itemsOnGrid)
                {
                    if (item.type == type)
                    {
                        found = true;
                        break;
                    }
                }
                return found;
            }
            public static void AddItemAmountByType(List<ItemOnGrid> itemsOnGrid, MyItemType type, MyFixedPoint amount)
            {
                foreach (ItemOnGrid item in itemsOnGrid)
                {
                    if (item.type == type)
                    {
                        item.amount += amount;
                        break;
                    }
                }
            }
            public string DisplayItemInfo()
            {
                string itemType = "";

                if (type.TypeId.Equals("MyObjectBuilder_Ore"))
                    itemType = " Ore";
                else if (type.TypeId.Equals("MyObjectBuilder_Ingot"))
                    itemType = " Ingot";
                else if (type.TypeId.Equals("MyObjectBuilder_Component"))
                    return $"{type.SubtypeId}{itemType}: {amount}x\n";

                return $"{type.SubtypeId}{itemType}: {amount} kg\n";
            }
        }
        public Program()
        {
            rotationCycle = 0;

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
                if (block.HasInventory && block.IsFunctional && block.IsSameConstructAs(Me))
                    myTerminalBlocksWithInventory.Add(block);
            }
            Echo($"{myTerminalBlocksWithInventory.Count}");

            //3 - Counting the amount of items in grid inventories
            List<ItemOnGrid> itemsOnGrid = new List<ItemOnGrid>();
            MyFixedPoint maxVolumeOfOreContainers = 0;
            MyFixedPoint filledVolumeOfOreContainers = 0;

            foreach (var block in myTerminalBlocksWithInventory)
            {
                if (!block.BlockDefinition.TypeId.ToString().Equals("MyObjectBuilder_Drill") && block.IsSameConstructAs(Me))
                {
                    List<VRage.Game.ModAPI.Ingame.IMyInventory> myInventories = new List<VRage.Game.ModAPI.Ingame.IMyInventory>();
                    for (int x = block.InventoryCount - 1; x >= 0; x--)
                    {
                        myInventories.Add(block.GetInventory(x));
                    }

                    List<MyInventoryItem> myInventoryItems = new List<MyInventoryItem>();
                    List<List<MyInventoryItem>> myMultipleInventoryItems = new List<List<MyInventoryItem>>();

                    foreach (var inventory in myInventories)
                    {
                        inventory.GetItems(myInventoryItems);
                        myMultipleInventoryItems.Add(myInventoryItems);

                        List<MyItemType> itemTypes = new List<MyItemType>();
                        inventory.GetAcceptedItems(itemTypes);

                        //Calculating the maxVolume of containers that can hold ore and their current filled volume
                        foreach (var itemType in itemTypes)
                        {
                            if (itemType.TypeId.Equals("MyObjectBuilder_Ore") && block.IsSameConstructAs(Me))
                            {
                                maxVolumeOfOreContainers += inventory.MaxVolume;
                                filledVolumeOfOreContainers += inventory.CurrentVolume;
                                break;
                            }
                        }
                    }
                    //Finding and recording the types of existing items on the grid
                    foreach (var inventoryItems in myMultipleInventoryItems)
                    {
                        foreach (var item in inventoryItems)
                        {
                            if (!ItemOnGrid.FindItemByType(itemsOnGrid, item.Type))
                            {
                                itemsOnGrid.Add(new ItemOnGrid(item.Type));
                            }
                        }
                    }
                }
            }
            //Counting the separate amounts of items in block inventories
            foreach (var block in myTerminalBlocksWithInventory)
            {
                List<VRage.Game.ModAPI.Ingame.IMyInventory> myInventories = new List<VRage.Game.ModAPI.Ingame.IMyInventory>();
                for (int x = block.InventoryCount - 1; x >= 0; x--)
                {
                    myInventories.Add(block.GetInventory(x));
                }
                foreach (ItemOnGrid item in itemsOnGrid)
                {
                    foreach (var inventory in myInventories)
                    {
                        ItemOnGrid.AddItemAmountByType(itemsOnGrid, item.type, inventory.GetItemAmount(item.type));
                    }
                }
            }
            MyFixedPoint currentVolumeOfOreContainersPercentage = (MyFixedPoint)((float)filledVolumeOfOreContainers / (float)maxVolumeOfOreContainers) * 100;
            //4 - Controls of drill
            Sandbox.ModAPI.Ingame.IMyShipDrill mainDrill = GridTerminalSystem.GetBlockWithName("Main Drill") as Sandbox.ModAPI.Ingame.IMyShipDrill;
            Sandbox.ModAPI.Ingame.IMyMotorAdvancedStator mainDrillRotor = GridTerminalSystem.GetBlockWithName("Drill Rotor") as Sandbox.ModAPI.Ingame.IMyMotorAdvancedStator;
            Sandbox.ModAPI.Ingame.IMyPistonBase mainDrillPistonHorizontal1 = GridTerminalSystem.GetBlockWithName("DrillPistonHorizontal 1") as Sandbox.ModAPI.Ingame.IMyPistonBase;
            Sandbox.ModAPI.Ingame.IMyPistonBase mainDrillPistonVertical1 = GridTerminalSystem.GetBlockWithName("DrillPistonVertical 1") as Sandbox.ModAPI.Ingame.IMyPistonBase;
            if (currentVolumeOfOreContainersPercentage > 30)
            {
                mainDrill.Enabled = false;
                mainDrillRotor.RotorLock = true;
            }
            else
            {
                float rotorDegrees = (float)(mainDrillRotor.Angle * (180 / Math.PI));
                Echo($"{rotorDegrees}");
                if (rotorDegrees <= mainDrillRotor.LowerLimitDeg + 0.1 || rotorDegrees >= mainDrillRotor.UpperLimitDeg - 0.1 && rotorDegrees <= mainDrillRotor.UpperLimitDeg)
                {
                    rotationCycle++;
                    mainDrillRotor.TargetVelocityRPM = -mainDrillRotor.TargetVelocityRPM;
                }
                //Horizontal and vertical control - extends horizontally every 2 wall bounces and when at max, retracts horizontally and extends vertically
                if (rotationCycle >= 2)
                {
                    rotationCycle = 0;
                    if (mainDrillPistonHorizontal1.MaxLimit >= 10)
                    {
                        mainDrillPistonHorizontal1.MaxLimit = 0;
                        mainDrillPistonHorizontal1.Retract();
                        //Vertical control - extends when horizontal is at max and retracts vertically when it reaches max
                        if (mainDrillPistonVertical1.MaxLimit >= 10)
                        {
                            mainDrillPistonVertical1.MaxLimit = 0;
                            mainDrillPistonVertical1.Retract();
                        }
                        else
                        {
                            mainDrillPistonVertical1.Extend();
                            mainDrillPistonVertical1.MaxLimit += 2f;
                        }
                    }
                    else
                    {
                        mainDrillPistonHorizontal1.MaxLimit += 2f;
                        mainDrillPistonHorizontal1.Extend();
                    }
                }
                mainDrill.Enabled = true;
                mainDrillRotor.RotorLock = false;
            }
            //5 - Finding and assigning a text panel based on its name, and displaying inventory info on it
            Sandbox.ModAPI.Ingame.IMyTextPanel myTextPanel = GridTerminalSystem.GetBlockWithName("InventoryLCD") as Sandbox.ModAPI.Ingame.IMyTextPanel;
            string textToDisplay = $"Current ore container volume: {currentVolumeOfOreContainersPercentage}%\n";
            foreach (ItemOnGrid item in itemsOnGrid)
            {
                if (item.type.TypeId.Equals("MyObjectBuilder_Ore") || item.type.TypeId.Equals("MyObjectBuilder_Ingot"))
                    textToDisplay += item.DisplayItemInfo();
            }
            myTextPanel.WriteText(textToDisplay);
        }
    }
}
