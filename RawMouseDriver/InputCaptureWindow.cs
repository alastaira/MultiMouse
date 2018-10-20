using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RawInputSharp;

namespace RawMouseDriver {

    // Windows Raw Input API requires a window to listen to WM_INPUT messages
    // So we'll just create a basic form that starts in the minimised state
    public partial class InputCaptureWindow : Form {

        private RawMouseInput _rawinput;

        public InputCaptureWindow(RawMouseInput input) {
            InitializeComponent();
            _rawinput = input;
        }

		protected const int WM_INPUT = 0x00FF;
        protected override void WndProc(ref Message m) {
            switch (m.Msg) {
                case WM_INPUT:
                    // Do something here when an input is received, if desired
                    break;
            }
            base.WndProc(ref m);
        }


        private void InputCaptureWindow_Load(object sender, EventArgs e) {
            listView1.Clear();
            for (int i = 0; i < _rawinput.Mice.Count; i++) {
                listView1.Items.Add(String.Format("{0}", ((RawMouse)_rawinput.Mice[i]).Name));
            }
        }
    }
}
