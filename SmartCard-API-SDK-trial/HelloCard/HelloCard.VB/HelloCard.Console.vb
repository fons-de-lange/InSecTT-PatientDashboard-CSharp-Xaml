' =============================================================================================
' HelloCard.vb
' This sample code shows how to connect to a smart card system using CardWerk's SmartCardAPI (professional)
' CardWerk SmartCard API
' Copyright © 2017-2019 CardWerk Technologies
' -------------------------------------------------------------------------------------------- 
' date: 31OCT2019
' 
' HIST:
' 30OCT2019 MJ move to new repo/SDK
' 21FEB2018 MJ port current C# HelloCard.Console; add terminal found/lost events
' 28APR2017 MJ start card terminal
' ============================= www.cardwerk.com =============================================

Option Strict On
Option Explicit On

Imports System.IO
Imports System.Threading
Imports System.Diagnostics
Imports Subsembly.SmartCard


Public Module SampleCode_HelloCard

#Region "Class Members"
    Private WithEvents CardManager As CardTerminalManager
    Dim nCountReaders As Integer
    Private ALWAYS_LOOK_FOR_MORE_CARD_READERS As Boolean = True ' increases the number of readers we are looking for
    Private programInfo As String
    Private cardInsertions As Integer = 0
#End Region

    Sub Main()
        programInfo = "HelloCard sample code rev.31OCT2019" & vbCrLf & "API info: " & Subsembly.SmartCard.SMARTCARDAPI.ApiVersionInfo
        System.Console.WriteLine(programInfo)
        CardManager = CardTerminalManager.Singleton

        Try
            nCountReaders = CardTerminalManager.Singleton.Startup(True) ' true: auto register PC/SC readers
            DisplayNumberOfAvailableReaders()

            If CardTerminalManager.Singleton.StartedUp = True Then
                System.Console.WriteLine("OK-CardManager started")
            Else
                Throw New System.Exception("CardManager can't be started.")
            End If


            If nCountReaders > 0 Then

                Dim readerList() As String = CardTerminalManager.Singleton.GetSlotNames()
                Dim index As Integer = 0

                For Each reader As String In readerList
                    System.Console.WriteLine(String.Format("Reader #{0}: {1}", index.ToString(), reader))
                    index += 1
                Next

                If ALWAYS_LOOK_FOR_MORE_CARD_READERS = True Then
                    CardTerminalManager.Singleton.SlotCountMinimum += 1 ' this is to continue looking for more readers
                End If

            Else
                System.Console.WriteLine("ERROR: No card reader/terminal available on this system!")
                System.Console.WriteLine("Please verify your PC/SC smart card system.")
                System.Console.WriteLine("Is a smart card reader attached?")
                System.Console.WriteLine("Is your PC/SC smart card service up and running?")
                System.Console.WriteLine("Is the reader driver installed?")
            End If


            While (System.Console.KeyAvailable <> True)
                System.Console.Write(".")
                Thread.Sleep(500)
            End While


        Catch ex As Exception
            System.Console.WriteLine("ERROR-" & ex.Message)

        Finally
            If (CardTerminalManager.Singleton.StartedUp = True) Then
                System.Console.WriteLine("Shutdown card manager")
                CardTerminalManager.Singleton.Shutdown()
            End If
            System.Console.WriteLine("END of HelloCard for VB.NET")
        End Try
        System.Console.ReadLine()

    End Sub

