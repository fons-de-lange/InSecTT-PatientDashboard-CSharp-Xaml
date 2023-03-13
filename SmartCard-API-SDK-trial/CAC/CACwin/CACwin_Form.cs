// --------------------------------------------------------------------------------------------
// CACwin_Form.cs
// CardWerk SmartCard API (Professional)
// Copyright © 2004-2019 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 
// HIST:
// 29OCT2019 MJ move to new repo
// 20FEB2017 MJ build against .NET framework version 4.0
// 17OCT2016 MJ new: CAC card personnel instance support
// 26JUL2016 MJ change card selection sequence
// 31OCT2014 MJ add security object
// 17JUL2014 MJ add facial image access to PIV Endpoint sample code
// 27NOV2013 MJ new: support lost/found terminal event
// 21NOV2013 MJ new: protect card transaction
// 05FEB2013 MJ new: X509 certificate, card capability support 
// 26JUL2012 MJ introduce CheckInterface() to detect card interface based on reader name and UID
// 23MAY2012 MJ support access control applet used in PIV/CAC transitional and Consolidated PIV + CACv2
// 14MAY2012 MJ support for DoD CAC and NIST PIV via CardModule.CAC and CardModule.PIV
// 08MAY2012 MJ explore card before accessing it
// 03FEB2012 MJ add printed information, call m_aCardForm.Close() on card removal
// 20JAN2012 MJ all containers supported, CardDialogs for secure PIN entry
// 09NOV2011 MJ initial version derived from HelloCard

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.CAC;


namespace SampleCode.CAC
{
	/// <summary>
	/// Sample application for CAC,PIV and TWIC cards. Demonstrates usage of CardWerk's SmartCardAPI
    /// and CAC/PIV sample card modules coded against SmartCard.DLL.
    /// This is by no means a complete, commercial implementation of PIV/CAC card integration. This sample should 
    /// just get you started accessing those cards using SmartCard API.
	/// </summary>

	class CACwin_Form : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label m_aCopyrightLabel;
        private System.Windows.Forms.LinkLabel m_aLinkLabel;
        private RichTextBox MainTextBox;
		private System.Windows.Forms.Label m_aPromptLabel;
        private Label m_aApiVersion;
        CardDialogsForm m_aCardForm;
        int MINIMUM_REMAINING_PIN_TRIALS = 2;
        private CheckBox chkVerifyPin; // prevent sample program from locking up your card; only change this value if you know your card PIN
        bool isSharedPinVerified = false;
        static bool ALWAYS_LOOK_FOR_MORE_CARD_READERS = true; // increases the number of readers we are looking for

		/// <summary>
		/// Standard constructor.
		/// </summary>

