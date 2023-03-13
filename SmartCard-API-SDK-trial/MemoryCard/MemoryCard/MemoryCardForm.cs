// --------------------------------------------------------------------------------------------
// HelloMemoryCardForm.cs
// CardWerk SmartCard API
// Copyright © 2014-2019 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 
// Sample program to show how to access storage cards (a/k/a memory cards, synchronous cards, secure EEPROM ..)
// using OMNIKEY contact readers with chipsets that require usage of OMNIKEY proprietary scardsyn.dll
// Other readers such as SCM/Identive readers are not supported by this sample code. For those, use memory card or CT-API adapter instead.
//
// ========== I2C cards =========
// I2C cards from Atmel (Inside):
// AT24C01A, AT24C02, AT24C04, AT24C08, AT24C1024, AT24C128, AT24C16, AT24C164, AT24C256, AT24C32, AT24C512, AT24C64, AT24CS128, AT24CS256
// I2C cards from Gemalto (GemPlus):
// GFM2K,GFM32K,GFM4K
// I2C cards from ST-Microelectronics:
// M14128,M14256,M14C04,M14C16,M14C32,M14C64,ST14C02C,ST14C04C,ST14E32
//
// ========== 2WBP cards =========
// SLE4432, 32-bit PROM, 256 byte EEPROM
// SLE4442, 32-bit PROM, 256 byte EEPROM, 3 byte PSC, error counter
//
// ========== 3WBP cards =========
// SLE4428, 1024 byte EEPROM, each byte protectable, 2-byte PIN
//
// HISTORY
// 09MAY2019 MJ AT24C256 is default
// 29MAY2017 MJ build against .NET v 4
// 18JUN2015 MJ complete 4442 integration, start 4428 integration (read working)
// 06JUN2015 MJ rename sample from HelloSyncAPI to HelloMemoryCard
// 04JUN2015 MJ move code to CardModule.MemoryCard and ReaderModule.Omnikey
// 02JUN2014 MJ support SLE 4442 and SLE 4432 
// 14MAY2014 MJ introduce generic read/write to allow support of additional cards without API changes
// 08MAY2014 MJ initial version to demo access of Atmel AT24C256 via OMNIKEY SyncAPI 

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Security.Cryptography;
using Subsembly.SmartCard;
using SmartCardAPI.ReaderModule.Omnikey;
using SmartCardAPI.CardModule.MemoryCard; // access synchronous cards such as SLE4448, AT 

namespace SampleCode_MemoryCard
{
    /// <summary>
    /// Sample application to show how to connect to a smart card terminal and card system.
    /// It demonstrates usage of care and terminal events and displays card detection 
    /// and card ATR.
    /// </summary>

    class MemoryCardForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label m_aCopyrightLabel;
        private System.Windows.Forms.LinkLabel m_aLinkLabel;
        private RichTextBox MainTextBox;
        private Label m_aPromptLabel;
        private Label label1;
        private ComboBox comboBox_cardTypeSelection;
        private Label m_aApiVersion;
        static bool ALWAYS_LOOK_FOR_MORE_CARD_READERS = true; // increases the number of readers we are looking for

        /// <summary>
        /// Standard constructor.
        /// </summary>

