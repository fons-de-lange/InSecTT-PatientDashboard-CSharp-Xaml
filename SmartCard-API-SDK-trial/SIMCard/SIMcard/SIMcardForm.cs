// --------------------------------------------------------------------------------------------
// SimCardForm.cs - Sample code for SmartCardAPI
// CardWerk SmartCard API
// Copyright © 2004-2019 CardWerk Technologies
// --------------------------------------------------------------------------------------------
// HIST
// 11DEC2019 MJ remove PIN dialogs (depreceated)
// 27NOV2013 MJ transaction protection; support for lost/found terminal event
using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using Subsembly.SmartCard;

namespace SampleCode_SIMcard
{
	/// <summary>
	/// Sample application that reads the phonebook entries of a GSM SIM.
    /// USAGE:
    /// run sample program
    /// enter PIN
    /// insert card
    /// 
	/// </summary>

	class SIMcardForm : System.Windows.Forms.Form
	{
		CardTerminalSlot m_aSlot;
        private System.Windows.Forms.Label m_aCopyrightLabel;
		private System.Windows.Forms.LinkLabel m_aLinkLabel;
		private System.Windows.Forms.Label m_aPromptLabel;
		private System.Windows.Forms.ColumnHeader m_aSecondColumnHeader;
		private System.Windows.Forms.ListView m_aPhoneBookListView;
        private TextBox txtPin;
        private Label label1;
        private System.Windows.Forms.ColumnHeader m_aFirstColumnHeader;

		/// <summary>
		/// Standard no brain constructor.
		/// </summary>

		public SIMcardForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Entry point of application.
		/// </summary>

		[STAThread]
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles(); 
			Application.DoEvents(); 

			// Create a new text writer using the output stream, and add it to the trace
			// listeners.

            string sPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            Stream aTraceFile = File.Create(Path.Combine(sPath, "SimTriviaTrace.txt"));
			TextWriterTraceListener aListener = new TextWriterTraceListener(aTraceFile);
			Trace.Listeners.Add(aListener);

