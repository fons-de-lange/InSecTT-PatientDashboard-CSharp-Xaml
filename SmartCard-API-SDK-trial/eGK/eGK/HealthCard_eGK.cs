// --------------------------------------------------------------------------------------------
// EgkHealthCard.cs
// CardWerk SmartCard API
// Copyright © 2004-2013 CardWerk Technologies
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

using System.Threading; // ... Thread.Sleep(50); 

using Subsembly.SmartCard;

namespace GermanHealthInsuranceCard
{
	/// <summary>
	/// This is a simple example for a card service implementation for the new, smart card
	/// based German health card (eGK).
	/// </summary>

	public class HealthCard_eGK : HealthCard
	{
		static Encoding g_aEncoding;

        const string AID_KVK  = "D27600000101";
		const string DF_HCA   = "D27600000102";       // health care application
        const string AID_EGK_ROOT = "D2760001448000"; // root/master of G1, G1plus and G2
		const int FID_EF_PD = 0xD001;
		const int FID_EF_VD = 0xD002;
        const int FID_EF_VERSION = 0x10;

        bool isG0 = false;
        bool isG1 = false;
        bool isG2 = false;

		/// <summary>
		/// 
		/// </summary>

		static HealthCard_eGK()
		{
			g_aEncoding = Encoding.GetEncoding("iso-8859-15");
		}

		/// <summary>
		/// 
		/// </summary>

		public override string CardName
		{
			get
			{
				return "eGK Elektronische Gesundheitskarte";
			}
		}

		/// <summary>
		/// todo: work on better return (boolean would do ) 
		/// </summary>
		/// <param name="aSlot"></param>
		/// <returns></returns>