        public MemoryCardForm()
        {
            InitializeComponent();
            this.Text = "MemoryCard rev. 29OCT2019";
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
            string sTraceFilePath = Path.Combine(sPath, "HelloMemoryCard.txt");
            Debug.WriteLine("Creating trace file: " + sTraceFilePath);
            Stream aTraceFile = File.Create(sTraceFilePath);
            TextWriterTraceListener aListener = new TextWriterTraceListener(aTraceFile);
            Trace.Listeners.Add(aListener);

            try
            {
                MemoryCardForm aHelloSyncAPIForm = new MemoryCardForm();

                // Run the primary application form.

                Application.Run(aHelloSyncAPIForm);
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

            CardTerminalManager.Singleton.CardInsertedEvent +=
                new CardTerminalEventHandler(InsertedEvent);
            CardTerminalManager.Singleton.CardRemovedEvent +=
                new CardTerminalEventHandler(RemovedEvent);
            CardTerminalManager.Singleton.CardTerminalLostEvent +=
                new CardTerminalEventHandler(TerminalLostEvent);
            CardTerminalManager.Singleton.CardTerminalFoundEvent +=
                new CardTerminalEventHandler(TerminalFoundEvent);

            StartupCardTerminalManager();
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
        int StartupCardTerminalManager()
        {
            //bool fStartedUp = false;

            try
            {
                // Startup the SmartCard API. The parameter "true" means that any
                // PC/SC smart card reader will automatically be added to the smart card
                // configuration registry. If startup fails, then this will throw an
                // exception.
                int nCountReaders = CardTerminalManager.Singleton.Startup(true);
                if(nCountReaders>0)
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
                    MessageBox.Show("No reader available",
                    "WARNING: Singleton Startup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop,
                    MessageBoxDefaultButton.Button1);
                }
            }
            catch (Exception x)
            {
                Trace.WriteLine(x.ToString());

                MessageBox.Show(
                    "Unable to run CardTerminal. Will " +
                    "exit this application.",
                    "SmartCardAPI sample application",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop,
                    MessageBoxDefaultButton.Button1);

                //fStartedUp = false;
            }

            return CardTerminalManager.Singleton.SlotCount;
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
                try
                {
                    this.AnalyzeCard(aEventArgs.Slot);
                }
                catch (Exception x)
                {
                    DisplayText("AnalyzeCard failed with " + x.ToString());
                    return;
                }
                DisplayText("OK-AnalyzeCard completed.");

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
                MainTextBox.Text = "";
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
                MainTextBox.Text = "";
                if (CardTerminalManager.Singleton.StartedUp)
                {
                    DisplayText("Lost reader: " + aEventArgs.Slot.CardTerminalName);
                    if (CardTerminalManager.Singleton.SlotCount == 0)
                    {
                        this.m_aPromptLabel.Text = "Connect reader ...";
                        // start looking for reader insertion
                        // done automatically by the singleton. The singleton raises a "new reader" event if it 
                        // finds a new reader.

                    }
                    else if (CardTerminalManager.Singleton.SlotCountMinimum + 1 > CardTerminalManager.Singleton.SlotCount)
                    {
                        CardTerminalManager.Singleton.SlotCountMinimum -= 1;
                    }
                    DisplayNumberOfAvailableReaders();
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="aCardSlot"></param>

        public void AnalyzeCard(CardTerminalSlot aCardSlot)
        {
            // Acquire any processor card (T=0 or T=1) that may be present in the given card terminal slot
            string readerName = aCardSlot.CardTerminalName;

            CardActivationResult nActivationResult;
            DisplayText("Reader Name: " + readerName);
            CardTypes anySyncCard = readerName.Contains("OMNIKEY") ? CardTypes.T0 : (CardTypes.T0 | CardTypes.T1);

            CardHandle aCard = aCardSlot.AcquireCard(anySyncCard, out nActivationResult); // for OMNIKEY readers, synchronous cards are mapped to T=0
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

            try

            // We are doing a few things here that any card system should support.
            // Note that the CardHandle represents the combination of card terminal and 
            // powered-up card.
            {
                
                // =========================== ATR DETECTION ======================================
                // Every card accessed through PC/SC must return an Answer To Reset (ATR). 
                // So let's see what we've got here.
                byte[] atr = aCard.GetATR();
                if (atr.Length == 0) throw new Exception("Invalid ATR");
                DisplayText("ATR: " + CardHex.FromByteArray(atr, 0, atr.Length));


                string cardType = comboBox_cardTypeSelection.Text;
                int length = 0;
                if (cardType == "")
                {
                    //DisplayText("WARNING - no card type selected.");

                    cardType = "AT24C256"; // tested on scardsyn, aviator pcsc, vendor specific
                    //cardType ="SLE4442"; //tested on xchip and aviator
                    //cardType = "SLE4428"; // tested
                    //cardType = "AT24C1024"; // tested  on scardsyn, aviator pcsc,


                    comboBox_cardTypeSelection.Text = cardType; // just for feedback on GUI level
                    // you can hard-code your card type here:
                }

                DisplayText(MemoryCard.ApiInfo()); // API info is always available
                MemoryCard memoryCard = new MemoryCard(aCard, cardType); // takes care of everything necessary to call proprietary, unmanaged OMNIKEY sync API code
                if (!memoryCard.IsReady)
                {
                    DisplayText("invalid card or reader");
                }
                else
                {
                    DisplayText("CardInfo: " + memoryCard.CardInfo);
                    length = memoryCard.PageSize;

                    if (cardType == "SLE4442" || cardType == "SLE4432" || cardType == "SLE5542")
                    {
                        byte[] defaultPIN = { 0xff, 0xff, 0xff };
                        byte[] currentPIN = defaultPIN;
                        if (!test_4442(memoryCard, currentPIN))
                        {
                            throw new Exception("memory card access test failed");
                        }
                        DisplayText("OK-passed SLE 4442 test");
                    }
                    else if (cardType == "SLE4428" || cardType == "SLE5528")
                    {
                        byte[] pin = { 0xFF, 0xFF };
                        if (!test_4428(memoryCard, pin))
                        {
                            throw new Exception("memory card access test failed");
                        }
                        DisplayText("OK-passed SLE 4428/5528 test");
                    }

                    else if (cardType == "AT24C1024" || cardType == "AT24C256")
                    {
                        bool includeWriteAccessTest = false;
                        if (!test_AT24C1024_or_256(memoryCard,includeWriteAccessTest))
                        {
                            throw new Exception("memory card access test failed");
                        }
                        DisplayText(string.Format("OK-passed {0} test.", cardType));
                        
                    } //AT24C1024, AT24C256 test

                    else
                    {
                        int address = 0;
                        //byte[] readBuffer = StorageCard.ReadData(0, StorageCard.PageSize);
                        byte[] readBuffer = memoryCard.Read(address, length);
                        DisplayText("OK-ReadData");
                        byte[] writeBuffer = new byte[length];

                        // ===================== WARNING ==========================================
                        // by default we don't change data to be in the safe side
                        // only change echoOffset if you know what you are doing 
                        // as it may corrupt your card data  
                        int echoOffset = 0;
                        // ========================================================================
                        for (int k = 0; k < writeBuffer.Length; k++)
                        {
                            writeBuffer[k] = (byte)(readBuffer[k] + echoOffset);
                        }
                        bool writeResult = memoryCard.Write(0, writeBuffer);
                        if(writeResult == false)
                        {
                            DisplayText("Write access failed");
                            throw new Exception("memory card write access test failed");
                        }
                        DisplayText("OK-WriteData");
                        readBuffer = memoryCard.Read(0, memoryCard.PageSize);
                        if (!CardHex.IsEqual(writeBuffer, readBuffer)) throw new Exception("read/write main memory test data integrity error");
                        DisplayText("OK-read/write main memory test");
                    }
                }

            } // try

            catch (Exception x)
            {
                Debug.WriteLine("Exception: " + x.ToString());
                m_aPromptLabel.Text = "Card access error"; 
            }
            finally
            {
               // ctr = aCardSlot.EndTransaction();
               // if (ctr != CardTransactionResult.Success)
               // {
               //     Debug.WriteLine("WARNING: unexpected transaction end ");
             //   }
                aCard.Dispose(); // release card handle
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MemoryCardForm));
            this.m_aLinkLabel = new System.Windows.Forms.LinkLabel();
            this.m_aCopyrightLabel = new System.Windows.Forms.Label();
            this.MainTextBox = new System.Windows.Forms.RichTextBox();
            this.m_aApiVersion = new System.Windows.Forms.Label();
            this.m_aPromptLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_cardTypeSelection = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // m_aLinkLabel
            // 
            this.m_aLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_aLinkLabel.AutoSize = true;
            this.m_aLinkLabel.Location = new System.Drawing.Point(340, 512);
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
            this.m_aCopyrightLabel.Location = new System.Drawing.Point(12, 512);
            this.m_aCopyrightLabel.Name = "m_aCopyrightLabel";
            this.m_aCopyrightLabel.Size = new System.Drawing.Size(234, 16);
            this.m_aCopyrightLabel.TabIndex = 0;
            this.m_aCopyrightLabel.Text = "Copyright 2004-2019 CardWerk Technologies";
            // 
            // MainTextBox
            // 
            this.MainTextBox.Location = new System.Drawing.Point(16, 157);
            this.MainTextBox.Name = "MainTextBox";
            this.MainTextBox.ReadOnly = true;
            this.MainTextBox.Size = new System.Drawing.Size(499, 336);
            this.MainTextBox.TabIndex = 6;
            this.MainTextBox.Text = "";
            // 
            // m_aApiVersion
            // 
            this.m_aApiVersion.AutoSize = true;
            this.m_aApiVersion.Location = new System.Drawing.Point(13, 496);
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
            this.m_aPromptLabel.Size = new System.Drawing.Size(514, 34);
            this.m_aPromptLabel.TabIndex = 2;
            this.m_aPromptLabel.Text = "Insert card ...";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 114);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 16);
            this.label1.TabIndex = 9;
            this.label1.Text = "CardType";
            // 
            // comboBox_cardTypeSelection
            // 
            this.comboBox_cardTypeSelection.FormattingEnabled = true;
            this.comboBox_cardTypeSelection.Items.AddRange(new object[] {
            "AT24C01A",
            "AT24C02",
            "AT24C04",
            "AT24C08",
            "AT24C1024",
            "AT24C128",
            "AT24C16",
            "AT24C164",
            "AT24C256",
            "AT24C32",
            "AT24C512",
            "AT24C64",
            "AT24CS128",
            "AT24CS256",
            "GFM2K",
            "GFM32K",
            "GFM4K",
            "M14128",
            "M14256",
            "M14C04",
            "M14C16",
            "M14C32",
            "M14C64",
            "SLE4432",
            "SLE5532",
            "SLE4442",
            "SLE5542",
            "SLE4428",
            "SLE5528",
            "ST14C02C",
            "ST14C04C",
            "ST14E32"});
            this.comboBox_cardTypeSelection.Location = new System.Drawing.Point(75, 111);
            this.comboBox_cardTypeSelection.Name = "comboBox_cardTypeSelection";
            this.comboBox_cardTypeSelection.Size = new System.Drawing.Size(211, 24);
            this.comboBox_cardTypeSelection.TabIndex = 10;
            this.comboBox_cardTypeSelection.SelectedIndexChanged += new System.EventHandler(this.comboBox_cardTypeSelection_SelectedIndexChanged);
            // 
            // MemoryCardForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(538, 533);
            this.Controls.Add(this.comboBox_cardTypeSelection);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.m_aApiVersion);
            this.Controls.Add(this.MainTextBox);
            this.Controls.Add(this.m_aPromptLabel);
            this.Controls.Add(this.m_aLinkLabel);
            this.Controls.Add(this.m_aCopyrightLabel);
            this.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MemoryCardForm";
            this.Text = "MemoryCard rev 29OCCT2019- storage card access for OMNIKEY card readers";
            this.Load += new System.EventHandler(this.HelloSyncAPIForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        /// <summary>
        /// Quick and dirty way to populate the main form with card data we explore throughout the 
        /// HelloSyncAPI sample code.
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

        private void HelloSyncAPIForm_Load(object sender, EventArgs e)
        {

        }

        private void comboBox_cardTypeSelection_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        //TEST MODULES
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memoryCard"></param>
        /// <param name="includeWriteAccessTest">// !!! CAUTION !!! true will overwrite card memory with random data  !!!!</param>
        /// <returns></returns>
        bool test_AT24C1024_or_256(MemoryCard memoryCard, bool includeWriteTest)
        {
            bool testAllMemory = false;
            int totalMem = memoryCard.MemorySize;
            int bytesRemaining = memoryCard.MemorySize;
            int loopCount = 0;
            byte[] testBuffer = new byte[memoryCard.PageSize];
            System.Diagnostics.Stopwatch watch = new Stopwatch();
            watch.Start();
            bool readError = false;

            while (loopCount < 10)
            {
                loopCount++;
                int dataSizeToRead = memoryCard.PageSize;

                testBuffer = memoryCard.Read(0, dataSizeToRead);
                if (testBuffer == null)
                {
                    Debug.WriteLine("ERROR: testBuffer returned null in loop #" + loopCount.ToString());
                    readError = true;
                    break;
                }
                //if (memoryCard.PageSize)
                if (testBuffer.Length != dataSizeToRead)
                {
                    Debug.WriteLine("ERROR: testBuffer returned wrong number of bytes in loop #" + loopCount.ToString());
                    readError = true;
                    break;
                }
                Debug.WriteLine("OK-loop #" + loopCount.ToString());
                DisplayText("OK-read test, loop #" + loopCount.ToString());
            }
            if (readError)
            {
                DisplayText("EERROR: read test: loop FAILED");
                return false;
            }

            watch.Stop();
            double elapsedTime_ms = watch.Elapsed.TotalMilliseconds;
            int numberOfBytes = loopCount * memoryCard.PageSize;
            string milliseconds = elapsedTime_ms.ToString();
            float transferRate = numberOfBytes / ((float)(elapsedTime_ms / 1000));
            DisplayText(string.Format("OK-read test: {0} Bytes/sec", transferRate.ToString("0.0")));

            if (testAllMemory)
            {
                loopCount = 0;
                int address = 0;
                bytesRemaining = memoryCard.MemorySize;
                while (bytesRemaining > 0)
                {
                    loopCount++;
                    int accessingThisTime = bytesRemaining >= memoryCard.PageSize ? memoryCard.PageSize : bytesRemaining;

                    if (includeWriteTest == true)
                    {
                        byte[] writeBuffer = new byte[accessingThisTime];
                        RandomNumberGenerator rng = RandomNumberGenerator.Create();
                        rng.GetBytes(writeBuffer);
                        if (!memoryCard.Write(address, writeBuffer))
                        {
                            DisplayText("ERROR: write test data FAILED");
                            return false;
                        }
                        byte[] readBuffer = memoryCard.Read(address, accessingThisTime);
                        if (!CardHex.IsEqual(readBuffer, writeBuffer))
                        {
                            DisplayText("ERROR: write test data integrity");
                            return false;
                        }
                        Debug.WriteLine("OK-loop #" + loopCount.ToString());
                        DisplayText("OK-read/write test, loop #" + loopCount.ToString());
                    }
                    else
                    {
                        Debug.WriteLine("OK-loop #" + loopCount.ToString());
                        byte[] readBuffer = memoryCard.Read(address, accessingThisTime);
                        if (readBuffer == null)
                        {
                            Debug.WriteLine("ERROR-read failed in loop #" + loopCount.ToString());
                            return false;
                        }
                        DisplayText("OK-read test, loop #" + loopCount.ToString());
                    }

                    address += accessingThisTime;
                    bytesRemaining -= accessingThisTime;
                    DisplayText("OK-(" + loopCount.ToString() + ") tested " + (totalMem - bytesRemaining).ToString() + " of " + totalMem.ToString());
                }
                DisplayText("OK-MemoryTest. Total = " + totalMem.ToString() + " of " + memoryCard.MemorySize.ToString() + " total memory");
            }
            return true;

        }

        /// <summary>
        /// Quick and dirty test of SLE 4442 and SLE4432 related functions exposed through OMNIKEY synchronous API
        /// </summary>
        /// <param name="storageCard"></param>
        bool test_4442(MemoryCard memoryCard_4442, byte[] currentPIN)
        {
            //let's not mess up this card; only access unprotected data
            int length = memoryCard_4442.PageSize; // 256
            int address = 0;
            int offsetUnprotectedMemory = 32; // according to specs
            byte[] readBuffer = memoryCard_4442.Read(address, length); // read all memory, including protectable data memory and manufacturer code
            if (readBuffer == null)
            {
                DisplayText("ERROR: read failed. check card, driver, reader");
                return false;
            }
            DisplayText("OK-ReadData");
            DisplayText("Manufacturer Code: " + CardHex.FromByteArray(readBuffer, 0, 1));
            DisplayText("Main memory (protectable):  " + CardHex.FromByteArray(readBuffer, 0, 31));
            DisplayText("Main Memory (standard):  " + CardHex.FromByteArray(readBuffer, 32, 224));

            int memoryZone = 1; // protection memory
            byte[] protectionMemory = memoryCard_4442.Read(0, 4, memoryZone);
            if (protectionMemory != null)
            {
                DisplayText("OK-protection memory: " + CardHex.FromByteArray(protectionMemory));
            }
            string protectedMemory   = "Protected memory: ";
            string protectableMemory = "Protectable memory: "; 
            for (int pma = 0; pma < 32; pma++)
            {
                if (memoryCard_4442.IsProtected(pma))
                {
                    protectedMemory += pma.ToString();
                    protectedMemory += ".";
                }
                else
                {
                    protectableMemory += pma.ToString();
                    protectableMemory += ".";
                }
            }
            DisplayText(protectedMemory);
            DisplayText(protectableMemory);

            // ============= VERIFY 3-byte PIN ====================
            bool pinVerified = memoryCard_4442.VerifyPIN(currentPIN);
            if (pinVerified == false)
            {
                DisplayText("ERROR: PIN verification failed");
                return false;
            }
            DisplayText("OK-VerifyPIN");

            // ========================== WARNING ===================================== 
            // CAUTION: this test overwrites protectable data and protection data
            //          only include compare and protect if you know what you are doing
            bool RUN_COMPARE_AND_PROTECT_TEST = false;
            // ========================================================================
            if (RUN_COMPARE_AND_PROTECT_TEST)
            {
                // write and protect:
                // tested 14DEC2016 with OMNIKEY xchip and aviator
                int testByteAddress = 9;
                byte[] testByte = memoryCard_4442.Read(testByteAddress, 1);
                if (testByte == null)
                {
                    DisplayText("ERROR: can't access test byte");
                    return false;
                }

                byte[] bytesToWtrite = { (byte)(testByte[0] + 1) }; // ensure we write a new value
                bool successfulWrite = memoryCard_4442.Write(testByteAddress, bytesToWtrite, WriteModeAttribute.COMPARE_AND_WRITE_AND_PROTECT); // returns 0x9000 if byte at targeted address already contains same value
                if (successfulWrite)
                {
                    DisplayText("card data protected");
                }
                else
                {
                    DisplayText("attempt to change protected card data.");
                }
            }
                                        
            byte[] writeBuffer = new byte[length - offsetUnprotectedMemory];

            // ===================== WARNING ==========================================
            // by default we don't change data to be in the safe side (with echoOffset=0)
            // only change echoOffset if you know what you are doing 
            // as it may corrupt your card data  
            int echoOffset = 0;
            // ========================================================================
            for (int k = 0; k < writeBuffer.Length; k++)
            {
                writeBuffer[k] = (byte)(readBuffer[k + offsetUnprotectedMemory] + echoOffset);
            }
            memoryCard_4442.Write(32,writeBuffer); // we only write to unprotected memory area 
            DisplayText("OK-WriteData");
            readBuffer = memoryCard_4442.Read(0, memoryCard_4442.PageSize);
            if (!CardHex.IsEqual(writeBuffer, 0, readBuffer, offsetUnprotectedMemory, writeBuffer.Length))
            {
                DisplayText("ERROR: read/write test data integrity error");
                return false;
            }
            DisplayText("OK-read/write test");

            memoryZone = 2; // security memory
            byte[] securityMemory = memoryCard_4442.Read(0, 4, memoryZone);
            if (securityMemory == null)
            {
                DisplayText("OK-security access failed.");    // Aviator: ok; xchip: fails for x64; this is a known issue due to missing entry point of third-party API DLL
            }
            else
            {
                DisplayText("OK-security memory: " + CardHex.FromByteArray(securityMemory));
            }
            

            if (pinVerified)
            {
                byte[] CARDWERK_TEST_PIN_SLE4442 = {0x31,0x32,0x33}; // CardWerk test PIN
                byte[] oldPin = currentPIN;
                byte[] newPin = CARDWERK_TEST_PIN_SLE4442;
                bool changePinResult = memoryCard_4442.ChangePIN(oldPin, newPin);
                if (!changePinResult)
                {
                    DisplayText("ERROR: PIN change failed");
                    return false;
                }

                newPin = currentPIN;
                oldPin = CARDWERK_TEST_PIN_SLE4442;
                changePinResult = memoryCard_4442.ChangePIN(oldPin, newPin);
                if (!changePinResult)
                {
                    DisplayText("ERROR: PIN change failed");
                    return false;
                }
                DisplayText("OK-ChangePIN");
            }

            // we attempt to write protectable memory area (bytes 0,1,2,3) that is usually already protected by the chip manufacturer
            // we only run this test when we read expected data usually found on SLE 4442
            byte[] expectedData   = { 0xA2, 0x13, 0x10, 0x91 };
            byte[] protectedBytes = memoryCard_4442.Read(0, 4); 
            if(CardHex.IsEqual(expectedData,protectedBytes))
            {

                protectedBytes[0] += 1; // just to ensure we are not writing the same value that's already programmed; writing identical values leads to card status 0x9000
                bool protectBytesResult = memoryCard_4442.Write(0, protectedBytes);
                if (protectBytesResult == true)
                {
                    DisplayText("WARNING: unexpected result; attempt to write 1st 4 bytes in protectable main memory should fail unless you are a chip manufacturer and these bytes are not yet protected.");
                    return false;
                }
            }    
            return true;
        }


        /// <summary>
        /// Quick and dirty test of SLE 4428 related functions exposed through OMNIKEY synchronous API
        /// </summary>
        /// <param name="storageCard"></param>
        bool test_4428(MemoryCard memoryCard_4428, byte[] currentPIN)
        {
            //let's not mess up this card; only access unprotected data
            int length = memoryCard_4428.PageSize; // 1024
            int address = 0;
            //int offsetUnprotectedMemory = 32; // according to specs
            byte[] readBuffer = memoryCard_4428.Read(address, length); // read all memory, including protectable data memory and manufacturer code
            if (readBuffer == null)
            {
                DisplayText("ERROR: read failed. check card, driver, reader");
                return false;
            }
            DisplayText("OK-ReadData");
            DisplayText("data: " + CardHex.FromByteArray(readBuffer, 0, 16) + "...." + CardHex.FromByteArray(readBuffer, readBuffer.Length-17, 16));

            bool pinVerified = false;

            // check protection flag of all data bytes
            string protectionFlags = "";
            for (int a = 0; a < 16; a++) // we could check all 1024 flags here but who has the time ;-) 
            {
                if (memoryCard_4428.IsProtected(a))
                {
                    protectionFlags += "1";
                }
                else
                {
                    protectionFlags += "0";
                }
            }
            Debug.WriteLine("protectionFlags of bytes 0..15: " + protectionFlags);
            if (currentPIN != null)
            {
                pinVerified = memoryCard_4428.VerifyPIN(currentPIN);
                if (pinVerified == false)
                {
                  DisplayText("ERROR: PIN verification with default value 0x0000 failed");
                  return false;
                }
                DisplayText("OK-PSC verified");
            }

            if (pinVerified)
            {
                byte[] oldPin = currentPIN;
                byte[] newPin = currentPIN;
                bool changePinResult = memoryCard_4428.ChangePIN(oldPin, newPin);
                if (!changePinResult)
                {
                    DisplayText("ERROR: PIN change failed");
                    return false;
                }
                DisplayText("OK-ChangePIN");
            }
            int offset = 32;
            byte[] writeBuffer = new byte[8];
            
            //byte[] writeBuffer = new byte[224];

            // ===================== WARNING ==========================================
            // by default we don't change data to be on the safe side (with echoOffset=0)
            // only change echoOffset if you know what you are doing 
            // as it may corrupt your card data  
            int echoOffset = 1;
            // ========================================================================
            for (int k = 0; k < writeBuffer.Length; k++)
            {
                writeBuffer[k] = (byte)(readBuffer[offset+k] + echoOffset);
            }
            memoryCard_4428.Write(offset, writeBuffer); // we only write to unprotected memory area 
            DisplayText("OK-WriteData");
            readBuffer = memoryCard_4428.Read(0, memoryCard_4428.PageSize);
            if (!CardHex.IsEqual(writeBuffer, 0, readBuffer, offset, writeBuffer.Length))
            {
                DisplayText("ERROR: main memory read/write test data integrity error");
                return false;
            }
            DisplayText("new data @32..39: " + CardHex.FromByteArray(readBuffer, offset, writeBuffer.Length));
            DisplayText("OK-main memory read/write test");

            return true;
        }

    }
}


