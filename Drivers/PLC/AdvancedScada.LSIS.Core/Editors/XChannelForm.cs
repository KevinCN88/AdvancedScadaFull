﻿using AdvancedScada.DriverBase.Devices;
using AdvancedScada.Management.BLManager;
using HslCommunication.Profinet.LSIS;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using static AdvancedScada.IBaseService.Common.XCollection;

namespace AdvancedScada.LSIS.Core.Editors
{
    public partial class XChannelForm : AdvancedScada.Management.Editors.XChannelForm
    {

        private string _DriverTypes;


        public XChannelForm()
        {
            InitializeComponent();
        }
        public XChannelForm(string DriverTypes, ChannelService chm = null, Channel chCurrent = null)
        {
            InitializeComponent();
            objChannelManager = chm;
            ch = chCurrent;
            _DriverTypes = DriverTypes;

        }



        private void XChannelForm_Load(object sender, EventArgs e)
        {




            try
            {
                cboxPort.Items.Clear();
                cboxBaudRate.Items.Clear();
                cboxPort.Items.AddRange(SerialPort.GetPortNames());
                cboxBaudRate.Items.AddRange(new string[] { "1200", "2400", "4800", "9600", "14400", "19200", "28800", "38400", "56000", "57600", "115200" });
                cboxDataBits.Items.AddRange(new string[] { "7", "8" });
                cboxParity.Items.AddRange(System.Enum.GetNames(typeof(Parity)));
                cboxStopBits.Items.AddRange(System.Enum.GetNames(typeof(StopBits)));
                cboxHandshake.Items.AddRange(System.Enum.GetNames(typeof(Handshake)));


                if (ch != null)
                {


                    cboxModel.DataSource = System.Enum.GetNames(typeof(LSCpuInfo));

                    cboxConnType.Enabled = false;
                    this.Text = "Edit Channel   " + ch.ChannelTypes;
                    this.txtChannelName.Text = ch.ChannelName;
                    this.cboxConnType.SelectedItem = $"{ch.ConnectionType}";
                    cboxModel.SelectedItem = $"{ch.CPU}";
                    txtDesc.Text = ch.ChannelName;
                    switch (ch.ConnectionType)
                    {
                        case "SerialPort":
                            DISerialPort dis = (DISerialPort)ch;
                            cboxPort.SelectedItem = dis.PortName;
                            cboxBaudRate.SelectedItem = $"{dis.BaudRate}";
                            cboxDataBits.SelectedItem = $"{dis.DataBits}";
                            cboxParity.SelectedItem = $"{dis.Parity}";
                            cboxStopBits.SelectedItem = $"{dis.StopBits}";
                            cboxHandshake.SelectedItem = $"{dis.Handshake}";


                            break;
                        case "Ethernet":

                            DIEthernet die = (DIEthernet)ch;
                            txtIPAddress.Text = die.IPAddress;
                            txtPort.Value = die.Port;
                            txtSlot.Value = die.Slot;

                            break;


                    }

                }
                else
                {

                    cboxModel.DataSource = System.Enum.GetNames(typeof(LSCpuInfo));

                    cboxConnType.Enabled = true;
                    this.Text = "Add Channel    " + _DriverTypes;
                    this.cboxConnType.SelectedIndex = 0;
                    cboxPort.SelectedIndex = 0;
                    cboxBaudRate.SelectedIndex = 3;
                    cboxDataBits.SelectedIndex = 1;
                    cboxParity.SelectedIndex = 0;
                    cboxStopBits.SelectedIndex = 1;
                    cboxHandshake.SelectedIndex = 0;

                }
            }
            catch (Exception)
            {
                return;
            }

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            try
            {

                errorProvider1.Clear();
                if (string.IsNullOrEmpty(txtChannelName.Text)
                               || string.IsNullOrWhiteSpace(txtChannelName.Text))
                {
                    errorProvider1.SetError(txtChannelName, "The channel name is empty");
                    return;
                }
                var ConnType = $"{cboxConnType.SelectedItem}";
                TabControlModbus.SelectedIndex = cboxConnType.SelectedIndex;
                errorProvider1.Clear();
                switch (ConnType)
                {
                    case "SerialPort":
                        if ("Finish".Equals(btnNext.Text))
                        {
                            DISerialPort dis = new DISerialPort()
                            {
                                ChannelId = objChannelManager.Channels.Count + 1,
                                ChannelName = txtChannelName.Text,
                                ChannelTypes = _DriverTypes,
                                CPU = $"{cboxModel.Text}",
                                PortName = $"{cboxPort.SelectedItem}",
                                BaudRate = int.Parse($"{cboxBaudRate.SelectedItem}"),
                                DataBits = int.Parse($"{cboxDataBits.SelectedItem}"),
                                StopBits = (StopBits)System.Enum.Parse(typeof(StopBits), $"{cboxStopBits.SelectedItem}"),
                                Parity = (Parity)System.Enum.Parse(typeof(Parity), $"{cboxParity.SelectedItem}"),
                                Handshake = (Handshake)System.Enum.Parse(typeof(Handshake),
                                    $"{cboxHandshake.SelectedItem}"),
                                ConnectionType = ConnType,
                                Mode = $"CNET",
                                Description = txtDesc.Text
                            };
                            if (ch == null)
                            {
                                dis.ChannelId = objChannelManager.Channels.Count + 1;

                                if (eventChannelChanged != null) eventChannelChanged(dis, true);
                                EventscadaLogger?.Invoke(1, "ChannelManager", $"{DateTime.Now}", "Add Channel");

                            }
                            else
                            {
                                dis.ChannelId = ch.ChannelId;
                                dis.Devices = ch.Devices;

                                if (eventChannelChanged != null) eventChannelChanged(dis, false);
                                EventscadaLogger?.Invoke(1, "ChannelManager", $"{DateTime.Now}", "Editor Channel");

                            }
                            Close();
                        }
                        btnNext.Text = "Finish";
                        btnBlack.Enabled = true;

                        break;
                    case "Ethernet":
                        if ("Finish".Equals(btnNext.Text))
                        {
                            DIEthernet die = null;

                            die = new DIEthernet()
                            {
                                ChannelName = txtChannelName.Text,
                                ChannelTypes = _DriverTypes,
                                CPU = $"{cboxModel.Text}",
                                Rack = 0,
                                Slot = int.Parse($"{ txtSlot.Value}"),
                                IPAddress = txtIPAddress.Text,
                                Port = (short)txtPort.Value,
                                ConnectionType = ConnType,
                                Mode = $"FENET",
                            };

                            if (ch == null)
                            {

                                die.ChannelId = objChannelManager.Channels.Count + 1;
                                die.Devices = new List<Device>();
                                eventChannelChanged?.Invoke(die, true);
                                EventscadaLogger?.Invoke(1, "ChannelManager", $"{DateTime.Now}", "Add Channel");

                            }
                            else
                            {

                                die.ChannelId = ch.ChannelId;
                                die.Devices = ch.Devices;
                                eventChannelChanged?.Invoke(die, false);
                                EventscadaLogger?.Invoke(1, "ChannelManager", $"{DateTime.Now}", "Editor Channel");

                            }
                            Close();
                        }
                        btnNext.Text = "Finish";
                        btnBlack.Enabled = true;

                        break;
                }
            }
            catch (Exception ex)
            {

                EventscadaException?.Invoke(this.GetType().Name, ex.Message);
            }
        }

        private void btnBlack_Click(object sender, EventArgs e)
        {
            try
            {
                TabControlModbus.SelectedIndex = 0;
                btnBlack.Enabled = false;
                btnNext.Text = "Next >";
            }
            catch (Exception)
            {
            }
        }
    }
}
