' CardWerk SmartCard API
' Copyright © 2004-2013 CardWerk Technologies
' HIST
'27NOV2013 MJ add support for protected transaction


Option Strict On
Option Explicit On

Imports System.IO
Imports Subsembly.SmartCard
Imports System.Text


''' <summary>
''' The goal of the example to show how to connect to smart card using SmartCard API. 
''' </summary>
''' <remarks></remarks>
Public Class HealthCardForm


#Region "Class Members"
    Private WithEvents CardManager As CardTerminalManager
    Private Shared ReadOnly FILE_NAME As String = "HelloCardTrace.txt"
    Private aTraceFile As Stream
#End Region

#Region "Initialization"

    Public Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        RegisterTraceListener()

        ' initialize card terminal manager
        InitializeCardManager()

    End Sub


    ''' <summary>
    ''' Register trace listener with path User_Folder/Documents/HelloCardTrace.txt
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub RegisterTraceListener()

        ' Create a new text writer using the output stream, and add it to the trace
        ' listeners.
        Dim sPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
        aTraceFile = File.Create(Path.Combine(sPath, FILE_NAME))
        Dim aListener As TextWriterTraceListener = New TextWriterTraceListener(aTraceFile)
        Trace.Listeners.Add(aListener)


    End Sub

    Private Sub InitializeCardManager()

        Trace.WriteLine("Initialiazing card manager")

        ' QUESTION Why cardterminal manager is a singleton
        ' ANSWER -There is always only a single instance of the CardTerminalManager class, 
        ' hence it is called a singleton. 
        ' Applications must use the public Singleton property in order to get access 
        ' to the card terminal manager.

        CardManager = CardTerminalManager.Singleton

    End Sub

#End Region


#Region "Card Manager events handlers"


    ''' <summary>
    ''' Event handler when card is inserted
    ''' </summary>
    ''' <param name="aSender">CardTerminalManager</param>
    ''' <param name="aEventArgs">This class provides the event information for the card terminal events 
    ''' that are raised by the CardTerminalManager whenever a smart card is inserted or is removed. 
    ''' </param>
    Protected Sub InsertedEvent(ByVal aSender As Object, ByVal aEventArgs As CardTerminalEventArgs) _
        Handles CardManager.CardInsertedEvent

        If (InvokeRequired = True) Then

            Dim vParms(1) As Object
            vParms(0) = aSender
            vParms(1) = aEventArgs
            BeginInvoke(New CardTerminalEventHandler(AddressOf InsertedEvent), vParms)

        Else

            ' We catch any exceptions during card I/O. This is particularly important
            ' for fuzzy communication conditions. Example: an contactless card that 
            ' is not in the field throuout the whole I/O might cause an error on the 
            ' within the unmanaged Windows code. SmartCardAPI catches this in a general 
            ' exception
            Try
                Trace.WriteLine("Card inserted in reader.")
                ReadHealthCard(aEventArgs.Slot)

            Catch ex As Exception
                Trace.WriteLine(String.Format("Error reading the card: {0}", ex.Message))
                PromptHealthCard()
            End Try

        End If

    End Sub

    ''' <summary>
    ''' Event handler when card is removed
    ''' </summary>
    ''' <param name="aSender">CardTerminalManager</param>
    ''' <param name="aEventArgs">This class provides the event information for the card terminal events 
    ''' that are raised by the CardTerminalManager whenever a smart card is inserted or is removed. 
    ''' </param>
    Protected Sub RemovedEvent(ByVal aSender As Object, ByVal aEventArgs As CardTerminalEventArgs) _
        Handles CardManager.CardRemovedEvent

        If (InvokeRequired) Then

            Dim vParms(1) As Object
            vParms(0) = aSender
            vParms(1) = aEventArgs

            BeginInvoke(New CardTerminalEventHandler(AddressOf RemovedEvent), vParms)

        Else

            Trace.WriteLine("Card ejected from the card reader")
            PromptHealthCard()
        End If
    End Sub

#End Region




