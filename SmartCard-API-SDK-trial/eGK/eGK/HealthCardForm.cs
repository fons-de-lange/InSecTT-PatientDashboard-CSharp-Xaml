// --------------------------------------------------------------------------------------------
// HelloMctForm.cs
// Subsembly SmartCard API
// Copyright © 2004-2017 CardWerk Technologies
// --------------------------------------------------------------------------------------------
// NOTES
// To access "old" KVK storage card (SLE4442 or similar) CT-API is required. Use CardTerminalConfigurator 
// to make vendor-proprietary CT-API DLL available to SmartCardAPI
// OMNIKEY: ctdeuin.dll is 32 bit windows DLL: requires both HelloMct and CardTerminalConfigurator built for x86 target
//
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Text;
using Subsembly.SmartCard;
using System.Reflection;


namespace GermanHealthInsuranceCard
{
	/// <summary>
	/// Sample application that reads the data from a German health card. Both, KVK storage card and eGK 
    /// processor cards are supported.
	/// </summary>

	public class HealthCardForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.LinkLabel m_aLinkLabel;
		private System.Windows.Forms.Label m_aCopyrightLabel;
		private System.Windows.Forms.Label m_aPromptLabel;
		private System.Windows.Forms.Label nameLabel;
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.TextBox nameTextBox;
		private System.Windows.Forms.TextBox insuranceTextBox;
		private System.Windows.Forms.Label insuranceLabel;
		private System.Windows.Forms.Label addressLabel;
		private System.Windows.Forms.TextBox addressTextBox;
		private System.Windows.Forms.Label birthdayLabel;
		private Label insuranceNumberLabel;
		private TextBox insuranceNumberTextBox;
		private TextBox numberTextBox;
		private Label numberLabel;
        private Label m_aApiVersion;
		private System.Windows.Forms.TextBox birthdayTextBox;

		/// <summary>
		/// Standard no brain constructor.
		/// </summary>

