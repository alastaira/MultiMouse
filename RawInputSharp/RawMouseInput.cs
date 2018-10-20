using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;

namespace RawInputSharp {

	/// <summary>
	/// Handles raw mouse input. Ignores the system mouse (handle == 0) and the RDP mouse. This uses
	/// the same means to identify the RDP mouse as raw_mouse.c, and those caveats apply. 
	/// </summary>
	public class RawMouseInput : RawInput{

		private ArrayList _mice;

        /// <summary>
		/// Returns an ArrayList of RawMouse objects. The system mouse is not included.
		/// </summary>
		public ArrayList Mice {
			get {
				return _mice;
			}
		}

		public RawMouseInput() : base() {
			GetRawInputMice();
		}        
        
		/// <summary>
		/// Gets all the raw mice and initializes the Mice property.
		/// </summary>
		private void GetRawInputMice() {
			_mice = new ArrayList();

			foreach(RAWINPUTDEVICELIST d in Devices) {
				//skip everything but mice.
				if(d.dwType != RIM_TYPEMOUSE) {
					continue;
				}

				// Call GetRawInputDeviceInfo once with IntPtr.Zero to allocate enough memory to pcbSize buffer to store RIDI_DEVICENAME
				Int32 pcbSize = 0;
				GetRawInputDeviceInfo(d.hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
                
                // Double the buffer size because sometimes the application is Unicode
                IntPtr sb = Marshal.AllocHGlobal((pcbSize*2)+2);
                
				// Now call GetRawInputDeviceInfo again, this time to populate the buffer with RIDI_DEVICENAME
				GetRawInputDeviceInfo(d.hDevice, RIDI_DEVICENAME, sb, ref pcbSize);
                
                // Store the device name in a stringbuilder
                StringBuilder sbb = new StringBuilder();
                string aa = Marshal.PtrToStringAuto(sb);
                char[] ab = aa.ToCharArray();
                Marshal.FreeHGlobal(sb);
                sbb.Append(ab);
                
                // Skip windows terminal (rdp) mouse
				if(sbb.ToString().IndexOf(@"\\?\Root#RDP_MOU#0000#") < 0) {
                
					// Get size of RID_DEVICE_INFO struct
					GetRawInputDeviceInfo(d.hDevice, RIDI_DEVICEINFO, IntPtr.Zero, ref pcbSize);

					// Populate the mouseInfo struct with information about this mouse
					RID_DEVICE_INFO mouseInfo = new RID_DEVICE_INFO();
					mouseInfo.cbSize = (uint)Marshal.SizeOf(typeof(RID_DEVICE_INFO));
					GetRawInputDeviceInfo(d.hDevice, RIDI_DEVICEINFO, ref mouseInfo, ref pcbSize);

					// Create a RawMouse instance for this device and add to the _mice array
					RawMouse mouse = new RawMouse(d.hDevice, (int)mouseInfo.mouse.dwNumberOfButtons, sbb.ToString());
					_mice.Add(mouse);
				}
                
			}
		}
        
        // Receive input even when not in the foreground
        // See https://msdn.microsoft.com/en-us/library/windows/desktop/ms645565(v=vs.85).aspx
        private const int RIDEV_INPUTSINK = 0x00000100;
        
		/// <summary>
		/// Registers the application to receive WM_INPUT messages for mice.
		/// </summary>
		/// <param name="hwndTarget">The application's hwnd.</param>
		public void RegisterForWM_INPUT(IntPtr hwndTarget) {
        
            // See https://msdn.microsoft.com/en-us/library/windows/desktop/ms645565(v=vs.85).aspx
			RAWINPUTDEVICE rid = new RAWINPUTDEVICE();
			rid.usUsagePage = 0x01;
			rid.usUsage = 0x02; // Mouse
            rid.dwFlags = RIDEV_INPUTSINK;
            rid.hwndTarget = hwndTarget;

			// Supposed to be a pointer to an array, we're only registering one device though
			IntPtr pRawInputDeviceArray = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
			Marshal.StructureToPtr(rid, pRawInputDeviceArray, true);
            uint retval = RegisterRawInputDevices(pRawInputDeviceArray, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
			Marshal.FreeHGlobal(pRawInputDeviceArray);
		}

		/// <summary>
		/// Updates the status of the mouse given a raw mouse handle
		/// </summary>
		/// <param name="dHandle"></param>
		public void UpdateRawMouse(IntPtr dHandle) {
			//Int32 hRawInput = dHandle.ToInt32();
			Int32 pcbSize = 0;

			// Call GetRawInputData with IntPtr.Zero to populate pcbSize with the size of the buffer required for the raw input
			Int32 retval = GetRawInputData(dHandle, RawInput.RID_INPUT, IntPtr.Zero, ref pcbSize, Marshal.SizeOf(typeof(RAWINPUTHEADER)));

			// Now call GetRawInputData again, but pass the ri RAWINPUT structure to be populated with data from the device
			RAWINPUT ri = new RAWINPUT();
			retval = GetRawInputData(dHandle, RawInput.RID_INPUT, ref ri, ref pcbSize, Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            // Loop through the arrayList of Mice and update the appropriate mouse with the new data received
            foreach(RawMouse mouse in Mice) {
                if (mouse.Handle == ri.header.hDevice)
                {
                    //Console.WriteLine("usflags: " + ri.data.mouse.usFlags + " button data: " + ri.data.mouse.usButtonData);
                    
					// Relative mouse movement
                    mouse.X += ri.data.mouse.lLastX;
                    mouse.Y += ri.data.mouse.lLastY;

					// Mouse buttons
                    if ((ri.data.mouse.usButtonFlags & RI_MOUSE_BUTTON_1_DOWN) > 0) mouse.Buttons[0] = true;
                    if ((ri.data.mouse.usButtonFlags & RI_MOUSE_BUTTON_1_UP) > 0) mouse.Buttons[0] = false;
                    if ((ri.data.mouse.usButtonFlags & RI_MOUSE_BUTTON_2_DOWN) > 0) mouse.Buttons[1] = true;
                    if ((ri.data.mouse.usButtonFlags & RI_MOUSE_BUTTON_2_UP) > 0) mouse.Buttons[1] = false;
                    if ((ri.data.mouse.usButtonFlags & RI_MOUSE_BUTTON_3_DOWN) > 0) mouse.Buttons[2] = true;
                    if ((ri.data.mouse.usButtonFlags & RI_MOUSE_BUTTON_3_UP) > 0) mouse.Buttons[2] = false;

					// Scroll wheel
                    if ((ri.data.mouse.usButtonFlags & RI_MOUSE_WHEEL) > 0)
                    {
                        if ((short)ri.data.mouse.usButtonData > 0)
                        {
							mouse.Z++;
						}
                        if ((short)ri.data.mouse.usButtonData < 0)
                        {
							mouse.Z--;
						}
					}
				}
			}
		}
	}
}
