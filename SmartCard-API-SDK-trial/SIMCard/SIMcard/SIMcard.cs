// --------------------------------------------------------------------------------------------
// SIMcard.cs
// CardWerk SmartCard API
// Copyright © 2004-2019 CardWerk Technologies
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Windows.Forms;
using System.Text;
using Subsembly.SmartCard;

namespace SampleCode_SIMcard
{
	public class SIMcard
	{
		//CardDialogsForm m_aCardForm = new CardDialogsForm();
		CardHandle m_aSim;
		byte[] m_vbIccIdentification;
		ArrayList m_aPhonebook = new ArrayList();

		/// <summary>
		/// </summary>

		public SIMcard(CardHandle aSim)
		{
			m_aSim = aSim;
		}

		/// <summary>
		/// 
		/// </summary>

		public ArrayList Phonebook
		{
			get
			{
				return m_aPhonebook;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aCommandAPDU"></param>
		/// <returns></returns>

		CardResponseAPDU _SendSimCommand(CardCommandAPDU aCommandAPDU)
		{
			CardResponseAPDU aResponseAPDU = m_aSim.SendCommand(aCommandAPDU);
			if ((aResponseAPDU.SW1 == 0x9F) && (aResponseAPDU.Lr == 0))
			{
				CardCommandAPDU aGetResponseAPDU = new CardCommandAPDU(0xA0, 0xC0, 0x00, 0x00,
					aResponseAPDU.SW2);
				aResponseAPDU = m_aSim.SendCommand(aGetResponseAPDU);
			}
			else if ((aResponseAPDU.SW1 == 0x67) && (aResponseAPDU.SW2 != 0x00))
			{
				aCommandAPDU.Le = aResponseAPDU.SW2;
				aResponseAPDU = m_aSim.SendCommand(aCommandAPDU);
			}

			return aResponseAPDU;
		}

		/// <summary>
		/// 
		/// </summary>

		CardResponseAPDU _Select(int nFileID)
		{
			CardCommandAPDU aCommandAPDU;
			byte[] vbFileID = new byte[2];
			
			vbFileID[0] = (byte)((nFileID >> 8) & 0xFF);
			vbFileID[1] = (byte)(nFileID & 0xFF);

			aCommandAPDU = new CardCommandAPDU(0xA0, 0xA4, 0x00, 0x00, vbFileID);
			return _SendSimCommand(aCommandAPDU);
		}

		/// <summary>
		/// 
		/// </summary>

		byte[] _ReadBinary(int nOffset, int nLength)
		{
			CardCommandAPDU aCommandAPDU;
			CardResponseAPDU aResponseAPDU;

			aCommandAPDU = new CardCommandAPDU(0xA0, 0xB0,
				(byte)((nOffset >> 8) & 0xFF), (byte)(nOffset & 0xFF), nLength);
			aResponseAPDU = _SendSimCommand(aCommandAPDU);
			if (!aResponseAPDU.IsSuccessful)
			{
				return null;
			}
			else
			{
				return aResponseAPDU.GetData();
			}
		}

		/// <summary>
		/// 
		/// </summary>

		byte[] _ReadRecord(int nRecord, int nLength)
		{
			CardCommandAPDU aCommandAPDU;
			CardResponseAPDU aResponseAPDU;

			aCommandAPDU = new CardCommandAPDU(0xA0, 0xB2, (byte)(nRecord), 0x04, nLength);
			aResponseAPDU = _SendSimCommand(aCommandAPDU);
			if (!aResponseAPDU.IsSuccessful)
			{
				return null;
			}
			else
			{
				return aResponseAPDU.GetData();
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public bool ReadIccIdentification()
		{
			try
			{
				CardResponseAPDU aResponseAPDU;

				aResponseAPDU = _Select(0x3F00);
				if (!aResponseAPDU.IsSuccessful)
				{
					return false;
				}

				aResponseAPDU = _Select(0x2FE2);
				if (!aResponseAPDU.IsSuccessful)
				{
					return false;
				}

				m_vbIccIdentification = _ReadBinary(0, 10);
				return (m_vbIccIdentification != null);
			}
			catch (CardTerminalException)
			{
				return false;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public bool SelectDFTelecom()
		{
			try
			{
				CardResponseAPDU aResponseAPDU = _Select(0x7F10);
				return aResponseAPDU.IsSuccessful;
			}
			catch (CardTerminalException)
			{
				return false;
			}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aOwner"></param>
        /// <returns></returns>
        public bool VerifyCardHolderPIN(string pin)
        {
            if (pin == null || pin.Length == 0)
            {
                return false;
            }
            if(pin.Length > 8)
            {
                return false;
            }
			try
			{
				CardCommandAPDU aVerifyAPDU = new CardCommandAPDU(0xA0, 0x20, 0x00, 0x01);
                CardResponseAPDU aResponse = null;
               
                int digitCount = 0;
                foreach (char digit in pin)
                {
                    digitCount++;
                    aVerifyAPDU.AppendData((byte) digit);
                }
                while(digitCount<8)
                {
                    digitCount++;
                    aVerifyAPDU.AppendData(CardHex.ToByteArray("FF"));
                }
                aResponse = m_aSim.SendCommand(aVerifyAPDU);

                if (aResponse == null)
                    {
                        return false;
                    }
                return aResponse.IsSuccessful || aResponse.IsWarning;
            
			}
			catch (CardTerminalException)
			{
				return false;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		void _AddPhonebookRecord(byte[] vbRecord)
		{
			if (vbRecord[0] == 0xFF)
			{
				return;
			}

			int nAlphaTagLen;
			for (nAlphaTagLen = vbRecord.Length - 14; nAlphaTagLen > 0; --nAlphaTagLen)
			{
				if (vbRecord[nAlphaTagLen - 1] != 0xFF)
				{
					break;
				}
			}

			string sName = Encoding.ASCII.GetString(vbRecord, 0, nAlphaTagLen);
			string sDigits = "0123456789*#cdef";
			// TODO: Decode number
			StringBuilder sbNumber = new StringBuilder(20);
			for (int i = vbRecord.Length - 12; i < vbRecord.Length - 2; ++i)
			{
				if (vbRecord[i] == 0xFF)
				{
					break;
				}
				sbNumber.Append(sDigits[vbRecord[i] & 0x0F]);
				if ((vbRecord[i] & 0xF0) != 0xF0)
				{
					sbNumber.Append(sDigits[(vbRecord[i] >> 4) & 0x0F]);
				}
			}
			m_aPhonebook.Add(new DictionaryEntry(sName, sbNumber.ToString()));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>

		public bool ReadAbbreviatedDiallingNumbers()
		{
			try
			{
				CardResponseAPDU aResponseAPDU;

				aResponseAPDU = _Select(0x6F3A);
				if (!aResponseAPDU.IsSuccessful)
				{
					return false;
				}
				byte[] vbFileInfo = aResponseAPDU.GetData();

				int nRecordLength = vbFileInfo[14];
				int nRecordCount = ((vbFileInfo[2] << 8) + vbFileInfo[3]) / nRecordLength;
		
				for (int iRecord = 1; iRecord <= nRecordCount; ++iRecord)
				{
					byte[] vbRecord = _ReadRecord(iRecord, nRecordLength);
					if (vbRecord != null)
					{
						_AddPhonebookRecord(vbRecord);
					}
					else
					{
						return false;
					}
				}

				return true;
			}
			catch (CardTerminalException)
			{
				return false;
			}
		}
	}
}
