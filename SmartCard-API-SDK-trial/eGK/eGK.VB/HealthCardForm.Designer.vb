<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class HealthCardForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(HealthCardForm))
        Me.m_aPromptLabel = New System.Windows.Forms.Label()
        Me.m_aLinkLabel = New System.Windows.Forms.LinkLabel()
        Me.m_aCopyrightLabel = New System.Windows.Forms.Label()
        Me.insuranceTextBox = New System.Windows.Forms.TextBox()
        Me.insuranceNumberTextBox = New System.Windows.Forms.TextBox()
        Me.nameTextBox = New System.Windows.Forms.TextBox()
        Me.numberTextBox = New System.Windows.Forms.TextBox()
        Me.addressTextBox = New System.Windows.Forms.TextBox()
        Me.birthdayTextBox = New System.Windows.Forms.TextBox()
        Me.insuranceLabel = New System.Windows.Forms.Label()
        Me.insuranceNumberLabel = New System.Windows.Forms.Label()
        Me.nameLabel = New System.Windows.Forms.Label()
        Me.numberLabel = New System.Windows.Forms.Label()
        Me.addressLabel = New System.Windows.Forms.Label()
        Me.birthdayLabel = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'm_aPromptLabel
        '
        Me.m_aPromptLabel.Font = New System.Drawing.Font("Trebuchet MS", 12.0!, CType((System.Drawing.FontStyle.Bold Or System.Drawing.FontStyle.Italic), System.Drawing.FontStyle), System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.m_aPromptLabel.Location = New System.Drawing.Point(7, 9)
        Me.m_aPromptLabel.Name = "m_aPromptLabel"
        Me.m_aPromptLabel.Size = New System.Drawing.Size(416, 36)
        Me.m_aPromptLabel.TabIndex = 3
        Me.m_aPromptLabel.Text = "Please insert card..."
        '
        'm_aLinkLabel
        '
        Me.m_aLinkLabel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.m_aLinkLabel.AutoSize = True
        Me.m_aLinkLabel.Location = New System.Drawing.Point(342, 264)
        Me.m_aLinkLabel.Name = "m_aLinkLabel"
        Me.m_aLinkLabel.Size = New System.Drawing.Size(156, 13)
        Me.m_aLinkLabel.TabIndex = 6
        Me.m_aLinkLabel.TabStop = True
        Me.m_aLinkLabel.Text = "http://www.smartcard-api.com/"
        '
        'm_aCopyrightLabel
        '
        Me.m_aCopyrightLabel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.m_aCopyrightLabel.AutoSize = True
        Me.m_aCopyrightLabel.Location = New System.Drawing.Point(12, 264)
        Me.m_aCopyrightLabel.Name = "m_aCopyrightLabel"
        Me.m_aCopyrightLabel.Size = New System.Drawing.Size(223, 13)
        Me.m_aCopyrightLabel.TabIndex = 5
        Me.m_aCopyrightLabel.Text = "Copyright 2004-2017 CardWerk Technologies"
        '
        'insuranceTextBox
        '
        Me.insuranceTextBox.Location = New System.Drawing.Point(170, 42)
        Me.insuranceTextBox.Name = "insuranceTextBox"
        Me.insuranceTextBox.Size = New System.Drawing.Size(328, 20)
        Me.insuranceTextBox.TabIndex = 10
        '
        'insuranceNumberTextBox
        '
        Me.insuranceNumberTextBox.Location = New System.Drawing.Point(170, 75)
        Me.insuranceNumberTextBox.Name = "insuranceNumberTextBox"
        Me.insuranceNumberTextBox.Size = New System.Drawing.Size(329, 20)
        Me.insuranceNumberTextBox.TabIndex = 11
        '
        'nameTextBox
        '
        Me.nameTextBox.Location = New System.Drawing.Point(171, 113)
        Me.nameTextBox.Name = "nameTextBox"
        Me.nameTextBox.Size = New System.Drawing.Size(328, 20)
        Me.nameTextBox.TabIndex = 12
        '
        'numberTextBox
        '
        Me.numberTextBox.Location = New System.Drawing.Point(171, 149)
        Me.numberTextBox.Name = "numberTextBox"
        Me.numberTextBox.Size = New System.Drawing.Size(328, 20)
        Me.numberTextBox.TabIndex = 13
        '
        'addressTextBox
        '
        Me.addressTextBox.Location = New System.Drawing.Point(171, 175)
        Me.addressTextBox.Multiline = True
        Me.addressTextBox.Name = "addressTextBox"
        Me.addressTextBox.Size = New System.Drawing.Size(327, 36)
        Me.addressTextBox.TabIndex = 14
        '
        'birthdayTextBox
        '
        Me.birthdayTextBox.Location = New System.Drawing.Point(171, 224)
        Me.birthdayTextBox.Name = "birthdayTextBox"
        Me.birthdayTextBox.Size = New System.Drawing.Size(328, 20)
        Me.birthdayTextBox.TabIndex = 15
        '
        'insuranceLabel
        '
        Me.insuranceLabel.AutoSize = True
        Me.insuranceLabel.Location = New System.Drawing.Point(12, 49)
        Me.insuranceLabel.Name = "insuranceLabel"
        Me.insuranceLabel.Size = New System.Drawing.Size(99, 13)
        Me.insuranceLabel.TabIndex = 16
        Me.insuranceLabel.Text = "Name of insurance:"
        '
        'insuranceNumberLabel
        '
        Me.insuranceNumberLabel.AutoSize = True
        Me.insuranceNumberLabel.Location = New System.Drawing.Point(12, 78)
        Me.insuranceNumberLabel.Name = "insuranceNumberLabel"
        Me.insuranceNumberLabel.Size = New System.Drawing.Size(108, 13)
        Me.insuranceNumberLabel.TabIndex = 17
        Me.insuranceNumberLabel.Text = "Number of insurance:"
        '
        'nameLabel
        '
        Me.nameLabel.AutoSize = True
        Me.nameLabel.Location = New System.Drawing.Point(12, 113)
        Me.nameLabel.Name = "nameLabel"
        Me.nameLabel.Size = New System.Drawing.Size(90, 13)
        Me.nameLabel.TabIndex = 18
        Me.nameLabel.Text = "Name of  insured:"
        '
        'numberLabel
        '
        Me.numberLabel.AutoSize = True
        Me.numberLabel.Location = New System.Drawing.Point(12, 152)
        Me.numberLabel.Name = "numberLabel"
        Me.numberLabel.Size = New System.Drawing.Size(96, 13)
        Me.numberLabel.TabIndex = 19
        Me.numberLabel.Text = "Number of insured:"
        '
        'addressLabel
        '
        Me.addressLabel.AutoSize = True
        Me.addressLabel.Location = New System.Drawing.Point(12, 175)
        Me.addressLabel.Name = "addressLabel"
        Me.addressLabel.Size = New System.Drawing.Size(97, 13)
        Me.addressLabel.TabIndex = 20
        Me.addressLabel.Text = "Address of insured:"
        '
        'birthdayLabel
        '
        Me.birthdayLabel.AutoSize = True
        Me.birthdayLabel.Location = New System.Drawing.Point(12, 227)
        Me.birthdayLabel.Name = "birthdayLabel"
        Me.birthdayLabel.Size = New System.Drawing.Size(100, 13)
        Me.birthdayLabel.TabIndex = 21
        Me.birthdayLabel.Text = "Birthday of  insured:"
        '
        'HelloMctForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(520, 282)
        Me.Controls.Add(Me.birthdayLabel)
        Me.Controls.Add(Me.addressLabel)
        Me.Controls.Add(Me.numberLabel)
        Me.Controls.Add(Me.nameLabel)
        Me.Controls.Add(Me.insuranceNumberLabel)
        Me.Controls.Add(Me.insuranceLabel)
        Me.Controls.Add(Me.birthdayTextBox)
        Me.Controls.Add(Me.addressTextBox)
        Me.Controls.Add(Me.numberTextBox)
        Me.Controls.Add(Me.nameTextBox)
        Me.Controls.Add(Me.insuranceNumberTextBox)
        Me.Controls.Add(Me.insuranceTextBox)
        Me.Controls.Add(Me.m_aLinkLabel)
        Me.Controls.Add(Me.m_aCopyrightLabel)
        Me.Controls.Add(Me.m_aPromptLabel)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "HelloMctForm"
        Me.Text = "HelloMCT 1.0.1 - KVK, eGK German HealthCard sample"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Private WithEvents m_aPromptLabel As System.Windows.Forms.Label
    Private WithEvents m_aLinkLabel As System.Windows.Forms.LinkLabel
    Private WithEvents m_aCopyrightLabel As System.Windows.Forms.Label
    Friend WithEvents insuranceTextBox As System.Windows.Forms.TextBox
    Friend WithEvents insuranceNumberTextBox As System.Windows.Forms.TextBox
    Friend WithEvents nameTextBox As System.Windows.Forms.TextBox
    Friend WithEvents numberTextBox As System.Windows.Forms.TextBox
    Friend WithEvents addressTextBox As System.Windows.Forms.TextBox
    Friend WithEvents birthdayTextBox As System.Windows.Forms.TextBox
    Friend WithEvents insuranceLabel As System.Windows.Forms.Label
    Friend WithEvents insuranceNumberLabel As System.Windows.Forms.Label
    Friend WithEvents nameLabel As System.Windows.Forms.Label
    Friend WithEvents numberLabel As System.Windows.Forms.Label
    Friend WithEvents addressLabel As System.Windows.Forms.Label
    Friend WithEvents birthdayLabel As System.Windows.Forms.Label

End Class
