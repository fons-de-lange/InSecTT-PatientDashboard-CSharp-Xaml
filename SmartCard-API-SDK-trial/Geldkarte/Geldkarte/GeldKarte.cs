// --------------------------------------------------------------------------------------------
// GeldKarte.cs
// CardWerk SmartCard API
// Copyright © 2004-2019 CardWerk Technologies
// --------------------------------------------------------------------------------------------

using System;
using Subsembly.SmartCard;

namespace SampleCode_Geldkarte
{
	/// <summary>
	/// This is a simple example for a card service implementation for the german electronic
	/// purse system (GeldKarte).
	/// </summary>

	public class GeldKarte
	{
		CardHandle	m_aCard;
		bool		   m_fDidReadID;
		byte[]		m_vbEFID;
		int			m_nType;
		string		m_sCurrency;
		bool		   m_fDidReadAmounts;
		decimal		m_dcAmount;
		decimal		m_dcMaxAmount;
		decimal		m_dcLimitAmount;

		/// <summary>
		/// Attempts to select the GeldKarte application on the given card,
		/// </summary>
		/// <param name="aCard"></param>
		/// <returns>
		/// If the GeldKarte application was successfully selected, then this class takes
		/// over the ownership of the given CardHandle and returns <c>true</c> to indicate
		/// success. In this case, the caller is responsible to ultimately release the card
		/// again via <see cref="Release"/>.
		/// If the GeldKarte could not be selected, then <c>false</c> is returned.
		/// </returns>

		public bool Select(CardHandle aCard)
		{
			if (aCard == null)
			{
				throw new ArgumentNullException();
			}
			if (m_aCard != null)
			{
				throw new InvalidOperationException();
			}

			CardResponseAPDU aRespAPDU;

			aRespAPDU = aCard.SelectApplication(CardHex.ToByteArray("D27600002545500200"));
			if (aRespAPDU.IsSuccessful)
			{
				m_aCard = aCard;
				m_nType = 1;
				return true;
			}

			aRespAPDU = aCard.SelectApplication(CardHex.ToByteArray("D27600002545500100"));
			if (aRespAPDU.IsSuccessful)
			{
				m_aCard = aCard;
				m_nType = 0;
				return true;
			}

			return false;
		}

		/// <summary>
		/// 
		/// </summary>

		public void Release()
		{
			if (m_aCard != null)
			{
				m_aCard.Dispose();
				m_aCard = null;
			}

			m_fDidReadID = false;
			m_vbEFID = null;
			m_nType = 0;
			m_sCurrency = null;
			m_fDidReadAmounts = false;
			m_dcAmount = 0;
			m_dcMaxAmount = 0;
			m_dcLimitAmount = 0;
		}

		/// <value>
		/// Provides the current balance amount of the GeldKarte in its own original
		/// currency.
		/// </value>

		public decimal OriginalAmount
		{
			get
			{
				if (!m_fDidReadAmounts)
				{
					throw new InvalidOperationException();
				}
				return m_dcAmount;
			}
		}

		/// <value>
		/// Provides the original currency of the GeldKarte. This is either EUR or DEM.
		/// </value>

		public string OriginalCurrency
		{
			get
			{
				if (!m_fDidReadID)
				{
					throw new InvalidOperationException();
				}
				return m_sCurrency;
			}
		}

		/// <value>
		/// Provides the current balance amount of the GeldKarte in Euro.
		/// </value>

		public decimal Amount
		{
			get
			{
				if (!m_fDidReadAmounts)
				{
					throw new InvalidOperationException();
				}

				return _EuroAmount(m_dcAmount);
			}
		}
		
		/// <value>
		/// Provides the maximum balance amount of the GeldKarte in Euro.
		/// </value>

		public decimal MaxAmount
		{
			get
			{
				if (!m_fDidReadAmounts)
				{
					throw new InvalidOperationException();
				}
				return _EuroAmount(m_dcMaxAmount);
			}
		}

		/// <value>
		/// Provides the maximum transaction amount of the GeldKarte in Euro.
		/// </value>

		public decimal Limitmount
		{
			get
			{
				if (!m_fDidReadAmounts)
				{
					throw new InvalidOperationException();
				}
				return _EuroAmount(m_dcLimitAmount);
			}
		}

		/// <summary>
		/// Reads and evaluates the card identification EF_ID.
		/// </summary>

		public bool ReadID()
		{
			// Read the EF_ID file.

			CardCommandAPDU aCommandAPDU;
			CardResponseAPDU aResponseAPDU;

			aCommandAPDU = new CardCommandAPDU(0x00, 0xB2, 0x01, 0xBC, 0);
			aResponseAPDU = m_aCard.SendCommand(aCommandAPDU);
			if (!aResponseAPDU.IsSuccessful || (aResponseAPDU.Lr == 0))
			{
				return false;
			}
			m_vbEFID = aResponseAPDU.GetData();

			// Validate the data.

			if (((m_nType == 0) && (m_vbEFID.Length != 22)) ||
				((m_nType == 1) && (m_vbEFID.Length < 24)))
			{
				return false;
			}
			if (m_vbEFID[20] != 1)
			{
				return false;
			}

			// Determine the currency from the EF_ID.

			char[] vchCurrency = new char[3];
			vchCurrency[0] = (char)m_vbEFID[17];
			vchCurrency[1] = (char)m_vbEFID[18];
			vchCurrency[2] = (char)m_vbEFID[19];

			m_sCurrency = new string(vchCurrency);

			if ((m_sCurrency != "DEM") && (m_sCurrency != "EUR"))
			{
				return false;
			}
			if (((m_nType == 0) && (m_sCurrency == "EUR")) ||
				((m_nType == 1) && (m_sCurrency == "DEM")))
			{
				return false;
			}

			m_fDidReadID = true;
			return true;
		}
		
		/// <summary>
		/// Reads and evaluates the card balances from EF_BETRAG.
		/// </summary>

		public bool ReadAmounts()
		{
			if (!m_fDidReadID)
			{
				throw new InvalidOperationException();
			}

			// Read the EF_BETRAG file.

			CardCommandAPDU aCommandAPDU;
			CardResponseAPDU aResponseAPDU;

			aCommandAPDU = new CardCommandAPDU(0x00, 0xB2, 0x01, 0xC4, 0);
			aResponseAPDU = m_aCard.SendCommand(aCommandAPDU);
			if (!aResponseAPDU.IsSuccessful)
			{
				return false;
			}
			byte[] vbEFBetrag = aResponseAPDU.GetData();

			// Determine the amounts from the EF_BETRAG.

			string sHexEFBetrag = CardHex.FromByteArray(vbEFBetrag);

			m_dcAmount = Decimal.Parse(sHexEFBetrag.Substring(0, 6)) / 100;
			m_dcMaxAmount = Decimal.Parse(sHexEFBetrag.Substring(6, 6)) / 100;
			m_dcLimitAmount = Decimal.Parse(sHexEFBetrag.Substring(12, 6)) / 100;
			
			//

			m_fDidReadAmounts = true;
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dcOriginalAmount"></param>
		/// <returns></returns>

		decimal _EuroAmount(decimal dcOriginalAmount)
		{
			if (m_sCurrency == "EUR")
			{
				return dcOriginalAmount;
			}

			// We have to do a manual rounding, because the Decimal.Round method uses a
			// different rounding method as we require!
			return Decimal.Floor((dcOriginalAmount / 1.95583m) * 100m + 0.5m) / 100m;
		}
	}
}
