' --------------------------------------------------------------------------------------------
' HealthCard.vb
' CardWerk SmartCard API
' Copyright © 2004-2019 CardWerk Technologies
' --------------------------------------------------------------------------------------------

Imports System
Namespace SampleCode_eGK

    ''' <summary>
    ''' Abstract base class for all German health insurance cards.
    ''' </summary>

    Public MustInherit Class HealthCard
        Protected m_sInsuranceName As String
        Protected m_sInsuranceNumber As String

        Protected m_sInsuredNumber As String
        Protected m_sTitleName As String
        Protected m_sFirstName As String
        Protected m_sLastName As String
        Protected m_sStreet As String
        Protected m_sZipCode As String
        Protected m_sCity As String
        Protected m_tBirthday As DateTime

        ''' <summary>
        ''' Resets all class members to their default values. Use this method whenever you are starting
        ''' a new session with a card.
        ''' </summary>

        Protected Sub Clear()
            m_sInsuranceName = Nothing
            m_sInsuranceNumber = Nothing
            m_sTitleName = Nothing
            m_sFirstName = Nothing
            m_sLastName = Nothing
            m_sStreet = Nothing
            m_sZipCode = Nothing
            m_sCity = Nothing
            m_tBirthday = DateTime.MinValue
        End Sub

        ''' <summary>
        ''' This methos must be overridden and should reflect  a friendly name 
        ''' such as KVK, Krankenversicherungskarte, eGK or electronische Gesundheitskarte.
        ''' </summary>

        Public MustOverride ReadOnly Property CardName() As String

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property InsuranceName() As String
            Get
                Return m_sInsuranceName
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property InsuranceNumber() As String
            Get
                Return m_sInsuranceNumber
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property InsuredNumber() As String
            Get
                Return m_sInsuredNumber
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property TitleName() As String
            Get
                Return m_sTitleName
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property FirstName() As String
            Get
                Return m_sFirstName
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property LastName() As String
            Get
                Return m_sLastName
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property Street() As String
            Get
                Return m_sStreet
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property ZipCode() As String
            Get
                Return m_sZipCode
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property City() As String
            Get
                Return m_sCity
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>

        Public ReadOnly Property Birthday() As DateTime
            Get
                Return m_tBirthday
            End Get
        End Property
    End Class
End Namespace