		public HealthCardForm()
		{
			InitializeComponent();
            this.Text = "eGK & KVK sample rev. 11OCT2020";
            m_aApiVersion.Text = SMARTCARDAPI.ApiVersionInfo;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>

		protected override void Dispose( bool fDisposing )
		{
			if (fDisposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(fDisposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HealthCardForm));
            this.m_aLinkLabel = new System.Windows.Forms.LinkLabel();
            this.m_aCopyrightLabel = new System.Windows.Forms.Label();
            this.m_aPromptLabel = new System.Windows.Forms.Label();
            this.nameLabel = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.insuranceTextBox = new System.Windows.Forms.TextBox();
            this.insuranceLabel = new System.Windows.Forms.Label();
            this.addressLabel = new System.Windows.Forms.Label();
            this.addressTextBox = new System.Windows.Forms.TextBox();
            this.birthdayLabel = new System.Windows.Forms.Label();
            this.birthdayTextBox = new System.Windows.Forms.TextBox();
            this.insuranceNumberLabel = new System.Windows.Forms.Label();
            this.insuranceNumberTextBox = new System.Windows.Forms.TextBox();
            this.numberTextBox = new System.Windows.Forms.TextBox();
            this.numberLabel = new System.Windows.Forms.Label();
            this.m_aApiVersion = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // m_aLinkLabel
            // 
            this.m_aLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_aLinkLabel.AutoSize = true;
            this.m_aLinkLabel.Location = new System.Drawing.Point(336, 308);
            this.m_aLinkLabel.Name = "m_aLinkLabel";
            this.m_aLinkLabel.Size = new System.Drawing.Size(156, 13);
            this.m_aLinkLabel.TabIndex = 7;
            this.m_aLinkLabel.TabStop = true;
            this.m_aLinkLabel.Text = "http://www.smartcard-api.com/";
            this.m_aLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelLinkClicked);
            // 
            // m_aCopyrightLabel
            // 
            this.m_aCopyrightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_aCopyrightLabel.AutoSize = true;
            this.m_aCopyrightLabel.Location = new System.Drawing.Point(16, 308);
            this.m_aCopyrightLabel.Name = "m_aCopyrightLabel";
            this.m_aCopyrightLabel.Size = new System.Drawing.Size(223, 13);
            this.m_aCopyrightLabel.TabIndex = 6;
            this.m_aCopyrightLabel.Text = "Copyright 2004-2020 CardWerk Technologies";
            // 
            // m_aPromptLabel
            // 
            this.m_aPromptLabel.Font = new System.Drawing.Font("Trebuchet MS", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_aPromptLabel.Location = new System.Drawing.Point(16, 16);
            this.m_aPromptLabel.Name = "m_aPromptLabel";
            this.m_aPromptLabel.Size = new System.Drawing.Size(488, 48);
            this.m_aPromptLabel.TabIndex = 8;
            this.m_aPromptLabel.Text = "Please insert KVK or eHealth card...";
            // 
            // nameLabel
            // 
            this.nameLabel.Location = new System.Drawing.Point(16, 132);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(152, 20);
            this.nameLabel.TabIndex = 9;
            this.nameLabel.Text = "Name of  insured:";
            this.nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nameTextBox
            // 
            this.nameTextBox.Location = new System.Drawing.Point(184, 133);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(304, 20);
            this.nameTextBox.TabIndex = 10;
            // 
            // insuranceTextBox
            // 
            this.insuranceTextBox.Location = new System.Drawing.Point(184, 72);
            this.insuranceTextBox.Name = "insuranceTextBox";
            this.insuranceTextBox.Size = new System.Drawing.Size(304, 20);
            this.insuranceTextBox.TabIndex = 12;
            // 
            // insuranceLabel
            // 
            this.insuranceLabel.Location = new System.Drawing.Point(16, 72);
            this.insuranceLabel.Name = "insuranceLabel";
            this.insuranceLabel.Size = new System.Drawing.Size(152, 20);
            this.insuranceLabel.TabIndex = 11;
            this.insuranceLabel.Text = "Name of insurance:";
            this.insuranceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // addressLabel
            // 
            this.addressLabel.Location = new System.Drawing.Point(16, 193);
            this.addressLabel.Name = "addressLabel";
            this.addressLabel.Size = new System.Drawing.Size(152, 20);
            this.addressLabel.TabIndex = 14;
            this.addressLabel.Text = "Address of insured:";
            this.addressLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // addressTextBox
            // 
            this.addressTextBox.Location = new System.Drawing.Point(184, 193);
            this.addressTextBox.Multiline = true;
            this.addressTextBox.Name = "addressTextBox";
            this.addressTextBox.Size = new System.Drawing.Size(304, 48);
            this.addressTextBox.TabIndex = 15;
            // 
            // birthdayLabel
            // 
            this.birthdayLabel.Location = new System.Drawing.Point(16, 254);
            this.birthdayLabel.Name = "birthdayLabel";
            this.birthdayLabel.Size = new System.Drawing.Size(152, 20);
            this.birthdayLabel.TabIndex = 16;
            this.birthdayLabel.Text = "Birthday of  insured:";
            this.birthdayLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // birthdayTextBox
            // 
            this.birthdayTextBox.Location = new System.Drawing.Point(184, 255);
            this.birthdayTextBox.Name = "birthdayTextBox";
            this.birthdayTextBox.Size = new System.Drawing.Size(304, 20);
            this.birthdayTextBox.TabIndex = 17;
            // 
            // insuranceNumberLabel
            // 
            this.insuranceNumberLabel.AutoSize = true;
            this.insuranceNumberLabel.Location = new System.Drawing.Point(16, 102);
            this.insuranceNumberLabel.Name = "insuranceNumberLabel";
            this.insuranceNumberLabel.Size = new System.Drawing.Size(108, 13);
            this.insuranceNumberLabel.TabIndex = 18;
            this.insuranceNumberLabel.Text = "Number of insurance:";
            // 
            // insuranceNumberTextBox
            // 
            this.insuranceNumberTextBox.Location = new System.Drawing.Point(184, 99);
            this.insuranceNumberTextBox.Name = "insuranceNumberTextBox";
            this.insuranceNumberTextBox.Size = new System.Drawing.Size(304, 20);
            this.insuranceNumberTextBox.TabIndex = 19;
            // 
            // numberTextBox
            // 
            this.numberTextBox.Location = new System.Drawing.Point(184, 159);
            this.numberTextBox.Name = "numberTextBox";
            this.numberTextBox.Size = new System.Drawing.Size(304, 20);
            this.numberTextBox.TabIndex = 20;
            // 
            // numberLabel
            // 
            this.numberLabel.AutoSize = true;
            this.numberLabel.Location = new System.Drawing.Point(16, 162);
            this.numberLabel.Name = "numberLabel";
            this.numberLabel.Size = new System.Drawing.Size(96, 13);
            this.numberLabel.TabIndex = 21;
            this.numberLabel.Text = "Number of insured:";
            // 
            // m_aApiVersion
            // 
            this.m_aApiVersion.AutoSize = true;
            this.m_aApiVersion.Location = new System.Drawing.Point(17, 286);
            this.m_aApiVersion.Name = "m_aApiVersion";
            this.m_aApiVersion.Size = new System.Drawing.Size(41, 13);
            this.m_aApiVersion.TabIndex = 22;
            this.m_aApiVersion.Text = "version";
            // 
            // HealthCardForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(512, 337);
            this.Controls.Add(this.m_aApiVersion);
            this.Controls.Add(this.numberLabel);
            this.Controls.Add(this.numberTextBox);
            this.Controls.Add(this.insuranceNumberTextBox);
            this.Controls.Add(this.insuranceNumberLabel);
            this.Controls.Add(this.birthdayTextBox);
            this.Controls.Add(this.addressTextBox);
            this.Controls.Add(this.insuranceTextBox);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.m_aLinkLabel);
            this.Controls.Add(this.m_aCopyrightLabel);
            this.Controls.Add(this.birthdayLabel);
            this.Controls.Add(this.addressLabel);
            this.Controls.Add(this.insuranceLabel);
            this.Controls.Add(this.nameLabel);
            this.Controls.Add(this.m_aPromptLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "HealthCardForm";
            this.Text = "eGK & KVK sample code";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// Entry point of application.
		/// </summary>

		[STAThread]
		static void Main() 
		{
			// Create a new text writer using the output stream, and add it to the trace listeners.
            string sPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            
            Stream aTraceFile = File.Create(Path.Combine(sPath, "HelloMctTrace.txt"));
			TextWriterTraceListener aListener = new TextWriterTraceListener(aTraceFile);
			Trace.Listeners.Add(aListener);

			try
			{
				HealthCardForm aHelloMctForm = new HealthCardForm();

				// Run the primary application form.

				Application.Run(aHelloMctForm);
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
			// manager. This ensures that we get a card inserted event for those cards that
			// are already inserted when this program is started.

			CardTerminalManager.Singleton.CardInsertedEvent +=
				new CardTerminalEventHandler(InsertedEvent);
			CardTerminalManager.Singleton.CardRemovedEvent +=
				new CardTerminalEventHandler(RemovedEvent);
            CardTerminalManager.Singleton.CardTerminalFoundEvent +=
                new CardTerminalEventHandler(TerminalFoundEvent);
            CardTerminalManager.Singleton.CardTerminalLostEvent +=
               new CardTerminalEventHandler(TerminalLostEvent);

			// Try to start up the card terminal manager. If this fails, then the application
			// is immediately aborted.

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
			// Whenever we did successfully call the CardTerminalManager Startup method,
			// we also must call the CardTerminalManager Shutdown method!

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
				// Startup the SmartCard Subsembly. The parameter "true" means that any
				// PC/SC smart card readers will automatically be added to the smart card
				// configuration registry. If startup fails, then this will throw an
				// exception.

				int nCountReaders = CardTerminalManager.Singleton.Startup(true);
                fStartedUp = true;

			}
			catch (Exception x)
			{
				// TODO: Better diagnstic and error handling would be appropriate here.

				Trace.WriteLine(x.ToString());

				MessageBox.Show(
					"The card terminal manager could not be started up. This program will " +
					"be aborted now.",
					"HelloMCT",
					MessageBoxButtons.OK,
					MessageBoxIcon.Stop);
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
				// Read and display card data of the inserted card. If multiple readers are
				// connected, and multiple cards are inserted, then this will overwrite the
				// display of the last insertion.

				this.ReadHealthCard(aEventArgs.Slot);
			}
		}
	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="aSender"></param>
		/// <param name="aEventArgs"></param>
	
		public void RemovedEvent(object aSender, CardTerminalEventArgs aEventArgs)
		{
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
				// Display prompt and clear displayed card data. If multiple card readers
				// are connected and multiple cards are inserted, then the first card that
				// is removed will clear the display.

				this.PromptHealthCard();
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
                    Debug.WriteLine("Found reader: " + aEventArgs.Slot.CardTerminalName);
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
                base.BeginInvoke(new CardTerminalEventHandler(TerminalLostEvent),
                    vParms);
            }
            else
            {
                Debug.WriteLine("lost terminal " + aEventArgs.Slot.CardTerminalName);
                 // update number of readers
                 CardTerminalManager.Singleton.DelistCardTerminal(aEventArgs.Slot.CardTerminal); // remove from monitored list of readers
                 if (CardTerminalManager.Singleton.SlotCount == 0)
                 {
                     this.m_aPromptLabel.Text = "Connect reader ...";
                     // start looking for reader insertion
                     // done automatically by the singleton. The singleton raises an event if it 
                     // finds a new reader.
                 }
            }
        }



		/// <summary>
		/// 
		/// </summary>
		/// <param name="aSlot"></param>

		public void ReadHealthCard(CardTerminalSlot aSlot)
		{
			m_aPromptLabel.Text = "Reading card ...";
			m_aPromptLabel.Update();

			CardActivationResult nActivationResult;

			try
			{
				HealthCard aHealthCard;

                // Try the new eGK processor card.
                aHealthCard = _ReadEgkHealthCard(aSlot, out nActivationResult);

                // If not successful, try old memory card.
                if (aHealthCard == null)
                {

                    Debug.WriteLine("eGK power-up failed. Trying KVK storage card.");
                    aHealthCard = _ReadMctHealthCard(aSlot, out nActivationResult);
                    if (aHealthCard == null) Debug.WriteLine("KVK power-up also failed.");
                }

				if (aHealthCard != null)
				{
					m_aPromptLabel.Text = aHealthCard.CardName;
					m_aPromptLabel.Update();

					insuranceTextBox.Text = aHealthCard.InsuranceName;
					insuranceNumberTextBox.Text = aHealthCard.InsuranceNumber;

					StringBuilder sbName = new StringBuilder();
					if (aHealthCard.TitleName != null)
					{
						sbName.Append(aHealthCard.TitleName);
						sbName.Append(' ');
					}
					sbName.Append(aHealthCard.FirstName);
					sbName.Append(' ');
					sbName.Append(aHealthCard.LastName);
					nameTextBox.Text = sbName.ToString();

					numberTextBox.Text = aHealthCard.InsuredNumber;

					StringBuilder sbAddress = new StringBuilder();
					if (aHealthCard.Street != null)
					{
						sbAddress.AppendLine(aHealthCard.Street);
					}
					sbAddress.Append(aHealthCard.ZipCode);
					sbAddress.Append(' ');
					sbAddress.Append(aHealthCard.City);
					addressTextBox.Text = sbAddress.ToString();

					birthdayTextBox.Text = aHealthCard.Birthday.ToLongDateString();
				}
				else
				{
					Trace.WriteLine("CardActivationResult: " + nActivationResult.ToString());

					switch (nActivationResult)
					{
					case CardActivationResult.NoCard:
						this.PromptHealthCard();
						break;
					case CardActivationResult.UnresponsiveCard:
						m_aPromptLabel.Text = "Unresponsive Card.";
						break;
					case CardActivationResult.InUse:
						m_aPromptLabel.Text = "Card reader blocked by another application.";
						break;
					default:
						m_aPromptLabel.Text = "Inserted card is not supported!";
						break;
					}
				}
			}
			catch (CardTerminalException x)
			{
				switch (x.Code)
				{
				case CardTerminalExceptionCode.CardWithdrawn:
					this.PromptHealthCard();
					break;
				default:
					m_aPromptLabel.Text = "Card Terminal Exception: " + x.Code;
					break;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>

		void PromptHealthCard()
		{
			m_aPromptLabel.Text = "Please insert a German health insurance card...";
			insuranceTextBox.Text = null;
			insuranceNumberTextBox.Text = null;
			nameTextBox.Text = null;
			numberTextBox.Text = null;
			addressTextBox.Text = null;
			birthdayTextBox.Text = null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aSlot"></param>
		/// <param name="nActivationResult"></param>
		/// <returns></returns>

		HealthCard _ReadMctHealthCard(
			CardTerminalSlot aSlot,
			out CardActivationResult nActivationResult)
		{
			HealthCard_KVK aMctHealthCard = new HealthCard_KVK();
			nActivationResult = aMctHealthCard.Read(aSlot);

			if (nActivationResult == CardActivationResult.Success)
			{
				return aMctHealthCard;
			}
            else if (nActivationResult == CardActivationResult.UnsupportedCard)
            {

                m_aPromptLabel.Text = "Unsupported card - Please insert a German health insurance card..."; 
                return null;
            }
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aSlot"></param>
		/// <param name="nActivationResult"></param>
		/// <returns></returns>

		HealthCard _ReadEgkHealthCard(
			CardTerminalSlot aSlot,
			out CardActivationResult nActivationResult)
		{
			HealthCard_eGK aEgkHealthCard = new HealthCard_eGK();
			nActivationResult = aEgkHealthCard.Read(aSlot);

			if (nActivationResult == CardActivationResult.Success)
			{
				return aEgkHealthCard;
			}
			else
			{
				return null;
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
	}
}
