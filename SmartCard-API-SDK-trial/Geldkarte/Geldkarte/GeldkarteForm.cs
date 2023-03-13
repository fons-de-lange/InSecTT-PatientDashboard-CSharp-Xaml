// --------------------------------------------------------------------------------------------
// GeldkarteForm.cs
// CardWerk SmartCard API (professional)
// Copyright © 2004-2013 CardWerk Technologies
// --------------------------------------------------------------------------------------------
/*
 HIST
 * 31OCT2019 MJ build against latest SmatcardAPI (.NET 4.0 based)
 * 26NOV2013 MJ add protected transaction; catch exceptions on PC/SC I/O level (example: card card edge issues)
 *              add support for TerminalLost/Found events
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Subsembly.SmartCard;

namespace SampleCode_Geldkarte
{
	/// <summary>
	/// Sample application that reads the current balance of a German GeldKarte.
	/// </summary>

	class GeldkarteForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label m_aCopyrightLabel;
		private System.Windows.Forms.LinkLabel m_aLinkLabel;
		private System.Windows.Forms.Label m_aPromptLabel;

		/// <summary>
		/// Standard no brain constructor.
		/// </summary>

		public GeldkarteForm()
		{
			InitializeComponent();
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
            Stream aTraceFile = File.Create(Path.Combine(sPath, "HelloGeldkarteTrace.txt"));
			TextWriterTraceListener aListener = new TextWriterTraceListener(aTraceFile);
			Trace.Listeners.Add(aListener);

			try
			{
             GeldkarteForm aHelloGeldkarteForm = new GeldkarteForm();

				// Run the primary application form.

             Application.Run(aHelloGeldkarteForm);
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
				// Startup the SmartCard API. The parameter "true" means that any
				// PC/SC smart card reader will automatically be added to the smart card
				// configuration registry. If startup fails, then this will throw an
				// exception.

				int nCountReaders = CardTerminalManager.Singleton.Startup(true);
                fStartedUp = true;

				// At least one card terminal is configured, enabled and was started up
				// successfully. This is all we need, thus we can exit and return
				// successfully.

				if (nCountReaders == 0)
				{
                    PromptReader();
				}
			}
			catch (Exception x)
			{
				// TODO: Better diagnstic and error handling would be appropriate here.

				Trace.WriteLine(x.ToString());

				MessageBox.Show(
					"Can't start CardTerminalManager. Will " +
					"exit this application.",
					"HelloGeldkarte",
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
				this.ReadGeldKarte(aEventArgs.Slot);
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
				this.PromptGeldKarte();
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
                    PromptGeldKarte();
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
                    // update number of monitored readers
                    CardTerminalManager.Singleton.DelistCardTerminal(aEventArgs.Slot.CardTerminal); // remove from monitored list of readers

                    if (CardTerminalManager.Singleton.SlotCount == 0)
                    {
                        PromptReader();

                    }
                }
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aCardSlot"></param>

		public void ReadGeldKarte(CardTerminalSlot aCardSlot)
		{
			// Acquire any processor card (T=0 or T=1) that may be present in the given card
			// terminal slot

			CardActivationResult nActivationResult;
			CardHandle aCard = aCardSlot.AcquireCard(
				CardTypes.ProcessorCards, out nActivationResult);

			if (nActivationResult != CardActivationResult.Success)
			{
				Debug.Assert(aCard == null);

				switch (nActivationResult)
				{
				case CardActivationResult.NoCard:
					m_aPromptLabel.Text = "Please insert GeldKarte...";
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

            GeldKarte aGeldKarte = new GeldKarte();

            // Now try to read some interesting data from the successfully selected
			// GeldKarte application. We are obliged to ultimately orderly release the
			// GeldKarte application and thus also the CardHandle owned by it.

            try
            {
                aCardSlot.BeginTransaction();

                // Try to select the GeldKarte application on the acquired card. If this is
                // successful, then the GeldKarte instance takes over ownership of the
                // CardHandle.

                if (!aGeldKarte.Select(aCard))
                {
                    m_aPromptLabel.Text = "This card is not a GeldKarte!";
                    aCard.Dispose();
                    return;
                }

                if (!aGeldKarte.ReadID())
                {
                    m_aPromptLabel.Text = "Error reading GeldKarte!";
                    return;
                }

                if (aGeldKarte.ReadAmounts())
                {
                    if (aGeldKarte.Amount > 0)
                    {
                        m_aPromptLabel.Text = String.Format("Credit {0} {1:F02}",
                            aGeldKarte.OriginalCurrency, aGeldKarte.OriginalAmount);
                    }
                    else
                    {
                        m_aPromptLabel.Text = "GeldKarte empty, no value loaded!";
                    }
                }
                else
                {
                    m_aPromptLabel.Text = "Error reading GeldKarte!";
                }
            }
            catch (Exception x)
            {
                m_aPromptLabel.Text = "Error reading GeldKarte!" + x;
            }
			finally
			{
                aCardSlot.EndTransaction();
                aGeldKarte.Release();
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public void PromptGeldKarte()
		{
			m_aPromptLabel.Text = "Please insert GeldKarte...";
		}

        /// <summary>
        /// 
        /// </summary>

        public void PromptReader()
        {
            m_aPromptLabel.Text = "Please connect card reader ...";
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeldkarteForm));
            this.m_aPromptLabel = new System.Windows.Forms.Label();
            this.m_aLinkLabel = new System.Windows.Forms.LinkLabel();
            this.m_aCopyrightLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // m_aPromptLabel
            // 
            this.m_aPromptLabel.Font = new System.Drawing.Font("Trebuchet MS", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_aPromptLabel.Location = new System.Drawing.Point(16, 16);
            this.m_aPromptLabel.Name = "m_aPromptLabel";
            this.m_aPromptLabel.Size = new System.Drawing.Size(416, 80);
            this.m_aPromptLabel.TabIndex = 2;
            this.m_aPromptLabel.Text = "Please insert card...";
            // 
            // m_aLinkLabel
            // 
            this.m_aLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_aLinkLabel.AutoSize = true;
            this.m_aLinkLabel.Location = new System.Drawing.Point(264, 112);
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
            this.m_aCopyrightLabel.Location = new System.Drawing.Point(8, 112);
            this.m_aCopyrightLabel.Name = "m_aCopyrightLabel";
            this.m_aCopyrightLabel.Size = new System.Drawing.Size(234, 16);
            this.m_aCopyrightLabel.TabIndex = 0;
            this.m_aCopyrightLabel.Text = "Copyright 2004-2019 CardWerk Technologies";
            // 
            // HelloGeldkarteForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(448, 133);
            this.Controls.Add(this.m_aPromptLabel);
            this.Controls.Add(this.m_aLinkLabel);
            this.Controls.Add(this.m_aCopyrightLabel);
            this.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HelloGeldkarteForm";
            this.Text = "Geldkarte sample code rev 30OCT2019";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
	}			
}

// history
// 29JUN2011 MJ change Taschenkartenleser to HelloGeldkarte
// 08MAR2011 MJ translate messages to English
