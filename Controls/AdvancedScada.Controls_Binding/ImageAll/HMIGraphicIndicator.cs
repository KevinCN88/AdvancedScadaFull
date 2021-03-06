﻿using AdvancedScada.Common;
using AdvancedScada.Common.Client;
using AdvancedScada.Controls_Binding.DialogEditor;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
//****************************************************************************
//* 12-DEC-08 Added line in OnPaint to exit is LegendText is nothing
//* 05-OCT-09 Exit OnPaint if GrayPen is Nothing
//****************************************************************************

namespace AdvancedScada.Controls_Binding.ImageAll
{
    public class HMIGraphicIndicator : MfgControl.AdvancedHMI.Controls.GraphicIndicator, IPropertiesControls
    {
        public HMIGraphicIndicator()
        {
            MaxHoldTimer.Tick += MaxHoldTimer_Tick;
            MinHoldTimer.Tick += HoldTimer_Tick;
        }
        #region PLC Properties
        public bool HoldTimeMet;
        private int m_MaximumHoldTime = 3000;
        private int m_MinimumHoldTime = 500;

        //*****************************************
        //* Property - Address in PLC to Link to
        //*****************************************
        private string m_PLCAddressClick = string.Empty;

        //*****************************************
        //* Property - Address in PLC to Link to
        //*****************************************
        private string m_PLCAddressSelect1 = string.Empty;

        //*****************************************
        //* Property - Address in PLC to Link to
        //*****************************************
        private string m_PLCAddressSelect2 = string.Empty;


        //*****************************************
        //* Property - Address in PLC to Link to
        //*****************************************
        private string m_PLCAddressText2 = string.Empty;

        //*****************************************
        //* Property - Address in PLC to Link to
        //*****************************************
        private string m_PLCAddressVisible = string.Empty;

        //**********************************************************************
        //* If output type is set to write value, then write this value to PLC
        //**********************************************************************

        //*****************************************
        //* Property - Hold time before bit reset
        //*****************************************
        private readonly Timer MaxHoldTimer = new Timer();

        //*****************************************
        //* Property - Hold time before bit reset
        //*****************************************
        private readonly Timer MinHoldTimer = new Timer();
        private readonly bool MouseIsDown = false;

        //***************************************
        //* Call backs for returned data
        //***************************************
        private string OriginalText;


        public string PLCAddressValueSelect1
        {
            get => m_PLCAddressSelect1;
            set
            {
                if (m_PLCAddressSelect1 != value)
                {
                    m_PLCAddressSelect1 = value;
                    try
                    {
                        //* When address is changed, re-subscribe to new address
                        if (string.IsNullOrEmpty(m_PLCAddressSelect1) ||
                            string.IsNullOrWhiteSpace(m_PLCAddressSelect1) || Licenses.LicenseManager.IsInDesignMode)
                        {
                            return;
                        }

                        Binding bd = new Binding("ValueSelect1", TagCollectionClient.Tags[m_PLCAddressSelect1], "Value", true);
                        DataBindings.Add(bd);
                    }
                    catch (Exception ex)
                    {
                        DisplayError(ex.Message);
                    }
                }
            }
        }

        public string PLCAddressValueSelect2
        {
            get => m_PLCAddressSelect2;
            set
            {
                if (m_PLCAddressSelect2 != value)
                {
                    m_PLCAddressSelect2 = value;
                }
            }
        }

        public string PLCAddressVisible
        {
            get => m_PLCAddressVisible;
            set
            {
                if (m_PLCAddressVisible != value)
                {
                    m_PLCAddressVisible = value;
                    //* When address is changed, re-subscribe to new address
                    try
                    {
                        //* When address is changed, re-subscribe to new address
                        if (string.IsNullOrEmpty(m_PLCAddressVisible) ||
                            string.IsNullOrWhiteSpace(m_PLCAddressVisible) || Licenses.LicenseManager.IsInDesignMode)
                        {
                            return;
                        }

                        Binding bd = new Binding("Visible", TagCollectionClient.Tags[m_PLCAddressVisible], "Value", true);
                        DataBindings.Add(bd);
                    }
                    catch (Exception ex)
                    {
                        DisplayError(ex.Message);
                    }
                }
            }
        }

        public string PLCAddressText2
        {
            get => m_PLCAddressText2;
            set
            {
                if (m_PLCAddressText2 != value)
                {
                    m_PLCAddressText2 = value;
                }
            }
        }

