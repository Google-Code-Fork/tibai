<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmMStatus
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
        Me.lblTask = New System.Windows.Forms.Label
        Me.pbarMStatus = New System.Windows.Forms.ProgressBar
        Me.lblTimeLeft = New System.Windows.Forms.Label
        Me.SuspendLayout()
        '
        'lblTask
        '
        Me.lblTask.AutoSize = True
        Me.lblTask.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblTask.Location = New System.Drawing.Point(12, 9)
        Me.lblTask.Name = "lblTask"
        Me.lblTask.Size = New System.Drawing.Size(249, 13)
        Me.lblTask.TabIndex = 1
        Me.lblTask.Text = "Building macro search data. Please Wait..."
        '
        'pbarMStatus
        '
        Me.pbarMStatus.Location = New System.Drawing.Point(12, 40)
        Me.pbarMStatus.Name = "pbarMStatus"
        Me.pbarMStatus.Size = New System.Drawing.Size(253, 23)
        Me.pbarMStatus.TabIndex = 2
        '
        'lblTimeLeft
        '
        Me.lblTimeLeft.AutoSize = True
        Me.lblTimeLeft.Location = New System.Drawing.Point(12, 24)
        Me.lblTimeLeft.Name = "lblTimeLeft"
        Me.lblTimeLeft.Size = New System.Drawing.Size(75, 13)
        Me.lblTimeLeft.TabIndex = 3
        Me.lblTimeLeft.Text = "Est. Time Left:"
        '
        'frmMStatus
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(277, 71)
        Me.Controls.Add(Me.lblTimeLeft)
        Me.Controls.Add(Me.pbarMStatus)
        Me.Controls.Add(Me.lblTask)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmMStatus"
        Me.RightToLeftLayout = True
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Working..."
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents lblTask As System.Windows.Forms.Label
    Friend WithEvents pbarMStatus As System.Windows.Forms.ProgressBar
    Friend WithEvents lblTimeLeft As System.Windows.Forms.Label

End Class
