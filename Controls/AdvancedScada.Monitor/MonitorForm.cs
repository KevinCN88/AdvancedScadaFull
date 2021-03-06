﻿using AdvancedScada.BaseService.Client;
using AdvancedScada.Common;
using AdvancedScada.DriverBase.Devices;
using AdvancedScada.IBaseService;
using AdvancedScada.Management.BLManager;
using ComponentFactory.Krypton.Docking;
using ComponentFactory.Krypton.Navigator;
using ComponentFactory.Krypton.Toolkit;
using HslScada.Studio.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using static AdvancedScada.Common.XCollection;

namespace AdvancedScada.Monitor
{
    public delegate void EventTagSelected(string tagName);
    [CallbackBehavior(UseSynchronizationContext = true)]
    public partial class MonitorForm : KryptonForm, IServiceCallback
    {
        private bool IsConnected;
        public bool IsDataChanged = false;


        private IReadService client;
        private ChannelService objChannelManager;
        private DataBlockService objDataBlockManager;
        private DeviceService objDeviceManager;
        private TagService objTagManager;
        private readonly string SelectedTag = string.Empty;
        public EventTagSelected OnTagSelected_Clicked = null;
        public MonitorForm()
        {
            InitializeComponent(); Application.DoEvents();
        }
        public MonitorForm(string tagNameSelected = null)
        {
            InitializeComponent();
        }

        private void InitializeData(string xmlPath)
        {
            objChannelManager.Channels.Clear();
            objChannelManager.XmlPath = xmlPath;
            List<Channel> chList = objChannelManager.GetChannels(xmlPath);
            treeViewSI.Nodes.Clear();
            foreach (Channel ch in chList)
            {
                List<TreeNode> dvList = new List<TreeNode>();
                ////Sort.
                ch.Devices.Sort(delegate (Device x, Device y)
                {
                    return x.DeviceName.CompareTo(y.DeviceName);
                });

                foreach (Device dv in ch.Devices)
                {
                    List<TreeNode> tgList = new List<TreeNode>();
                    foreach (DataBlock db in dv.DataBlocks)
                    {
                        TreeNode dbNode = new TreeNode(db.DataBlockName)
                        {
                            StateImageIndex = 2
                        };
                        tgList.Add(dbNode);
                    }

                    TreeNode dvNode = new TreeNode(dv.DeviceName, tgList.ToArray())
                    {
                        StateImageIndex = 1
                    };
                    dvList.Add(dvNode);
                }
                TreeNode chNode = new TreeNode(ch.ChannelName, dvList.ToArray())
                {
                    StateImageIndex = 0
                };
                treeViewSI.Nodes.Add(chNode);
            }
        }
        private void treeViewSI_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (treeViewSI.SelectedNode == null)
                {
                    return;
                }

