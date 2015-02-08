﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MouseControl
{
    public class Edid
    {
        [DllImport("setupapi.dll")]
        internal static extern IntPtr SetupDiGetClassDevsEx(ref Guid ClassGuid,
            [MarshalAs(UnmanagedType.LPStr)]String enumerator,
            IntPtr hwndParent, Int32 Flags, IntPtr DeviceInfoSet,
            [MarshalAs(UnmanagedType.LPStr)]String MachineName, IntPtr Reserved);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList
        (
             IntPtr DeviceInfoSet
        );

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("Setupapi", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SetupDiOpenDevRegKey(
            IntPtr hDeviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            int scope,
            int hwProfile,
            int parameterRegistryValueKind,
            int samDesired);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern uint RegEnumValue(
              IntPtr hKey,
              uint dwIndex,
              StringBuilder lpValueName,
              ref uint lpcValueName,
              IntPtr lpReserved,
              ref UInt32 lpType,
              IntPtr lpData,
              ref UInt32 lpcbData);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCloseKey(
            IntPtr hKey);


        const int DIGCF_PRESENT = 0x2;
        const int DICS_FLAG_GLOBAL = 0x1;
        const int DIREG_DEV = 0x1;

        const int KEY_READ = 0x20019;

        static Guid GUID_CLASS_MONITOR = new Guid(0x4d36e96e, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18 );

        const uint ERROR_NO_MORE_ITEMS = 259;
        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid classGuid;
            public uint devInst;
            public IntPtr reserved;
        }

        private const Int32 NAME_SIZE = 128;
        private const UInt32 ERROR_SUCCESS = 0;

        static public Size GetMonitorSizeFromEDID(IntPtr hDevRegKey)
        {
            UInt32 dwType = NAME_SIZE;
            UInt32 AcutalValueNameLength = NAME_SIZE;

            StringBuilder valueName=new StringBuilder(NAME_SIZE);

            byte[] EDIDdata = new byte[1024];
            UInt32 edidsize = 1024;

            GCHandle pinnedArray = GCHandle.Alloc(EDIDdata, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();

            for (uint i = 0, retValue = ERROR_SUCCESS; retValue != ERROR_NO_MORE_ITEMS; ++i)
            {
                retValue = RegEnumValue(hDevRegKey, i, valueName,
                    ref AcutalValueNameLength, IntPtr.Zero, ref dwType,
                    pointer, // buffer
                    ref edidsize); // buffer size

                if (retValue != ERROR_SUCCESS || valueName.ToString() != "EDID")
                    continue;

                int w = ((EDIDdata[68] & 0xF0) << 4) + EDIDdata[66];
                int h = ((EDIDdata[68] & 0x0F) << 8) + EDIDdata[67];

                return new Size(w,h); // valid EDID found
            }

            return new Size(); // EDID not found
        }

        public static Size GetSizeForDevID(String TargetDevID)
    {
        IntPtr devInfo = SetupDiGetClassDevsEx(
        ref GUID_CLASS_MONITOR, //class GUID
        null, //enumerator
        IntPtr.Zero, //HWND
        DIGCF_PRESENT, // Flags //DIGCF_ALLCLASSES|
        IntPtr.Zero, // device info, create a new one.
        null, // machine name, local machine
        IntPtr.Zero);// reserved

        if (devInfo == IntPtr.Zero)
            return new Size(0, 0);

        Size size = new Size();

        for (uint i = 0; ERROR_NO_MORE_ITEMS != GetLastError(); ++i)
        {

            SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
            //memset(&devInfoData, 0, sizeof(devInfoData));
            devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);

            if (SetupDiEnumDeviceInfo(devInfo, i, ref devInfoData))
            {
                    IntPtr hDevRegKey = SetupDiOpenDevRegKey(devInfo, ref devInfoData,
                    DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_READ);

                if (hDevRegKey==IntPtr.Zero || (hDevRegKey.ToInt32() == -1))
                    continue;

                size = GetMonitorSizeFromEDID(hDevRegKey);

                RegCloseKey(hDevRegKey);
            }
        }
        SetupDiDestroyDeviceInfoList(devInfo);
        return size;
    }
}


}