		public CardActivationResult Read(CardTerminalSlot aSlot)
		{
			base.Clear();

			// Try to acquire a CardHandle to the inserted card. We try any processor card.

			CardHandle aCard = null;
            CardActivationResult nActivationResult;

			aCard = aSlot.AcquireCard(CardTypes.ProcessorCards, out nActivationResult);
			if (aCard == null)
			{
                /*
                 * // 02JUL2012 MJ: try again after 100 ms to improve tests with eGK001
                Debug.WriteLine("1st activation attempt failed .. trying again");
                Thread.Sleep(100); 
                aCard = aSlot.AcquireCard(CardTypes.ProcessorCards, out nActivationResult); 
                if(aCard==null) */ return nActivationResult;
			}
            


			try
			{
				CardResponseAPDU aResponseAPDU;

                // aSlot.BeginTransaction();
				// Try to select eGK application

                aResponseAPDU = aCard.SelectApplication(CardHex.ToByteArray(AID_EGK_ROOT));
				if (aResponseAPDU.IsSuccessful)
				{
                    byte[] r1 = null;
                    byte[] r2 = null;
                    byte[] r3 = null;
                    // read 1st two bytes of version data
                    CardCommandAPDU readVersionRecord1 = new CardCommandAPDU(0x00, 0xB2, 0x01, 0x84, 256);
                    CardResponseAPDU respReadVersion = aCard.SendCommand(readVersionRecord1);
                    if (respReadVersion.IsSuccessful)
                    {
                        r1 = respReadVersion.GetData();
                    }
                    readVersionRecord1.P2 = 0x02;
                    readVersionRecord1.P2 = 0x04;
                    respReadVersion = aCard.SendCommand(readVersionRecord1);
                    if (respReadVersion.IsSuccessful)
                    {
                        r2 = respReadVersion.GetData();
                    }
                    readVersionRecord1.P2 = 0x03;
                    readVersionRecord1.P2 = 0x04;
                    respReadVersion = aCard.SendCommand(readVersionRecord1);
                    if (respReadVersion.IsSuccessful)
                    {
                        r3 = respReadVersion.GetData();
                    }
                    if(CardHex.IsEqual(r1, CardHex.ToByteArray("0040000000")) &&
                       CardHex.IsEqual(r2, CardHex.ToByteArray("0040000000")) &&
                       CardHex.IsEqual(r3, CardHex.ToByteArray("0040000000")))
                    {
                        isG2 = true;
                    }
                    else if (CardHex.IsEqual(r1, CardHex.ToByteArray("0030000000")) &&
                             CardHex.IsEqual(r2, CardHex.ToByteArray("0030000000")) &&
                             (CardHex.IsEqual(r3, CardHex.ToByteArray("0030000000")) || CardHex.IsEqual(r3, CardHex.ToByteArray("0030001000")) || CardHex.IsEqual(r3, CardHex.ToByteArray("0030002000")))) // <= 3.0.2
                    {
                        isG1 = true;
                    }

			    }
                if (isG1 || isG2)
                    {
                        aResponseAPDU = aCard.SelectApplication(CardHex.ToByteArray(DF_HCA));
                        if (!aResponseAPDU.IsSuccessful)
                        {
                            return CardActivationResult.UnsupportedCard;
                        }
                        Debug.WriteLine("This eGK is not a credential that is accepted anymore.");
                    }
                else
                {
                    aResponseAPDU = aCard.SelectApplication(CardHex.ToByteArray(AID_KVK)); // this is a virtual AID available via CT-API
                    if (aResponseAPDU.IsSuccessful)
                    {
                        Debug.WriteLine("KVK is not accepted anymore. It can be accessed on selected readers (OMNIKEY and CHERRY)");
                        isG0 = true;
                    }
                }

				//
                if (isG1 || isG2) // eGK G1 card; works with sample card eGK001, 02FEB2012
                {

                    byte[] vbPD = _ReadBinaryFile(aCard, FID_EF_PD); // persoenliche versicherungsdaten
                    if (vbPD == null)
                    {
                        Trace.WriteLine("Failed to read EF_PD!");
                        return CardActivationResult.UnsupportedCard;
                    }
                    if (vbPD.Length < 2)
                    {
                        Trace.WriteLine("EF_PD too short!");
                        return CardActivationResult.UnsupportedCard;
                    }

                    int nLenPD = (vbPD[0] << 8) + vbPD[1];
                    if (nLenPD == 0)
                    {
                        Trace.WriteLine("Length in EF_PD too short!");
                        return CardActivationResult.UnsupportedCard;
                    }
                    if ((nLenPD + 2) > vbPD.Length)
                    {
                        Trace.WriteLine("Length in EF_PD too long!");
                        return CardActivationResult.UnsupportedCard;
                    }

                    byte[] vbUnzippedPD = _Unzip(vbPD, 2, nLenPD);

                    if (vbUnzippedPD == null)
                    {
                        Trace.WriteLine("Failed to unzip EF_PD!");
                        return CardActivationResult.UnsupportedCard;
                    }

                    string sPD = g_aEncoding.GetString(vbUnzippedPD);
                    Trace.WriteLine(sPD);

                    // Finally decode the XML data.

                    _DecodePD(sPD);

                    //============ G1 and G2 data is clearly visible but not shown in form ==============

                    //

                    byte[] vbVD = _ReadBinaryFile(aCard, FID_EF_VD); // versicherungsdaten
                    if (vbVD == null)
                    {
                        Trace.WriteLine("Failed to read EF_VD!");
                        return CardActivationResult.UnsupportedCard;
                    }
                    if (vbVD.Length < 8)
                    {
                        Trace.WriteLine("EF_VD too short!");
                        return CardActivationResult.UnsupportedCard;
                    }

                    int nStartVD = (vbVD[0] << 8) + vbVD[1];
                    int nEndVD = (vbVD[2] << 8) + vbVD[3];
                    int nLenVD = nEndVD - nStartVD;
                    if (nLenVD <= 0)
                    {
                        Trace.WriteLine("Length in EF_VD too short!");
                        return CardActivationResult.UnsupportedCard;
                    }
                    if (nEndVD > vbVD.Length)
                    {
                        Trace.WriteLine("End offset in EF_VD after end of file!");
                        return CardActivationResult.UnsupportedCard;
                    }

                    byte[] vbUnzippedVD = _Unzip(vbVD, nStartVD, nLenVD);

                    if (vbUnzippedVD == null)
                    {
                        Trace.WriteLine("Failed to unzip EF_VD!");
                        return CardActivationResult.UnsupportedCard;
                    }

                    string sVD = g_aEncoding.GetString(vbUnzippedVD);
                    Trace.WriteLine(sVD);

                    // Finally decode the XML data.

                    _DecodeVD(sVD);

                    return CardActivationResult.Success;
                }
			}
			finally
			{
                //aSlot.EndTransaction(); 
                //aCard.Dispose();
                aCard.Disconnect();
			}

            return CardActivationResult.Unknown;
		}

		/// <summary>
		/// Reads the complete content of a transparent file.
		/// </summary>
		/// <param name="aCard">
		/// Handle of card to read the file from. Must not be <c>null</c>.
		/// </param>
		/// <param name="nFID">
		/// The file ID of the EF that should be read.
		/// </param>
		/// <returns>
		/// If the requested EF could not be selected successfully, then <c>null</c> is
		/// returned. Otherwise the complete content of the file is returned in a single byte
		/// array.
		/// </returns>

