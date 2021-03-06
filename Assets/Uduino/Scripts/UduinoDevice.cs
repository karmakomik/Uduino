﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uduino
{
    public class UduinoDevice : SerialArduino
    {
        public string name {
            get
            {
                return _name;
            } set
            {
                if (_name == "")
                    _name = value;
            }
        }
        private string _name = "";

        public bool continuousRead = false;
        public string read = null;

        public string lastRead = null;
        public string lastWrite = null;

        public System.Action<string> callback = null;

        private Dictionary<string, List<string>> bundles = new Dictionary<string, List<string>>();
         
        public UduinoDevice(string port, int baudrate = 9600, int readTimeout = 100, int writeTimeout= 100 ) : base(port, baudrate)
        {
            this.readTimeout = readTimeout;
            this.writeTimeout = writeTimeout;
        }

        /// <summary>
        /// Add a message to the bundle
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="bundle">Bundle Name</param>
        public void AddToBundle( string message , string bundle)
        {
            List<string> existing;
            if (!bundles.TryGetValue(bundle, out existing))
            {
                existing = new List<string>();
                bundles[bundle] = existing;
            }
            existing.Add("," + message);
          //  Log.Debug("Message <color=#4CAF50>" + message + "</color> added to the bundle " + bundle);
        }

        /// <summary>
        /// Send a Bundle to the arduino
        /// </summary>
        /// TODO : Max Length, matching avec arduino
        /// <param name="bundleName">Name of the bundle to send</param>
        public void SendBundle(string bundleName)
        {
            List<string> bundleValues;
            if (bundles.TryGetValue(bundleName, out bundleValues))
            {
                string fullMessage = "b " + bundleValues.Count;

                if (bundleValues.Count == 1 ) // If there is one message
                {
                    string message = bundleValues[0].Substring(1, bundleValues[0].Length - 1);
                    if (message.Contains("r")) ReadFromArduino(message);
                    else WriteToArduino(message);
                    return;
                }

                for (int i = 0; i < bundleValues.Count; i++)
                    fullMessage += bundleValues[i];

                if (fullMessage.Contains("r")) ReadFromArduino(fullMessage);
                else WriteToArduino(fullMessage);

                if (fullMessage.Length >= 120)
                    Log.Warning("The bundle message is too big. Try to not send too many messages or increase UDUINOBUFFER in Uduino library.");

                bundles.Remove(bundleName);
            }
            else
            {
                Log.Info("You are tring to send the bundle \"" + bundleName + "\" but it seems that it's empty.");
            }
        }

        public void SendAllBundles()
        {
            Log.Debug("Send all bundles");
            List<string> bundleNames = new List<string>(bundles.Keys);
            foreach (string key in bundleNames)
                SendBundle(key);
        }

        public override void WritingSuccess(string message)
        {
            lastWrite = message;
        }

        public override void ReadingSuccess(string message)
        {
            lastRead = message;
            if (UduinoManager.Instance)
            {
                UduinoManager.Instance.InvokeAsync(() =>
                {
                    if (callback != null) callback(message);
                    UduinoManager.Instance.TriggerEvent(message, _name);
                });
            } else if(!Application.isPlaying && callback != null) //if it's the editor
                callback(message);
        }
    }
}