#Region "Card Manager event handlers"

    Sub InsertedEvent(ByVal aSender As Object, ByVal aEventArgs As CardTerminalEventArgs) Handles CardManager.CardInsertedEvent
        Dim aCardSlot As CardTerminalSlot = aEventArgs.Slot
        Dim nActivationResult As CardActivationResult
        cardInsertions += 1

        System.Console.Beep()
        System.Console.Clear()
        System.Console.WriteLine(programInfo)
        System.Console.WriteLine(String.Format("  Total readers: {0}", CardTerminalManager.Singleton.SlotCount.ToString()))
        System.Console.WriteLine(String.Format("Card insertions: {0}", cardInsertions.ToString()))

        ' Acquire any processor card (T=0 or T=1) that may be present in the given card terminal slot
        Dim readerName As String = aCardSlot.CardTerminalName
        System.Console.WriteLine("card inserted in " & readerName)

        Dim aCard As CardHandle = aCardSlot.AcquireCard((CardTypes.T0 Or CardTypes.T1), nActivationResult) ' power up card

        If (nActivationResult <> CardActivationResult.Success) Then

            Select Case nActivationResult
                Case CardActivationResult.NoCard
                    System.Console.WriteLine("Please insert card")

                Case CardActivationResult.UnresponsiveCard
                    System.Console.WriteLine("Unresponsive card.")

                Case CardActivationResult.InUse
                    System.Console.WriteLine("Card in use.")

                Case Else
                    System.Console.WriteLine("Can't power up card!")
            End Select

            Exit Sub

        End If
        'DisplayReaderProperties(aCardSlot)

        ' =========================== ATR DETECTION ======================================
        ' Every card accessed through PC/SC must return an Answer To Reset (ATR). 
        ' this ATR is available through a cardHandle object
        Dim atr As Byte() = aCard.GetATR()
        If (atr.Length = 0) Then
            Throw New Exception("Invalid ATR")
        End If

        System.Console.WriteLine("ATR: " & CardHex.FromByteArray(atr, 0, atr.Length))
        ' ===================== now we could start exchanging APDUs ======================


    End Sub

    Sub CardRemovedEvent(ByVal aSender As Object, ByVal aEventArgs As CardTerminalEventArgs) Handles CardManager.CardRemovedEvent
        Dim aCardSlot As CardTerminalSlot = aEventArgs.Slot
        System.Console.Beep(1000, 200)
        System.Console.Beep(1000, 200)
        System.Console.Write(vbCrLf & "card removed from " & aCardSlot.CardTerminalName)
        System.Console.Write(vbCrLf & "please insert card")
    End Sub

    Sub TerminalFoundEvent(ByVal aSender As Object, ByVal aEventArgs As CardTerminalEventArgs) Handles CardManager.CardTerminalFoundEvent

        If (CardTerminalManager.Singleton.StartedUp) Then
            DisplayNumberOfAvailableReaders()
            System.Console.WriteLine(vbCrLf & "Found reader: " & aEventArgs.Slot.CardTerminalName)
            CardTerminalManager.Singleton.SlotCountMinimum += 1 'let's continue looking for readers
        End If

    End Sub

    Sub TerminalLostEvent(ByVal aSender As Object, ByVal aEventArgs As CardTerminalEventArgs) Handles CardManager.CardTerminalLostEvent
        If (CardTerminalManager.Singleton.StartedUp) Then

            System.Console.WriteLine(vbCrLf & "lost reader " & aEventArgs.Slot.CardTerminalName)
            If (CardTerminalManager.Singleton.SlotCount = 0) Then

                System.Console.WriteLine("Please connect reader so we can start accessing smart cards")
                'start looking for reader insertion
                'done automatically by the singleton. The singleton raises a "found reader" event if it finds a new reader.

            ElseIf (CardTerminalManager.Singleton.SlotCountMinimum > 1) Then
                CardTerminalManager.Singleton.SlotCountMinimum -= 1
            End If
        End If
    End Sub

#End Region




    ''' <summary>
    ''' Shutdown Card Terminal Manager
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub ShutdownCardTerminalManager()

        ' Every successful CardTerminalManager Startup method call,
        ' requires a mandatory CardTerminalManager Shutdown method call!
        If (CardTerminalManager.Singleton.StartedUp = True) Then

            System.Console.WriteLine("Shutdown card manager")
            CardTerminalManager.Singleton.Shutdown()

        End If
    End Sub



    ' <summary>
    ' Displays number of readers currently available. Unless overwritten by the host application,
    ' the requested number of readers is at least one or the number of readers connected at program 
    ' start. Whichever is greater.
    ' </summary>
    Sub DisplayNumberOfAvailableReaders()

        Dim slotCount As Integer = CardTerminalManager.Singleton.SlotCount
        Dim slotCountMinimum As Integer = CardTerminalManager.Singleton.SlotCountMinimum
        System.Console.WriteLine("Available readers: " & slotCount)
        If (slotCount < slotCountMinimum) Then
            System.Console.WriteLine(String.Format("Looking for {0} more reader(s)", slotCountMinimum - slotCount))
        End If
    End Sub

End Module
