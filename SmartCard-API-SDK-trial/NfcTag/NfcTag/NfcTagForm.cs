// --------------------------------------------------------------------------------------------
// HelloNfcTagForm.cs
// CardWerk SmartCard API
// Copyright © 2004-2019 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 

// HISTORY
// 08MAR2011 MJ translate messages to English
// 24MAR2011 MJ simplified; show ATR when connected
// 29MAR2011 MJ contactless reader detection via reader name analysis contactless: OMNIKEY and -CL
// 31MAR2011 MJ catch communication errors in unmanaged module (driver)
// 13JAN2012 MJ improve error message during StartupCardTerminalManager()
// 14JAN2012 MJ stopping smart card service causes CardTerminalLost event, exits application
// 10APR2012 MJ main text window for easier, more versatile API feedback
// 15MAY2013 MJ PC/SC 2.01 part 3 storage card support
// 25NOV2013 MJ add support for found reader event and reader recovery 
// 14OCT2016 MJ generic/transparent sessions with OMNIKEY 5021, 5427 to access memory page 16 and up
// 17JAN2017 MJ support Ultralight and Ultralight C (raw, not-NFC-formatted)
// 01MAR2019 MJ set known card type to "" to trigger discovery
// 05AUG2019 MJ tested signed libs with OMNIKEY 5021, OMNIKEY 5022

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.NfcTag;


namespace HelloNfcTag
{
    /// <summary>
    /// Sample application to show how to connect to a smart card terminal and card system.
    /// It demonstrates usage of care and terminal events and displays card detection 
    /// and card ATR.
    /// </summary>

    class HelloNfcTagForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label m_aCopyrightLabel;
        private System.Windows.Forms.LinkLabel m_aLinkLabel;
        private RichTextBox MainTextBox;
        private Label m_aPromptLabel;
        private Label m_aApiVersion;
        private Label m_aApiVersionNfcTagApi;
        private int terminalFoundCount = 0;
        private int terminalLostCount = 0;

        /// <summary>
        /// Standard constructor.
        /// </summary>