        [Category("PLC Properties")]
        [Editor(typeof(TestDialogEditor), typeof(UITypeEditor))]
        public string PLCAddressClick
        {
            get => m_PLCAddressClick;
            set
            {
                if (m_PLCAddressClick != value)
                {
                    m_PLCAddressClick = value;
                }
            }
        }

        [DefaultValue(false)]
        public bool SuppressErrorDisplay { get; set; }

        [Category("PLC Properties")]
        public int MinimumHoldTime
        {
            get => m_MinimumHoldTime;
            set
            {
                m_MinimumHoldTime = value;
                if (value > 0)
                {
                    MinHoldTimer.Interval = value;
                }
            }
        }

        [Category("PLC Properties")]
        public int MaximumHoldTime
        {
            get => m_MaximumHoldTime;
            set
            {
                m_MaximumHoldTime = value;
                if (value > 0)
                {
                    MaxHoldTimer.Interval = value;
                }
            }
        }

        [Category("PLC Properties")]
        public int ValueToWrite { get; set; }
        public string PLCAddressValue { get; set; }
        public string PLCAddressEnabled { get; set; }
        #endregion
        private void ReleaseValue()
        {
            try
            {
                switch (OutputType)
                {
                    case OutputTypes.MomentarySet:
                        Utilities.Write(m_PLCAddressClick, false);
                        break;
                    case OutputTypes.MomentaryReset:
                        Utilities.Write(m_PLCAddressClick, true);
                        break;
                }
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
            }
        }

        private void HoldTimer_Tick(object sender, EventArgs e)
        {
            MinHoldTimer.Enabled = false;
            HoldTimeMet = true;
            if (!MouseIsDown)
            {
                ReleaseValue();
            }
        }

        private void MaxHoldTimer_Tick(object sender, EventArgs e)
        {
            MaxHoldTimer.Enabled = false;
            ReleaseValue();
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);


            if (!string.IsNullOrWhiteSpace(m_PLCAddressClick) & Enabled && m_PLCAddressClick != null)
            {
                try
                {
                    switch (OutputType)
                    {
                        case OutputTypes.MomentarySet:
                            Utilities.Write(m_PLCAddressClick, true);
                            break;
                        case OutputTypes.MomentaryReset:
                            Utilities.Write(m_PLCAddressClick, false);
                            break;
                        case OutputTypes.SetTrue:
                            Utilities.Write(m_PLCAddressClick, true);
                            break;
                        case OutputTypes.SetFalse:
                            Utilities.Write(m_PLCAddressClick, false);
                            break;
                        case OutputTypes.Toggle:

                            bool CurrentValue = false;
                            if (CurrentValue)
                            {
                                Utilities.Write(m_PLCAddressClick, false);
                            }
                            else
                            {
                                Utilities.Write(m_PLCAddressClick, true);
                            }

                            break;
                        default:

                            break;
                    }
                }
                catch (Exception ex)
                {
                    DisplayError("WRITE FAILED!" + ex.Message);
                }

                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!string.IsNullOrWhiteSpace(m_PLCAddressClick) & Enabled)
            {
                try
                {
                    switch (OutputType)
                    {
                        case OutputTypes.MomentarySet:
                            Utilities.Write(m_PLCAddressClick, false);
                            break;
                        case OutputTypes.MomentaryReset:
                            Utilities.Write(m_PLCAddressClick, true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DisplayError("WRITE FAILED!" + ex.Message);
                }

                Invalidate();
            }
        }

        #region "Error Display"

        //********************************************************
        //* Show an error via the text property for a short time
        //********************************************************
        private Timer ErrorDisplayTime;

        public void DisplayError(string ErrorMessage)
        {
            if (!SuppressErrorDisplay)
            {
                if (ErrorDisplayTime == null)
                {
                    ErrorDisplayTime = new Timer();
                    ErrorDisplayTime.Tick += ErrorDisplay_Tick;
                    ErrorDisplayTime.Interval = 5000;
                }

                //* Save the text to return to
                if (!ErrorDisplayTime.Enabled)
                {
                    OriginalText = Text;
                }

                ErrorDisplayTime.Enabled = true;
                Utilities.DisplayError(this, ErrorMessage);
                Text = ErrorMessage;
            }
        }

        //**************************************************************************************
        //* Return the text back to its original after displaying the error for a few seconds.
        //**************************************************************************************
        private void ErrorDisplay_Tick(object sender, EventArgs e)
        {
            Text = OriginalText;

            if (ErrorDisplayTime != null)
            {
                ErrorDisplayTime.Enabled = false;
                ErrorDisplayTime.Dispose();
                ErrorDisplayTime = null;
            }
        }

        #endregion
    }


}