using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PPt = Microsoft.Office.Interop.PowerPoint;

namespace PowerPointArtNet
{
    public partial class Form1 : Form
    {
        private ArtDotNet.ArtNetController Controller = null;
        private static int Universe = 0;
        private static int Channel = 1;
        private static bool Running = false;
        private static byte CurrentValue = 0;
        public Form1()
        {
            InitializeComponent();
        }

        private void IsEnabled_CheckStateChanged(object sender, EventArgs e)
        {
            Running = IsEnabled.Checked;

            if (Controller==null && Running)
            {
                IsEnabled.Enabled = false;
                IsEnabled.Text = "Connecting ...";
                NetworkControl.Enabled = false;
                DisconnectButton.Enabled = true;
                Application.DoEvents();
                Controller = new ArtDotNet.ArtNetController();
                Controller.Address = GetAddressesFromInterfaceType().ElementAt(NetworkControl.SelectedIndex);
                StartPowerPoint();
                Controller.DmxPacketReceived += (s, p) =>
                {
                    if (p.SubUni != Universe)
                        return;

                    byte NewValue = p.Data[Channel - 1];

                    if (NewValue == CurrentValue)
                        return;

                    this.Invoke(new Action(() => StatusControl.Text = NewValue.ToString()));
                    if ((int)NewValue==255)
                    {
                        try
                        {
                            NextSlide();
                        }
                        catch (Exception) { }
                    }
                    CurrentValue = NewValue;
                };
                Controller.Start();
                Task.Delay(1000);
                IsEnabled.Enabled = true;
                IsEnabled.Text = "Running";
            }

            IsEnabled.Text = Running ? "Running" : "Paused";
        }

        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            int i = (int)Math.Floor((sender as NumericUpDown).Value);
            if (i < 0 || i > 256)
                return;

            Universe = i;
        }

        private void NumericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            int i = (int)Math.Floor((sender as NumericUpDown).Value);
            if (i < 1 || i > 512)
                return;

            Channel = i;
        }

        private IEnumerable<IPAddress> GetAddressesFromInterfaceType(NetworkInterfaceType? interfaceType = null, Func<NetworkInterface, bool> predicate = null)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.SupportsMulticast && (!interfaceType.HasValue || adapter.NetworkInterfaceType == interfaceType) &&
                    adapter.OperationalStatus == OperationalStatus.Up)
                {
                    if (predicate != null)
                        if (!predicate(adapter))
                            continue;

                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();

                    foreach (var ipAddress in ipProperties.UnicastAddresses)
                    {
                        if (ipAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            yield return ipAddress.Address;
                    }
                }
            }
        }

        private void NetworkControl_DropDown(object sender, EventArgs e)
        {
            NetworkControl.Items.Clear();
            NetworkControl.Items.AddRange(GetAddressesFromInterfaceType().ToArray());
        }

        private void NetworkControl_SelectionChangeCommitted(object sender, EventArgs e)
        {
            IsEnabled.Enabled = true;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }







        // Define PowerPoint Application object
        PPt.Application pptApplication;
        // Define Presentation object
        PPt.Presentation presentation;
        // Define Slide collection
        PPt.Slides slides;
        PPt.Slide slide;

        // Slide count
        int slidescount;
        // slide index
        int slideIndex;
        private void StartPowerPoint()
        {
            try
            {
                // Get Running PowerPoint Application object
                pptApplication = Marshal.GetActiveObject("PowerPoint.Application") as PPt.Application;
            }
            catch
            {
                MessageBox.Show("Please Run PowerPoint First", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (pptApplication != null)
            {
                // Get Presentation Object
                presentation = pptApplication.ActivePresentation;
                // Get Slide collection object
                slides = presentation.Slides;
                // Get Slide count
                slidescount = slides.Count;
            }

        }

        private void NextSlide()
        {
            slide = pptApplication.SlideShowWindows[1].View.Slide;
            slideIndex = slide.SlideIndex + 1;
            if (slideIndex > slidescount)
            {
                MessageBox.Show("It is already last page");
            }
            else
            {
                pptApplication.SlideShowWindows[1].View.Next();
            }
        }
    }
}
