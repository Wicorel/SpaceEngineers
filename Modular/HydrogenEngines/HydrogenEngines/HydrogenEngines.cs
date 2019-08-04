﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {
        class HydrogenEngines
        {
            List<IMyTerminalBlock> localHydrogenEngines = new List<IMyTerminalBlock>();

            Program thisProgram;
            public HydrogenEngines(Program program)
            {
                thisProgram = program;

                thisProgram.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                thisProgram.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if(tb.BlockDefinition.TypeIdString== "MyObjectBuilder_HydrogenEngine")
                {
                    localHydrogenEngines.Add(tb);
                }
            }

            void LocalGridChangedHandler()
            {
                localHydrogenEngines.Clear();
            }

            /// <summary>
            /// gets current and max output and returns number of hydrogen engines
            /// </summary>
            /// <param name="currentoutput">total current output</param>
            /// <param name="maxoutput">total max output</param>
            /// <returns></returns>
            public int CurrentOutput( ref double currentoutput, ref double maxoutput)
            {
                currentoutput = 0;
                maxoutput = 0;
                int count = 0;
                foreach(var tb in localHydrogenEngines)
                {
                    if(tb is IMyPowerProducer)
                    {
                        count++;
                        var pp = tb as IMyPowerProducer;

                        currentoutput += pp.CurrentOutput;
                        maxoutput += pp.MaxOutput;
                    }
                }
                return count;
            }

            public double tanksFill()
            {
                double totalLevel = 0;
                int iTanksCount = 0;

                foreach (var tb in localHydrogenEngines)
                {
                    if (tb.BlockDefinition.TypeIdString == "MyObjectBuilder_HydrogenEngine")
                    {
                        double tankLevel = 0;
                        //Type: Hydrogen Engine
                        //Max Output: 500.00 kW
                        //Current Output: 260 W
                        //Filled: 100.0 % (16000L / 16000L)
//                        thisProgram.Echo("DetailedInfo=\n" + tb.DetailedInfo);
                        string[] lines = tb.DetailedInfo.Trim().Split('\n');
//                        thisProgram.Echo("#lines=" + lines.Length);
                        if (lines.Length < 3) // not what we expected
                            continue;

                        //Filled: 100.0% (16000L/16000L)
                        string[] aParams = lines[3].Split(' ');
//                        thisProgram.Echo("#params=" + aParams.Length);
                        if (aParams.Length < 2) // not what we expected
                            continue;
//                        thisProgram.Echo("Param=" + aParams[1]);
                        string sPercent = aParams[1].Replace('%', ' ');
                        bool bOK = double.TryParse(sPercent.Trim(), out tankLevel);
//                        bool bOK = double.TryParse(aParams[1], out tankLevel);
//                        if (!bOK) thisProgram.Echo("Tryparse fail!");
//                        thisProgram.Echo("Tanklevel=" + tankLevel.ToString());
                        tankLevel /= 100.0; // convert from 0->100 to 0->1.0
                        totalLevel += tankLevel;
                        iTanksCount++;
                    }
                }
                if (iTanksCount > 0)
                {
                    return totalLevel / iTanksCount;
                }
                else return -1;
            }
            /*
            Type: Hydrogen Engine
            Max Output: 500.00 kW
            Current Output: 260 W
            Filled: 100.0% (16000L/16000L)
            */
        }
    }
}