		static byte[] _ReadBinaryFile(CardHandle aCard, int nFID)
		{
			if (aCard == null)
			{
				throw new ArgumentNullException();
			}

			CardResponseAPDU aRespAPDU;

			// Try to select the requested file.
            if (nFID > 256)
            {
                aRespAPDU = aCard.SelectFile(nFID);
                if (!aRespAPDU.IsSuccessful)
                {
                    Trace.WriteLine(String.Format(
                        "Failed to select file '{0:X4}'. SW = '{1:X4}'.",
                        nFID, aRespAPDU.SW));
                    return null;
                }
            }
            else
            {
                // work with shior ID
            }

			// Read the entire file in max size chunks with progressing offset until an error
			// is encountered. The data chunks are collected in an array list. Make sure the reader & driver 
            // allow max chunk size. Adapt according to terminal name and third party reader specs otherwise.

			ArrayList vList = new ArrayList();

			int nOffset = 0;
            int nMaxChunk = 0xFE;
			int nMaxLength = 0x7FFF;
            if (aCard.Slot.CardTerminalName.Contains("SCM Microsystems"))
            {
                nMaxChunk = 0x64; //30JAN2012 SCR335 fails with 0xFE
            }
			while (nOffset < nMaxLength)
			{
				int nChunk = Math.Min(nMaxChunk, nMaxLength - nOffset);

				aRespAPDU = aCard.ReadBinary(nOffset, (byte)nChunk);
				if (aRespAPDU.IsError)
				{
					break;
				}

				byte[] vbChunk = aRespAPDU.GetData();
				if ((vbChunk == null) || (vbChunk.Length == 0))
				{
					break;
				}

				vList.Add(vbChunk);
				nOffset += vbChunk.Length;

				if (vbChunk.Length < nChunk)
				{
					break;
				}
			}

			// The final offset equals the total size of the collected data. This is used to
			// create a new byte array where all the collected data chunks are merged.

			byte[] vbFile = new byte[nOffset];

			nOffset = 0;
			for (int i = 0; i < vList.Count; ++i)
			{
				byte[] vb = (byte[])vList[i];
				Buffer.BlockCopy(vb, 0, vbFile, nOffset, vb.Length);
				nOffset += vb.Length;
			}

			return vbFile;
		}

		/// <summary>
		/// ZIP-decompression.
		/// </summary>
		/// <param name="vb"></param>
		/// <returns></returns>

