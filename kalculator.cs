// -------------------------------------------------------------------------------------------------
// kalculator.cs 0.2.2
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
        private readonly float[] _registers = new float[2];
        private string _currentnumber = "0";
        private string _mem = "0";
        private const int _displayfontsize = 24;
        private const int _opfontsize = 15;
        private string _optoperform = "";
        private const int _maxdigits = 16;
        private bool _isfirst = true;
        private bool _clearscreen;
        private bool _visible;
        private string _version;
        private string _versionlastrun;
        private IButton _button;
        private bool _useKspSkin;
        private const string _tooltipOn = "Hide Kalculator";
        private const string _tooltipOff = "Show Kalculator";
        private const string _btextureOn = "Kalculator/Textures/icon_on";
        private const string _btextureOff = "Kalculator/Textures/icon_off";
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
            info += "Register to use: " + (_isfirst ? "0" : "1") + "\n";
            info += "MR: " + _mem;

            return info;
        }
#endif
        void Awake()
        {
            LoadVersion();
            VersionCheck();
            LoadSettings();
        }

        void Start()
        {
            if (!ToolbarManager.ToolbarAvailable) return;
            _button = ToolbarManager.Instance.add("kalculator", "toggle");
            _button.TexturePath = _btextureOff;
            _button.ToolTip = _tooltipOff;
            _button.OnClick += e => Toggle();

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
            GUISkin _defGuiSkin = GUI.skin;
            if (_visible)
            {
                GUI.skin = _useKspSkin ? HighLogic.Skin : _defGuiSkin;
                _calcsize = GUI.Window(GUIUtility.GetControlID(0, FocusType.Passive), _calcsize, CalcWindow, "Kalculator");
            }
            GUI.skin = _defGuiSkin;
        }

        void CalcWindow(int windowID)
        {
            GUI.SetNextControlName("kalculator");
            int tmpSize = GUI.skin.GetStyle("Label").fontSize;
            GUI.skin.GetStyle("TextField").fontSize = _displayfontsize;
            GUILayout.TextField(_currentnumber);
            GUI.skin.GetStyle("TextField").fontSize = tmpSize;
            tmpSize = GUI.skin.GetStyle("Label").fontSize;
            GUI.skin.GetStyle("Label").fontSize = _opfontsize;
            GUILayout.Label(_optoperform);
            GUI.skin.GetStyle("Label").fontSize = tmpSize;
            GUILayout.BeginVertical();
            var _butttonOpts = new[] { GUILayout.Width(47f), GUILayout.ExpandWidth(false), GUILayout.Height(30f), GUILayout.ExpandHeight(false) };
            var _butttonOpts2 = new[] { GUILayout.Width(40f), GUILayout.ExpandWidth(false), GUILayout.Height(30f), GUILayout.ExpandHeight(false) };
            var _butzeroOpts = new[] { GUILayout.Width(98f), GUILayout.ExpandWidth(false), GUILayout.Height(30f), GUILayout.ExpandHeight(false) };
            var _butbackOpts = new[] { GUILayout.Width(84f), GUILayout.ExpandWidth(false), GUILayout.Height(30f), GUILayout.ExpandHeight(false) };

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("C", _butttonOpts))
            {
                ClearCalcData();
            }
            if (GUILayout.Button("+/-", _butttonOpts))
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
            if (GUILayout.Button("/", _butttonOpts))
                OperatorPressed("/");
            if (GUILayout.Button("x", _butttonOpts))
                OperatorPressed("x");
            if (GUILayout.Button("log", _butttonOpts2))
                OperatorPressed("log");
            if (GUILayout.Button("ln", _butttonOpts2))
                OperatorPressed("ln");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("7", _butttonOpts))
                AppendNumber("7");
            if (GUILayout.Button("8", _butttonOpts))
                AppendNumber("8");
            if (GUILayout.Button("9", _butttonOpts))
                AppendNumber("9");
            if (GUILayout.Button("-", _butttonOpts))
                OperatorPressed("-");
            if (GUILayout.Button("10^x", _butttonOpts2))
                OperatorPressed("10^x");
            if (GUILayout.Button("e^x", _butttonOpts2))
                OperatorPressed("e^x");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("4", _butttonOpts))
                AppendNumber("4");
            if (GUILayout.Button("5", _butttonOpts))
                AppendNumber("5");
            if (GUILayout.Button("6", _butttonOpts))
                AppendNumber("6");
            if (GUILayout.Button("+", _butttonOpts))
                OperatorPressed("+");
            if (GUILayout.Button("π", _butttonOpts2))
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
            if (GUILayout.Button("e", _butttonOpts2))
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
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1", _butttonOpts))
                AppendNumber("1");
            if (GUILayout.Button("2", _butttonOpts))
                AppendNumber("2");
            if (GUILayout.Button("3", _butttonOpts))
                AppendNumber("3");
            GUI.contentColor = Color.green;
            if (GUILayout.Button("=", _butttonOpts))
            {
                PerformOperation();
            }
            GUI.contentColor = Color.white;
            if (GUILayout.Button("sin", _butttonOpts2))
                OperatorPressed("sin");
            if (GUILayout.Button("cos", _butttonOpts2))
                OperatorPressed("cos");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0", _butzeroOpts))
                AppendNumber("0");
            if (GUILayout.Button(".", _butttonOpts))
            {
                if (!_currentnumber.Contains(".") || _clearscreen)
                    AppendNumber(".");
            }
            if (GUILayout.Button("acos", _butttonOpts))
                OperatorPressed("acos");
            if (GUILayout.Button("tan", _butttonOpts2))
                OperatorPressed("tan");
            if (GUILayout.Button("sinh", _butttonOpts2))
                OperatorPressed("sinh");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("√x", _butttonOpts))
                OperatorPressed("√x");
            if (GUILayout.Button("x^2", _butttonOpts))
                OperatorPressed("x^2");
            if (GUILayout.Button("1/x", _butttonOpts))
                OperatorPressed("1/x");
            if (GUILayout.Button("x^y", _butttonOpts))
                OperatorPressed("x^y");
            if (GUILayout.Button("cosh", _butttonOpts2))
                OperatorPressed("cosh");
            if (GUILayout.Button("tanh", _butttonOpts2))
                OperatorPressed("tanh");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("cot", _butttonOpts))
                OperatorPressed("cot");
            if (GUILayout.Button("sec", _butttonOpts))
                OperatorPressed("sec");

            if (GUILayout.Button("%", _butttonOpts))
                OperatorPressed("%");
            if (GUILayout.Button("y√x", _butttonOpts))
                OperatorPressed("y√x");
            if (GUILayout.Button("asin", _butttonOpts2))
                OperatorPressed("asin");
            if (GUILayout.Button("atan", _butttonOpts2))
                OperatorPressed("atan");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("MS", _butttonOpts))
                StoreMem();
            if (GUILayout.Button("MR", _butttonOpts))
                RestoreMem();
            if (GUILayout.Button("csc", _butttonOpts))
                OperatorPressed("csc");
            if (GUILayout.Button("!", _butttonOpts))
                OperatorPressed("!");
            if (GUILayout.Button("<--", _butbackOpts))
            {
                RemoveNumber();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if (GUI.Button(new Rect(20f, 2f, 22f, 16f), "S"))
            {
                _useKspSkin = !_useKspSkin;
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
                _currentnumber = s == "." ? "0." : s;
            else
                if (_currentnumber.Length < _maxdigits)
                    _currentnumber += s;

            if (_clearscreen)
                _clearscreen = false;
            StoreCurrentNumberInReg(_isfirst ? 0 : 1);
        }

        private void RemoveNumber()
        {
            _currentnumber = (_currentnumber.Length == 1) ? "0" : _currentnumber.Remove(_currentnumber.Length - 1);

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
            if (Input.GetKeyDown(KeyCode.Backspace))
                RemoveNumber();
        }

        private void LoadSettings()
        {
            KSPLog.print("[kalculator.dll] Loading Config...");
            KSP.IO.PluginConfiguration _configfile = KSP.IO.PluginConfiguration.CreateForType<Kalculator>();
            _configfile.load();

            _calcsize = _configfile.GetValue("windowpos", new Rect(360, 20, 308, 365));
            _keybind = _configfile.GetValue("keybind", "k");
            _versionlastrun = _configfile.GetValue<string>("version");
            _useKspSkin = _configfile.GetValue<bool>("KSPSkin", false);

            KSPLog.print("[kalculator.dll] Config Loaded Successfully");
        }

        private void SaveSettings()
        {
            KSPLog.print("[kalculator.dll] Saving Config...");
            KSP.IO.PluginConfiguration _configfile = KSP.IO.PluginConfiguration.CreateForType<Kalculator>();

            _configfile.SetValue("windowpos", _calcsize);
            _configfile.SetValue("keybind", _keybind);
            _configfile.SetValue("version", _version);
            _configfile.SetValue("KSPSkin", _useKspSkin);

            _configfile.save();
            print("[kalculator.dll] Config Saved ");
        }

        private void StoreMem()
        {
            _mem = _currentnumber;
        }

        private void RestoreMem()
        {
            _currentnumber = _mem;
            if (_clearscreen)
                _clearscreen = false;
            StoreCurrentNumberInReg(_isfirst ? 0 : 1);

        }

        private void Toggle()
        {
            if (_visible == true)
            {
                _visible = false;
                _button.TexturePath = _btextureOff;
                _button.ToolTip = _tooltipOff;
            }
            else
            {
                _visible = true;
                _button.TexturePath = _btextureOn;
                _button.ToolTip = _tooltipOn;
            }
        }

        private void VersionCheck()
        {
            _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            print("kalculator.dll version: " + _version);
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