#Region "Windows From Events"

    Private Overloads Sub OnLoad(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        ' start the card terminal manager
        ' if start is failed or no card terminal found  - exit from app
        If (StartupCardTerminalManager() <> True) Then

            Application.Exit()

        End If

    End Sub

    Private Sub OnClose(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles MyBase.FormClosed

        ' shutdown card terminal manager
        ShutdownCardTerminalManager()

        ' close the trace file
        Trace.Flush()
        aTraceFile.Flush()
        aTraceFile.Close()

    End Sub

#End Region





#Region "Private functions"

    ' <summary>
    ' Helper that starts up the card terminal manager and cares about the case when no
    ' card terminals are installed.
    ' </summary>
    ''' <returns>Returns true if card terminal manager started sucessfulyy and card terminal 
    ''' slot is found, otherwise false</returns>
    Private Function StartupCardTerminalManager() As Boolean

        Dim fStartedUp As Boolean = False

        Trace.WriteLine("Starting card manager")

        Try
            ' Startup the SmartCard API. The parameter "true" means that any
            ' PC/SC smart card reader will automatically be added to the smart card
            ' configuration registry. If startup fails, then this will throw an
            ' exception.

            'QUESTION  what true means?
            Dim nCountReaders As Integer = CardManager.Startup(True)


            ' At least one card terminal is configured, enabled and was started up
            ' successfully. This is all we need, thus we can exit and return
            ' successfully.
            If (nCountReaders > 0) Then

                Trace.WriteLine(String.Format("{0} card terminals found.", nCountReaders))

                fStartedUp = True

            Else

                Trace.WriteLine(String.Format("{0} card terminals found.", nCountReaders))

                ' If the returned count of installed and enabled readers is zero, then it
                ' does not make sense to continue. Anyway we are obliged to call the
                ' CardTerminalManager Shutdown method.
                ShutdownCardTerminalManager()

                MessageBox.Show("No card reader/terminal available on this system. Please verify" & vbNewLine & _
                  "your smart card system. Check CardTerminal Configurator if available. ", _
                  "HelloCard", MessageBoxButtons.OK, MessageBoxIcon.Warning)

            End If

        Catch ex As Exception

            Trace.WriteLine(String.Format("StartCardTerminalManager Error: {0}", ex.Message))

            MessageBox.Show("Unable to run CardTerminalConfigurator. Will " & vbNewLine & _
              "exit this application.", "HelloCard", MessageBoxButtons.OK, _
              MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1)

            fStartedUp = False
        End Try

        Return fStartedUp

    End Function


    ''' <summary>
    ''' Shutdown Card Terminal Manager
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub ShutdownCardTerminalManager()

        ' Every successful CardTerminalManager Startup method call,
        ' requires a mandatory CardTerminalManager Shutdown method call!
        If (CardManager.StartedUp = True) Then

            Trace.WriteLine("Shutdown card manager")
            CardManager.Shutdown()

        End If
    End Sub



    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="aSlot"></param>

    Public Sub ReadHealthCard(ByVal aSlot As CardTerminalSlot)
        m_aPromptLabel.Text = "Reading card ..."
        m_aPromptLabel.Update()

        Dim nActivationResult As CardActivationResult

        Try
            Dim aHealthCard As SampleCode_eGK.HealthCard
            aHealthCard = _ReadEgkHealthCard(aSlot, nActivationResult)
            ' If not successful, try the new eGK processor card.

            If aHealthCard Is Nothing Then
                Debug.WriteLine("eGK power-up failed. Trying KVK storage card.")
                aHealthCard = _ReadMctHealthCard(aSlot, nActivationResult)
                If aHealthCard Is Nothing Then
                    Debug.WriteLine("KVK power-up also failed.")
                End If

            End If


            If aHealthCard IsNot Nothing Then
                m_aPromptLabel.Text = aHealthCard.CardName
                m_aPromptLabel.Update()

                insuranceTextBox.Text = aHealthCard.InsuranceName
                insuranceNumberTextBox.Text = aHealthCard.InsuranceNumber

                Dim sbName As New StringBuilder()
                If aHealthCard.TitleName IsNot Nothing Then
                    sbName.Append(aHealthCard.TitleName)
                    sbName.Append(" "c)
                End If
                sbName.Append(aHealthCard.FirstName)
                sbName.Append(" "c)
                sbName.Append(aHealthCard.LastName)
                nameTextBox.Text = sbName.ToString()

                numberTextBox.Text = aHealthCard.InsuredNumber

                Dim sbAddress As New StringBuilder()
                If aHealthCard.Street IsNot Nothing Then
                    sbAddress.AppendLine(aHealthCard.Street)
                End If
                sbAddress.Append(aHealthCard.ZipCode)
                sbAddress.Append(" "c)
                sbAddress.Append(aHealthCard.City)
                addressTextBox.Text = sbAddress.ToString()

                birthdayTextBox.Text = aHealthCard.Birthday.ToLongDateString()
            Else
                Trace.WriteLine("CardActivationResult: " + nActivationResult.ToString())

                Select Case nActivationResult
                    Case CardActivationResult.NoCard
                        Me.PromptHealthCard()
                        Exit Select
                    Case CardActivationResult.UnresponsiveCard
                        m_aPromptLabel.Text = "Unresponsive Card."
                        Exit Select
                    Case CardActivationResult.InUse
                        m_aPromptLabel.Text = "Card reader blocked by another application."
                        Exit Select
                    Case Else
                        m_aPromptLabel.Text = "Inserted card is not supported!"
                        Exit Select
                End Select
            End If
        Catch x As CardTerminalException
            Select Case x.Code
                Case CardTerminalExceptionCode.CardWithdrawn
                    Me.PromptHealthCard()
                    Exit Select
                Case Else
                    m_aPromptLabel.Text = "Card Terminal Exception: " + x.Message
                    Exit Select
            End Select
        End Try
    End Sub



    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="aSlot"></param>
    ''' <param name="nActivationResult"></param>
    ''' <returns></returns>

    Private Function _ReadMctHealthCard(ByVal aSlot As CardTerminalSlot, ByVal nActivationResult As CardActivationResult) As SampleCode_eGK.HealthCard
        Dim aMctHealthCard As New SampleCode_eGK.MctHealthCard()
        nActivationResult = aMctHealthCard.Read(aSlot)

        If nActivationResult = CardActivationResult.Success Then
            Return aMctHealthCard
        Else
            Return Nothing
        End If
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="aSlot"></param>
    ''' <param name="nActivationResult"></param>
    ''' <returns></returns>

    Private Function _ReadEgkHealthCard(ByVal aSlot As CardTerminalSlot, ByVal nActivationResult As CardActivationResult) As SampleCode_eGK.HealthCard
        Dim aEgkHealthCard As New SampleCode_eGK.EgkHealthCard()
        nActivationResult = aEgkHealthCard.Read(aSlot)

        If nActivationResult = CardActivationResult.Success Then
            Return aEgkHealthCard
        Else
            Return Nothing
        End If
    End Function


    ''' <summary>
    ''' 
    ''' </summary>

    Private Sub PromptHealthCard()
        m_aPromptLabel.Text = "Please insert card..."
        insuranceTextBox.Text = Nothing
        insuranceNumberTextBox.Text = Nothing
        nameTextBox.Text = Nothing
        numberTextBox.Text = Nothing
        addressTextBox.Text = Nothing
        birthdayTextBox.Text = Nothing
    End Sub



#End Region



End Class
