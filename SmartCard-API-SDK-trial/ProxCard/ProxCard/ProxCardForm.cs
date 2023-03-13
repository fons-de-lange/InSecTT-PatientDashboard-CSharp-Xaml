// --------------------------------------------------------------------------------------------
// ProxCardForm.cs
// CardWerk SmartCard API
// Copyright © 2004-2019 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 
/*
 *HIST
 *28OCT2019 MJ copy to new repo
 *19APR2016 MJ H10302 full 35 bit card number range supported
 *26MAY2015 MJ use SmartCardAPI.CardModule.HID.PROX, SmartCardAPI.DataModule.Wiegand, Corp 1000 48 bit support
 *27NOV2013 MJ transaction protection; support for lost/found terminal
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.HID.PROX;
using SmartCardAPI.DataModule.Wiegand;


namespace ProxCardSample
{
    /// <summary>
    /// Sample application to show how to connect to a reader/card system and get an ATR whenever
    /// a card is detected in any of the available card terminals
    /// </summary>

    class ProxCardForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label m_aCopyrightLabel;
        private System.Windows.Forms.LinkLabel m_aLinkLabel;
        private RichTextBox MainTextBox;
        private Label m_aApiVersionScapi;
        private NumericUpDown bitOffset;
        private NumericUpDown bitNumber;
        private TextBox txtBxCustomData;
        private Label lblCustomData;
        private Label label1;
        private Label label2;
        private Label label3;
        private CheckBox chkBoxKnownCardData;
        private NumericUpDown knownCardData;
        private System.Windows.Forms.Label m_aPromptLabel;

        /// <summary>
        /// Standard constructor.
        /// </summary>

        public ProxCardForm()
        {
            InitializeComponent();
            m_aApiVersionScapi.Text = SMARTCARDAPI.ApiVersionInfo;
        }

        /// <summary>
        /// Entry point of application.
        /// </summary>

        [STAThread]
        public static void Main(string[] args)
        {
            // Create a new text writer using the output stream, and add it to the trace
            // listeners.

            string sPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            Stream aTraceFile = File.Create(Path.Combine(sPath, "HelloCardTrace.txt"));
            TextWriterTraceListener aListener = new TextWriterTraceListener(aTraceFile);
            Trace.Listeners.Add(aListener);

            try
            {
                ProxCardForm aHelloCardForm = new ProxCardForm();

                // Run the primary application form.

                Application.Run(aHelloCardForm);
            }
            catch (Exception x)
            {
                Trace.WriteLine(x.ToString());
            }
            finally
            {
                // Flush and close the trace output.

                Trace.Flush();
                aTraceFile.Flush();
                aTraceFile.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>

        protected override void OnLoad(EventArgs e)
        {
            // We attach card terminal event handlers before starting up the card terminal
            // manager. This ensures that we get a card insertion event for those cards that
            // are already inserted when this program is started.

            CardTerminalManager.Singleton.CardInsertedEvent +=
                new CardTerminalEventHandler(InsertedEvent);
            CardTerminalManager.Singleton.CardRemovedEvent +=
                new CardTerminalEventHandler(RemovedEvent);
            CardTerminalManager.Singleton.CardTerminalLostEvent +=
                new CardTerminalEventHandler(TerminalLostEvent);
            CardTerminalManager.Singleton.CardTerminalFoundEvent +=
                new CardTerminalEventHandler(TerminalFoundEvent);

            if (!StartupCardTerminalManager())
            {
                Application.Exit();
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>

        protected override void OnClosed(EventArgs e)
        {
            // Every successful CardTerminalManager Startup method call,
            // requires a mandatory CardTerminalManager Shutdown method call!
            if (CardTerminalManager.Singleton.StartedUp)
            {
                CardTerminalManager.Singleton.Shutdown();
            }
            base.OnClosed(e);
        }

        /// <summary>
        /// Helper that starts up the card terminal manager. This is essential to
        /// check the integrity of the smart card system. This method will only
        /// succeed if there is an available reader. This means that it 
        /// card terminals are installed.
        /// </summary>
        bool StartupCardTerminalManager()
        {
            bool fStartedUp = false;

            try
            {
                // Startup the SmartCard API. The parameter "true" means that any
                // PC/SC smart card reader will automatically be added to the smart card
                // configuration registry. If startup fails, then this will throw an
                // exception.

                int nCountReaders = CardTerminalManager.Singleton.Startup(true);
                fStartedUp = CardTerminalManager.Singleton.StartedUp;
            }
            catch (Exception x)
            {
                // TODO: Better diagnstic and error handling would be appropriate here.

                Trace.WriteLine(x.ToString());

                MessageBox.Show(
                    "Unable to run CardTerminalManager. Will " +
                    "exit this application.",
                    "HelloProx",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop,
                    MessageBoxDefaultButton.Button1);

                fStartedUp = false;
            }
            return fStartedUp;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
        public void InsertedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            if (base.InvokeRequired)
            {
                object[] vParms = new object[2];
                vParms[0] = aSender;
                vParms[1] = aEventArgs;
                base.BeginInvoke(new CardTerminalEventHandler(InsertedEvent),
                    vParms);
            }
            else
            {
                // 01APR2011
                // We catch any exceptions during card I/O. This is particularly important
                // for fuzzy communication conditions. Example: an contactless card that 
                // is not in the field throuout the whole I/O might cause an error on the 
                // within the unmanaged Windows code. SmartCardAPI catches this in a general 
                // exception
                try
                {
                    this.AnalyzeCard(aEventArgs.Slot);
                }
                catch
                {
                    //DisplayText("last read failed");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>

        public void RemovedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            Debug.WriteLine(aSender); //29MAR2011
            if (base.InvokeRequired)
            {
                object[] vParms = new object[2];
                vParms[0] = aSender;
                vParms[1] = aEventArgs;
                base.BeginInvoke(new CardTerminalEventHandler(RemovedEvent),
                    vParms);
            }
            else
            {
                this.PromptAnyCard();
            }
        }

        /// <summary>
        /// We add the reader to be monitored.
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>

        public void TerminalFoundEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            Debug.WriteLine(aSender);
            if (base.InvokeRequired)
            {
                object[] vParms = new object[2];
                vParms[0] = aSender;
                vParms[1] = aEventArgs;
                base.BeginInvoke(new CardTerminalEventHandler(TerminalFoundEvent), vParms);
            }
            else
            {
                if (CardTerminalManager.Singleton.StartedUp)
                {
                    Debug.WriteLine("available readers: " + CardTerminalManager.Singleton.SlotCount);
                    DisplayText("Found reader: " + aEventArgs.Slot.CardTerminalName);
                    this.m_aPromptLabel.Text = "Insert card ...";
                }
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>

        public void TerminalLostEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            Debug.WriteLine(aSender);
            if (base.InvokeRequired)
            {
                object[] vParms = new object[2];
                vParms[0] = aSender;
                vParms[1] = aEventArgs;
                base.BeginInvoke(new CardTerminalEventHandler(TerminalLostEvent), vParms);
            }
            else
            {
                if (CardTerminalManager.Singleton.StartedUp)
                {
                    DisplayText("Lost reader: " + aEventArgs.Slot.CardTerminalName);

                    // CardTerminalManager.Singleton.Shutdown();
                    // update number of readers
                    CardTerminalManager.Singleton.DelistCardTerminal(aEventArgs.Slot.CardTerminal); // remove from monitored list of readers

                    if (CardTerminalManager.Singleton.SlotCount == 0)
                    {
                        this.m_aPromptLabel.Text = "Connect reader ...";
                        // start looking for reader insertion
                        // done automatically by the singleton. The singleton raises a "new reader" event if it 
                        // finds a new reader.

                    }
                    Debug.WriteLine("available readers: " + CardTerminalManager.Singleton.SlotCount);
                }
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="aCardSlot"></param>

        public void AnalyzeCard(CardTerminalSlot aCardSlot)
        {
            byte[] cardUID = new byte[8];
            bool isCustomData = false;

            if ((int) bitNumber.Value > 0)
            {
                isCustomData = true;
            }
            CardActivationResult nActivationResult;
            DisplayText("Reader Name: " + aCardSlot.CardTerminalName);
            CardHandle aCard = aCardSlot.AcquireCard((CardTypes.T0 | CardTypes.T1), out nActivationResult);
            if (nActivationResult != CardActivationResult.Success)
            {
                Debug.Assert(aCard == null);

                switch (nActivationResult)
                {
                    case CardActivationResult.NoCard:
                        m_aPromptLabel.Text = "Please insert card ...";
                        break;
                    case CardActivationResult.UnresponsiveCard:
                        m_aPromptLabel.Text = "Defective card or card inserted incorrectly.";
                        break;
                    case CardActivationResult.InUse:
                        m_aPromptLabel.Text = "Card reader blocked by other application.";
                        break;
                    default:
                        m_aPromptLabel.Text = "Can't power up card!";
                        break;
                }
                return;
            }

            // we now have a card in the reader that is powered up
            m_aPromptLabel.Text = "Found card";
            try
            {
                // =========================== ATR DETECTION ======================================
                // Every card accessed through PC/SC must return an Answer To Reset (ATR). 
                // So let's see what we've got here.
                byte[] atr = aCard.GetATR();
                if (atr.Length == 0) throw new Exception("Invalid ATR");
                DisplayText("ATR: " + CardHex.FromByteArray(atr, 0, atr.Length));

                aCardSlot.BeginTransaction();
                HidProxCard proxCard = new HidProxCard(aCard);
                if (proxCard.IsValid)
                {
                    byte[] pacsBits = proxCard.GetRawWiegandData();
                    if (pacsBits != null)
                    {
                        DisplayText("HID Prox card returned Wiegand data: " +
                                CardHex.FromByteArray(pacsBits, 0, pacsBits.Length));
                        if (isCustomData)
                        {
                            DisplayText("* Applying custom format");
                            int byteOffset = 0;
                            long customData = CardHex.LongFromByteArray(pacsBits, byteOffset, (int)bitOffset.Value, (int)bitNumber.Value);
                            DisplayText("* " + txtBxCustomData.Text + "=" + customData);
                            if (chkBoxKnownCardData.Checked)
                            {
                                if (customData == (long)knownCardData.Value)
                                {
                                    DisplayText("* card data matches expected value");
                                }
                                else
                                {
                                    DisplayText("* NO MATCH (" + (long)knownCardData.Value + " expected!)");
                                }
                            }
                        }
                        else
                        {
                            DisplayText("* Applying standard HID PROX formats");
                            long cardData = 0;
                            // test with data that triggers Kevin's issue
                            //                 PPAAAAAA.AAAAAAAA.AAAAAAAA.BBBBBBBB.BBBBBBBB.BBBBBBBP                                    
                            // 0x00FFF800028 = 00000000.00001111.11111111.10000000.00000000.00101000
                            //byte[] tv_pacsBitsCorp1000_CN4194324 = { 0x00, 0x0F, 0xFF, 0x80, 0x00, 0x28 };
                            //WiegandData wiegandData_CN4194324 = new WiegandData(tv_pacsBitsCorp1000_CN4194324);
                            //cardData = wiegandData_CN4194324.Extract(WiegandDataFormat.HID_CORP1000, WiegandDataIdentifier.CARD_NUMBER);

                            ;

                            // we have Wieegand raw data. Now it depends on the data format what all these bits really mean
                            // our Wiegand data module provides a convenuient way to access data items that are encoded in 
                            // raw Wiegand data
                            WiegandData wiegandData = new WiegandData(pacsBits);
                            cardData = wiegandData.Extract(WiegandDataFormat.HID_CORP1000, WiegandDataIdentifier.CARD_NUMBER);
                            DisplayText("* Corp1000..CN=" + cardData);
                            if (chkBoxKnownCardData.Checked)
                            {
                                if (cardData == (int)knownCardData.Value)
                                {
                                    DisplayText("* MATCH - your card might be formatted as Corporate 1000 card.");
                                }
                            }

                            cardData = wiegandData.Extract(WiegandDataFormat.HID_H10301, WiegandDataIdentifier.CARD_NUMBER);
                            DisplayText("* H10301....CN=" + cardData);
                            if (chkBoxKnownCardData.Checked)
                            {
                                if (cardData == (int)knownCardData.Value)
                                {
                                    DisplayText("* MATCH - your card might be formatted as H10301 card.");
                                }
                            }

                            cardData = wiegandData.Extract(WiegandDataFormat.HID_H10302, WiegandDataIdentifier.CARD_NUMBER);
                            DisplayText("* H10302....CN=" + cardData);
                            if (chkBoxKnownCardData.Checked)
                            {
                                if (cardData == (int)knownCardData.Value)
                                {
                                    DisplayText("* MATCH - your card might be formatted as H10302 card.");
                                }
                            }

                            cardData = wiegandData.Extract(WiegandDataFormat.HID_H10304, WiegandDataIdentifier.CARD_NUMBER);
                            DisplayText("* H10304....CN=" + cardData);
                            if (chkBoxKnownCardData.Checked)
                            {
                                if (cardData == (int)knownCardData.Value)
                                {
                                    DisplayText("* MATCH - your card might be formatted as H10302 card.");
                                }
                            }

                            cardData = wiegandData.Extract(WiegandDataFormat.HID_H10320, WiegandDataIdentifier.CARD_NUMBER);
                            DisplayText("* H10320....CN=" + cardData);
                            if (chkBoxKnownCardData.Checked)
                            {
                                if (cardData == (int)knownCardData.Value)
                                {
                                    DisplayText("* MATCH - your card might be formatted as H10320 card.");
                                }
                            }



                        }
                    }
                    else
                    {
                        DisplayText("Can't read raw data from this card.");
                    }
                }
                else
                {
                    DisplayText("ERROR: not a PROX (125 KHz, LF card) card ");
                }

            } //try
            catch (Exception x)
            {
                Trace.WriteLine(x.ToString());
                DisplayText(x.Message.ToString());
                m_aPromptLabel.Text = "Card access error";

            }
            finally
            {
                aCardSlot.EndTransaction(); 
                aCard.Dispose(); // release card handle
            }
        }

        /// <summary>
        /// 
        /// </summary>

        public void PromptAnyCard()
        {
            Prompt("Insert card ...");
            MainTextBox.Text = ""; 
        }

        /// <summary>
        /// Show some feedback in the title line. 
        /// </summary>

        public void Prompt(string text)
        {
            m_aPromptLabel.Text = text;
            m_aPromptLabel.Update();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        void LinkLabelLinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(m_aLinkLabel.Text);
        }

        /// <summary>
        /// 
        /// </summary>

        void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProxCardForm));
            this.m_aPromptLabel = new System.Windows.Forms.Label();
            this.m_aLinkLabel = new System.Windows.Forms.LinkLabel();
            this.m_aCopyrightLabel = new System.Windows.Forms.Label();
            this.MainTextBox = new System.Windows.Forms.RichTextBox();
            this.m_aApiVersionScapi = new System.Windows.Forms.Label();
            this.bitOffset = new System.Windows.Forms.NumericUpDown();
            this.bitNumber = new System.Windows.Forms.NumericUpDown();
            this.txtBxCustomData = new System.Windows.Forms.TextBox();
            this.lblCustomData = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.chkBoxKnownCardData = new System.Windows.Forms.CheckBox();
            this.knownCardData = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.bitOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bitNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.knownCardData)).BeginInit();
            this.SuspendLayout();
            // 
            // m_aPromptLabel
            // 
            this.m_aPromptLabel.Font = new System.Drawing.Font("Trebuchet MS", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_aPromptLabel.Location = new System.Drawing.Point(7, 74);
            this.m_aPromptLabel.Name = "m_aPromptLabel";
            this.m_aPromptLabel.Size = new System.Drawing.Size(416, 34);
            this.m_aPromptLabel.TabIndex = 2;
            this.m_aPromptLabel.Text = "Insert card ...";
            // 
            // m_aLinkLabel
            // 
            this.m_aLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_aLinkLabel.AutoSize = true;
            this.m_aLinkLabel.Location = new System.Drawing.Point(331, 335);
            this.m_aLinkLabel.Name = "m_aLinkLabel";
            this.m_aLinkLabel.Size = new System.Drawing.Size(175, 16);
            this.m_aLinkLabel.TabIndex = 1;
            this.m_aLinkLabel.TabStop = true;
            this.m_aLinkLabel.Text = "http://www.smartcard-api.com/";
            this.m_aLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelLinkClicked);
            // 
            // m_aCopyrightLabel
            // 
            this.m_aCopyrightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_aCopyrightLabel.AutoSize = true;
            this.m_aCopyrightLabel.Location = new System.Drawing.Point(8, 335);
            this.m_aCopyrightLabel.Name = "m_aCopyrightLabel";
            this.m_aCopyrightLabel.Size = new System.Drawing.Size(234, 16);
            this.m_aCopyrightLabel.TabIndex = 0;
            this.m_aCopyrightLabel.Text = "Copyright 2004-2019 CardWerk Technologies";
            // 
            // MainTextBox
            // 
            this.MainTextBox.Location = new System.Drawing.Point(11, 111);
            this.MainTextBox.Name = "MainTextBox";
            this.MainTextBox.ReadOnly = true;
            this.MainTextBox.Size = new System.Drawing.Size(492, 194);
            this.MainTextBox.TabIndex = 6;
            this.MainTextBox.Text = "";
            // 
            // m_aApiVersionScapi
            // 
            this.m_aApiVersionScapi.AutoSize = true;
            this.m_aApiVersionScapi.Location = new System.Drawing.Point(8, 319);
            this.m_aApiVersionScapi.Name = "m_aApiVersionScapi";
            this.m_aApiVersionScapi.Size = new System.Drawing.Size(89, 16);
            this.m_aApiVersionScapi.TabIndex = 7;
            this.m_aApiVersionScapi.Text = "ApiVersionSCAPI";
            // 
            // bitOffset
            // 
            this.bitOffset.Location = new System.Drawing.Point(222, 19);
            this.bitOffset.Maximum = new decimal(new int[] {
            64,
            0,
            0,
            0});
            this.bitOffset.Name = "bitOffset";
            this.bitOffset.Size = new System.Drawing.Size(45, 20);
            this.bitOffset.TabIndex = 9;
            // 
            // bitNumber
            // 
            this.bitNumber.Location = new System.Drawing.Point(63, 17);
            this.bitNumber.Maximum = new decimal(new int[] {
            35,
            0,
            0,
            0});
            this.bitNumber.Name = "bitNumber";
            this.bitNumber.Size = new System.Drawing.Size(45, 20);
            this.bitNumber.TabIndex = 10;
            // 
            // txtBxCustomData
            // 
            this.txtBxCustomData.Location = new System.Drawing.Point(63, -2);
            this.txtBxCustomData.Name = "txtBxCustomData";
            this.txtBxCustomData.Size = new System.Drawing.Size(74, 20);
            this.txtBxCustomData.TabIndex = 11;
            this.txtBxCustomData.Text = "CardData";
            // 
            // lblCustomData
            // 
            this.lblCustomData.AutoSize = true;
            this.lblCustomData.Location = new System.Drawing.Point(12, -2);
            this.lblCustomData.Name = "lblCustomData";
            this.lblCustomData.Size = new System.Drawing.Size(45, 16);
            this.lblCustomData.TabIndex = 12;
            this.lblCustomData.Text = "Custom";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(114, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(111, 16);
            this.label1.TabIndex = 13;
            this.label1.Text = "bits starting at bit #";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 16);
            this.label2.TabIndex = 14;
            this.label2.Text = "extract";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(273, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 16);
            this.label3.TabIndex = 15;
            this.label3.Text = "from the right (LSB)";
            // 
            // chkBoxKnownCardData
            // 
            this.chkBoxKnownCardData.AutoSize = true;
            this.chkBoxKnownCardData.Location = new System.Drawing.Point(15, 47);
            this.chkBoxKnownCardData.Name = "chkBoxKnownCardData";
            this.chkBoxKnownCardData.Size = new System.Drawing.Size(139, 20);
            this.chkBoxKnownCardData.TabIndex = 16;
            this.chkBoxKnownCardData.Text = "Find known card data:";
            this.chkBoxKnownCardData.UseVisualStyleBackColor = true;
            // 
            // knownCardData
            // 
            this.knownCardData.Location = new System.Drawing.Point(177, 46);
            this.knownCardData.Maximum = new decimal(new int[] {
            -1,
            0,
            0,
            0});
            this.knownCardData.Name = "knownCardData";
            this.knownCardData.Size = new System.Drawing.Size(90, 20);
            this.knownCardData.TabIndex = 17;
            // 
            // ProxCardForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(515, 356);
            this.Controls.Add(this.knownCardData);
            this.Controls.Add(this.chkBoxKnownCardData);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblCustomData);
            this.Controls.Add(this.txtBxCustomData);
            this.Controls.Add(this.bitNumber);
            this.Controls.Add(this.bitOffset);
            this.Controls.Add(this.m_aApiVersionScapi);
            this.Controls.Add(this.MainTextBox);
            this.Controls.Add(this.m_aPromptLabel);
            this.Controls.Add(this.m_aLinkLabel);
            this.Controls.Add(this.m_aCopyrightLabel);
            this.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProxCardForm";
            this.Text = "ProxCard sample rev. 28OCT2019";
            this.Load += new System.EventHandler(this.HelloCardForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.bitOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bitNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.knownCardData)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        /// <summary>
        /// Quick and dirty way to populate the main form with card data we explore throughout the 
        /// HelloCard sample code.
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void DisplayText(string text)
        {
            MainTextBox.AppendText(text + "\n");
            MainTextBox.Refresh();
        }

        private void HelloCardForm_Load(object sender, EventArgs e)
        {

        }
    }
}