		public CACwin_Form()
		{
			InitializeComponent();
            this.Text = "HelloCAC 1.2.17.411 - CAC card sample";
            m_aApiVersion.Text = SMARTCARDAPI.ApiVersionInfo + " and " + CACC.API_VERSION;
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
            Stream aTraceFile = File.Create(Path.Combine(sPath, "HelloCACTrace.txt"));
			TextWriterTraceListener aListener = new TextWriterTraceListener(aTraceFile);
			Trace.Listeners.Add(aListener);

			try
			{
				CACwin_Form aHelloCACForm = new CACwin_Form();

				// Run the primary application form.

				Application.Run(aHelloCACForm);
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

			// Try to start up the card terminal manager. If this fails, then the application
			// is immediately aborted.

			if (!StartupCardTerminalManager())
			{
				Application.Exit();
			}

            // have a card dialog ready in case we use card dialog based PIN entry
            m_aCardForm = new CardDialogsForm();
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
		/// Helper that starts up the card terminal manager and cares about the case when no
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
                    throw new Exception("NO READER");
                }
				fStartedUp = true;
			}
			catch (Exception x)
			{
				// TODO: Better diagnstic and error handling would be appropriate here.

				Trace.WriteLine(x.ToString());

				MessageBox.Show(
					"Unable to start CardTerminalManager. Will " +
					"exit this application.",
					"HelloCAC",
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
            isSharedPinVerified = false; 
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
                    this.AccessCAC(aEventArgs.Slot);
                }
                catch
                {
                    //ResetCardRelatedText();
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
                m_aCardForm.Close(); // close CardDialog in case it is open
                MainTextBox.Text = "";
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
                    Debug.WriteLine("available readers: " + CardTerminalManager.Singleton.SlotCount);
                }
            }
        }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="aCardSlot"></param>

		public void AccessCAC(CardTerminalSlot aCardSlot)
		{
            Prompt("Reading Card ...");
            MainTextBox.Text = ""; // fresh start
            // Acquire any processor card (T=0 or T=1) that may be present in the given card terminal slot
            CardActivationResult nActivationResult;
            CardHandle aCard = aCardSlot.AcquireCard((CardTypes.T0|CardTypes.T1) , out nActivationResult);
           	if (nActivationResult != CardActivationResult.Success)
			{
				Debug.Assert(aCard == null);
                
                switch (nActivationResult)
				{
				case CardActivationResult.NoCard:
                    Prompt("Please insert card ...");
					break;
				case CardActivationResult.UnresponsiveCard:
                    Prompt("Defective card or card inserted incorrectly.");
					break;
				case CardActivationResult.InUse:
                    Prompt("Card reader blocked by other application.");
					break;
				default:
                    Prompt("Can't power up card!");
					break;
				}
				return;
			}
            
            // we now have a card in the reader that is powered up
            DisplayText("Card connected");
            CardTransactionResult nCardTransactionResult = aCardSlot.BeginTransaction();
            if (nCardTransactionResult != CardTransactionResult.Success)
            {
                throw new Exception("WARNING: can't start card transaction");
            }
            DisplayText("OK-begin card transaction.");

            // CardHandle represents the combination of card terminal and powered-up card.
            try
            {
       
                // ================ Get the card's ATR ======================
                // Every card accessed through PC/SC must return an Answer To Reset (ATR). 
                // So let's see what we've got here.
                byte[] atr = aCard.GetATR();
                if (atr.Length == 0) throw new Exception("Invalid ATR");
                DisplayText("ATR: " + CardHex.FromByteArray(atr, 0, atr.Length)); 

                CacCardEdge cacCard = new CacCardEdge(aCard);
                    
                DisplayText("using " + cacCard.GetVersion());
                byte cla_verify = 0x00;
                bool isPINverified = false;
                byte p2_pinReference = 0x00;

                byte[] accessControlAppletId = new byte[CACC.RID_DOD_CAC.Length + CACC.PIX_ACCESS_CONTROL_APPLET.Length];
                CACC.RID_DOD_CAC.CopyTo(accessControlAppletId,0);
                CACC.PIX_ACCESS_CONTROL_APPLET.CopyTo(accessControlAppletId,CACC.RID_DOD_CAC.Length);

                if (cacCard.SelectApplet(accessControlAppletId) == false)
                {
                    if(!cacCard.SelectApplet(CACC.RID_DOD_CAC))
                    {
                        throw new Exception("This is not a CAC card.");
                    }
                    if (chkVerifyPin.Checked)
                    {
                        cla_verify = 0;
                        p2_pinReference = 0x80;
                        if (CheckPinCounter(aCard, MINIMUM_REMAINING_PIN_TRIALS, cla_verify, p2_pinReference)) // to protect your card
                        {
                            isPINverified = VerifyPIN_viaCardDialog(this, aCard, cla_verify, p2_pinReference);
                        }               
                    }
                    else
                    {
                        DisplayText("CheckPinCounter: we skip VERIFY PIN (protected functions won't be available)");
                    }
                }
                else
                {
                    if (chkVerifyPin.Checked)
                    {
                        // todo: CACv1 seems to be fine with PIN reference 0x80 whereas CACv2 cards need pin ref 0
                        cla_verify = 0x80;
                        p2_pinReference = 0x80;
                        if (CheckPinCounter(aCard, MINIMUM_REMAINING_PIN_TRIALS, cla_verify, p2_pinReference)) // to protect your card
                        {
                            isPINverified = VerifyPIN_viaCardDialog(this, aCard, cla_verify, p2_pinReference);
                            isSharedPinVerified = isPINverified;
                        }
                        else
                        {
                            DisplayText("CheckPinCounter: we skip VERIFY PIN (protected functions won't be available)");
                        }
                    }
                    else
                    {
                        DisplayText("PIN verification skipped (check box if you want to verify PIN to access additional data)");
                    }
                }

                if (isPINverified)
                {
                    byte[] personInstanceRawData = cacCard.GetData(CAC_OBJECTID.PERSON_INSTANCE);
                    if (personInstanceRawData == null)
                    {
                        DisplayText("NO Person Instance data found.");
                    }
                    else
                    {
                        CacCardPersonInstance personInstance = new CacCardPersonInstance(personInstanceRawData);
                        DisplayText(personInstance.ToString());
                    }


                    byte[] personnelInstanceRawData = cacCard.GetData(CAC_OBJECTID.PERSONNEL_INSTANCE);
                    if (personnelInstanceRawData == null)
                    {
                        DisplayText("NO Personnel Instance data found.");
                    }
                    else
                    {
                        CacCardPersonnelInstance personnelInstance = new CacCardPersonnelInstance(personnelInstanceRawData);
                        DisplayText(personnelInstance.ToString());
                    }
                }
        } // try
        catch (ArgumentException x)
        {
            Trace.WriteLine(x.ToString());
            Prompt("Card access error");

        }
        catch (Exception x)
        {
            Trace.WriteLine(x.ToString());
            Prompt("Card access error");
            DisplayText("ERROR: " + x.Message);
        }
		finally
		{
            nCardTransactionResult = aCardSlot.EndTransaction();
            if (nCardTransactionResult != CardTransactionResult.Success)
            {
                throw new Exception("WARNING: ending card transaction failed");
            }
            DisplayText("OK-end card transaction.");
            //aCard.Dispose(); // release card handle
            DisplayText("END of CAC card access test.");
            //Prompt("Insert next card ..."); 
		}

		} //CAC card test

        
       


        public void DisplayText(string text)
        {
            MainTextBox.AppendText(text + "\n");
            MainTextBox.Refresh();
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

        public bool CheckPinCounter(CardHandle aCard, int minimumAvailableTries, byte CLA, byte pinReference)
        {
            // we can access PIN protected containers.                    
            // VERIFY PIN
            // we do a quick check of the remaining tries before 
            // sending a VERIFY with a card holder PIN
            CardCommandAPDU quickVerify = new CardCommandAPDU(CLA, 0x20, 0x00, pinReference, 00);
            CardResponseAPDU aRespAPDU = aCard.SendCommand(quickVerify);
            int remainingTries = 0;
            if ((aRespAPDU.SW & 0xFFF0) == 0x63C0 ||  //  
                (aRespAPDU.SW & 0xFF00) == 0x6300)    // older CAC cards return 0x63xx instead of 0x63Cx
            {
                remainingTries = (aRespAPDU.SW2 & 0x0F);
                DisplayText("CheckPinCounter: " + remainingTries + " PIN tries remaining. Ok to use VERIFY");
            }

            //string pin = "112233";
            //VerifyPIN_noDialog(aCard, pin);

            // ================== CAUTION =================== 
            //     !!!!! WRONG PIN CAN LOCK YOUR CARD !!!!
            //
            if (remainingTries < minimumAvailableTries) // we don't want to risk messing up a card 
            {
                //throw new Exception("Only " + remainingTries + " tries left. Therfore we won't run any PIN VERIFY command in this demo");
                DisplayText("CheckPinCounter: " + remainingTries + " PIN tries remaining. This test program requires at least " + minimumAvailableTries + "tries left so we won't lock a card.");
                return false;
            }
            return true;
        }


        /// <summary>
        /// This is a PIN verification without dialog. Only use it if you want to 
        /// hard-code a PIN for test purposes.
        /// </summary>
        /// <param name="aOwner"></param>
        /// <param name="cardHandle">SmartCard API card handle</param>
        /// <returns></returns>
        public bool VerifyPIN_noDialog(CardHandle cardHandle, string pin, byte CLA, byte pinReference)
        {
            if (pin == null)
            {
                throw new ArgumentNullException("PIN must not be null.");
            }
            if (pin.Length > 8)
            {
                throw new ArgumentException("PIN must not exceed 8 digits");

            }
            char[] chars = pin.ToCharArray();
          
            
            byte[] paddedPin = CardHex.ToByteArray("FFFFFFFFFFFFFFFF");
            for(int i=0; i<pin.Length;i++)
            {
                paddedPin[i] = (byte)chars[i]; 
            }

            CardCommandAPDU aVerifyAPDU = new CardCommandAPDU(CLA, 0x20, 0x00, pinReference, paddedPin);
            CardResponseAPDU aRespAPDU = cardHandle.SendCommand(aVerifyAPDU);
            if ((aRespAPDU.SW & 0xFFF0) == 0x63C0)
            {
                int remainingTries = (aRespAPDU.SW2 & 0x0F);
                throw new ArgumentException("Wrong PIN. You have " + remainingTries + " left before the PIN is blocked.");
            }
            if(aRespAPDU.SW !=0x9000)
            {
                throw new Exception("Unexpected card response");
            }
            return true;
        }
            

        /// <summary>
        /// This is really all you need for to validate a PIV card. Note that this 
        /// code automatically detects secure PIN entry readers. With such reader the
        /// PIN NEVER TRAVELS TO THE HOST effectively protecting from keyboard loggers 
        /// or spyware
        /// </summary>
        /// <param name="aOwner"></param>
        /// <param name="cardHandle">SmartCard API card handle</param>
        /// <returns></returns>
        public bool VerifyPIN_viaCardDialog(IWin32Window aOwner, CardHandle cardHandle, byte CLA, byte pinReference)
        {
            try
            {
                // creating a default 8-byte PIN block to provide padding values
                CardCommandAPDU aVerifyAPDU = new CardCommandAPDU(CLA, 0x20, 0x00, pinReference,
                                                                  CardHex.ToByteArray("FFFFFFFFFFFFFFFF")); 
                CardPinControl aPinControl = new CardPinControl(aVerifyAPDU, CardPinEncoding.Ascii, 0);
                aPinControl.MinLength = 4; // we require at least 4 digits
                aPinControl.MaxLength = 8; // our PIN can't exceed 8 bytes
                CardResponseAPDU aResponse = cardHandle.VerifyPin(aOwner, m_aCardForm, aPinControl,"Card holder PIN required","Please enter your PIN (4 digits minimum).");
                if (aResponse == null)
                {
                    return false;
                }
                int remainingTries = 0;
                if ((aResponse.SW & 0xFFF0) == 0x63C0)
                {
                    remainingTries = (aResponse.SW2 & 0x0F);
                    MessageBox.Show(
                    "WRONG PIN - " + remainingTries + " tries remaining!",
                    "HelloCAC - PIN Verification Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);
                    return false;
                }
                else if(aResponse.SW != 0x9000)
                {
                    MessageBox.Show(
                    "Unexpected SW=0x" + CardHex.FromByte(aResponse.SW1) + CardHex.FromByte(aResponse.SW2),
                    "HelloCAC - PIN Verification Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);
                    return false;
                }
                if((aResponse.IsSuccessful || aResponse.IsWarning) == false)
                {
                    DisplayText("ERROR - WRONG PIN");
                    return false;
                }
                DisplayText("OK-PIN Verified.");
                return true;
            }
            catch (CardTerminalException)
            {
                return false;
            }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CACwin_Form));
            this.m_aPromptLabel = new System.Windows.Forms.Label();
            this.m_aLinkLabel = new System.Windows.Forms.LinkLabel();
            this.m_aCopyrightLabel = new System.Windows.Forms.Label();
            this.MainTextBox = new System.Windows.Forms.RichTextBox();
            this.m_aApiVersion = new System.Windows.Forms.Label();
            this.chkVerifyPin = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // m_aPromptLabel
            // 
            this.m_aPromptLabel.Font = new System.Drawing.Font("Trebuchet MS", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_aPromptLabel.Location = new System.Drawing.Point(-3, 0);
            this.m_aPromptLabel.Name = "m_aPromptLabel";
            this.m_aPromptLabel.Size = new System.Drawing.Size(487, 32);
            this.m_aPromptLabel.TabIndex = 2;
            this.m_aPromptLabel.Text = "Insert card ...";
            // 
            // m_aLinkLabel
            // 
            this.m_aLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_aLinkLabel.AutoSize = true;
            this.m_aLinkLabel.Location = new System.Drawing.Point(474, 501);
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
            this.m_aCopyrightLabel.Location = new System.Drawing.Point(3, 501);
            this.m_aCopyrightLabel.Name = "m_aCopyrightLabel";
            this.m_aCopyrightLabel.Size = new System.Drawing.Size(234, 16);
            this.m_aCopyrightLabel.TabIndex = 0;
            this.m_aCopyrightLabel.Text = "Copyright 2011-2019 CardWerk Technologies";
            // 
            // MainTextBox
            // 
            this.MainTextBox.Location = new System.Drawing.Point(1, 54);
            this.MainTextBox.Name = "MainTextBox";
            this.MainTextBox.ReadOnly = true;
            this.MainTextBox.Size = new System.Drawing.Size(648, 422);
            this.MainTextBox.TabIndex = 11;
            this.MainTextBox.Text = "";
            // 
            // m_aApiVersion
            // 
            this.m_aApiVersion.AutoSize = true;
            this.m_aApiVersion.Location = new System.Drawing.Point(3, 485);
            this.m_aApiVersion.Name = "m_aApiVersion";
            this.m_aApiVersion.Size = new System.Drawing.Size(120, 16);
            this.m_aApiVersion.TabIndex = 12;
            this.m_aApiVersion.Text = "SmartCard API version";
            // 
            // chkVerifyPin
            // 
            this.chkVerifyPin.AllowDrop = true;
            this.chkVerifyPin.AutoSize = true;
            this.chkVerifyPin.Location = new System.Drawing.Point(528, 28);
            this.chkVerifyPin.Name = "chkVerifyPin";
            this.chkVerifyPin.Size = new System.Drawing.Size(121, 20);
            this.chkVerifyPin.TabIndex = 13;
            this.chkVerifyPin.Text = "include PIN VERIFY";
            this.chkVerifyPin.UseVisualStyleBackColor = true;
            // 
            // CACwin_Form
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(653, 516);
            this.Controls.Add(this.chkVerifyPin);
            this.Controls.Add(this.m_aApiVersion);
            this.Controls.Add(this.MainTextBox);
            this.Controls.Add(this.m_aPromptLabel);
            this.Controls.Add(this.m_aLinkLabel);
            this.Controls.Add(this.m_aCopyrightLabel);
            this.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CACwin_Form";
            this.Text = "CACwin 1.19.1029 - CAC card access  with SmartCard-API";
            this.Load += new System.EventHandler(this.HelloCACForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

        private void HelloCACForm_Load(object sender, EventArgs e)
        {

        }



	}			
}

