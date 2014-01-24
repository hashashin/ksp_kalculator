// -------------------------------------------------------------------------------------------------
// kalculator.cs 0.1.1
//
// Based on public domain code by: Juan Sebastian Muñoz Arango
// http://www.pencilsquaregames.com/2013/10/calculator-for-unity3d/
// naruse@gmail.com
// 2013
//
// KSP kalculator plugin.
// Copyright (C) 2014 Iván Atienza
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
// 
// Email: mecagoenbush at gmail dot com
// Freenode: hashashin
//
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace kalculator
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class Kalculator : MonoBehaviour
    {
        private Rect _calcsize;
        private string _keybind;
        private float[] _registers = new float[2];
        private string _currentnumber = "0";
        private int _displayfontsize = 24;
        private int _opfontsize = 15;
        private string _optoperform = "";
        private int _maxdigits = 16;
        private bool _isfirst = true;
        private bool _clearscreen = false;
        private bool _visible = false;
        private string _version;
        private string _versionlastrun;
        private ToolbarButtonWrapper _button;
        private string _tooltipon = "Hide Kalculator";
        private string _tooltipoff = "Show Kalculator";
        private string _btexture_on = "Kalculator/Textures/icon_on";
        private string _btexture_off = "Kalculator/Textures/icon_off";
        private const ControlTypes BLOCK_ALL_CONTROLS = ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.ACTIONS_ALL | ControlTypes.EVA_INPUT | ControlTypes.TIMEWARP | ControlTypes.MISC | ControlTypes.GROUPS_ALL | ControlTypes.CUSTOM_ACTION_GROUPS;
        
#if DEBUG
        private string GetCalcInternalsInfo()
        {
            string info = "";
            info += "Screen: " + _currentnumber + "\n";
            info += "Clear Screen?: " + _clearscreen + "\n";
            for (int i = 0; i < _registers.Length; i++)
            {
                info += "Reg[" + i + "] <= " + _registers[i] + "\n";
            }
            info += "Current op: " + _optoperform + "\n";
            info += "Register to use: " + (_isfirst ? "0" : "1");

            return info;
        }
#endif
        void Awake()
        {
            LoadVersion();
            VersionCheck();
            LoadSettings();
            CheckDefaults();
        }

        void Start()
        {
            if (ToolbarButtonWrapper.ToolbarManagerPresent)
            {
                _button = ToolbarButtonWrapper.TryWrapToolbarButton("kalculator", "toggle");
                _button.TexturePath = _btexture_off;
                _button.ToolTip = _tooltipoff;
                
                _button.AddButtonClickHandler((e) =>
                {
                    Toggle();
                });
            }
        }

        void Update()
        {

            if (_visible)
            {
                GUI.FocusControl("kalculator");
                InputLockManager.SetControlLock(BLOCK_ALL_CONTROLS, "kalculator");
                GetInputFromCalc();
            }
            else
            {
                if (InputLockManager.GetControlLock("kalculator") == (BLOCK_ALL_CONTROLS))
                { 
                    InputLockManager.RemoveControlLock("kalculator"); 
                }
            }                      
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(_keybind))
            {
                Toggle();
            }
        }

        void OnDestroy()
        {
            SaveSettings();
            if (_button != null)
            {
                _button.Destroy();
            }
        }
                

        void OnGUI()
        {
#if DEBUG
            GUILayout.Label(GetCalcInternalsInfo());//DEBUG.
#endif          
            if (_visible)
            {
                _calcsize = GUI.Window(GUIUtility.GetControlID(0, FocusType.Passive), _calcsize, CalcWindow, "Kalculator");
            }
        }

        void CalcWindow(int windowID)
        {
            GUI.SetNextControlName("kalculator");
            GUI.Box(new Rect(10, 20, _calcsize.width - 20, 43), "");
            int tmpSize = GUI.skin.GetStyle("Label").fontSize;
            GUI.skin.GetStyle("TextField").fontSize = _displayfontsize;
            GUI.TextField(new Rect(10, 20, _calcsize.width - 20, 43), _currentnumber);
            GUI.skin.GetStyle("TextField").fontSize = tmpSize;

            tmpSize = GUI.skin.GetStyle("Label").fontSize;
            GUI.skin.GetStyle("Label").fontSize = _opfontsize;
            GUI.Label(new Rect(260, 42, _calcsize.width - 20, 37), _optoperform);
            GUI.skin.GetStyle("Label").fontSize = tmpSize;

            if (GUI.Button(new Rect(8, 70, 47, 30), "C"))
            {
                ClearCalcData();
            }
            if (GUI.Button(new Rect(61, 70, 47, 30), "+/-"))
            {
                if (_currentnumber != "0")
                {
                    if (_currentnumber[0] != '-')
                        _currentnumber = _currentnumber.Insert(0, "-");
                    else
                        _currentnumber = _currentnumber.Remove(0, 1);
                }
                if (_isfirst && !_registers[0].ToString().Contains("-"))
                    _registers[0] = -_registers[0];
                else if (_isfirst)
                    _registers[0] = -_registers[0];
                else if (!_isfirst && !_registers[1].ToString().Contains("-"))
                    _registers[1] = -_registers[1];
                else if (!_isfirst)
                    _registers[1] = -_registers[1];
            }

            if (GUI.Button(new Rect(165, 141, 47, 30), "+"))
                OperatorPressed("+");
            if (GUI.Button(new Rect(165, 106, 47, 30), "-"))
                OperatorPressed("-");
            if (GUI.Button(new Rect(113, 70, 47, 30), "/"))
                OperatorPressed("/");
            if (GUI.Button(new Rect(165, 70, 47, 30), "x"))
                OperatorPressed("x");
            if (GUI.Button(new Rect(8, 248, 47, 30), "√x"))
                OperatorPressed("√x");
            if (GUI.Button(new Rect(61, 248, 47, 30), "x^2"))
                OperatorPressed("x^2");
            if (GUI.Button(new Rect(113, 248, 47, 30), "1/x"))
                OperatorPressed("1/x");
            if (GUI.Button(new Rect(165, 248, 47, 30), "x^y"))
                OperatorPressed("x^y");
            if (GUI.Button(new Rect(8, 284, 47, 30), "3√x"))
                OperatorPressed("3√x");
            if (GUI.Button(new Rect(61, 284, 47, 30), "x^3"))
                OperatorPressed("x^3");
            if (GUI.Button(new Rect(113, 284, 47, 30), "%"))
                OperatorPressed("%");
            if (GUI.Button(new Rect(165, 284, 47, 30), "y√x"))
                OperatorPressed("y√x");
            if (GUI.Button(new Rect(218, 70, 40, 30), "log"))
                OperatorPressed("log");
            if (GUI.Button(new Rect(260, 70, 40, 30), "ln"))
                OperatorPressed("ln");
            if (GUI.Button(new Rect(218, 106, 40, 30), "10^x"))
                OperatorPressed("10^x");
            if (GUI.Button(new Rect(260, 106, 40, 30), "e^x"))
                OperatorPressed("e^x");
            if (GUI.Button(new Rect(218, 176, 40, 30), "sin"))
                OperatorPressed("sin");
            if (GUI.Button(new Rect(260, 176, 40, 30), "cos"))
                OperatorPressed("cos");
            if (GUI.Button(new Rect(218, 212, 40, 30), "tan"))
                OperatorPressed("tan");
            if (GUI.Button(new Rect(260, 212, 40, 30), "sinh"))
                OperatorPressed("sinh");
            if (GUI.Button(new Rect(218, 248, 40, 30), "cosh"))
                OperatorPressed("cosh");
            if (GUI.Button(new Rect(260, 248, 40, 30), "tanh"))
                OperatorPressed("tanh");
            if (GUI.Button(new Rect(218, 284, 40, 30), "asin"))
                OperatorPressed("asin");
            if (GUI.Button(new Rect(260, 284, 40, 30), "atan"))
                OperatorPressed("atan");
            if (GUI.Button(new Rect(218, 320, 40, 30), "acos"))
                OperatorPressed("acos");
            if (GUI.Button(new Rect(61, 320, 47, 30), "cot"))
                OperatorPressed("cot");
            if (GUI.Button(new Rect(113, 320, 47, 30), "sec"))
                OperatorPressed("sec");
            if (GUI.Button(new Rect(165, 320, 47, 30), "csc"))
                OperatorPressed("csc");
            
            if (GUI.Button(new Rect(8, 320, 47, 30), "!"))
                OperatorPressed("!");
            
            if (GUI.Button(new Rect(8, 176, 47, 30), "1"))
                AppendNumber("1");
            if (GUI.Button(new Rect(61, 176, 47, 30), "2"))
                AppendNumber("2");
            if (GUI.Button(new Rect(113, 176, 47, 30), "3"))
                AppendNumber("3");
            if (GUI.Button(new Rect(8, 141, 47, 30), "4"))
                AppendNumber("4");
            if (GUI.Button(new Rect(61, 141, 47, 30), "5"))
                AppendNumber("5");
            if (GUI.Button(new Rect(113, 141, 47, 30), "6"))
                AppendNumber("6");
            if (GUI.Button(new Rect(8, 106, 47, 30), "7"))
                AppendNumber("7");
            if (GUI.Button(new Rect(61, 106, 47, 30), "8"))
                AppendNumber("8");
            if (GUI.Button(new Rect(113, 106, 47, 30), "9"))
                AppendNumber("9");
            if (GUI.Button(new Rect(8, 212, 100, 30), "0"))
                AppendNumber("0");

            if (GUI.Button(new Rect(218, 141, 40, 30), "π"))
            {
                if (_isfirst)
                {
                    ClearCalcData();
                    AppendNumber(Math.PI.ToString());
                }
                else
                {
                    AppendNumber(Math.PI.ToString());
                }
            }
            if (GUI.Button(new Rect(260, 141, 40, 30), "e"))
            {
                if (_isfirst)
                {
                    ClearCalcData();
                    AppendNumber(Math.E.ToString());
                }
                else
                {
                    AppendNumber(Math.E.ToString());
                }
            }

            if (GUI.Button(new Rect(113, 212, 47, 30), "."))
            {
                if (!_currentnumber.Contains(".") || _clearscreen)
                    AppendNumber(".");
            }

            if (GUI.Button(new Rect(165, 176, 47, 66), "="))
            {
                PerformOperation();
            }

            if (GUI.Button(new Rect(2f, 2f, 13f, 13f), "X"))
            {
                Toggle();
            }
            GUI.DragWindow();
        }

        private void OperatorPressed(string op)
        {
            StoreCurrentNumberInReg(0);
            _isfirst = false;
            _clearscreen = true;
            _optoperform = op;
        }

        private void ClearCalcData()
        {
            _isfirst = true;
            _clearscreen = true;
            _optoperform = "";
            _currentnumber = "0";
            for (int i = 0; i < _registers.Length; i++)
                _registers[i] = 0;
        }

        private void PerformOperation()
        {
            switch (_optoperform)
            {
                case "+":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] + _registers[1]).ToString();
                    break;
                case "-":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] - _registers[1]).ToString();
                    break;
                case "x":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] * _registers[1]).ToString();
                    break;
                case "/":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[1] != 0) ? (_registers[0] / _registers[1]).ToString() : "NaN";
                    break;
                case "√x":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Sqrt(_registers[0])).ToString() : "NaN";
                    break;
                case "x^2":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Pow((_registers[0]), 2)).ToString() : "NaN";
                    break;
                case "1/x":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (1 / (_registers[0])).ToString() : "NaN";
                    break;
                case "x^y":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Pow(Convert.ToDouble(_registers[0]), Convert.ToDouble(_registers[1])).ToString()) : "NaN";
                    break;
                case "y√x":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Pow(Convert.ToDouble(_registers[0]), 1 / Convert.ToDouble(_registers[1])).ToString()) : "NaN";
                    break;
                case "x^3":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Pow(Convert.ToDouble(_registers[0]), 3)).ToString() : "NaN";
                    break;
                case "3√x":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Pow(Convert.ToDouble(_registers[0]), 1 / 3.0)).ToString() : "NaN";
                    break;
                case "%":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (_registers[0] * _registers[1] / 100).ToString() : "NaN";
                    break;
                case "log":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Log10(_registers[0])).ToString() : "NaN";
                    break;
                case "ln":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Log(_registers[0])).ToString() : "NaN";
                    break;
                case "10^x":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Pow(10, (_registers[0]))).ToString() : "NaN";
                    break;
                case "e^x":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Pow(Math.E, (_registers[0]))).ToString() : "NaN";
                    break;
                case "sin":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Sin((_registers[0]) * (Math.PI / 180))).ToString() : "NaN";
                    break;
                case "cos":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Cos((_registers[0]) * (Math.PI / 180))).ToString() : "NaN";
                    break;
                case "tan":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Tan((_registers[0]) * (Math.PI / 180))).ToString() : "NaN";
                    break;
                case "sinh":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Sinh((_registers[0]) * (Math.PI / 180))).ToString() : "NaN";
                    break;
                case "cosh":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Cosh((_registers[0]) * (Math.PI / 180))).ToString() : "NaN";
                    break;
                case "tanh":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Tanh((_registers[0]) * (Math.PI / 180))).ToString() : "NaN";
                    break;
                case "asin":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Asin(_registers[0]) * (180 / Math.PI)).ToString() : "NaN";
                    break;
                case "acos":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Acos(_registers[0]) * (180 / Math.PI)).ToString() : "NaN";
                    break;
                case "atan":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (Math.Atan(_registers[0]) * (180 / Math.PI)).ToString() : "NaN";
                    break;
                case "!":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (MathHelper.fact(_registers[0])).ToString() : "NaN";
                    break;
                case "cot":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (MathHelper.Cotan((_registers[0]) * (Math.PI / 180))).ToString() : "NaN";
                    break;
                case "sec":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (MathHelper.Sec((_registers[0]) * (Math.PI / 180))).ToString() : "NaN";
                    break;
                case "csc":
                    if (_currentnumber != "NaN")
                        _currentnumber = (_registers[0] != 0) ? (MathHelper.Cosec((_registers[0]) * (Math.PI / 180))).ToString() : "NaN";
                    break;
                case "":
                    break;
                default:
                    Debug.LogError("Unknown operation: " + _optoperform);
                    break;
                    
            }
            StoreCurrentNumberInReg(0);
            _isfirst = true;
            _clearscreen = true;
        }

        private void StoreCurrentNumberInReg(int regNumber)
        {
            _registers[regNumber] = float.Parse(_currentnumber, CultureInfo.InvariantCulture.NumberFormat);
        }

        private void AppendNumber(string s)
        {
            if ((_currentnumber == "0") || _clearscreen)
                _currentnumber = (s == ".") ? "0." : s;
            else
                if (_currentnumber.Length < _maxdigits)
                    _currentnumber += s;

            if (_clearscreen)
                _clearscreen = false;
            StoreCurrentNumberInReg(_isfirst ? 0 : 1);
        }

        private void GetInputFromCalc()
        {
            if (Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Alpha0))
                AppendNumber("0");
            if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1))
                AppendNumber("1");
            if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
                AppendNumber("2");
            if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3))
                AppendNumber("3");
            if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Alpha4))
                AppendNumber("4");
            if (Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Alpha5))
                AppendNumber("5");
            if (Input.GetKeyDown(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.Alpha6))
                AppendNumber("6");
            if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.Alpha7))
                AppendNumber("7");
            if (Input.GetKeyDown(KeyCode.Keypad8) || Input.GetKeyDown(KeyCode.Alpha8))
                AppendNumber("8");
            if (Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.Alpha9))
                AppendNumber("9");

            if (Input.GetKeyDown(KeyCode.C))
                ClearCalcData();

            if (Input.GetKeyDown(KeyCode.KeypadPeriod) || Input.GetKeyDown(KeyCode.Period))
                if (!_currentnumber.Contains(".") || _clearscreen)
                    AppendNumber(".");

            if (Input.GetKeyDown(KeyCode.KeypadDivide) || Input.GetKeyDown(KeyCode.Slash))
                OperatorPressed("/");
            if (Input.GetKeyDown(KeyCode.KeypadMultiply) || Input.GetKeyDown(KeyCode.Asterisk))
                OperatorPressed("*");
            if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Plus))
                OperatorPressed("+");
            if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
                OperatorPressed("-");

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                PerformOperation();
        }
        
        private void LoadSettings()
        {
            KSPLog.print("[kalculator.dll] Loading Config...");
            KSP.IO.PluginConfiguration configfile = KSP.IO.PluginConfiguration.CreateForType<Kalculator>();
            configfile.load();

            _calcsize = configfile.GetValue<Rect>("windowpos");
            _keybind = configfile.GetValue<string>("keybind");
            _versionlastrun = configfile.GetValue<string>("version");
            
            KSPLog.print("[kalculator.dll] Config Loaded Successfully");
        }

        private void SaveSettings()
        {
            KSPLog.print("[kalculator.dll] Saving Config...");
            KSP.IO.PluginConfiguration configfile = KSP.IO.PluginConfiguration.CreateForType<Kalculator>();

            configfile.SetValue("windowpos", _calcsize);
            configfile.SetValue("keybind", _keybind);
            configfile.SetValue("version", _version);
            
            configfile.save();
            KSPLog.print("[kalculator.dll] Config Saved ");
        }
        
        private void CheckDefaults()
        {
            if (_calcsize == new Rect(0, 0, 0, 0))
            {
                _calcsize = new Rect(360, 20, 308, 358);
            }
            if (_keybind == null)
            {
                _keybind = "k";
            }
        }
        
        private void Toggle()
        {
            if (_visible == true)
            {
                _visible = false;
                _button.TexturePath = _btexture_off;
                _button.ToolTip = _tooltipoff;
            }
            else
            {
                _visible = true;
                _button.TexturePath = _btexture_on;
                _button.ToolTip = _tooltipon;
            }
        }

        private void VersionCheck()
        {
            _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            KSPLog.print("kalculator.dll version: " + _version);
            if ((_version != _versionlastrun) && (KSP.IO.File.Exists<Kalculator>("config.xml")))
            {
                KSP.IO.File.Delete<Kalculator>("config.xml");
            }
        }

        private void LoadVersion()
        {
            KSP.IO.PluginConfiguration configfile = KSP.IO.PluginConfiguration.CreateForType<Kalculator>();
            configfile.load();
            _versionlastrun = configfile.GetValue<string>("version");
        }
    }
}