        public HelloNfcTagForm()
        {
            InitializeComponent();
            m_aApiVersion.Text = SMARTCARDAPI.ApiVersionInfo;
            m_aApiVersionNfcTagApi.Text = NFCTAGAPI.API_VERSION;
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
                HelloNfcTagForm aHelloCardForm = new HelloNfcTagForm();

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
                CardTerminalManager.Singleton.Startup(true);
                if (CardTerminalManager.Singleton.SlotCount == 0)
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
                    "Unable to run CardTerminalConfigurator. Will " +
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
                // 01APR2011
                // We catch any exceptions during card I/O. This is particularly important
                // for fuzzy communication conditions. Example: a contactless card that 
                // is not in the field throughout the whole I/O might cause an error within 
                // the unmanaged Windows API code. SmartCardAPI catches this in a general 
                // exception.
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
                terminalFoundCount++;
                terminalLostCount--; 
                Debug.WriteLine("TerminalFoundEvent: count=" + terminalFoundCount);
                object[] vParms = new object[2];
                vParms[0] = aSender;
                vParms[1] = aEventArgs;
                base.BeginInvoke(new CardTerminalEventHandler(TerminalFoundEvent), vParms);
            }
            else
            {
                if (CardTerminalManager.Singleton.StartedUp)
                {
                    DisplayNumberOfAvailableReaders();
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
                terminalLostCount++;
                terminalFoundCount--; 
                Debug.WriteLine("TerminalLostEvent: count=" + terminalLostCount);
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
            bool USE_TRANSACTION_PROTECTION = false; // transactions supported: 5x21 readers
            string readerName = aCardSlot.CardTerminalName;

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

            // remove to analyze 5022/ntag issue
            if (USE_TRANSACTION_PROTECTION)
            {
                aCardSlot.BeginTransaction();
            }

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
                // ============================ READ UID ==========================================
                CardCommandAPDU getUid = new CardCommandAPDU(0xFF, 0xCA, 0, 0, 256);
                CardResponseAPDU getUidResponse = aCard.SendCommand(getUid);
                if (!getUidResponse.IsSuccessful) throw new Exception("Can't access UID");
                byte[] uid = getUidResponse.GetData();
                DisplayText("UID: " + CardHex.FromByteArray(uid));

                // ================================================================================
                // if you are experimenting issues with unexpected memory size:
                // use empty string to trigger auto detection instead of "Ultralight"
                // this will populate card discovery and initialize page size and memory size to correct
                // values if they are advertised in card capability data
                // tested values for known cards:
                // - Ultralight
                // - Utralight C
                // ================================================================================
                string knownExpectedCardType = "";
                NfcTag nfcTag = new NfcTag(aCard, knownExpectedCardType); // will also work with Ultralight C card's lower 16 pages
              
                if(nfcTag==null)
                {
                    throw new Exception("ERROR: can't instantiate NFC Tag object");
                }
                if(!nfcTag.IsValid)
                {
                    throw new Exception("ERROR: unknown card");                   
                }
                // get card info (name, memory, pages)
                DisplayText("Card type: " + nfcTag.Name);
                DisplayText("Memory    (total): " + nfcTag.Memory.ToString());
                DisplayText("Pages (incl. cfg): " + nfcTag.Pages.ToString());
                DisplayText("Page size: " + nfcTag.PageSize.ToString());
                DisplayText("Read NFC tag memory.");

                DisplayText(String.Format("Trying to read all {0} pages", nfcTag.Pages));
                byte[] readBuffer = new byte[16];
                int numberOfPagesToRead = 4; // block size is 16, therefore we can only read up to 4 pages at a time
                for (int page = 0; page < nfcTag.Pages; page += numberOfPagesToRead) 
                {
                   if ((page + 4) > nfcTag.Pages) // only read remaining pages without rollover to page 0
                   {
                       numberOfPagesToRead = nfcTag.Pages - page; 
                       Array.Resize(ref readBuffer, numberOfPagesToRead * 4);
                   }
                   readBuffer = nfcTag.Read(page, numberOfPagesToRead);
                   if (readBuffer == null)
                   {
                       throw new Exception("ERROR: Can't read page " + page.ToString());
                   }
                   else
                   {
                       DisplayText("page (" + page.ToString() + "-" + (page + numberOfPagesToRead - 1).ToString() + "): " + CardHex.FromByteArray(readBuffer));
                   }
                }
                DisplayText(String.Format("OK - read {0} pages", nfcTag.Pages));
                
                bool WRITE_NDEF_RECORD = false;

                if (WRITE_NDEF_RECORD)
                {
                    DisplayText("Write test: writing an NDEF record");
                    // this will overwrite pages 4..13
                    byte[] ndefRecord = CardHex.ToByteArray("0325D10220537091010D5503636172647765726B2E636F6D51010B5402656E436172645765726BFE");
                    int startPage = 0x04;
                    if (!nfcTag.Write(startPage, ndefRecord))
                    {
                        DisplayText("Can't write NDEF record " + CardHex.FromByteArray(ndefRecord));
                    }
                }
                else
                {
                    DisplayText("skipped - WRITE_NDEF_RECORD");
                }

                bool WRITE_TO_BLOCK_16 = false;

                if (WRITE_TO_BLOCK_16) // work in progress!! only 
                {
                    byte[] allZeroes    = new byte[16]; // original data is all zero
                    byte[] page16Backup = new byte[16]; 
                    byte[] testData     = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
                    int startPage = 16;
                    //before
                    readBuffer = nfcTag.Read(startPage, 4);
                    if (page16Backup == null)
                    {
                        DisplayText("Can't access page 16");
                    }
                    else
                    {
                        DisplayText("page 16 before: " + CardHex.FromByteArray(readBuffer));
                    }

                    DisplayText("Write test: writing page 16..19");
                    if (!nfcTag.Write(startPage, testData))
                    {
                        DisplayText("Can't write data " + CardHex.FromByteArray(testData));
                    }
                    // after
                    readBuffer = nfcTag.Read(startPage, 4);
                    if (page16Backup == null)
                    {
                        DisplayText("Can't access page 16");
                    }
                    else
                    {
                        DisplayText("page 16..19 after write: " + CardHex.FromByteArray(readBuffer));
                    }


                    // back to zeroes
                    if (!nfcTag.Write(startPage, allZeroes))
                    {
                        DisplayText("Can't write data " + CardHex.FromByteArray(allZeroes));
                    }

                    
                    readBuffer = nfcTag.Read(startPage, 4);
                    if (page16Backup == null)
                    {
                        DisplayText("Can't access page 16");
                    }
                    else
                    {
                        DisplayText("page 16..19 after init: " + CardHex.FromByteArray(readBuffer));
                    }
                }
                else
                {
                    DisplayText("skipped - WRITE_TO_BLOCK_16");
                }
                DisplayText("END of test");
                
            }
            catch (Exception x)
            {
                Trace.WriteLine(x.ToString());
                DisplayText(x.Message.ToString());
                m_aPromptLabel.Text = "Card access error";
            }
            finally
            {
                // remove to analyze 5022/ntag issue
                if (USE_TRANSACTION_PROTECTION)
                {
                    aCardSlot.EndTransaction();
                }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HelloNfcTagForm));
            this.m_aLinkLabel = new System.Windows.Forms.LinkLabel();
            this.m_aCopyrightLabel = new System.Windows.Forms.Label();
            this.MainTextBox = new System.Windows.Forms.RichTextBox();
            this.m_aApiVersion = new System.Windows.Forms.Label();
            this.m_aPromptLabel = new System.Windows.Forms.Label();
            this.m_aApiVersionNfcTagApi = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // m_aLinkLabel
            // 
            this.m_aLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_aLinkLabel.AutoSize = true;
            this.m_aLinkLabel.Location = new System.Drawing.Point(261, 393);
            this.m_aLinkLabel.Name = "m_aLinkLabel";
            this.m_aLinkLabel.Size = new System.Drawing.Size(216, 18);
            this.m_aLinkLabel.TabIndex = 1;
            this.m_aLinkLabel.TabStop = true;
            this.m_aLinkLabel.Text = "http://www.smartcard-api.com/";
            this.m_aLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelLinkClicked);
            // 
            // m_aCopyrightLabel
            // 
            this.m_aCopyrightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_aCopyrightLabel.AutoSize = true;
            this.m_aCopyrightLabel.Location = new System.Drawing.Point(17, 393);
            this.m_aCopyrightLabel.Name = "m_aCopyrightLabel";
            this.m_aCopyrightLabel.Size = new System.Drawing.Size(282, 18);
            this.m_aCopyrightLabel.TabIndex = 0;
            this.m_aCopyrightLabel.Text = "Copyright 2004-2016 CardWerk Technologies";
            // 
            // MainTextBox
            // 
            this.MainTextBox.Location = new System.Drawing.Point(21, 53);
            this.MainTextBox.Name = "MainTextBox";
            this.MainTextBox.ReadOnly = true;
            this.MainTextBox.Size = new System.Drawing.Size(700, 378);
            this.MainTextBox.TabIndex = 6;
            this.MainTextBox.Text = "";
            // 
            // m_aApiVersion
            // 
            this.m_aApiVersion.AutoSize = true;
            this.m_aApiVersion.Location = new System.Drawing.Point(18, 453);
            this.m_aApiVersion.Name = "m_aApiVersion";
            this.m_aApiVersion.Size = new System.Drawing.Size(112, 18);
            this.m_aApiVersion.TabIndex = 7;
            this.m_aApiVersion.Text = "ApiVersionSCAPI";
            // 
            // m_aPromptLabel
            // 
            this.m_aPromptLabel.Font = new System.Drawing.Font("Trebuchet MS", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_aPromptLabel.Location = new System.Drawing.Point(17, 22);
            this.m_aPromptLabel.Name = "m_aPromptLabel";
            this.m_aPromptLabel.Size = new System.Drawing.Size(582, 42);
            this.m_aPromptLabel.TabIndex = 2;
            this.m_aPromptLabel.Text = "Insert card ...";
            // 
            // m_aApiVersionNfcTagApi
            // 
            this.m_aApiVersionNfcTagApi.AutoSize = true;
            this.m_aApiVersionNfcTagApi.Location = new System.Drawing.Point(18, 473);
            this.m_aApiVersionNfcTagApi.Name = "m_aApiVersionNfcTagApi";
            this.m_aApiVersionNfcTagApi.Size = new System.Drawing.Size(146, 18);
            this.m_aApiVersionNfcTagApi.TabIndex = 8;
            this.m_aApiVersionNfcTagApi.Text = "ApiVersionNFCTAGAPI";
            // 
            // HelloNfcTagForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(7, 16);
            this.ClientSize = new System.Drawing.Size(538, 419);
            this.Controls.Add(this.m_aApiVersionNfcTagApi);
            this.Controls.Add(this.m_aApiVersion);
            this.Controls.Add(this.MainTextBox);
            this.Controls.Add(this.m_aPromptLabel);
            this.Controls.Add(this.m_aLinkLabel);
            this.Controls.Add(this.m_aCopyrightLabel);
            this.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HelloNfcTagForm";
            this.Text = "HelloNfcTag 1.0.17.602 - card detection with SmartCard-API";
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
            terminalFoundCount = slotCount;
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


