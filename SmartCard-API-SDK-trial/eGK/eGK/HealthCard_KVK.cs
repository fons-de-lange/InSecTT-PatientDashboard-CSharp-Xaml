// --------------------------------------------------------------------------------------------
// HealthCard_KVK.cs
// CardWerk SmartCard API
// Copyright © 2004-2020 CardWerk Technologies
// 
// CAUTION: 
// OMNIKEY readers with Aviator chipset must be set to 5V power-up voltage
// 
// HIST:
// 11OCT2020 MJ removed CT-API support. OMNIKEY and CHERRY readers are supported via pseudo APDUs implemented on firmware level
// 
// TODO:
// - take card handle as input for generic constructor
// - move eGK, KVK code to CardModule.eGK
// --------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Text;

using Subsembly.SmartCard;
using SmartCardAPI.CardModule.MemoryCard;

namespace GermanHealthInsuranceCard
{
    /// <summary>
    /// This is a simple example for a card service implementation for legacy German health card 
    /// This Krankenversicherungskarte (KVK) is based on a memory chip i.e synchronous card that
    /// is not supported by all card readers.
    /// </summary>

    public class HealthCard_KVK : HealthCard
	{
		/// <summary>
		/// We overrride the abstract CardName as part of our contract with the base class. 
		/// </summary>

