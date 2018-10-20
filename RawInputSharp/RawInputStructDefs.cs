using System;
using System.Runtime.InteropServices;

namespace RawInputSharp {
/*
 * This file includes all the struct definitions needed for the P/Invoke calls. I'm hardly a P/Invoke expert,
 * so a lot of the data types I picked probably don't make any practical sense -- I'm casting a lot. 
 * (uint when it should be int, etc.). Feel free to change it. :)
 * 
 * Useful reference sources for mapping P/Invoke datatypes
 * http://www.kobashicomputing.com/mapping-data-types-c-c-net
 * http://www.codeproject.com/Articles/17123/Using-Raw-Input-from-C-to-handle-multiple-keyboard
 * https://social.msdn.microsoft.com/Forums/en-US/2b7e3955-4fd8-4f15-b11f-0a4947f4a009/lacking-hid-device-support-under-vista-bizarre-structure-alignment-after-marshalptrtostructure?forum=netfx64bit discusses problems in above
 */

    // See https://msdn.microsoft.com/en-us/library/windows/desktop/ms645536%28v=vs.85%29.aspx
	public struct RAWINPUTDEVICELIST {
		public IntPtr hDevice; // A handle to the raw input device
		public Int32 dwType; // The type of device
	}
	[StructLayout(LayoutKind.Explicit)]
	public struct RID_DEVICE_INFO {
		[FieldOffset(0)] public uint cbSize;
		[FieldOffset(4)] public uint dwType;
		[FieldOffset(8)] public RID_DEVICE_INFO_MOUSE mouse;
		[FieldOffset(8)] public RID_DEVICE_INFO_KEYBOARD keyboard;
		[FieldOffset(8)] public RID_DEVICE_INFO_HID hid;
	}
	[StructLayout(LayoutKind.Explicit)]
	public struct RID_DEVICE_INFO_MOUSE {
		[FieldOffset(0)] public uint dwId; 
		[FieldOffset(4)] public uint dwNumberOfButtons; 
		[FieldOffset(8)] public uint dwSampleRate;
        [FieldOffset(12)] public bool fHasHorizontalWheel; 
	}
	[StructLayout(LayoutKind.Explicit)]
	public struct RID_DEVICE_INFO_KEYBOARD {
		[FieldOffset(0)] public uint dwType; 
		[FieldOffset(4)] public uint dwSubType; 
		[FieldOffset(8)] public uint dwKeyboardMode; 
		[FieldOffset(12)] public uint dwNumberOfFunctionKeys; 
		[FieldOffset(16)] public uint dwNumberOfIndicators; 
		[FieldOffset(20)] public uint dwNumberOfKeysTotal; 
	}
	[StructLayout(LayoutKind.Explicit)]
	public struct RID_DEVICE_INFO_HID {
		[FieldOffset(0)] public uint dwVendorId; 
		[FieldOffset(4)] public uint dwProductId; 
		[FieldOffset(8)] public uint dwVersionNumber; 
		[FieldOffset(12)] public ushort usUsagePage; 
		[FieldOffset(14)] public ushort usUsage; 
	}
	[StructLayout(LayoutKind.Explicit)]
	public struct RAWINPUTDEVICE {
		[FieldOffset(0)] public ushort usUsagePage; //Toplevel collection UsagePage
		[FieldOffset(2)] public ushort usUsage; //Toplevel collection Usage
		[FieldOffset(4)] public uint dwFlags; 
		[FieldOffset(8)] public IntPtr hwndTarget; // Target hwnd. NULL = follows keyboard focus //AA Changed from uint
	}
    
    // Contains the raw input from a device, made up of a header and data section
    // See https://msdn.microsoft.com/en-us/library/windows/desktop/ms645562(v=vs.85).aspx
    // Use sequential layout rather than explicit, since size of RAWINPUTHEADER varies from x86 to x64 builds
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUT {
        public RAWINPUTHEADER header;
        public RAWDATA data;
    }
    
    // The "header" section of the RAWINPUT
    // Note that the size of this header is 16 bytes on x86, but 24 bytes on x64 (as IntPtr variables are 4 bytes on x86, 8 bytes on x64)
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER {
        public uint dwType;        // Type of raw input (RIM_TYPEHID 2, RIM_TYPEKEYBOARD 1, RIM_TYPEMOUSE 0)
        public uint dwSize;        // Size in bytes of the entire input packet of data. This includes RAWINPUT plus possible extra input reports in the RAWHID variable length array. 
        public IntPtr hDevice;     // A handle to the device generating the raw input data. 
        public IntPtr wParam;      // RIM_INPUT 0 if input occurred while application was in the foreground else RIM_INPUTSINK 1 if it was not.

        public override string ToString() {
            return string.Format("RawInputHeader\n dwType : {0}\n dwSize : {1}\n hDevice : {2}\n wParam : {3}", dwType, dwSize, hDevice, wParam);
        }
    }

    // The "data" section of the RAWINPUT
    // https://msdn.microsoft.com/en-us/library/windows/desktop/ms645562(v=vs.85).aspx
    // Note that data is a "union" from either mouse, keyboard, or hid, so use Explicit layout and write them all to the same FieldOffset
    [StructLayout(LayoutKind.Explicit)]
    public struct RAWDATA {
        [FieldOffset(0)] internal RAWMOUSE mouse;
        [FieldOffset(0)] internal RAWKEYBOARD keyboard;
        [FieldOffset(0)] internal RAWHID hid;
    }

	/// <summary>
	/// I had to play with the layout of this one quite a bit. The usFlags field is listed as a USHORT in winuser.h.
	/// Changing it to a uint makes all the fields line up properly for the WM_INPUT messages.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct RAWMOUSE {
		[FieldOffset(0)] public uint usFlags; //indicator flags   // USHORT = ushort 16bits
		[FieldOffset(4)] public ushort usButtonFlags;
		[FieldOffset(6)] public ushort usButtonData;
		[FieldOffset(8)] public uint ulRawButtons; //The raw state of the mouse buttons
		[FieldOffset(12)] public int lLastX; //The signed relative or absolute motion in the X direction.
		[FieldOffset(16)] public int lLastY; //The signed relative or absolute motion in the Y direction.
		[FieldOffset(20)] public uint ulExtraInformation; //Device-specific additional information for the event.
	}  
	[StructLayout(LayoutKind.Explicit)]
	public struct RAWKEYBOARD {
		[FieldOffset(0)] public ushort MakeCode; //The "make" scan code (key depression).
		[FieldOffset(2)] public ushort Flags; //The flags field indicates a "break" (key release) and other miscellaneous scan code information defined in ntddkbd.h.
		[FieldOffset(4)] public ushort Reserved;
		[FieldOffset(6)] public ushort VKey; //Windows message compatible information
		[FieldOffset(8)] public uint Message; 
		[FieldOffset(12)] public uint ExtraInformation; //Device-specific additional information for the event.
	}
	[StructLayout(LayoutKind.Explicit)]
	public struct RAWHID {
		[FieldOffset(0)] uint dwSizeHid;    // byte size of each report
		[FieldOffset(4)] uint dwCount;      // number of input packed
		[FieldOffset(8)] byte bRawData; // winuser.h has this as BYTE bRawData[1]... should it be uint pbRawData then instead?
	}
}