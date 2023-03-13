// --------------------------------------------------------------------------------------------
// HelloCardForm.cs
// CardWerk SmartCard API
// Copyright © 2004-2020 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 

// HISTORY
// 31DEC2019 MJ add to zip repo
// 21FEB2017 MJ detect contactless cards and demo PC/SC 2.01 part 3 card module CardModule.CLICS
// 13JUN2016 MJ reader serial number
// 08MAR2011 MJ translate messages to English
// 24MAR2011 MJ simplified; show ATR when connected
// 29MAR2011 MJ contactless reader detection via reader name analysis contactless: OMNIKEY and -CL
// 31MAR2011 MJ catch communication errors in unmanaged module (driver)
// 13JAN2012 MJ improve error message during StartupCardTerminalManager()
// 14JAN2012 MJ stopping smart card service causes CardTerminalLost event, exits application
// 10APR2012 MJ main text window for easier, more versatile API feedback
// 15MAY2013 MJ PC/SC 2.01 part 3 storage card support
// 25NOV2013 MJ add support for found reader event and reader recovery 

/*
 * Use cases with readers connected/disconnected
 * 
 * UC001: No readers at startup. 
 * The application indicates that there is no reader. It allow the user to connect a reader; 
 * Newly connected reader is detected by CardTerminalManager singleton and added inmternally to  
 * the list of slots to monitor. Monitoring will start if the reader list contains at least one reader. 
 * 
 * UC002: Reader added after startup.
 * Application update reader list without interrupting any active card session. Additional reader is 
 * added to slots to monitor unless a minimum number of readers has already been detected. The application 
 * can increase that number if need be.
 * 
 * UC003: Reader removed after startup.
 * CardTerminal manager will try to recover by polling for more readers until a reader is reconnected to
 * the system. 
 * 
 */


using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.CLICS;


namespace HelloCard
{
    /// <summary>
    /// Sample application to show how to connect to a smart card terminal and card system.
    /// It demonstrates usage of care and terminal events and displays card detection 
    /// and card ATR.
    /// </summary>

    class HelloCardForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label m_aCopyrightLabel;
        private System.Windows.Forms.LinkLabel m_aLinkLabel;
        private RichTextBox MainTextBox;
        private Label m_aPromptLabel;
        private Label m_aApiVersion;
        static bool ALWAYS_LOOK_FOR_MORE_CARD_READERS = true; // increases the number of readers we are looking for

        /// <summary>
        /// Standard constructor.
        /// </summary>

