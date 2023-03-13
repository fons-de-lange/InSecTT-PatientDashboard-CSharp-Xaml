' --------------------------------------------------------------------------------------------
' MctHealthCard.cs
' CardWerk SmartCard API
' Copyright © 2004-2014 CardWerk Technologies
' --------------------------------------------------------------------------------------------
'HIST
'27NOV2013 MJ protected transaction

Imports System
Imports System.Diagnostics
Imports System.Text

Imports Subsembly.SmartCard

Namespace SampleCode_eGK
    ''' <summary>
    ''' This is a simple example for a card service implementation for the old, memory card
    ''' based german health card - Krankenversicherungskarte (KVK).
    ''' </summary>

    Public Class MctHealthCard
        Inherits SampleCode_eGK.HealthCard
        ''' <summary>
        ''' We overrride the abstract CardName as part of our contract with the base class. 
        ''' </summary>

        Public Overrides ReadOnly Property CardName() As String
            Get
                Return "MCT Versichertenkarte (KVK)"
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="aSlot"></param>
        ''' <returns></returns>

        Public Function Read(ByVal aSlot As CardTerminalSlot) As CardActivationResult
            MyBase.Clear()

            ' Try to acquire a CardHandle to the inserted card. We try three different
            ' memory card types that are used for german health insurance cards.

            Dim aCard As CardHandle = Nothing
            Dim nActivationResult As CardActivationResult

            aCard = aSlot.AcquireCard(CardTypes.SLE4432, nActivationResult)
            If aCard Is Nothing Then
                aCard = aSlot.AcquireCard(CardTypes.SLE4418, nActivationResult)
            End If
            If aCard Is Nothing Then
                aCard = aSlot.AcquireCard(CardTypes.AT24C02SC, nActivationResult)
            End If
            If aCard Is Nothing Then
                Return nActivationResult
            End If

            '

            Try
                ' Now read all data from the card.
                aSlot.BeginTransaction()

                Dim aResponseAPDU As CardResponseAPDU
                Dim vbData As Byte() = Nothing
                Dim nAppOffset As Integer
                Dim nAppLength As Integer

                ' First try a SELECT APPLICATION. This is the official way to get at the
                ' data of the card. However, this is not supported by all readers.

                Dim vbAppID1 As Byte() = CardHex.ToByteArray("D27600000101")
                Dim vbAppID2 As Byte() = CardHex.ToByteArray("D28000000101")

                Dim aSelectAPDU As New CardCommandAPDU(&H0, &HA4, &H4, &H0, vbAppID1)

                aResponseAPDU = aCard.SendCommand(aSelectAPDU)
                If Not aResponseAPDU.IsSuccessful Then
                    aSelectAPDU.SetData(vbAppID2)
                    aResponseAPDU = aCard.SendCommand(aSelectAPDU)
                End If

                ' If successful, then read the application data without ATR header.

                If aResponseAPDU.IsSuccessful Then
                    aResponseAPDU = aCard.ReadBinary(0, 0)
                    If aResponseAPDU.IsError Then
                        Trace.WriteLine("Failed to read application data!")
                        Return CardActivationResult.UnsupportedCard
                    End If

                    vbData = aResponseAPDU.GetData()
                    nAppOffset = 0
                Else
                    ' If the SELECT APPLICATION failed, then try SELECT FILE.
                    ' This SELECT FILE ensures that the entire memory card is available
                    ' through READ BINARY. It might be, that this command is not supported
                    ' by the driver. In this case we just ignore that failure.

                    aResponseAPDU = aCard.SelectFile(&H3F00)
                    ' Ignore!
                    If Not aResponseAPDU.IsSuccessful Then
                    End If

                    ' Read the entire 256 bytes from the memory card using a simple READ
                    ' BINARY command APDU.

                    aResponseAPDU = aCard.ReadBinary(0, 0)
                    If aResponseAPDU.IsError Then
                        Trace.WriteLine("Failed to read card data!")
                        Return CardActivationResult.UnsupportedCard
                    End If

                    vbData = aResponseAPDU.GetData()

                    ' The returned data should now contain the complete health card data.

                    If _IsAtrHeader(vbData) Then
                        Dim nDirOffset As Integer = vbData(3) And &H7F
                        Dim nDirLength As Integer = 2 + vbData(nDirOffset + 1)

                        nAppOffset = nDirOffset + nDirLength
                    Else
                        nAppOffset = 0
                    End If
                End If

                ' Check whether we have a valid template at the suggested nAppOffset.

                If (nAppOffset + 51) > vbData.Length Then
                    Trace.WriteLine("Not enough template data!")
                    Return CardActivationResult.UnsupportedCard
                End If

                If vbData(nAppOffset) <> &H60 Then
                    Trace.WriteLine("Template not found!")
                    Return CardActivationResult.UnsupportedCard
                End If

                Dim nLenByte As Byte = vbData(nAppOffset + 1)
                If nLenByte = &H81 Then
                    nAppLength = 3 + vbData(nAppOffset + 2)
                ElseIf nLenByte < &H80 Then
                    nAppLength = 2 + nLenByte
                Else
                    Trace.WriteLine("Bad template length format!")
                    Return CardActivationResult.UnsupportedCard
                End If

                If (nAppOffset + nAppLength) > vbData.Length Then
                    Trace.WriteLine("Bad template length!")
                    Return CardActivationResult.UnsupportedCard
                End If

                ' Now try to parse the complete BER encoded application data.

                Dim aRootDO As CardDataObject = CardDataObject.Parse(vbData, nAppOffset, nAppLength, CardDataObjectEncoding.BER)

                Dim aInsuranceNameDO As CardDataObject = aRootDO.Find(&H80, False)
                If aInsuranceNameDO IsNot Nothing Then
                    m_sInsuranceName = _GetString(aInsuranceNameDO)
                End If

                Dim aInsuranceNumberDO As CardDataObject = aRootDO.Find(&H81, False)
                If aInsuranceNumberDO IsNot Nothing Then
                    m_sInsuranceNumber = _GetString(aInsuranceNumberDO)
                End If

                Dim aInsuredNumberDO As CardDataObject = aRootDO.Find(&H82, False)
                If aInsuredNumberDO IsNot Nothing Then
                    m_sInsuredNumber = _GetString(aInsuredNumberDO)
                End If

                Dim aTitleNameDO As CardDataObject = aRootDO.Find(&H84, False)
                If aTitleNameDO IsNot Nothing Then
                    m_sTitleName = _GetString(aTitleNameDO)
                End If

                Dim aFirstNameDO As CardDataObject = aRootDO.Find(&H85, False)
                If aFirstNameDO IsNot Nothing Then
                    m_sFirstName = _GetString(aFirstNameDO)
                End If

                Dim aLastNameDO As CardDataObject = aRootDO.Find(&H87, False)
                If aLastNameDO IsNot Nothing Then
                    m_sLastName = _GetString(aLastNameDO)
                End If

                Dim aStreetDO As CardDataObject = aRootDO.Find(&H89, False)
                If aStreetDO IsNot Nothing Then
                    m_sStreet = _GetString(aStreetDO)
                End If

                Dim aZipCodeDO As CardDataObject = aRootDO.Find(&H8B, False)
                If aZipCodeDO IsNot Nothing Then
                    m_sZipCode = _GetString(aZipCodeDO)
                End If

                Dim aCityCodeDO As CardDataObject = aRootDO.Find(&H8C, False)
                If aCityCodeDO IsNot Nothing Then
                    m_sCity = _GetString(aCityCodeDO)
                End If

                Dim aBirthdayDO As CardDataObject = aRootDO.Find(&H88, False)
                If aBirthdayDO IsNot Nothing Then
                    Dim sBirthday As String = _GetString(aBirthdayDO)
                    m_tBirthday = DateTime.ParseExact(sBirthday, "ddMMyyyy", Nothing)
                End If

                Return CardActivationResult.Success
            Finally
                aSlot.EndTransaction()
                aCard.Dispose()
            End Try
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="vbData"></param>
        ''' <returns></returns>

        Private Shared Function _IsAtrHeader(ByVal vbData As Byte()) As Boolean
            Debug.Assert(vbData IsNot Nothing)
            Debug.Assert(vbData.Length >= 4)

            Return ((vbData(0) = &H82) OrElse (vbData(0) = &H92) OrElse (vbData(0) = &HA2)) AndAlso (vbData(1) = &H13) AndAlso (vbData(2) = &H10) AndAlso (vbData(3) = &H91)
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="aDO"></param>
        ''' <returns></returns>

        Private Shared Function _GetString(ByVal aDO As CardDataObject) As String
            Dim sb As New StringBuilder(aDO.Length)
            Dim vbValue As Byte() = aDO.Value
            For i As Integer = 0 To vbValue.Length - 1
                Dim b As Byte = vbValue(i)
                If (b < &H20) OrElse (b > &H7F) Then
                    sb.Append("?"c)
                Else
                    sb.Append(g_vchDin66003Table(b - &H20))
                End If
            Next
            Return sb.ToString()
        End Function

        Shared g_vchDin66003Table As Char() = New Char() {" "c, "?"c, "?"c, "?"c, "?"c, "?"c, _
         "&"c, "'"c, "("c, ")"c, "?"c, "+"c, _
         "?"c, "-"c, "."c, "/"c, "0"c, "1"c, _
         "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, _
         "8"c, "9"c, "?"c, "?"c, "?"c, "?"c, _
         "?"c, "?"c, "?"c, "A"c, "B"c, "C"c, _
         "D"c, "E"c, "F"c, "G"c, "H"c, "I"c, _
         "J"c, "K"c, "L"c, "M"c, "N"c, "O"c, _
         "P"c, "Q"c, "R"c, "S"c, "T"c, "U"c, _
         "V"c, "W"c, "X"c, "Y"c, "Z"c, "Ä"c, _
         "Ö"c, "Ü"c, "?"c, "?"c, "?"c, "a"c, _
         "b"c, "c"c, "d"c, "e"c, "f"c, "g"c, _
         "h"c, "i"c, "j"c, "k"c, "l"c, "m"c, _
         "n"c, "o"c, "p"c, "q"c, "r"c, "s"c, _
         "t"c, "u"c, "v"c, "w"c, "x"c, "y"c, _
         "z"c, "ä"c, "ö"c, "ü"c, "ß"c, "?"c}
    End Class
End Namespace
