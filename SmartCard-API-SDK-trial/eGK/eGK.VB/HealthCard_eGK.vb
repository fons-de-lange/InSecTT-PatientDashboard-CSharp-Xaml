' --------------------------------------------------------------------------------------------
' EgkHealthCard.vb
' CardWerk SmartCard API
' Copyright © 2004-2020 CardWerk Technologies
' --------------------------------------------------------------------------------------------
' HIST
' 06OCT2020 MJ: change: accept eGK 2 data format
' 27NOV2013 MJ: fix: calculation of Start/End/lenVD 

Imports System
Imports System.Collections
Imports System.Diagnostics
Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports System.Xml

Imports Subsembly.SmartCard

Namespace SampleCode_eGK
    ''' <summary>
    ''' This is a simple example for a card service implementation for the new, smart card
    ''' based german health card (eGK).
    ''' </summary>

    Public Class EgkHealthCard
        Inherits HealthCard
        Shared g_aEncoding As Encoding

        Const DF_HCA As String = "D27600000102"
        Const FID_EF_PD As Integer = &HD001
        Const FID_EF_VD As Integer = &HD002

        ''' <summary>
        ''' 
        ''' </summary>

        Shared Sub New()
            g_aEncoding = Encoding.GetEncoding("iso-8859-15")
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>

        Public Overrides ReadOnly Property CardName() As String
            Get
                Return "eGK Gesundheitskarte"
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="aSlot"></param>
        ''' <returns></returns>

        Public Function Read(ByVal aSlot As CardTerminalSlot) As CardActivationResult
            MyBase.Clear()

            ' Try to acquire a CardHandle to the inserted card. We try any processor card.

            Dim aCard As CardHandle = Nothing
            Dim nActivationResult As CardActivationResult

            aCard = aSlot.AcquireCard(CardTypes.ProcessorCards, nActivationResult)
            If aCard Is Nothing Then
                Return nActivationResult
            End If

            Try
                aSlot.BeginTransaction()

                Dim aResponseAPDU As CardResponseAPDU

                ' Try to select eGK application

                aResponseAPDU = aCard.SelectApplication(CardHex.ToByteArray(DF_HCA))
                If aResponseAPDU.IsError Then
                    Trace.WriteLine("Failed to select DF_HCA!")
                    Return CardActivationResult.UnsupportedCard
                End If

                '

                Dim vbPD As Byte() = _ReadBinaryFile(aCard, FID_EF_PD)
                If vbPD Is Nothing Then
                    Trace.WriteLine("Failed to read EF_PD!")
                    Return CardActivationResult.UnsupportedCard
                End If
                If vbPD.Length < 2 Then
                    Trace.WriteLine("EF_PD too short!")
                    Return CardActivationResult.UnsupportedCard
                End If

                Dim nLenPD As Integer = vbPD(0)
                nLenPD = nLenPD << 8
                nLenPD = nLenPD + vbPD(1)

                If nLenPD = 0 Then
                    Trace.WriteLine("Length in EF_PD too short!")
                    Return CardActivationResult.UnsupportedCard
                End If
                If (nLenPD + 2) > vbPD.Length Then
                    Trace.WriteLine("Length in EF_PD too long!")
                    Return CardActivationResult.UnsupportedCard
                End If

                Dim vbUnzippedPD As Byte() = _Unzip(vbPD, 2, nLenPD)

                If vbUnzippedPD Is Nothing Then
                    Trace.WriteLine("Failed to unzip EF_PD!")
                    Return CardActivationResult.UnsupportedCard
                End If

                Dim sPD As String = g_aEncoding.GetString(vbUnzippedPD)
                Trace.WriteLine(sPD)

                ' Finally decode the XML data.

                _DecodePD(sPD)

                '

                Dim vbVD As Byte() = _ReadBinaryFile(aCard, FID_EF_VD)
                If vbVD Is Nothing Then
                    Trace.WriteLine("Failed to read EF_VD!")
                    Return CardActivationResult.UnsupportedCard
                End If
                If vbVD.Length < 8 Then
                    Trace.WriteLine("EF_VD too short!")
                    Return CardActivationResult.UnsupportedCard
                End If

                Dim nStartVD As Integer = vbVD(0)
                nStartVD = nStartVD << 8
                nStartVD = nStartVD + vbVD(1)

                Dim nEndVD As Integer = vbVD(2)
                nEndVD = (nEndVD << 8)
                nEndVD = nEndVD + vbVD(3)
                Dim nLenVD As Integer = nEndVD - nStartVD
                If nLenVD <= 0 Then
                    Trace.WriteLine("Length in EF_VD too short!")
                    Return CardActivationResult.UnsupportedCard
                End If
                If nEndVD > vbVD.Length Then
                    Trace.WriteLine("End offset in EF_VD after end of file!")
                    Return CardActivationResult.UnsupportedCard
                End If

                Dim vbUnzippedVD As Byte() = _Unzip(vbVD, nStartVD, nLenVD)

                If vbUnzippedVD Is Nothing Then
                    Trace.WriteLine("Failed to unzip EF_VD!")
                    Return CardActivationResult.UnsupportedCard
                End If

                Dim sVD As String = g_aEncoding.GetString(vbUnzippedVD)
                Trace.WriteLine(sVD)

                ' Finally decode the XML data.

                _DecodeVD(sVD)

                '

                Return CardActivationResult.Success
            Finally
                aSlot.EndTransaction()
                aCard.Dispose()
            End Try
        End Function

        ''' <summary>
        ''' Reads the complete content of a transparent file.
        ''' </summary>
        ''' <param name="aCard">
        ''' Handle of card to read the file from. Must not be <c>null</c>.
        ''' </param>
        ''' <param name="nFID">
        ''' The file ID of the EF that should be read.
        ''' </param>
        ''' <returns>
        ''' If the requested EF could not be selected successfully, then <c>null</c> is
        ''' returned. Otherwise the complete content of the file is returned in a single byte
        ''' array.
        ''' </returns>

        Private Shared Function _ReadBinaryFile(ByVal aCard As CardHandle, ByVal nFID As Integer) As Byte()
            If aCard Is Nothing Then
                Throw New ArgumentNullException()
            End If

            Dim aRespAPDU As CardResponseAPDU

            ' Try to select the requested file.

            aRespAPDU = aCard.SelectFile(nFID)
            If Not aRespAPDU.IsSuccessful Then
                Trace.WriteLine([String].Format("Failed to select file '{0:X4}'. SW = '{1:X4}'.", nFID, aRespAPDU.SW))
                Return Nothing
            End If

            ' Read the entire file in max size chunks with progressing offset until an error
            ' is encountered. The data chunks are collected in an array list. Make sure the reader & driver 
            ' allow max chunk size. Adapt according to terminal name and third party reader specs otherwise.

            Dim vList As New ArrayList()

            Dim nOffset As Integer = 0
            Dim nMaxChunk As Integer = &HFE
            Dim nMaxLength As Integer = &H7FFF
            If aCard.Slot.CardTerminalName.Contains("SCM Microsystems") Then
                '30JAN2012 SCR335 fails with &HFE
                nMaxChunk = &H64
            End If
            While nOffset < nMaxLength
                Dim nChunk As Integer = Math.Min(nMaxChunk, nMaxLength - nOffset)

                aRespAPDU = aCard.ReadBinary(nOffset, CByte(nChunk))
                If aRespAPDU.IsError Then
                    Exit While
                End If

                Dim vbChunk As Byte() = aRespAPDU.GetData()
                If (vbChunk Is Nothing) OrElse (vbChunk.Length = 0) Then
                    Exit While
                End If

                vList.Add(vbChunk)
                nOffset += vbChunk.Length

                If vbChunk.Length < nChunk Then
                    Exit While
                End If
            End While

            ' The final offset equals the total size of the collected data. This is used to
            ' create a new byte array where all the collected data chunks are merged.

            Dim vbFile As Byte() = New Byte(nOffset - 1) {}

            nOffset = 0
            For i As Integer = 0 To vList.Count - 1
                Dim vb As Byte() = DirectCast(vList(i), Byte())
                Buffer.BlockCopy(vb, 0, vbFile, nOffset, vb.Length)
                nOffset += vb.Length
            Next

            Return vbFile
        End Function

        ''' <summary>
        ''' ZIP-decompression.
        ''' </summary>
        ''' <param name="vb"></param>
        ''' <returns></returns>

        Private Shared Function _Unzip(ByVal vb As Byte(), ByVal nOffset As Integer, ByVal nLength As Integer) As Byte()
            ' Decompress data.

            Dim aCompressedStream As New MemoryStream(vb, nOffset, nLength, False)
            Dim aGZipStream As New GZipStream(aCompressedStream, CompressionMode.Decompress)

            Dim vbBuffer As Byte() = New Byte(16383) {}

            Dim nActual As Integer = aGZipStream.Read(vbBuffer, 0, vbBuffer.Length)
            If nActual = 0 Then
                Return Nothing
            End If
            If nActual = vbBuffer.Length Then
                ' TODO: Buffer overflow!
                Return Nothing
            End If

            aGZipStream.Close()

            '

            Dim vbUnzipped As Byte() = New Byte(nActual - 1) {}
            Buffer.BlockCopy(vbBuffer, 0, vbUnzipped, 0, nActual)

            Return vbUnzipped
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sPD"></param>

        Private Sub _DecodePD(ByVal sPD As String)
            ' <?xml version="1.0" encoding="iso-8859-15"?>
            ' <UC_PersoenlicheVersichertendatenXML xmlns="http://ws.gematik.de/fa/vsds/UC_PersoenlicheVersichertendatenXML/v5.1" CDM_VERSION="5.1.0">
            ' 	<Versicherter>
            ' 		<Versicherten_ID>D123456786</Versicherten_ID>
            ' 		<Person>
            ' 			<Geburtsdatum>19660324</Geburtsdatum>
            ' 			<Vorname>Hans</Vorname>
            ' 			<Nachname>Mustermann</Nachname>
            ' 			<Geschlecht>M</Geschlecht>
            ' 			<Titel>Professor</Titel>
            ' 			<StrassenAdresse>
            ' 				<Postleitzahl>10117</Postleitzahl>
            ' 				<Ort>Berlin</Ort>
            ' 				<Land>
            ' 					<Wohnsitzlaendercode>D</Wohnsitzlaendercode>
            ' 				</Land>
            ' 				<Strasse>Friedrichstraße</Strasse>
            ' 				<Hausnummer>136</Hausnummer>
            ' 				<Anschriftenzusatz>erste Etage</Anschriftenzusatz>
            ' 			</StrassenAdresse>
            ' 		</Person>
            ' 	</Versicherter>
            ' </UC_PersoenlicheVersichertendatenXML>

            Dim sNamespace As String = "http://ws.gematik.de/fa/vsds/UC_PersoenlicheVersichertendatenXML/v5.1"
            If sPD.Contains("http://ws.gematik.de/fa/vsdm/vsd/v5.2") Then
                sNamespace = "http://ws.gematik.de/fa/vsdm/vsd/v5.2"
            End If

            Dim xmlDocument As New XmlDocument()
            xmlDocument.LoadXml(sPD)

            Dim xmlPersoenlicheVersichertendatenXML As XmlElement = xmlDocument.DocumentElement
            Dim xmlVersicherter As XmlElement = xmlPersoenlicheVersichertendatenXML("Versicherter", sNamespace)
            If xmlVersicherter IsNot Nothing Then
                Dim xmlVersichertenID As XmlElement = xmlVersicherter("Versicherten_ID", sNamespace)
                If xmlVersichertenID IsNot Nothing Then
                    m_sInsuredNumber = xmlVersichertenID.InnerText
                End If

                Dim xmlPerson As XmlElement = xmlVersicherter("Person", sNamespace)
                If xmlPerson IsNot Nothing Then
                    Dim xmlTitel As XmlElement = xmlPerson("Titel", sNamespace)
                    If xmlTitel IsNot Nothing Then
                        m_sTitleName = xmlTitel.InnerText
                    End If

                    Dim xmlVorname As XmlElement = xmlPerson("Vorname", sNamespace)
                    If xmlVorname IsNot Nothing Then
                        m_sFirstName = xmlVorname.InnerText
                    End If

                    Dim xmlNachname As XmlElement = xmlPerson("Nachname", sNamespace)
                    If xmlNachname IsNot Nothing Then
                        m_sLastName = xmlNachname.InnerText
                    End If

                    Dim xmlGeburtsdatum As XmlElement = xmlPerson("Geburtsdatum", sNamespace)
                    If xmlGeburtsdatum IsNot Nothing Then
                        m_tBirthday = DateTime.ParseExact(xmlGeburtsdatum.InnerText, "yyyyMMdd", Nothing)
                    End If

                    Dim xmlAdresse As XmlElement = xmlPerson("StrassenAdresse", sNamespace)
                    If xmlAdresse Is Nothing Then
                        xmlAdresse = xmlPerson("PostfachAdresse", sNamespace)
                    End If

                    If xmlAdresse IsNot Nothing Then
                        Dim sb As New StringBuilder()

                        Dim xmlStrasse As XmlElement = xmlAdresse("Strasse", sNamespace)
                        If xmlStrasse IsNot Nothing Then
                            sb.Append(xmlStrasse.InnerText)

                            Dim xmlHausnummer As XmlElement = xmlAdresse("Hausnummer", sNamespace)
                            If xmlHausnummer IsNot Nothing Then
                                sb.Append(" "c)
                                sb.Append(xmlHausnummer.InnerText)
                            End If

                            Dim xmlAnschriftenzusatz As XmlElement = xmlAdresse("Anschriftenzusatz", sNamespace)
                            If xmlAnschriftenzusatz IsNot Nothing Then
                                sb.Append(Environment.NewLine)
                                sb.Append(xmlAnschriftenzusatz.InnerText)
                            End If
                        Else
                            Dim xmlPostfach As XmlElement = xmlAdresse("Postfach", sNamespace)
                            If xmlPostfach IsNot Nothing Then
                                sb.Append("Postfach ")
                                sb.Append(xmlPostfach.InnerText)
                            End If
                        End If

                        m_sStreet = sb.ToString()

                        '

                        Dim xmlPostleitzahl As XmlElement = xmlAdresse("Postleitzahl", sNamespace)
                        If xmlPostleitzahl IsNot Nothing Then
                            m_sZipCode = xmlPostleitzahl.InnerText
                        End If

                        Dim xmlOrt As XmlElement = xmlAdresse("Ort", sNamespace)
                        If xmlOrt IsNot Nothing Then
                            m_sCity = xmlOrt.InnerText
                        End If

                    End If
                End If
            End If

            ' TODO: Much more interesting data could be extracted ...
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sVD"></param>

        Private Sub _DecodeVD(ByVal sVD As String)
            Dim sNamespace As String = "http://ws.gematik.de/fa/vsds/UC_AllgemeineVersicherungsdatenXML/v5.1"

            Dim xmlDocument As New XmlDocument()
            xmlDocument.LoadXml(sVD)

            Dim xmlAllgemeineVersicherungsdatenXML As XmlElement = xmlDocument.DocumentElement
            Dim xmlVersicherter As XmlElement = xmlAllgemeineVersicherungsdatenXML("Versicherter", sNamespace)
            If xmlVersicherter IsNot Nothing Then
                Dim xmlVersicherungsschutz As XmlElement = xmlVersicherter("Versicherungsschutz", sNamespace)
                If xmlVersicherungsschutz IsNot Nothing Then
                    Dim xmlKostentraeger As XmlElement = xmlVersicherungsschutz("Kostentraeger", sNamespace)
                    If xmlKostentraeger IsNot Nothing Then
                        Dim xmlKostentraegerkennung As XmlElement = xmlKostentraeger("Kostentraegerkennung", sNamespace)
                        If xmlKostentraegerkennung IsNot Nothing Then
                            m_sInsuranceNumber = xmlKostentraegerkennung.InnerText
                        End If

                        Dim xmlName As XmlElement = xmlKostentraeger("Name", sNamespace)
                        If xmlName IsNot Nothing Then
                            m_sInsuranceName = xmlName.InnerText
                        End If
                    End If
                End If
            End If

            ' TODO: Much more interesting data could be extracted ...
        End Sub
    End Class
End Namespace