/*
# include <atlstr.h>
# include <SetupApi.h>
#pragma comment(lib, "setupapi.lib")

#define NAME_SIZE 128

const GUID GUID_CLASS_MONITOR = { 0x4d36e96e, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18 };

// Assumes hDevRegKey is valid
bool GetMonitorSizeFromEDID(const HKEY hDevRegKey, short& WidthMm, short& HeightMm)
{
    DWORD dwType, AcutalValueNameLength = NAME_SIZE;
    TCHAR valueName[NAME_SIZE];

    BYTE EDIDdata[1024];
    DWORD edidsize = sizeof(EDIDdata);

    for (LONG i = 0, retValue = ERROR_SUCCESS; retValue != ERROR_NO_MORE_ITEMS; ++i)
    {
        retValue = RegEnumValue(hDevRegKey, i, &valueName[0],
            &AcutalValueNameLength, NULL, &dwType,
            EDIDdata, // buffer
            &edidsize); // buffer size

        if (retValue != ERROR_SUCCESS || 0 != _tcscmp(valueName, _T("EDID")))
            continue;

        WidthMm = ((EDIDdata[68] & 0xF0) << 4) + EDIDdata[66];
        HeightMm = ((EDIDdata[68] & 0x0F) << 8) + EDIDdata[67];

        return true; // valid EDID found
    }

    return false; // EDID not found
}

bool GetSizeForDevID(const CString& TargetDevID, short& WidthMm, short& HeightMm)
{
    HDEVINFO devInfo = SetupDiGetClassDevsEx(
        &GUID_CLASS_MONITOR, //class GUID
        NULL, //enumerator
        NULL, //HWND
        DIGCF_PRESENT, // Flags //DIGCF_ALLCLASSES|
        NULL, // device info, create a new one.
        NULL, // machine name, local machine
        NULL);// reserved

    if (NULL == devInfo)
        return false;

    bool bRes = false;

    for (ULONG i = 0; ERROR_NO_MORE_ITEMS != GetLastError(); ++i)
    {
        SP_DEVINFO_DATA devInfoData;
        memset(&devInfoData, 0, sizeof(devInfoData));
        devInfoData.cbSize = sizeof(devInfoData);

        if (SetupDiEnumDeviceInfo(devInfo, i, &devInfoData))
        {
            HKEY hDevRegKey = SetupDiOpenDevRegKey(devInfo, &devInfoData,
                DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_READ);

            if (!hDevRegKey || (hDevRegKey == INVALID_HANDLE_VALUE))
                continue;

            bRes = GetMonitorSizeFromEDID(hDevRegKey, WidthMm, HeightMm);

            RegCloseKey(hDevRegKey);
        }
    }
    SetupDiDestroyDeviceInfoList(devInfo);
    return bRes;
}

int _tmain(int argc, _TCHAR* argv[])
{
    short WidthMm, HeightMm;

    DISPLAY_DEVICE dd;
    dd.cb = sizeof(dd);
    DWORD dev = 0; // device index
    int id = 1; // monitor number, as used by Display Properties > Settings

    CString DeviceID;
    bool bFoundDevice = false;
    while (EnumDisplayDevices(0, dev, &dd, 0) && !bFoundDevice)
    {
        DISPLAY_DEVICE ddMon;
        ZeroMemory(&ddMon, sizeof(ddMon));
        ddMon.cb = sizeof(ddMon);
        DWORD devMon = 0;

        while (EnumDisplayDevices(dd.DeviceName, devMon, &ddMon, 0) && !bFoundDevice)
        {
            if (ddMon.StateFlags & DISPLAY_DEVICE_ACTIVE &&
                !(ddMon.StateFlags & DISPLAY_DEVICE_MIRRORING_DRIVER))
            {
                DeviceID.Format(L"%s", ddMon.DeviceID);
                DeviceID = DeviceID.Mid(8, DeviceID.Find(L"\\", 9) - 8);

                bFoundDevice = GetSizeForDevID(DeviceID, WidthMm, HeightMm);
            }
            devMon++;

            ZeroMemory(&ddMon, sizeof(ddMon));
            ddMon.cb = sizeof(ddMon);
        }

        ZeroMemory(&dd, sizeof(dd));
        dd.cb = sizeof(dd);
        dev++;
    }

    return 0;
}
*/