		static byte[] _Unzip(byte[] vb, int nOffset, int nLength)
		{
			// Decompress data.

			MemoryStream aCompressedStream = new MemoryStream(
				vb, nOffset, nLength, false);
			GZipStream aGZipStream = new GZipStream(aCompressedStream, CompressionMode.Decompress);

			byte[] vbBuffer = new byte[16384];

			int nActual = aGZipStream.Read(vbBuffer, 0, vbBuffer.Length);
			if (nActual == 0)
			{
				return null;
			}
			if (nActual == vbBuffer.Length)
			{
				// TODO: Buffer overflow!
				return null;
			}

			aGZipStream.Close();

			//

			byte[] vbUnzipped = new byte[nActual];
			Buffer.BlockCopy(vbBuffer, 0, vbUnzipped, 0, nActual);

			return vbUnzipped;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sPD"></param>

		void _DecodePD(string sPD)
		{
			// <?xml version="1.0" encoding="iso-8859-15"?>
			// <UC_PersoenlicheVersichertendatenXML xmlns="http://ws.gematik.de/fa/vsds/UC_PersoenlicheVersichertendatenXML/v5.1" CDM_VERSION="5.1.0">
			// 	<Versicherter>
			// 		<Versicherten_ID>D123456786</Versicherten_ID>
			// 		<Person>
			// 			<Geburtsdatum>19660324</Geburtsdatum>
			// 			<Vorname>Hans</Vorname>
			// 			<Nachname>Mustermann</Nachname>
			// 			<Geschlecht>M</Geschlecht>
			// 			<Titel>Professor</Titel>
			// 			<StrassenAdresse>
			// 				<Postleitzahl>10117</Postleitzahl>
			// 				<Ort>Berlin</Ort>
			// 				<Land>
			// 					<Wohnsitzlaendercode>D</Wohnsitzlaendercode>
			// 				</Land>
			// 				<Strasse>Friedrichstraﬂe</Strasse>
			// 				<Hausnummer>136</Hausnummer>
			// 				<Anschriftenzusatz>erste Etage</Anschriftenzusatz>
			// 			</StrassenAdresse>
			// 		</Person>
			// 	</Versicherter>
			// </UC_PersoenlicheVersichertendatenXML>

            string sNamespace = "http://ws.gematik.de/fa/vsds/UC_PersoenlicheVersichertendatenXML/v5.1"; // G0 & G1
            if(sPD.Contains("http://ws.gematik.de/fa/vsdm/vsd/v5.2"))
            {
                sNamespace = "http://ws.gematik.de/fa/vsdm/vsd/v5.2";
            }

			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(sPD);

			XmlElement xmlPersoenlicheVersichertendatenXML = xmlDocument.DocumentElement;
			XmlElement xmlVersicherter = xmlPersoenlicheVersichertendatenXML["Versicherter", sNamespace];
			if (xmlVersicherter != null)
			{
				XmlElement xmlVersichertenID = xmlVersicherter["Versicherten_ID", sNamespace];
				if (xmlVersichertenID != null)
				{
					m_sInsuredNumber = xmlVersichertenID.InnerText;
				}

				XmlElement xmlPerson = xmlVersicherter["Person", sNamespace];
				if (xmlPerson != null)
				{
					XmlElement xmlTitel = xmlPerson["Titel", sNamespace];
					if (xmlTitel != null)
					{
						m_sTitleName = xmlTitel.InnerText;
					}

					XmlElement xmlVorname = xmlPerson["Vorname", sNamespace];
					if (xmlVorname != null)
					{
						m_sFirstName = xmlVorname.InnerText;
					}

					XmlElement xmlNachname = xmlPerson["Nachname", sNamespace];
					if (xmlNachname != null)
					{
						m_sLastName = xmlNachname.InnerText;
					}

					XmlElement xmlGeburtsdatum = xmlPerson["Geburtsdatum", sNamespace];
					if (xmlGeburtsdatum != null)
					{
						m_tBirthday = DateTime.ParseExact(xmlGeburtsdatum.InnerText, "yyyyMMdd", null);
					}

					XmlElement xmlAdresse = xmlPerson["StrassenAdresse", sNamespace];
					if (xmlAdresse == null)
					{
						xmlAdresse = xmlPerson["PostfachAdresse", sNamespace];
					}

					if (xmlAdresse != null)
					{
						StringBuilder sb = new StringBuilder();

						XmlElement xmlStrasse = xmlAdresse["Strasse", sNamespace];
						if (xmlStrasse != null)
						{
							sb.Append(xmlStrasse.InnerText);

							XmlElement xmlHausnummer = xmlAdresse["Hausnummer", sNamespace];
							if (xmlHausnummer != null)
							{
								sb.Append(' ');
								sb.Append(xmlHausnummer.InnerText);
							}

							XmlElement xmlAnschriftenzusatz = xmlAdresse["Anschriftenzusatz", sNamespace];
							if (xmlAnschriftenzusatz != null)
							{
								sb.Append(Environment.NewLine);
								sb.Append(xmlAnschriftenzusatz.InnerText);
							}
						}
						else
						{
							XmlElement xmlPostfach = xmlAdresse["Postfach", sNamespace];
							if (xmlPostfach != null)
							{
								sb.Append("Postfach ");
								sb.Append(xmlPostfach.InnerText);
							}
						}
						
						m_sStreet = sb.ToString();

						//

						XmlElement xmlPostleitzahl = xmlAdresse["Postleitzahl", sNamespace];
						if (xmlPostleitzahl != null)
						{
							m_sZipCode = xmlPostleitzahl.InnerText;
						}

						XmlElement xmlOrt = xmlAdresse["Ort", sNamespace];
						if (xmlOrt != null)
						{
							m_sCity = xmlOrt.InnerText;
						}
					}

				}
			}

			// TODO: Much more interesting data could be extracted ...
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sVD"></param>

		void _DecodeVD(string sVD)
		{
            string sNamespace = "http://ws.gematik.de/fa/vsds/UC_PersoenlicheVersichertendatenXML/v5.1"; // G0 & G1
            if (sVD.Contains("http://ws.gematik.de/fa/vsdm/vsd/v5.2"))
            {
                sNamespace = "http://ws.gematik.de/fa/vsdm/vsd/v5.2";
            }
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(sVD);

			XmlElement xmlAllgemeineVersicherungsdatenXML = xmlDocument.DocumentElement;
			XmlElement xmlVersicherter = xmlAllgemeineVersicherungsdatenXML["Versicherter", sNamespace];
			if (xmlVersicherter != null)
			{
				XmlElement xmlVersicherungsschutz = xmlVersicherter["Versicherungsschutz", sNamespace];
				if (xmlVersicherungsschutz != null)
				{
					XmlElement xmlKostentraeger = xmlVersicherungsschutz["Kostentraeger", sNamespace];
					if (xmlKostentraeger != null)
					{
						XmlElement xmlKostentraegerkennung = xmlKostentraeger["Kostentraegerkennung", sNamespace];
						if (xmlKostentraegerkennung != null)
						{
							m_sInsuranceNumber = xmlKostentraegerkennung.InnerText;
						}

						XmlElement xmlName = xmlKostentraeger["Name", sNamespace];
						if (xmlName != null)
						{
							m_sInsuranceName = xmlName.InnerText;
						}
					}
				}
			}

			// TODO: Much more interesting data could be extracted ...
		}
	}
}
