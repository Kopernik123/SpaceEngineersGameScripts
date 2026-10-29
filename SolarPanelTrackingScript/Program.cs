using Sandbox.Game.EntityComponents;
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
        float lastAlignment;
        int negativeCycleCount;
        bool rotationMode;//false = horizontal tracking, true = vertical tracking
        float desiredRPM;
        public Program()
        {
            lastAlignment = 0;
            negativeCycleCount = 0;
            rotationMode = false;
            desiredRPM = 0.2f;

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        public void Main(string argument, UpdateType updateSource)
        {
            //1 - Finding solar panel from which to base sun tracking calculations
            IMySolarPanel mySolarPanel = GridTerminalSystem.GetBlockWithName("MainSolar") as IMySolarPanel;
            if(mySolarPanel == null)
            {
                Echo("Couldn't find solar panel with name - \"MainSolar\"");
                return;
            }
            //2 - Calculating percentage of max power on current alignment
            float currentAlignmentPercentage = 0;
            float minAlignment = 99;
            //Example: 0.016 MW = 16 KW
            //MaxOutput is multiplied by 1000 to change the value from MW to KW
            currentAlignmentPercentage = ((mySolarPanel.MaxOutput * 1000) / 160)*100;
            if (currentAlignmentPercentage >= minAlignment)
                Echo($"Current solar alignment: {currentAlignmentPercentage}% \nAt acceptable level");
            else
                Echo($"Current solar alignment: {currentAlignmentPercentage}% \nBelow acceptable level");

            //3 - Finding and assigning horizontal and vertical motors
            IMyMotorStator horizontalRotor = GridTerminalSystem.GetBlockWithName("Solar - Horizontal") as IMyMotorStator;
            if (horizontalRotor == null)
            {
                Echo("Couldn't find rotor with name - \"Solar - Horizontal\"");
                return;
            }
            IMyMotorStator verticalRotor = GridTerminalSystem.GetBlockWithName("Solar - Vertical") as IMyMotorStator;
            if (verticalRotor == null)
            {
                Echo("Couldn't find rotor with name - \"Solar - Vertical\"");
                return;
            }
            //Degrees = Radians * (180/PI)
            //Radians = Degrees * (PI/180)
            Echo($"Current horizontal rotation in radians: {horizontalRotor.Angle}");
            Echo($"Current horizontal rotation in degrees: {horizontalRotor.Angle*(180/Math.PI)}");

            //4 - Tracking the sun on the horizontal axis
            if (currentAlignmentPercentage > 0)
            {
                if (lastAlignment > currentAlignmentPercentage)
                {
                    desiredRPM = -desiredRPM;
                    negativeCycleCount++;
                    if (negativeCycleCount >= 2)
                    {
                        rotationMode = !rotationMode;
                        negativeCycleCount = 0;
                    }
                }

                if (rotationMode == false)
                {
                    verticalRotor.TargetVelocityRPM = 0;
                    horizontalRotor.TargetVelocityRPM = desiredRPM * (float)Math.Pow((100 / currentAlignmentPercentage), 1.4);
                }
                else
                {
                    horizontalRotor.TargetVelocityRPM = 0;
                    verticalRotor.TargetVelocityRPM = desiredRPM * (float)Math.Pow((100 / currentAlignmentPercentage), 1.4);
                }
            }
            else
            {
                horizontalRotor.TargetVelocityRPM = 0;
                verticalRotor.TargetVelocityRPM = 0;
                Echo("Sun not detected, engaging brakes.");
            }
            //Saving info for next run
            lastAlignment = currentAlignmentPercentage;
        }
    }
}