        public HelloCardForm()
        {
            InitializeComponent();
            this.Text = "HelloCard rev 06MAR2018";
            m_aApiVersion.Text = SMARTCARDAPI.ApiVersionInfo;
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
            string sTraceFilePath = Path.Combine(sPath, "HelloCardTrace.txt");
            Debug.WriteLine("Creating trace file: " + sTraceFilePath);
            Stream aTraceFile = File.Create(sTraceFilePath);
            TextWriterTraceListener aListener = new TextWriterTraceListener(aTraceFile);
            Trace.Listeners.Add(aListener);

            try
            {
                HelloCardForm aHelloCardForm = new HelloCardForm();

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

            // Determine the path and filename of the SmartCard API registration file.
            // FYI section
            Environment.SpecialFolder nSpecialFolderLocalUser = Environment.SpecialFolder.LocalApplicationData;
            string sDataFolderUser = Environment.GetFolderPath(nSpecialFolderLocalUser);
            Debug.WriteLine("Local registry (option): " + Path.Combine(sDataFolderUser, "Subsembly" + Path.DirectorySeparatorChar + "SmartCard" + Path.DirectorySeparatorChar + "registry.xml"));

            // shared folder example C:\ProgramData\Subsembly\SmartCard\Registry.xml
            Environment.SpecialFolder nSpecialFolderShared = Environment.SpecialFolder.CommonApplicationData;
            string sDataFolderShared = Environment.GetFolderPath(nSpecialFolderShared);
            Debug.WriteLine("Common registry: " + Path.Combine(sDataFolderShared, "Subsembly" + Path.DirectorySeparatorChar + "SmartCard" + Path.DirectorySeparatorChar + "registry.xml"));
            // FYI section end

            // We attach card terminal event handlers before starting up the card terminal
            // manager. We will get a card insertion event for cards that are already 
            // inserted when this program is started. The terminal manager will also raise
            // an event whenever a terminal is lost or found.

            CardTerminalManager.Singleton.CardInsertedEvent += new CardTerminalEventHandler(InsertedEvent);
            CardTerminalManager.Singleton.CardRemovedEvent +=  new CardTerminalEventHandler(RemovedEvent);
            CardTerminalManager.Singleton.CardTerminalLostEvent += new CardTerminalEventHandler(TerminalLostEvent);
            CardTerminalManager.Singleton.CardTerminalFoundEvent += new CardTerminalEventHandler(TerminalFoundEvent);


            // Try to start up the card terminal manager. If this fails, then the application
            // is immediately aborted.

            if (!StartupCardTerminalManager())
            {
                Application.Exit();
            }
            DisplayNumberOfAvailableReaders();
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
                if (nCountReaders > 0)
                {
                    string[] readerList = CardTerminalManager.Singleton.GetSlotNames();
                    int index = 0;
                    foreach (string reader in readerList)
                    {
                        Debug.WriteLine(string.Format("Reader #{0}: {1}", index++, reader));
                    }
                    if (ALWAYS_LOOK_FOR_MORE_CARD_READERS)
                    {
                        // By default SlotCountMinimum is set to the number of readers detected at startup.
                        // We increase this value to continue looking for more readers.
                        CardTerminalManager.Singleton.SlotCountMinimum = nCountReaders + 1;
                    }
                }
                else
                {
                    Debug.WriteLine("ERROR: No card reader/terminal available on this system!");
                    Debug.WriteLine("Please verify your PC/SC smart card system.\n" +
                                      "Is a smart card reader attached?\n" +
                                      "Is your PC/SC smart card service up and running?\n" +
                                      "Is the reader driver installed?\n");
                    //throw new Exception("NO READER");
                }

                fStartedUp = true;
            }
            catch (Exception x)
            {
                // TODO: Better diagnostic and error handling would be appropriate here.

                Trace.WriteLine(x.ToString());

                MessageBox.Show(
                    "Unable to start CardTerminalManager. Will " +
                    "exit this application.",
                    "PIVcardWin",
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
                // for fuzzy communication conditions. Example: a contactless card that 
                // is not in the field throughout the whole I/O might cause an error within 
                // the unmanaged Windows API code. SmartCardAPI catches this in a general 
                // exception.
                try
                {
                    this.AccessCard(aEventArgs.Slot);
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
                this.PromptAnyCard(aEventArgs.Slot.CardTerminalName);
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
                    if (ALWAYS_LOOK_FOR_MORE_CARD_READERS && CardTerminalManager.Singleton.SlotCount == CardTerminalManager.Singleton.SlotCountMinimum)
                    {
                        CardTerminalManager.Singleton.SlotCountMinimum += 1; // let's continue looking for more readers
                    }
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
                    if (CardTerminalManager.Singleton.SlotCount == 0)
                    {
                        this.m_aPromptLabel.Text = "Connect reader ...";
                        // start looking for reader insertion
                        // done automatically by the singleton. The singleton raises an event if it 
                        // finds a new reader.

                    }
                    else if (CardTerminalManager.Singleton.SlotCountMinimum + 1 > CardTerminalManager.Singleton.SlotCount)
                    {
                        CardTerminalManager.Singleton.SlotCountMinimum -= 1;
                    }
                    Debug.WriteLine("available readers: " + CardTerminalManager.Singleton.SlotCount);
                }
            }
        }


        /// <summary>
        /// This is the core method to connect to a card and explore it. Once you have a CardHandle, you can
        /// exchange command APDUs and receive response APDUs. 
        /// </summary>
        /// <param name="aCardSlot"></param>

        public void AccessCard(CardTerminalSlot aCardSlot)
        {
            // Acquire any processor card (T=0 or T=1) that may be present in the given card
            // terminal slot
            string readerName = aCardSlot.CardTerminalName;
            bool isContactlessInterface = false;

            CardActivationResult nActivationResult;
            DisplayText("Reader Name: " + readerName);

            CardHandle aCard = aCardSlot.AcquireCard((CardTypes.T0 | CardTypes.T1), out nActivationResult);
            if (nActivationResult != CardActivationResult.Success)
            {
                Debug.Assert(aCard == null);

                switch (nActivationResult)
                {
                    case CardActivationResult.NoCard:
                        m_aPromptLabel.Text = readerName + ": Please insert card ...";
                        break;
                    case CardActivationResult.UnresponsiveCard:
                        m_aPromptLabel.Text = readerName + ": Unresponsive card.";
                        break;
                    case CardActivationResult.InUse:
                        m_aPromptLabel.Text = readerName + ": Card in use";
                        break;
                    default:
                        m_aPromptLabel.Text = readerName + ": Can't power up card!";
                        break;
                }
                return;
            }
            m_aPromptLabel.Text = aCardSlot.CardTerminalName + ": Found card";
            DisplayText("Found card in reader " + aCardSlot.CardTerminalName);
            DisplayReaderProperties(aCardSlot);

           //aCardSlot.BeginTransaction();

            try

               

            // We are doing a few things here that any card system should support.
            // Note that the CardHandle represents the combination of card terminal and 
            // powered-up card.
            {
                bool checkForFileSystem = false;

                DisplayText("card inserted");

                // =========================== ATR DETECTION ======================================
                // Every card accessed through PC/SC must return an Answer To Reset (ATR). 
                // So let's see what we've got here.
                byte[] atr = aCard.GetATR();
                if (atr.Length == 0) throw new Exception("Invalid ATR");
                DisplayText("ATR: " + CardHex.FromByteArray(atr, 0, atr.Length));
                // ================================================================================

                // Go a little deeper: is this a contact or contactless card system we are dealing with?
                isContactlessInterface = SmartCardAPI.CardModule.CLICS.ReaderInterface.IsContactless(aCardSlot.CardTerminalName);

                if (isContactlessInterface)
                {
                    DisplayText("Contactless card interface detected");
                  
                    // let's check if this is a PC/SC 2.01 Part 3 compatible card & reader system
                    // we are using methods residing in CardModule.CLICS (contactless card integrated chip system)
                    ClicsCard contactlessCard = new ClicsCard(aCard);
                    if(contactlessCard.IsValid)
                    {
                        if (contactlessCard.IsStorageCard)
                        {
                            DisplayText("Storage card: " + contactlessCard.CardName);
                        }
                        else
                        {
                            DisplayText("Processor card (based on ATR)");
                        }
                        byte[] uid = contactlessCard.UID;
                        if (uid != null)
                        {
                            DisplayText("UID: " + CardHex.FromByteArray(uid) + " (via CardModule.CLICS)");

                            // UID via explicit APDU EXCHANGE
                            // Known issues:
                            // SendCommand with CLA=0xFF can cause an exception with some smart card systems,  
                            // triggered by an "Unknown Error" (-2146435025) on PC/SC level. 
                            byte CL_CLA = 0xFF;
                            byte CL_INS_GET_UID = 0xCA;
                            byte P1 = 0;
                            byte P2 = 0;
                            CardCommandAPDU aCmdAPDU = new CardCommandAPDU(CL_CLA, CL_INS_GET_UID, P1, P2, 256);
                            CardResponseAPDU aRespAPDU;
                            aRespAPDU = aCard.SendCommand(aCmdAPDU);
                            if (!aRespAPDU.IsSuccessful)
                            {
                                DisplayText("WARNING - 0x" + CardHex.FromWord(aRespAPDU.SW) + " - " + CardISO7816.StringifyStatusWord(aRespAPDU.SW));
                                DisplayText("Can't read contactless card's UID (Is this might be a contact card?");
                            }
                            else
                            {
                                byte[] uidWithSw12 = aRespAPDU.GenerateBytes();
                                if (uidWithSw12.Length < 2) throw new Exception("Invalid UID");
                                DisplayText("UID: " + CardHex.FromByteArray(uidWithSw12, 0, uidWithSw12.Length - 2) + " (via explicit command APDU)");
                            }
                        }

                    }

                }

                else if (checkForFileSystem)
                {
                    DisplayText("checking for file system");
                    // ============================================================================
                    // unfortunately there is no command that is available for all contact cards
                    // if your card has a file system, try to select a master file
                    // if you have a multi-application card, it usually allows you to select an 
                    // application based on it's application identifier (AID)
                    // SmartCardAPI implemented a few ISO 7816 compliant commands. Examples
                    // aCard.SelectFile() ... to select a file
                    // aCard.VerifyPin()  ... to verify a PIN
                    // aCard.ReadBinary/WriteBinary
                    // aCard.ReadRecord/UpdateRecord
                    // aCard.SelectRoot/SelectApplication/SelectRoot

                    // we try selecting a master file 
                    CardResponseAPDU selectRootResponse = aCard.SelectRoot();
                    if(!selectRootResponse.IsSuccessful)
                    {
                        DisplayText("SelectRoot() returned StatusWord 0x" + CardHex.FromWord(selectRootResponse.SW) + " - " + CardISO7816.StringifyStatusWord(selectRootResponse.SW));
                    }
                    else
                    {
                        DisplayText("OK-select root via built-in method");

                        // let's do the same root selection using an explicit command APDU 
                        byte[] rootFileID = { 0x3F, 0x00 };
  
                        CardCommandAPDU aSelectFileAPDU = new CardCommandAPDU(CardISO7816.CLA, CardISO7816.INS_SELECT, 0x00, 0x0C, rootFileID);
                        selectRootResponse = aCard.SelectRoot();
                        if (!selectRootResponse.IsSuccessful)
                        {
                            DisplayText(CardISO7816.StringifyStatusWord(selectRootResponse.SW));
                        }
                        else
                        {
                            DisplayText("OK-select root via explicit command APDU 00A4000C3F00");
                        }
                    }

                }

            }
            catch (Exception x)
            {
                Trace.WriteLine(x.ToString());
                DisplayText(x.ToString());
                m_aPromptLabel.Text = "Card access error";
            }
            finally
            {
                aCard.Dispose(); // release card handle
                DisplayText("Please modify this sample code according to your card specifications.");
                DisplayText("======== Sample code for the following cards is available ========");
                DisplayText("CAC            - Common Access card (CAC)");
                DisplayText("DESFire EV1    - NXP Mifare DESFire EV1");
                DisplayText("eGK            - German electronic health insurance card");
                DisplayText("EMV            - EMV credit/debit card");
                DisplayText("GeldKarte      - German e-purse card");
                DisplayText("iCLASS         - HID Global iCLASS card");
                DisplayText("iCode          - NXP iCode card");
                DisplayText("MemoryCard     - storage (synchronous) cards: SLE 4442, AT24Cxx etc.");
                DisplayText("Mifare         - NXP Mifare Classic");
                DisplayText("NfcTag         - Mifare Ultralight, NTAG");
                DisplayText("PIV            - PIV card");
                DisplayText("ProxCard       - HID 125KHz Proximity card");
                DisplayText("SEOS         - HID Global SEOS card");
                DisplayText("SIMcard        - phone SIM card");
                DisplayText("============================= END ================================");
            }
        }

        /// <summary>
        /// 
        /// </summary>

        public void PromptAnyCard(string readerName)
        {
            if (CardTerminalManager.Singleton.SlotCount == 0)
            {
                Prompt("Connect reader ...");
                MainTextBox.Text = "";
                DisplayText("waiting for reader");
            }
            else
            {
                Prompt(readerName + ": Insert card ...");
                MainTextBox.Text = "";
                DisplayText(readerName + ": waiting for card");
            }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HelloCardForm));
            this.m_aLinkLabel = new System.Windows.Forms.LinkLabel();
            this.m_aCopyrightLabel = new System.Windows.Forms.Label();
            this.MainTextBox = new System.Windows.Forms.RichTextBox();
            this.m_aApiVersion = new System.Windows.Forms.Label();
            this.m_aPromptLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // m_aLinkLabel
            // 
            this.m_aLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_aLinkLabel.AutoSize = true;
            this.m_aLinkLabel.Location = new System.Drawing.Point(340, 398);
            this.m_aLinkLabel.Name = "m_aLinkLabel";
            this.m_aLinkLabel.Size = new System.Drawing.Size(152, 16);
            this.m_aLinkLabel.TabIndex = 1;
            this.m_aLinkLabel.TabStop = true;
            this.m_aLinkLabel.Text = "https://smartcard-api.com/";
            this.m_aLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelLinkClicked);
            // 
            // m_aCopyrightLabel
            // 
            this.m_aCopyrightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_aCopyrightLabel.AutoSize = true;
            this.m_aCopyrightLabel.Location = new System.Drawing.Point(12, 398);
            this.m_aCopyrightLabel.Name = "m_aCopyrightLabel";
            this.m_aCopyrightLabel.Size = new System.Drawing.Size(234, 16);
            this.m_aCopyrightLabel.TabIndex = 0;
            this.m_aCopyrightLabel.Text = "Copyright 2004-2020 CardWerk Technologies";
            // 
            // MainTextBox
            // 
            this.MainTextBox.Location = new System.Drawing.Point(15, 43);
            this.MainTextBox.Name = "MainTextBox";
            this.MainTextBox.ReadOnly = true;
            this.MainTextBox.Size = new System.Drawing.Size(500, 336);
            this.MainTextBox.TabIndex = 6;
            this.MainTextBox.Text = "";
            // 
            // m_aApiVersion
            // 
            this.m_aApiVersion.AutoSize = true;
            this.m_aApiVersion.Location = new System.Drawing.Point(12, 382);
            this.m_aApiVersion.Name = "m_aApiVersion";
            this.m_aApiVersion.Size = new System.Drawing.Size(45, 16);
            this.m_aApiVersion.TabIndex = 7;
            this.m_aApiVersion.Text = "version";
            // 
            // m_aPromptLabel
            // 
            this.m_aPromptLabel.Font = new System.Drawing.Font("Trebuchet MS", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_aPromptLabel.Location = new System.Drawing.Point(12, 18);
            this.m_aPromptLabel.Name = "m_aPromptLabel";
            this.m_aPromptLabel.Size = new System.Drawing.Size(416, 34);
            this.m_aPromptLabel.TabIndex = 2;
            this.m_aPromptLabel.Text = "Insert card ...";
            // 
            // HelloCardForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(538, 419);
            this.Controls.Add(this.m_aApiVersion);
            this.Controls.Add(this.MainTextBox);
            this.Controls.Add(this.m_aPromptLabel);
            this.Controls.Add(this.m_aLinkLabel);
            this.Controls.Add(this.m_aCopyrightLabel);
            this.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HelloCardForm";
            this.Text = "HelloCard.WinForm 2.1.20.101 - card detection with SmartCard-API";
            this.Load += new System.EventHandler(this.HelloCardForm_Load);
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
            MainTextBox.ScrollToCaret();
            MainTextBox.Refresh();
        }

        /// <summary>
        /// Displays number of readers currently available. Unless overwritted by the host application,
        /// the requested number of readers is at least one or the number of readers connected at program 
        /// start. Whichever is greater.
        /// </summary>
        public void DisplayNumberOfAvailableReaders()
        {
            int slotCount = CardTerminalManager.Singleton.SlotCount;
            int slotCountMinimum = CardTerminalManager.Singleton.SlotCountMinimum;
            DisplayText("Available readers: " + slotCount + " of " + slotCountMinimum + " requested readers.");
            if (slotCount < slotCountMinimum)
            {
                Debug.WriteLine("WARNING: not all readers connected!");
                Prompt("Connect reader ...");
            }
        }

        /// <summary>
        /// We display reader properties available via CardTerminal class. Note that availability
        /// of these properties depends on the terminal.
        /// </summary>
        /// <param name="aCardSlot">current slot, activated and with powered card</param>
        public void DisplayReaderProperties(CardTerminalSlot aCardSlot)
        {
            string defaultInfo = "not available";
            // we now have a reader and a powered card

            DisplayText("*** Card terminal info ***");

            string manufacturerName = aCardSlot.CardTerminal.ManufacturerName;
            if (manufacturerName == null) manufacturerName = defaultInfo;
            DisplayText("manufacturer: " + manufacturerName);

            string productName = aCardSlot.CardTerminal.ProductName;
            if (productName == null) productName = defaultInfo;
            DisplayText("name:      " + productName);

            string productVersion = aCardSlot.CardTerminal.ProductVersion;
            if (productVersion == null) productVersion = defaultInfo;
            DisplayText("version: " + productVersion);

            string productSerialNumber = aCardSlot.CardTerminal.ProductSerialNumber;
            if (productSerialNumber == null) productSerialNumber = defaultInfo;
            DisplayText("serial number: " + productSerialNumber);

            string productAdditionalInfo = aCardSlot.CardTerminal.ProductAdditionalInfo;
            if (productAdditionalInfo == null) productAdditionalInfo = defaultInfo;
            DisplayText("additional info:  " + productAdditionalInfo);

            string physicalConnection = aCardSlot.CardTerminal.PhysicalConnection;
            if (physicalConnection == null) physicalConnection = defaultInfo;
            DisplayText("physical connection:  " + physicalConnection);

            DisplayText("*** End of card terminal info ***");

        }

        private void HelloCardForm_Load(object sender, EventArgs e)
        {

        }
    }
}


