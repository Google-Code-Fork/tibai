<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmMain
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
        Me.lblX = New System.Windows.Forms.Label
        Me.txtX = New System.Windows.Forms.TextBox
        Me.txtY = New System.Windows.Forms.TextBox
        Me.Label1 = New System.Windows.Forms.Label
        Me.txtZ = New System.Windows.Forms.TextBox
        Me.Label2 = New System.Windows.Forms.Label
        Me.btnAddtoList = New System.Windows.Forms.Button
        Me.ListBox1 = New System.Windows.Forms.ListBox
        Me.btnStartQueue = New System.Windows.Forms.Button
        Me.Button1 = New System.Windows.Forms.Button
        Me.Button2 = New System.Windows.Forms.Button
        Me.btnTest1 = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'lblX
        '
        Me.lblX.AutoSize = True
        Me.lblX.Location = New System.Drawing.Point(12, 9)
        Me.lblX.Name = "lblX"
        Me.lblX.Size = New System.Drawing.Size(14, 13)
        Me.lblX.TabIndex = 0
        Me.lblX.Text = "X"
        '
        'txtX
        '
        Me.txtX.CausesValidation = False
        Me.txtX.Location = New System.Drawing.Point(32, 6)
        Me.txtX.Name = "txtX"
        Me.txtX.Size = New System.Drawing.Size(67, 20)
        Me.txtX.TabIndex = 1
        '
        'txtY
        '
        Me.txtY.CausesValidation = False
        Me.txtY.Location = New System.Drawing.Point(32, 32)
        Me.txtY.Name = "txtY"
        Me.txtY.Size = New System.Drawing.Size(67, 20)
        Me.txtY.TabIndex = 3
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 35)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(14, 13)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Y"
        '
        'txtZ
        '
        Me.txtZ.CausesValidation = False
        Me.txtZ.Location = New System.Drawing.Point(32, 58)
        Me.txtZ.Name = "txtZ"
        Me.txtZ.Size = New System.Drawing.Size(67, 20)
        Me.txtZ.TabIndex = 5
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(12, 61)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(14, 13)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "Z"
        '
        'btnAddtoList
        '
        Me.btnAddtoList.Location = New System.Drawing.Point(24, 84)
        Me.btnAddtoList.Name = "btnAddtoList"
        Me.btnAddtoList.Size = New System.Drawing.Size(75, 23)
        Me.btnAddtoList.TabIndex = 6
        Me.btnAddtoList.Text = "Add to List"
        Me.btnAddtoList.UseVisualStyleBackColor = True
        '
        'ListBox1
        '
        Me.ListBox1.FormattingEnabled = True
        Me.ListBox1.Location = New System.Drawing.Point(105, 6)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(109, 134)
        Me.ListBox1.TabIndex = 7
        '
        'btnStartQueue
        '
        Me.btnStartQueue.Location = New System.Drawing.Point(24, 117)
        Me.btnStartQueue.Name = "btnStartQueue"
        Me.btnStartQueue.Size = New System.Drawing.Size(75, 23)
        Me.btnStartQueue.TabIndex = 8
        Me.btnStartQueue.Text = "Start Queue"
        Me.btnStartQueue.UseVisualStyleBackColor = True
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(24, 146)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(189, 23)
        Me.Button1.TabIndex = 9
        Me.Button1.Text = "Get Players Coordinates"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(220, 6)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(75, 23)
        Me.Button2.TabIndex = 10
        Me.Button2.Text = "Clear List"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'btnTest1
        '
        Me.btnTest1.Location = New System.Drawing.Point(220, 231)
        Me.btnTest1.Name = "btnTest1"
        Me.btnTest1.Size = New System.Drawing.Size(75, 23)
        Me.btnTest1.TabIndex = 12
        Me.btnTest1.Text = "XYZ Set 1"
        Me.btnTest1.UseVisualStyleBackColor = True
        '
        'frmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(300, 266)
        Me.Controls.Add(Me.btnTest1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.btnStartQueue)
        Me.Controls.Add(Me.ListBox1)
        Me.Controls.Add(Me.btnAddtoList)
        Me.Controls.Add(Me.txtZ)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.txtY)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.txtX)
        Me.Controls.Add(Me.lblX)
        Me.Name = "frmMain"
        Me.Text = "TibiaAI"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents lblX As System.Windows.Forms.Label
    Friend WithEvents txtX As System.Windows.Forms.TextBox
    Friend WithEvents txtY As System.Windows.Forms.TextBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents txtZ As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents btnAddtoList As System.Windows.Forms.Button
    Friend WithEvents ListBox1 As System.Windows.Forms.ListBox
    Friend WithEvents btnStartQueue As System.Windows.Forms.Button
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents btnTest1 As System.Windows.Forms.Button

End Class
