﻿using System;
using System.Management;
using UpdatesClient.Core;

namespace Security
{
    public class SystemFunctions
    {
        public static string GetHWID()
        {
            try
            {
                ManagementObjectCollection mbsList = new ManagementObjectSearcher("Select ProcessorId From Win32_processor").Get();
                foreach (ManagementObject mo in mbsList)
                {
                    return mo["ProcessorId"].ToString();
                }
            }
            catch (Exception e) { Logger.Error("CheckHWID", e); }
            return "UnsafeEnvironment";
        }
    }
}