                int Level = treeViewSI.SelectedNode.Level;
                string selectedNode = treeViewSI.SelectedNode.Text;
                DataBlock dbCurrent = null; //Node: DataBlock
                Device dvCurrent = null; //Node: Device
                Channel chCurrent = null; //Node: Channel
                switch (Level)
                {
                    case 0:
                        DGMonitorForm.Rows.Clear();
                        break;
                    case 1:
                        DGMonitorForm.Rows.Clear();
                        break;
                    case 2:
                        chCurrent = objChannelManager.GetByChannelName(treeViewSI.SelectedNode.Parent.Parent.Text);
                        dvCurrent = objDeviceManager.GetByDeviceName(chCurrent, treeViewSI.SelectedNode.Parent.Text);
                        dbCurrent = objDataBlockManager.GetByDataBlockName(dvCurrent, treeViewSI.SelectedNode.Text);
                        DGMonitorForm.Rows.Clear();
                        string[] row = new string[7];
                        foreach (Tag tg in dbCurrent.Tags)
                        {
                            if (tg.Value == null)
                            {
                                row[0] = string.Format("{0}", tg.TagId);
                                row[1] = tg.TagName;
                                row[2] = string.Format("{0}", tg.Address);
                                row[3] = string.Format("{0}", tg.DataType);
                                row[4] = "0";
                                row[5] = string.Format("{0}", tg.TimeSpan);
                                row[6] = tg.Description;

                                DGMonitorForm.Rows.Add(row);
                            }
                            else
                            {
                                string[] row1 = { string.Format("{0}", tg.TagId), tg.TagName, string.Format("{0}", tg.Address), string.Format("{0}", tg.DataType), string.Format("{0}", tg.Value), string.Format("{0}", tg.TimeSpan), tg.Description };

                                DGMonitorForm.Rows.Add(row1);
                            }


                        }
                        break;
                    default:
                        DGMonitorForm.Rows.Clear();
                        break;
                }
                if (chCurrent == null)
                {
                    EventPvGridChannelGet?.Invoke(chCurrent, false);

                }
                else
                {
                    switch (chCurrent.ConnectionType)
                    {
                        case "SerialPort":

                            DISerialPort dis = (DISerialPort)chCurrent;
                            EventPvGridChannelGet?.Invoke(dis, true);

                            break;
                        case "Ethernet":

                            DIEthernet die = (DIEthernet)chCurrent;
                            EventPvGridChannelGet?.Invoke(die, true);

                            break;
                    }


                }
                if (dvCurrent != null)
                {
                    EventPvGridDeviceGet?.Invoke(dvCurrent, true);

                }
                else
                {
                    EventPvGridDeviceGet?.Invoke(dvCurrent, false);
                }
                if (dbCurrent != null)
                {
                    EventPvGridDataBlockGet?.Invoke(dbCurrent, true);

                }
                else
                {
                    EventPvGridDataBlockGet?.Invoke(dbCurrent, false);

                    lblSelectedTagName.Text = string.Format("Selected tag: {0}", "None");

                }
            }
            catch (Exception ex)
            {
                EventscadaException?.Invoke(GetType().Name, ex.Message);
            }
        }
        private KryptonPage NewUserPropertyGrid()
        {
            return NewPages("UserProperty ", 0, new UserPropertyGrid());
        }
        private KryptonPage NewPages(string name, int image, Control content)
        {
            // Create new page with title and image
            KryptonPage p = new KryptonPage
            {
                Text = name,
                TextTitle = name,
                TextDescription = name
            };


            // Add the control for display inside the page
            content.Dock = DockStyle.Fill;
            p.Controls.Add(content);


            return p;
        }
        private void PLC_MonitorForm_Load(object sender, EventArgs e)
        {
            KryptonDockingWorkspace w = kryptonDockingManager1.ManageWorkspace("Workspace", kryptonDockableWorkspace1);
            kryptonDockingManager1.ManageControl("Control", kryptonSplitContainer1.Panel2, w);
            kryptonDockingManager1.ManageFloating("Floating", this);


            kryptonDockingManager1.AddAutoHiddenGroup("Control", DockingEdge.Right, new KryptonPage[] { NewUserPropertyGrid() });

            objChannelManager = ChannelService.GetChannelManager();
            objDeviceManager = DeviceService.GetDeviceManager();
            objDataBlockManager = DataBlockService.GetDataBlockManager();
            objTagManager = TagService.GetTagManager();
            string xmlFile = objChannelManager.ReadKey(objChannelManager.XML_NAME_DEFAULT);
            if (string.IsNullOrEmpty(xmlFile) || string.IsNullOrWhiteSpace(xmlFile))
            {
                return;
            }

            InitializeData(xmlFile);
            SetCreateChannel();
        }
        #region IServiceCallback
        public void SetCreateChannel()
        {
            try
            {



                InstanceContext ic = new InstanceContext(this);
                XCollection.CURRENT_MACHINE = new Machine
                {
                    MachineName = Environment.MachineName,
                    Description = "Free"
                };
                IPAddress[] hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (IPAddress iPAddress in hostAddresses)
                {
                    if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        XCollection.CURRENT_MACHINE.IPAddress = $"{iPAddress}";
                        break;
                    }
                }
                client = ClientDriverHelper.GetInstance().GetReadService(ic);
                client.Connect(XCollection.CURRENT_MACHINE);

                IsConnected = true;
            }
            catch (Exception ex)
            {

                EventscadaException?.Invoke(GetType().Name, ex.Message);
            }



        }



        #endregion
        private void PLC_MonitorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {

                try
                {
                    if (client != null)
                    {
                        client.Disconnect(XCollection.CURRENT_MACHINE);
                    }

                    IsConnected = false;
                }
                catch (Exception ex)
                {
                    EventscadaException?.Invoke(GetType().Name, ex.Message);
                }


            }
            catch (Exception ex)
            {
                EventscadaException?.Invoke(GetType().Name, ex.Message);
            }
        }
        private void DGMonitorForm_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {

                    popupMenuLeft.Show(MousePosition);
                }
            }
            catch (Exception ex)
            {

                EventscadaException?.Invoke(GetType().Name, ex.Message);
            }
        }
        #region WriteTagValue

        public void WriteTagValue(string NumValue)
        {
            try
            {
                if (treeViewSI.SelectedNode == null)
                {
                    return;
                }

                int Level = treeViewSI.SelectedNode.Level;
                string selectedNode = treeViewSI.SelectedNode.Text;
                if (Level == 2)
                {
                    Channel chCurrent = objChannelManager.GetByChannelName(treeViewSI.SelectedNode.Parent.Parent.Text);
                    Device dvCurrent = objDeviceManager.GetByDeviceName(chCurrent, treeViewSI.SelectedNode.Parent.Text);
                    DataBlock dbCurrent = objDataBlockManager.GetByDataBlockName(dvCurrent, treeViewSI.SelectedNode.Text);

                    string channelName = chCurrent.ChannelName;
                    string DeviceName = dvCurrent.DeviceName;

                    string DataBlockName = dbCurrent.DataBlockName;



                    if (DGMonitorForm.SelectedRows.Count == 1)
                    {
                        string tgName = (string)DGMonitorForm.SelectedRows[0].Cells[1].Value;

                        lblSelectedTagName.Text = $"{SelectedTag}{channelName}.{DeviceName}.{DataBlockName}.{tgName}";
                        client.WriteTag(lblSelectedTagName.Text, NumValue);




                        Thread.Sleep(50);
                    }
                    else
                    {
                        lblSelectedTagName.Text = string.Empty;
                    }
                }


            }
            catch (Exception ex)
            {
                EventscadaException?.Invoke(GetType().Name, ex.Message);
            }
        }
        private void DGMonitorForm_SelectionChanged(object sender, EventArgs e)
        {

        }

        private void mSetON_Click(object sender, EventArgs e)
        {
            WriteTagValue("1");
        }

        private void mSetOFF_Click(object sender, EventArgs e)
        {
            WriteTagValue("0");
        }

        private void mWriteTagValue_Click(object sender, EventArgs e)
        {
            try
            {


                if (treeViewSI.SelectedNode == null)
                {
                    return;
                }

                int Level = treeViewSI.SelectedNode.Level;
                string selectedNode = treeViewSI.SelectedNode.Text;

                if (Level == 2)
                {
                    Channel chCurrent = objChannelManager.GetByChannelName(treeViewSI.SelectedNode.Parent.Parent.Text);
                    Device dvCurrent = objDeviceManager.GetByDeviceName(chCurrent, treeViewSI.SelectedNode.Parent.Text);
                    DataBlock dbCurrent = objDataBlockManager.GetByDataBlockName(dvCurrent, treeViewSI.SelectedNode.Text);

                    string channelName = chCurrent.ChannelName;
                    string DeviceName = dvCurrent.DeviceName;

                    string DataBlockName = dbCurrent.DataBlockName;



                    if (DGMonitorForm.SelectedRows.Count == 1)
                    {
                        string tgName = (string)DGMonitorForm.SelectedRows[0].Cells[1].Value;

                        lblSelectedTagName.Text = $"{SelectedTag}{channelName}.{DeviceName}.{DataBlockName}.{tgName}";

                        WriteTagForm objWriteTagForm = new WriteTagForm(lblSelectedTagName.Text, client) { StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false };

                        objWriteTagForm.ShowDialog();
                        Thread.Sleep(50);
                    }
                    else
                    {
                        lblSelectedTagName.Text = string.Empty;
                    }
                }

            }
            catch (Exception ex)
            {
                EventscadaException?.Invoke(GetType().Name, ex.Message);
            }
        }


        #endregion

        private void DGMonitorForm_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (treeViewSI.SelectedNode == null)
                {
                    return;
                }

                int Level = treeViewSI.SelectedNode.Level;
                string selectedNode = treeViewSI.SelectedNode.Text;

                if (Level == 2)
                {
                    Channel chCurrent = objChannelManager.GetByChannelName(treeViewSI.SelectedNode.Parent.Parent.Text);
                    Device dvCurrent = objDeviceManager.GetByDeviceName(chCurrent, treeViewSI.SelectedNode.Parent.Text);
                    DataBlock dbCurrent = objDataBlockManager.GetByDataBlockName(dvCurrent, treeViewSI.SelectedNode.Text);

                    string channelName = chCurrent.ChannelName;
                    string DeviceName = dvCurrent.DeviceName;

                    string DataBlockName = dbCurrent.DataBlockName;



                    if (DGMonitorForm.SelectedRows.Count == 1)
                    {
                        string tgName = (string)DGMonitorForm.SelectedRows[0].Cells[1].Value;

                        lblSelectedTagName.Text = $"{channelName}.{DeviceName}.{DataBlockName}.{tgName}";
                        OnTagSelected_Clicked?.Invoke(lblSelectedTagName.Text);
                        DialogResult = DialogResult.OK;

                    }
                    else
                    {
                        lblSelectedTagName.Text = string.Empty;
                    }
                }


            }
            catch (Exception ex)
            {
                EventscadaException?.Invoke(GetType().Name, ex.Message);
            }
        }

        public void UpdateCollection(ConnectionState status, Dictionary<string, Tag> Tags)
        {
            try
            {
                eventConnectionChanged?.Invoke(status);
                int Level = treeViewSI.SelectedNode.Level;
                string selectedNode = treeViewSI.SelectedNode.Text;
                if (IsConnected)
                {

                    switch (Level)
                    {
                        case 0:
                            DGMonitorForm.Rows.Clear();
                            break;
                        case 1:
                            DGMonitorForm.Rows.Clear();
                            break;
                        case 2:
                            Channel chCurrent = objChannelManager.GetByChannelName(treeViewSI.SelectedNode.Parent.Parent.Text);
                            Device dvCurrent = objDeviceManager.GetByDeviceName(chCurrent, treeViewSI.SelectedNode.Parent.Text);
                            DataBlock dbCurrent = objDataBlockManager.GetByDataBlockName(dvCurrent, treeViewSI.SelectedNode.Text);



                            if (Tags != null)
                            {
                                List<KeyValuePair<string, Tag>> List2 = Tags.Where(item => dbCurrent.Tags.Any(p => p.ChannelId == item.Value.ChannelId
                                                                                      && p.DeviceId == item.Value.DeviceId && p.DataBlockId == item.Value.DataBlockId)).ToList();

                                for (int i = 0; i < DGMonitorForm.RowCount; i++)
                                {

                                    for (int y = 0; y < List2.Count; y++)
                                    {
                                        if (DGMonitorForm[0, i].Value != null)
                                        {
                                            if (List2[y].Value.TagId.Equals(int.Parse(DGMonitorForm[0, i].Value.ToString())))
                                            {

                                                if (DGMonitorForm[1, i].Value != null)
                                                {
                                                    for (int j = 0; j < List2.Count; j++)
                                                    {
                                                        if (List2[j].Value.TagName.Equals(DGMonitorForm[1, i].Value.ToString()))
                                                        {

                                                            DGMonitorForm[4, i].Value = List2[j].Value.Value;
                                                            DGMonitorForm[5, i].Value = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}";
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }


                            }

                            break;
                        default:
                            DGMonitorForm.Rows.Clear();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                EventscadaException?.Invoke(GetType().Name, ex.Message);
            }
        }

        public void UpdateCollectionDataBlock(ConnectionState status, Dictionary<string, DataBlock> collection)
        {
            throw new NotImplementedException();
        }
    }


}
