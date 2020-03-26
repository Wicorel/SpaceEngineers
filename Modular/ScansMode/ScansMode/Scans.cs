﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class ScansMode : ScanBase
        {

            Program _program;
            WicoControl _wicoControl;
            WicoIGC _wicoIGC;
            WicoBlockMaster _wicoBlockMaster;

            public ScansMode(Program program, WicoControl wicoControl, WicoBlockMaster wicoBlockMaster, WicoIGC igc) : base(program)
            {
                _program = program;
                _wicoControl = wicoControl;
                _wicoBlockMaster = wicoBlockMaster;
                _wicoIGC = igc;

                _program.moduleName += " Scans";
                _program.moduleList += "\nScans V4";

//                _program._CustomDataIni.Get(sNavSection, "NAVEmulateOld").ToBoolean(NAVEmulateOld);
//                _program._CustomDataIni.Set(sNavSection, "NAVEmulateOld", NAVEmulateOld);

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _wicoIGC.AddPublicHandler(NavCommon.WICOB_NAVADDTARGET, BroadcastHandler);
                _wicoIGC.AddPublicHandler(NavCommon.WICOB_NAVRESET, BroadcastHandler);
                _wicoIGC.AddPublicHandler(NavCommon.WICOB_NAVSTART, BroadcastHandler);

            }

        void LoadHandler(MyIni Ini)
        {

            //dtNavStartShip = DateTime.FromBinary(Ini.Get(sNavSection, "dStartShip").ToInt64());
        }

        void SaveHandler(MyIni Ini)
        {
            
//            Ini.Set(sNavSection, "dStartShip", dtNavStartShip.ToBinary());
        }
        /// <summary>
        /// Modes have changed and we are being called as a handler
        /// </summary>
        /// <param name="fromMode"></param>
        /// <param name="fromState"></param>
        /// <param name="toMode"></param>
        /// <param name="toState"></param>
        public void ModeChangeHandler(int fromMode, int fromState, int toMode, int toState)
        {
  //          if (fromMode == WicoControl.MODE_GOINGTARGET)
            {
            }
            // need to check if this is us
            if (toMode == WicoControl.MODE_SCANS
//                || toMode == WicoControl.MODE_STARTNAV
                )
            {
                _wicoControl.WantOnce();
            }
        }
        /// <summary>
        /// just after program init, we are starting with these modes
        /// </summary>
        void ModeInitHandler()
        {
            int iMode = _wicoControl.IMode;
            int iState = _wicoControl.IState;

            if (iMode == WicoControl.MODE_SCANS)
            {
                // TODO: Check state and re-init as needed
                _wicoControl.WantFast();
            }
        }
        void LocalGridChangedHandler()
        {
            //               shipController = null;
        }

        /// <summary>
        /// Handler for processing any of the 'trigger' upatetypes
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="updateSource"></param>
        public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
        {
        }

        void UpdateHandler(UpdateType updateSource)
        {
            int iMode = _wicoControl.IMode;
            int iState = _wicoControl.IState;

            // need to check if this is us
            if (iMode == WicoControl.MODE_SCANS)  { doModeScans(); }
        }

        void BroadcastHandler(MyIGCMessage msg)
        {
            // NOTE: called on ALL received messages; not just 'our' tag
        }


        int ScansDoneMode = WicoControl.MODE_NAVNEXTTARGET;
            int ScansDoneState = 0;

            string sScansSection = "SCANS";
            void ScansInitCustomData(INIHolder iNIHolder)
            {
                //           iNIHolder.GetValue(sScansSection, "CameraViewOnly", ref sCameraViewOnly, true);
            }
            void ScansSerialize(INIHolder iNIHolder)
            {
                //            iNIHolder.SetValue(sNavSection, "vNavHome", vNavHome);
                //            iNIHolder.SetValue(sNavSection, "ValidNavHome", bValidNavHome);
                iNIHolder.SetValue(sScansSection, "DoneMode", ScansDoneMode);
                iNIHolder.SetValue(sScansSection, "DoneState", ScansDoneState);

            }
            void ScansDeserialize(INIHolder iNIHolder)
            {
                iNIHolder.GetValue(sScansSection, "DoneMode", ref ScansDoneMode, true);
                iNIHolder.GetValue(sScansSection, "DoneState", ref ScansDoneState, true);
            }

            // TODO: Flags for other options (TBD)
            // TODO: scan range
            // TODO: stop on first hit (by type?)
            // TODO: all sides or specific sides?
            public override void StartScans(int doneMode = WicoControl.MODE_NAVNEXTTARGET, int doneState = 0)
            {
                ScansDoneMode = doneMode;
                ScansDoneState = doneState;
//                current_state = 0;
                _wicoControl.SetMode(WicoControl.MODE_DOSCANS,0);
            }



            QuadrantCameraScanner scanfrontScanner;
            QuadrantCameraScanner scanbackScanner;
            QuadrantCameraScanner scanleftScanner;
            QuadrantCameraScanner scanrightScanner;
            QuadrantCameraScanner scantopScanner;
            QuadrantCameraScanner scanbottomScanner;

            double scanElapsedMs = 0;

            void doModeScans()
            {
                StatusLog("clear", textPanelReport);
                StatusLog(moduleName + ":SCAN!", textPanelReport);
                Echo("Scan:current_state=" + current_state.ToString());

                switch (current_state)
                {
                    case 0:
                        { // init camera scan for asteroids/objects
                            ResetMotion();
                            scanElapsedMs = 0;

                            // initialize cameras
                            scanfrontScanner = new QuadrantCameraScanner(this, cameraForwardList, 5000);
                            scanbackScanner = new QuadrantCameraScanner(this, cameraBackwardList, 5000);
                            scanleftScanner = new QuadrantCameraScanner(this, cameraLeftList, 5000);
                            scanrightScanner = new QuadrantCameraScanner(this, cameraRightList, 5000);
                            scantopScanner = new QuadrantCameraScanner(this, cameraUpList, 5000);
                            scanbottomScanner = new QuadrantCameraScanner(this, cameraDownList, 5000);

                            current_state = 410;
                            bWantFast = true;
                            break;
                        }
                    case 410:
                        {
                            StatusLog("Long Range Scan", textPanelReport);
                            if (scanfrontScanner == null) // in case we reload/compile in this state..
                            {
                                bWantFast = true;
                                current_state = 0;
                                return;
                            }
                            bWantMedium = true;
                            scanElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                            // use for timeout...

                            // do camera scans

                            if (scanfrontScanner.DoScans())
                            {
                                AsteroidProcessLDEI(scanfrontScanner.myLDEI);
                            }
                            if (scanbackScanner.DoScans())
                            {
                                AsteroidProcessLDEI(scanbackScanner.myLDEI);
                            }
                            if (scanleftScanner.DoScans())
                            {
                                AsteroidProcessLDEI(scanleftScanner.myLDEI);
                            }
                            if (scanrightScanner.DoScans())
                            {
                                AsteroidProcessLDEI(scanrightScanner.myLDEI);
                            }
                            if (scantopScanner.DoScans())
                            {
                                AsteroidProcessLDEI(scantopScanner.myLDEI);
                            }
                            if (scanbottomScanner.DoScans())
                            {
                                AsteroidProcessLDEI(scanbottomScanner.myLDEI);
                            }

                            // TODO: if missing cameras on a side(s), rotate ship to point cameras at that side
                            // TODO: Flags for other options (TBD)
                            // TODO: scan range
                            // TODO: stop on first hit (by type?)
                            // TODO: all sides or specific sides?

                            string s = "";
                            s += "Front: ";
                            if (scanfrontScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
                                s += scanfrontScanner.SCAN_DISTANCE.ToString("0") + " meters";
                            }
                            s += " " + scanfrontScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Back: ";
                            if (scanbackScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
                                s += scanbackScanner.SCAN_DISTANCE.ToString("0") + " meters";
                            }
                            s += " " + scanbackScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Left: ";
                            if (scanleftScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
                                s += scanleftScanner.SCAN_DISTANCE.ToString("0") + " meters";
                            }
                            s += " " + scanleftScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Right: ";
                            if (scanrightScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
                                s += scanrightScanner.SCAN_DISTANCE.ToString("0") + " meters";
                            }
                            s += " " + scanrightScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Top: ";
                            if (scantopScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
                                s += scantopScanner.SCAN_DISTANCE.ToString("0") + " meters";
                            }
                            s += " " + scantopScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Bottom: ";
                            if (scanbottomScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
                                s += scanbottomScanner.SCAN_DISTANCE.ToString("0") + " meters";
                            }
                            s += " " + scanbottomScanner.myLDEI.Count + " objects";
                            s += "\n";

                            if (AsteroidFindNearest() < 0)
                                s += "No Known Asteroid";
                            else s += "FOUND at least one asteroid!";

                            StatusLog(s, textPanelReport);
                            Echo(s);

                            if (
                                scanfrontScanner.DoneScanning() &&
                                scanbackScanner.DoneScanning() &&
                                scanleftScanner.DoneScanning() &&
                                scanrightScanner.DoneScanning() &&
                                scantopScanner.DoneScanning() &&
                                scanbottomScanner.DoneScanning()
                                )
                            {
                                setMode(ScansDoneMode);
                                current_state = ScansDoneState;

                                // reset to default for possible next run
                                ScansDoneMode = MODE_SCANCOMPLETED;
                                ScansDoneState = 0;
                            }
                            break;
                        }
                }

            }


        }
    }
}