		public override string CardName
		{
			get
			{
				return "Krankenversichertenkarte KVK";
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aSlot"></param>
		/// <returns></returns>

		public CardActivationResult Read(CardTerminalSlot aSlot)
		{
            /// <summary>   Size of KVK data block stored on memory card. </summary>
            const int KVK_DATA_SIZE = 256;

            const string ATR_KVK_SLE4432 = "3B04A2131091";

            base.Clear();

            // Try to acquire a CardHandle to the inserted card. We try three different memory card types that are used for German health insurance cards. All these
            // cards are storage cards with synchronous interface. 
            CardHandle aCard = null; 
			CardActivationResult nActivationResult = CardActivationResult.Unknown;
            aCard = aSlot.AcquireCard(CardTypes.ProcessorCards, out nActivationResult);   // only works for OMNIKEY and CHERRY card readers. For support of other card readers, please use CT-API or contact us for custom implementation 

            if (aCard == null)
            {
                return nActivationResult;
            }

            MemoryCard kvk_memory_card = null;
            string cardName = "SLE4432"; // todo: get name by analyzing the ATR to support the folloowing cards: SLE4432, SLE4442, SLE4418, AT24C02SC
            kvk_memory_card = new MemoryCard(aCard, cardName);

            if(!kvk_memory_card.IsReady)
            {
                return CardActivationResult.UnsupportedCard;
            }

			try
			{
                // Now read all data from the card.

				byte[] vbData = null;
				int nAppOffset;
				int nAppLength;
                
			    vbData = kvk_memory_card.Read(0, KVK_DATA_SIZE);

                // The returned data should now contain the complete health card data.

                if (_IsAtrHeader(vbData))
					{
						int nDirOffset = vbData[3] & 0x7F;
						int nDirLength = 2 + vbData[nDirOffset + 1];

						nAppOffset = nDirOffset + nDirLength;
					}
					else
					{
						nAppOffset = 0;
					}

				// Check whether we have a valid template at the suggested nAppOffset.

				if ((nAppOffset + 51) > vbData.Length)
				{
					Trace.WriteLine("Not enough template data!");
					return CardActivationResult.UnsupportedCard;
				}

				if (vbData[nAppOffset] != 0x60)
				{
					Trace.WriteLine("Template not found!");
					return CardActivationResult.UnsupportedCard;
				}

				byte nLenByte = vbData[nAppOffset + 1];
				if (nLenByte == 0x81)
				{
					nAppLength = 3 + vbData[nAppOffset + 2];
				}
				else if (nLenByte < 0x80)
				{
					nAppLength = 2 + nLenByte;
				}
				else
				{
					Trace.WriteLine("Bad template length format!");
					return CardActivationResult.UnsupportedCard;
				}

				if ((nAppOffset + nAppLength) > vbData.Length)
				{
					Trace.WriteLine("Bad template length!");
					return CardActivationResult.UnsupportedCard;
				}

				// Now try to parse the complete BER encoded application data.

				CardDataObject aRootDO = CardDataObject.Parse(
					vbData, ref nAppOffset, ref nAppLength,
					CardDataObjectEncoding.BER);

				CardDataObject aInsuranceNameDO = aRootDO.Find(0x80, false);
				if (aInsuranceNameDO != null)
				{
					m_sInsuranceName = _GetString(aInsuranceNameDO);
				}

				CardDataObject aInsuranceNumberDO = aRootDO.Find(0x81, false);
				if (aInsuranceNumberDO != null)
				{
					m_sInsuranceNumber = _GetString(aInsuranceNumberDO);
				}

				CardDataObject aInsuredNumberDO = aRootDO.Find(0x82, false);
				if (aInsuredNumberDO != null)
				{
					m_sInsuredNumber = _GetString(aInsuredNumberDO);
				}

				CardDataObject aTitleNameDO = aRootDO.Find(0x84, false);
				if (aTitleNameDO != null)
				{
					m_sTitleName = _GetString(aTitleNameDO);
				}

				CardDataObject aFirstNameDO = aRootDO.Find(0x85, false);
				if (aFirstNameDO != null)
				{
					m_sFirstName = _GetString(aFirstNameDO);
				}

				CardDataObject aLastNameDO = aRootDO.Find(0x87, false);
				if (aLastNameDO != null)
				{
					m_sLastName = _GetString(aLastNameDO);
				}

				CardDataObject aStreetDO = aRootDO.Find(0x89, false);
				if (aStreetDO != null)
				{
					m_sStreet = _GetString(aStreetDO);
				}

				CardDataObject aZipCodeDO = aRootDO.Find(0x8B, false);
				if (aZipCodeDO != null)
				{
					m_sZipCode = _GetString(aZipCodeDO);
				}

				CardDataObject aCityCodeDO = aRootDO.Find(0x8C, false);
				if (aCityCodeDO != null)
				{
					m_sCity = _GetString(aCityCodeDO);
				}

				CardDataObject aBirthdayDO = aRootDO.Find(0x88, false);
				if (aBirthdayDO != null)
				{
					string sBirthday = _GetString(aBirthdayDO);
					m_tBirthday = DateTime.ParseExact(sBirthday, "ddMMyyyy", null);
				}

                
				return CardActivationResult.Success;
			}
			finally
			{
                aCard.Dispose();
			}
		}

		/// <summary>
		/// Memory card contains 4-byte ATR starting at offset 0 
		/// </summary>
		/// <param name="vbData">raw data stored on KVK memory card</param>
		/// <returns></returns>

		static bool _IsAtrHeader(byte[] vbData)
		{
			Debug.Assert(vbData != null);
			Debug.Assert(vbData.Length >= 4);

			return
				((vbData[0] == 0x82) || (vbData[0] == 0x92) || (vbData[0] == 0xA2)) &&
				(vbData[1] == 0x13) && (vbData[2] == 0x10) && (vbData[3] == 0x91);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aDO"></param>
		/// <returns></returns>

		static string _GetString(CardDataObject aDO)
		{
			StringBuilder sb = new StringBuilder(aDO.Length);
			byte[] vbValue = aDO.Value;
			for (int i = 0; i < vbValue.Length; ++i)
			{
				byte b = vbValue[i];
				if ((b < 0x20) || (b > 0x7F))
				{
					sb.Append('?');
				}
				else
				{
					sb.Append(g_vchDin66003Table[b - 0x20]);
				}
			}
			return sb.ToString();
		}

		static char[] g_vchDin66003Table = new char[]
		{
			' ', '?', '?', '?', '?', '?', '&', '\'', '(', ')', '?', '+', '?', '-', '.', '/',
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '?', '?', '?', '?', '?', '?',
			'?', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
			'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'Ä', 'Ö', 'Ü', '?', '?',
			'?', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
			'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'ä', 'ö', 'ü', 'ß', '?'
		};
	}
}