			try
			{
				SIMcardForm aSimTriviaForm = new SIMcardForm();

				// Run the primary application form.

				Application.Run(aSimTriviaForm);
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
				// Startup SmartCard API. The parameter "true" means that any
				// PC/SC smart card readers will automatically be added to the smart card
				// configuration registry. If startup fails, then this will throw an
				// exception.

				int nCountReaders = CardTerminalManager.Singleton.Startup(true);

				// At least one card terminal is configured, enabled and was started up
				// successfully. This is all we need, thus we can exit and return
				// successfully.

				if (nCountReaders > 0)
				{
					fStartedUp = true;
				}
				else
				{
					// If the returned count of installed and enabled readers is zero, then it
					// does not make sense to continue. Anyway we are obliged to call the
					// CardTerminalManager Shutdown method.

					CardTerminalManager.Singleton.Shutdown();

					//

					MessageBox.Show(
						"There is no smart card reader available. Please install a " +
						"smart card reader first. Use the smart card reader configurator in " +
						"order to configure and enable installed smart card readers.",
						"SIM Trivia",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
				}
			}
			catch (Exception x)
			{
				// TODO: Better diagnostic and error handling would be appropriate here.

				Trace.WriteLine(x.ToString());

				MessageBox.Show(
					"The card terminal manager could not be started up. This program will " +
					"be aborted now.",
					"SIM Trivia",
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
				m_aSlot = aEventArgs.Slot;
				ReadSim(aEventArgs.Slot, txtPin.Text);
			}
		}
	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="aSender"></param>
		/// <param name="aEventArgs"></param>
	
		public void RemovedEvent(object aSender, CardTerminalEventArgs aEventArgs)
		{
			if (aEventArgs.Slot != m_aSlot)
			{
				return;
			}

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
				PromptSim();
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
                base.BeginInvoke(new CardTerminalEventHandler(TerminalLostEvent), vParms);
            }
            else
            {
                if (CardTerminalManager.Singleton.StartedUp)
                {
                    Debug.WriteLine("Lost reader: " + aEventArgs.Slot.CardTerminalName);
                    // update number of readers
                    CardTerminalManager.Singleton.DelistCardTerminal(aEventArgs.Slot.CardTerminal); // remove from monitored list of readers

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
		/// <param name="aSlot"></param>

		public void ReadSim(CardTerminalSlot aSlot, string pin)
		{
			CardActivationResult nActivationResult;

			// Acquire a CardHandle to the inserted card (hopefully a SIM). Unfortunately a
			// GSM SIM is not completely ISO 7816-4 conforming, therefore we cannot use the
			// automatic card filter provided by the CardCriteria structure. Instead we pass
			// null and manually treat the card.

			using (CardHandle aCard = aSlot.AcquireCard(CardTypes.ProcessorCards, out nActivationResult))
			{
				if (aCard != null)
				{
					SIMcard aSim = new SIMcard(aCard);
					// TODO: Read all
                    aSlot.BeginTransaction();
					if (!aSim.ReadIccIdentification())
					{
						m_aPromptLabel.Text = "Error reading GSM SIM card!";
                        aSlot.EndTransaction();
						return;
					}
					
					if (!aSim.SelectDFTelecom())
					{
						m_aPromptLabel.Text = "Error reading GSM SIM card!";
                        aSlot.EndTransaction();
						return;
					}
					
                    if(pin == "")
                    {
                        m_aPromptLabel.Text = "Enter PIN and try again";
                        aSlot.EndTransaction();
                        return;
                    }

					if (!aSim.VerifyCardHolderPIN(pin))
					{
                        aSlot.EndTransaction();
                        m_aPromptLabel.Text = "WRONG PIN!";
                        return;
					}

					if (!aSim.ReadAbbreviatedDiallingNumbers())
					{
						m_aPromptLabel.Text = "Error reading GSM SIM card!";
                        aSlot.EndTransaction();
						return;
					}

					m_aPhoneBookListView.Items.Clear();
					foreach (DictionaryEntry aEntry in aSim.Phonebook)
					{
						m_aPhoneBookListView.Items.Add(new ListViewItem(
							new string[] { (string)aEntry.Key, (string)aEntry.Value }));
					}
                    aSlot.EndTransaction();
					m_aPromptLabel.Text = "OK .. read all phone entries ...";
				}
				else
				{
					switch (nActivationResult)
					{
					case CardActivationResult.NoCard:
						m_aPromptLabel.Text = "Please insert a GSM Subscriber Identity Module (SIM)...";
						break;
					case CardActivationResult.UnresponsiveCard:
						m_aPromptLabel.Text = "Card wrongly inserted or broken.";
						break;
					case CardActivationResult.InUse:
						m_aPromptLabel.Text = "Card reader blocked by another application.";
						break;
					default:
						m_aPromptLabel.Text = "Inserted card is no GSM SIM card!";
						break;
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public void PromptSim()
		{
			m_aPromptLabel.Text = "Please insert a GSM Subscriber Identity Module (SIM)...";
			m_aPhoneBookListView.Items.Clear();
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

		// THIS METHOD IS MAINTAINED BY THE FORM DESIGNER
		// DO NOT EDIT IT MANUALLY! YOUR CHANGES ARE LIKELY TO BE LOST
		void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SIMcardForm));
            this.m_aFirstColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.m_aPhoneBookListView = new System.Windows.Forms.ListView();
            this.m_aSecondColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.m_aPromptLabel = new System.Windows.Forms.Label();
            this.m_aLinkLabel = new System.Windows.Forms.LinkLabel();
            this.m_aCopyrightLabel = new System.Windows.Forms.Label();
            this.txtPin = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // m_aFirstColumnHeader
            // 
            this.m_aFirstColumnHeader.Text = "Name";
            this.m_aFirstColumnHeader.Width = 200;
            // 
            // m_aPhoneBookListView
            // 
            this.m_aPhoneBookListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_aPhoneBookListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.m_aPhoneBookListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.m_aFirstColumnHeader,
            this.m_aSecondColumnHeader});
            this.m_aPhoneBookListView.FullRowSelect = true;
            this.m_aPhoneBookListView.GridLines = true;
            this.m_aPhoneBookListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.m_aPhoneBookListView.HideSelection = false;
            this.m_aPhoneBookListView.Location = new System.Drawing.Point(20, 67);
            this.m_aPhoneBookListView.MultiSelect = false;
            this.m_aPhoneBookListView.Name = "m_aPhoneBookListView";
            this.m_aPhoneBookListView.Size = new System.Drawing.Size(484, 343);
            this.m_aPhoneBookListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.m_aPhoneBookListView.TabIndex = 6;
            this.m_aPhoneBookListView.UseCompatibleStateImageBehavior = false;
            this.m_aPhoneBookListView.View = System.Windows.Forms.View.Details;
            // 
            // m_aSecondColumnHeader
            // 
            this.m_aSecondColumnHeader.Text = "Number";
            this.m_aSecondColumnHeader.Width = 128;
            // 
            // m_aPromptLabel
            // 
            this.m_aPromptLabel.Font = new System.Drawing.Font("Trebuchet MS", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_aPromptLabel.Location = new System.Drawing.Point(16, 16);
            this.m_aPromptLabel.Name = "m_aPromptLabel";
            this.m_aPromptLabel.Size = new System.Drawing.Size(488, 48);
            this.m_aPromptLabel.TabIndex = 3;
            this.m_aPromptLabel.Text = "Please insert a GSM Subscriber Identity Module (SIM)...";
            // 
            // m_aLinkLabel
            // 
            this.m_aLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_aLinkLabel.AutoSize = true;
            this.m_aLinkLabel.Location = new System.Drawing.Point(359, 472);
            this.m_aLinkLabel.Name = "m_aLinkLabel";
            this.m_aLinkLabel.Size = new System.Drawing.Size(152, 16);
            this.m_aLinkLabel.TabIndex = 5;
            this.m_aLinkLabel.TabStop = true;
            this.m_aLinkLabel.Text = "https://smartcard-api.com/";
            this.m_aLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelLinkClicked);
            // 
            // m_aCopyrightLabel
            // 
            this.m_aCopyrightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.m_aCopyrightLabel.AutoSize = true;
            this.m_aCopyrightLabel.Location = new System.Drawing.Point(8, 472);
            this.m_aCopyrightLabel.Name = "m_aCopyrightLabel";
            this.m_aCopyrightLabel.Size = new System.Drawing.Size(234, 16);
            this.m_aCopyrightLabel.TabIndex = 4;
            this.m_aCopyrightLabel.Text = "Copyright 2004-2019 CardWerk Technologies";
            // 
            // txtPin
            // 
            this.txtPin.Location = new System.Drawing.Point(48, 425);
            this.txtPin.Name = "txtPin";
            this.txtPin.Size = new System.Drawing.Size(132, 20);
            this.txtPin.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 425);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(25, 16);
            this.label1.TabIndex = 8;
            this.label1.Text = "PIN";
            // 
            // SIMcardForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(543, 493);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtPin);
            this.Controls.Add(this.m_aPhoneBookListView);
            this.Controls.Add(this.m_aLinkLabel);
            this.Controls.Add(this.m_aCopyrightLabel);
            this.Controls.Add(this.m_aPromptLabel);
            this.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SIMcardForm";
            this.Text = "SIMcard - SmartCard API Sample rev 11DEC2019";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
	}			
}
