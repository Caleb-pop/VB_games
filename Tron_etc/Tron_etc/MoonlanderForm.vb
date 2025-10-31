Imports System.Windows.Forms
Imports System.Drawing

Class MoonlanderForm
    '**Run the Game**:
    '   - Enable PID And observe thrust adjustments to target 15 mps.
    '**PID Performance**:
    '   - With target 15 mps, velocity should stabilize around 15 during descent.
    '   - Tune gains (e.g., Kp=0.8, Ki=0.2, Kd=0.4) for optimal performance.
    ' Game constants
    Private Const Gravity As Integer = 13  ' Units pulled down per turn
    Private Const ThrustDown As Integer = -10  ' Extra thrust down
    Private Const ThrustNone As Integer = 0
    Private Const FuelCost As Integer = 50  ' Base fuel cost for max thrust (75 units)
    Private Const MaxTurns As Integer = 120  ' Max turns before game over
    Private Const SafeLandingVelocity As Integer = 33  ' Max velocity for safe landing
    Private Const MaxAltitude As Integer = 3500  ' Starting altitude for scaling
    Private Const MaxThrust As Integer = 80  ' Maximum thrust value' PID constants and state
    Private Kp As Double = 0.6  ' Proportional gain
    Private Ki As Double = 0.2  ' Integral gain
    Private TargetVelocity As Double = 35
    Private previousError As Double = 0
    Private integral As Double = 0
    Private pidThrust As Integer = 0
    Private isPIDEnabled As Boolean = False

    ' Game state
    Private altitude As Integer = MaxAltitude
    Private velocity As Integer = 0  ' Downward positive
    Private fuel As Integer = 1500
    Private turns As Integer = 0
    Private isGameOver As Boolean = False
    Private currentThrust As Integer = 0  ' Track last thrust for graphics
    Private gameTimer As New Timer()
    Private score As Integer = 0
    Private velocityHistory As New List(Of Integer)  ' Track velocity over time

    ' UI elements
    Private gamePanel As New Panel()
    Private chartPanel As New Panel()  ' Panel for velocity vs. time plot
    Private lblAltitude As New Label()
    Private lblVelocity As New Label()
    Private lblFuel As New Label()
    Private lblTurns As New Label()
    Private btnThrustUp As New Button()  ' Retained for max thrust
    Private btnThrustNone As New Button()
    Private btnThrustDown As New Button()
    Private txtLog As New TextBox()
    Private lblGameOver As New Label()
    Private trkThrust As New TrackBar()  ' Thrust slider
    Private lblThrustValue As New Label()  ' Display current slider value
    Private trkKp As New TrackBar()  ' PID Proportional gain slider
    Private trkVelocity As New TrackBar() ' Setpoint Velocity slider
    Private lblKpValue As New Label()  ' Display Kp value
    Private lblVelocityValue As New Label()
    Private trkKi As New TrackBar()  ' PID Integral gain slider
    Private lblKiValue As New Label()  ' Display Ki value
    Private trkKd As New TrackBar()  ' PID Derivative gain slider
    Private lblKdValue As New Label()  ' Display Kd value
    Private btnPIDToggle As New Button()  ' Toggle PID control

    Private trkKiAndVelocity As New TrackBar()  ' Slider for Ki and Target Velocity
    Private lblKiAndVelocityValue As New Label()  ' Display Ki and Target Velocity values


    Private Sub MoonlanderForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Moonlander - Land Safely!"
        Me.Size = New Size(800, 800)  ' Wider and taller form for PID controls
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False

        ' Set up game panel for graphics
        gamePanel.Size = New Size(360, 300)
        gamePanel.Location = New Point(20, 20)
        gamePanel.BackColor = Color.Black  ' Space background
        AddHandler gamePanel.Paint, AddressOf GamePanel_Paint
        Me.Controls.Add(gamePanel)

        ' Set up chart panel for velocity vs. time
        chartPanel.Size = New Size(400, 300)
        chartPanel.Location = New Point(380, 20)
        chartPanel.BackColor = Color.White
        AddHandler chartPanel.Paint, AddressOf ChartPanel_Paint
        Me.Controls.Add(chartPanel)

        ' Set up status labels
        lblAltitude.Text = "Altitude: 3500"
        lblAltitude.Location = New Point(20, 330)
        lblAltitude.Size = New Size(200, 25)
        Me.Controls.Add(lblAltitude)

        lblVelocity.Text = "Velocity: 0"
        lblVelocity.Location = New Point(20, 360)
        lblVelocity.Size = New Size(200, 25)
        Me.Controls.Add(lblVelocity)

        lblFuel.Text = "Fuel: 1500"
        lblFuel.Location = New Point(20, 390)
        lblFuel.Size = New Size(200, 25)
        Me.Controls.Add(lblFuel)

        lblTurns.Text = "Turns: 0"
        lblTurns.Location = New Point(20, 420)
        lblTurns.Size = New Size(200, 25)
        Me.Controls.Add(lblTurns)

        ' Set up thrust buttons
        btnThrustUp.Text = "Thrust Up (Max)"
        btnThrustUp.Location = New Point(20, 460)
        btnThrustUp.Size = New Size(100, 40)
        AddHandler btnThrustUp.Click, AddressOf BtnThrustUp_Click
        Me.Controls.Add(btnThrustUp)

        btnThrustNone.Text = "No Thrust (0)"
        btnThrustNone.Location = New Point(130, 460)
        btnThrustNone.Size = New Size(100, 40)
        AddHandler btnThrustNone.Click, AddressOf BtnThrustNone_Click
        Me.Controls.Add(btnThrustNone)

        btnThrustDown.Text = "Thrust Down (-10)"
        btnThrustDown.Location = New Point(240, 460)
        btnThrustDown.Size = New Size(100, 40)
        AddHandler btnThrustDown.Click, AddressOf BtnThrustDown_Click
        Me.Controls.Add(btnThrustDown)

        ' Set up thrust slider
        trkThrust.Minimum = 25
        trkThrust.Maximum = MaxThrust
        trkThrust.Value = 50  ' Start at mid thrust
        trkThrust.Location = New Point(20, 510)
        trkThrust.Size = New Size(240, 45)
        trkThrust.TickFrequency = 5
        AddHandler trkThrust.Scroll, AddressOf TrkThrust_Scroll
        Me.Controls.Add(trkThrust)

        ' Set up thrust value label
        lblThrustValue.Text = "Thrust: 0"
        lblThrustValue.Location = New Point(270, 510)
        lblThrustValue.Size = New Size(90, 20)
        Me.Controls.Add(lblThrustValue)

        ' Set up log
        txtLog.Multiline = True
        txtLog.WordWrap = True
        txtLog.ScrollBars = ScrollBars.Vertical
        txtLog.ReadOnly = True
        txtLog.Location = New Point(20, 560)
        txtLog.Size = New Size(340, 110)
        txtLog.AppendText("Moonlander: Control the lunar module to land safely!" & vbCrLf & vbCrLf &
                      "Thrust Up: Max counter gravity (75 thrust, 50 fuel)" & vbCrLf &
                      "Slider: Adjust thrust (25-75, 0-50 fuel)" & vbCrLf &
                      "No Thrust: Free fall (0 thrust, 0 fuel)" & vbCrLf &
                      "Thrust Down: Accelerate descent (-10 thrust, 50 fuel)" & vbCrLf & vbCrLf &
                      "Goal: Land at altitude 0 with velocity < 30. Avoid crashing!" & vbCrLf & vbCrLf)
        Me.Controls.Add(txtLog)

        ' Move slider positions to right of log
        trkKp.Minimum = 0
        trkKp.Maximum = 10
        trkKp.Value = CInt(Kp * 10)  ' Scale to 0-10
        trkKp.Location = New Point(380, 560)
        trkKp.Size = New Size(200, 45)
        AddHandler trkKp.Scroll, AddressOf TrkKp_Scroll
        Me.Controls.Add(trkKp)

        lblKpValue.Text = $"Kp: {Kp:F1}"
        lblKpValue.Location = New Point(590, 560)
        lblKpValue.Size = New Size(90, 20)
        Me.Controls.Add(lblKpValue)

        trkKi.Minimum = 0
        trkKi.Maximum = 20
        trkKi.Value = CInt(Ki * 10)  ' Scale to 0-10
        trkKi.Location = New Point(380, 610)
        trkKi.Size = New Size(200, 45)
        AddHandler trkKi.Scroll, AddressOf TrkKi_Scroll
        Me.Controls.Add(trkKi)

        lblKiValue.Text = $"Ki: {Ki:F1}"
        lblKiValue.Location = New Point(590, 610)
        lblKiValue.Size = New Size(90, 20)
        Me.Controls.Add(lblKiValue)

        trkVelocity.Minimum = 0
        trkVelocity.Maximum = 200
        trkVelocity.Value = CInt(TargetVelocity)  ' Scale to 5-100
        trkVelocity.Location = New Point(380, 660)
        trkVelocity.Size = New Size(200, 45)
        AddHandler trkVelocity.Scroll, AddressOf TrkVelocity_Scroll
        Me.Controls.Add(trkVelocity)

        lblVelocityValue.Text = $"Target Velocity: {TargetVelocity} mps"
        lblVelocityValue.Location = New Point(590, 660)
        lblVelocityValue.Size = New Size(130, 20)
        Me.Controls.Add(lblVelocityValue)

        ' Set up combined Ki and Target Velocity slider
        trkKiAndVelocity.Minimum = 0
        trkKiAndVelocity.Maximum = 200
        trkKiAndVelocity.Value = CInt(Ki * 100)  ' Initialize based on Ki (0 to 1 scaled to 0-100)
        trkKiAndVelocity.Location = New Point(380, 460)
        trkKiAndVelocity.Size = New Size(200, 45)
        trkKiAndVelocity.TickFrequency = 5
        AddHandler trkKiAndVelocity.Scroll, AddressOf TrkKiAndVelocity_Scroll
        Me.Controls.Add(trkKiAndVelocity)

        ' Set up combined Ki and Target Velocity value label
        lblKiAndVelocityValue.Text = $"Ki: {Ki:F1}, Target: {TargetVelocity} mps"
        lblKiAndVelocityValue.Location = New Point(590, 760)
        lblKiAndVelocityValue.Size = New Size(130, 20)
        Me.Controls.Add(lblKiAndVelocityValue)

        ' Set up PID toggle button
        btnPIDToggle.Text = "Enable PI"
        btnPIDToggle.Location = New Point(380, 710)
        btnPIDToggle.Size = New Size(100, 40)
        AddHandler btnPIDToggle.Click, AddressOf BtnPIDToggle_Click
        Me.Controls.Add(btnPIDToggle)

        ' Game over label
        lblGameOver.Visible = False
        lblGameOver.Location = New Point(20, 560)
        lblGameOver.Size = New Size(340, 50)
        lblGameOver.Font = New Font("Arial", 12, FontStyle.Bold)
        lblGameOver.ForeColor = Color.Red
        Me.Controls.Add(lblGameOver)

        ' Timer setup
        gameTimer.Interval = 1000  ' 1 second per turn after thrust
        AddHandler gameTimer.Tick, AddressOf GameTimer_Tick

        UpdateStatus()
        gamePanel.Invalidate()
        chartPanel.Invalidate()
    End Sub

    Private Sub BtnThrustUp_Click(sender As Object, e As EventArgs)
        If isGameOver OrElse fuel < FuelCost OrElse isPIDEnabled Then Return
        currentThrust = MaxThrust
        ApplyThrust(MaxThrust)
    End Sub

    Private Sub BtnThrustNone_Click(sender As Object, e As EventArgs)
        If isGameOver OrElse isPIDEnabled Then Return
        currentThrust = ThrustNone
        ApplyThrust(ThrustNone)
    End Sub

    Private Sub BtnThrustDown_Click(sender As Object, e As EventArgs)
        If isGameOver OrElse fuel < FuelCost OrElse isPIDEnabled Then Return
        currentThrust = ThrustDown
        ApplyThrust(ThrustDown)
    End Sub

    Private Sub TrkThrust_Scroll(sender As Object, e As EventArgs)
        lblThrustValue.Text = $"Thrust: {trkThrust.Value}"
        If isGameOver OrElse fuel < (trkThrust.Value / MaxThrust * FuelCost) OrElse isPIDEnabled Then Return
        currentThrust = trkThrust.Value
        ApplyThrust(trkThrust.Value)
    End Sub

    Private Sub TrkKp_Scroll(sender As Object, e As EventArgs)
        Kp = trkKp.Value / 10.0
        lblKpValue.Text = $"Kp: {Kp:F1}"
    End Sub

    'add Setpoint Velocity control here
    Private Sub TrkVelocity_Scroll(sender As Object, e As EventArgs)
        TargetVelocity = trkVelocity.Value
        lblVelocityValue.Text = $"Target Velocity: {TargetVelocity} mps"
        ' Update combined slider (based on TargetVelocity)
        trkKiAndVelocity.Value = CInt((TargetVelocity - 5) / (100 - 5) * 100)
        lblKiAndVelocityValue.Text = $"Ki: {Ki:F1}, Target: {CInt(TargetVelocity)} mps"
    End Sub

    Private Sub TrkKi_Scroll(sender As Object, e As EventArgs)
        Ki = trkKi.Value / 10.0
        lblKiValue.Text = $"Ki: {Ki:F1}"
        ' Update combined slider (based on Ki)
        trkKiAndVelocity.Value = CInt(Ki * 100)
        lblKiAndVelocityValue.Text = $"Ki: {Ki:F1}, Target: {CInt(TargetVelocity)} mps"
    End Sub

    Private Sub TrkKiAndVelocity_Scroll(sender As Object, e As EventArgs)
        ' Update Ki (0 to 1)
        Ki = TargetVelocity / 200 'trkKiAndVelocity.Value / 100.0
        ' Update TargetVelocity (5 to 100)
        TargetVelocity = Math.Max(25, ((5 + (trkKiAndVelocity.Value / 100.0) * (100 - 5)) * Kp) * ((altitude / MaxAltitude) * (turns / velocity + 1)))
        ' Update labels
        lblKiAndVelocityValue.Text = $"Ki: {Ki:F1}, Target: {CInt(TargetVelocity)} mps"
        lblKiValue.Text = $"Ki: {Ki:F1}"  ' Sync with individual Ki label
        lblVelocityValue.Text = $"Target Velocity: {CInt(TargetVelocity)} mps"  ' Sync with individual velocity label
        ' Update individual sliders to reflect combined slider's values
        trkKi.Value = CInt(Ki * 10)  ' Scale to 0-10 for trkKi
        trkVelocity.Value = Math.Min(200, CInt(TargetVelocity)) ' Set to TargetVelocity for trkVelocity
    End Sub

    Private Sub BtnPIDToggle_Click(sender As Object, e As EventArgs)
        isPIDEnabled = Not isPIDEnabled
        btnPIDToggle.Text = If(isPIDEnabled, "Disable PID", "Enable PID")
        If isPIDEnabled Then
            previousError = 0
            integral = 0
            txtLog.AppendText("PID control enabled (target: 35 mps)." & vbCrLf)
        Else
            txtLog.AppendText("PID control disabled." & vbCrLf)
        End If
        txtLog.ScrollToCaret()
    End Sub

    Private Function CalculatePIDThrust(targetVelocity As Integer) As Integer
        'Dim targetVelocity As Integer = 45  ' Target setpoint velocity
        Dim Verror As Double = -(targetVelocity - velocity)
        integral += Verror
        'Dim derivative As Double = Verror - previousError
        'previousError = Verror

        Dim output As Double = (Kp * Verror) + (Ki * integral) '+ (Kd * derivative)
        pidThrust = Math.Max(0, Math.Min(MaxThrust, CInt(output)))  ' Constrain to 0-MaxThrust
        Return pidThrust
    End Function

    Private Sub ApplyThrust(thrust As Integer)
        If fuel < (thrust / MaxThrust * FuelCost) AndAlso thrust <> ThrustNone AndAlso thrust <> ThrustDown Then Return

        ' Apply manual or PID thrust
        If isPIDEnabled Then
            thrust = CalculatePIDThrust(TargetVelocity)
            currentThrust = thrust
        End If

        velocity -= thrust  ' Up thrust reduces downward velocity
        If thrust <> ThrustNone AndAlso thrust <> ThrustDown Then fuel -= CInt(thrust / MaxThrust * FuelCost) ' Scale fuel cost with thrust

        ' Advance turn
        turns += 1
        velocityHistory.Add(velocity)  ' Record current velocity
        If velocityHistory.Count > MaxTurns Then velocityHistory.RemoveAt(0)  ' Limit to MaxTurns

        txtLog.AppendText($"Turn {turns}: Thrust {thrust}, Fuel: {fuel}, Velocity: {velocity}, Altitude: {altitude}" & vbCrLf)
        txtLog.ScrollToCaret()

        ' Update status
        UpdateStatus()

        ' Advance physics
        altitude = Math.Max(0, altitude - velocity)  ' Prevent negative altitude
        velocity += Gravity

        ' Check landing/crash
        If altitude <= 0 Then
            CheckLanding()
            isGameOver = True
            gameTimer.Stop()
            Return
        End If

        If turns >= MaxTurns Then
            isGameOver = True
            gameTimer.Stop()
            txtLog.AppendText("Game Over: Out of turns!" & vbCrLf)
            txtLog.ScrollToCaret()
            lblGameOver.Text = "Out of turns! Final Score: " & score
            lblGameOver.Visible = True
            Return
        End If

        ' Start timer for next turn
        gameTimer.Start()
        gamePanel.Invalidate()  ' Redraw graphics
        chartPanel.Invalidate()  ' Redraw chart
    End Sub

    Private Sub CheckLanding()
        If velocity < SafeLandingVelocity Then
            score = CInt(10000 / velocity) + (fuel * 5)  ' Score formula
            txtLog.AppendText($"You have landed safely with approach velocity of {velocity} mps." & vbCrLf & $"Score: {score}" & vbCrLf)
            txtLog.ScrollToCaret()
            lblGameOver.Text = $"Well done, intrepid captain! Final Score: {score}"
            lblGameOver.ForeColor = Color.Green
            lblGameOver.Visible = True
        Else
            txtLog.AppendText($"Your ship crashed at {velocity} mps" & vbCrLf & $"creating a {velocity * 2 * 3.14} meter deep crater" & vbCrLf)
            txtLog.ScrollToCaret()
            lblGameOver.Text = $"{velocity * 2 * 3.14} meter deep crater"
            lblGameOver.Visible = True
        End If
    End Sub

    Private Sub GameTimer_Tick(sender As Object, e As EventArgs)
        If isGameOver Then Return

        If turns = 0 Then
            lblKiAndVelocityValue.Enabled = False

        End If
        ' Advance physics (gravity only, no thrust if PID disabled)
        altitude = Math.Max(0, altitude - velocity)  ' Prevent negative altitude
        velocity += Gravity

        turns += 1
        velocityHistory.Add(velocity)  ' Record current velocity
        If velocityHistory.Count > MaxTurns Then velocityHistory.RemoveAt(0)  ' Limit to MaxTurns

        txtLog.AppendText($"Turn {turns}: Free fall. Velocity: {velocity}, Altitude: {altitude}" & vbCrLf)
        txtLog.ScrollToCaret()

        UpdateStatus()

        If altitude <= 0 Then
            CheckLanding()
            isGameOver = True
            Return
        End If

        If turns >= MaxTurns Then
            isGameOver = True
            txtLog.AppendText("Game Over: Out of turns!" & vbCrLf)
            txtLog.ScrollToCaret()
            lblGameOver.Text = "Out of turns! Final Score: " & score
            lblGameOver.Visible = True
            Return
        End If
        If isPIDEnabled Then
            ApplyThrust(15) 'not actual thrust, just to trigger PID calculation
        End If
        gamePanel.Invalidate()  ' Redraw graphics
        chartPanel.Invalidate()  ' Redraw chart
    End Sub

    Private Sub GamePanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim panelHeight As Integer = gamePanel.Height
        Dim panelWidth As Integer = gamePanel.Width

        ' Background: Black space with stars
        g.FillRectangle(New SolidBrush(Color.Black), 0, 0, panelWidth, panelHeight)

        ' Moon surface (brown line at bottom)
        Using surfaceBrush As New SolidBrush(Color.SaddleBrown)
            g.FillRectangle(surfaceBrush, 0, panelHeight - 20, panelWidth, 20)
        End Using

        ' Velocity indicator bar (red bar on left side)
        Dim velocityBarHeight As Integer = Math.Max(0, Math.Min(velocity * 5, 200))  ' Scale velocity to bar height
        Using velocityBrush As New SolidBrush(Color.Red)
            g.FillRectangle(velocityBrush, 10, panelHeight - 20 - velocityBarHeight, 20, velocityBarHeight)
        End Using

        ' Lunar module position (scaled from altitude)
        Dim moduleY As Integer = panelHeight - 40 - (altitude / MaxAltitude * (panelHeight - 60))  ' Scale altitude to Y
        Dim moduleX As Integer = panelWidth / 2  ' Center horizontally

        ' Draw lunar module (simple rocket shape)
        Using moduleBrush As New SolidBrush(Color.Silver)
            ' Main body (rectangle)
            g.FillRectangle(moduleBrush, moduleX - 8, moduleY - 16, 16, 24)
            ' Top (nose cone - triangle)
            Dim nosePoints() As Point = {New Point(moduleX - 8, moduleY - 16), New Point(moduleX + 8, moduleY - 16), New Point(moduleX, moduleY - 24)}
            g.FillPolygon(moduleBrush, nosePoints)
            ' Landing legs (lines at bottom)
            Using legPen As New Pen(Color.Gray, 2)
                g.DrawLine(legPen, moduleX - 12, moduleY + 8, moduleX - 20, moduleY + 12)
                g.DrawLine(legPen, moduleX + 12, moduleY + 8, moduleX + 20, moduleY + 12)
            End Using
        End Using

        ' Flames (if thrusting)
        If currentThrust > 0 Then  ' Only show flames for positive thrust
            Using flameBrush As New SolidBrush(Color.Orange)
                Dim flameY = moduleY + 8  ' Below module
                Dim flameHeight = currentThrust * 2  ' Scale flame size with thrust
                Dim flamePoints() As Point = {New Point(moduleX - 4, flameY), New Point(moduleX + 4, flameY), New Point(moduleX, flameY + flameHeight)}
                g.FillPolygon(flameBrush, flamePoints)
            End Using
        End If
    End Sub

    Private Sub ChartPanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim panelWidth As Integer = chartPanel.Width
        Dim panelHeight As Integer = chartPanel.Height

        ' Clear panel
        g.Clear(Color.White)

        ' Draw axes
        Using pen As New Pen(Color.Black, 1)
            g.DrawLine(pen, 20, 20, 20, panelHeight - 20)  ' Y-axis
            g.DrawLine(pen, 20, panelHeight - 20, panelWidth - 20, panelHeight - 20)  ' X-axis
        End Using

        ' Label axes
        Using font As New Font("Arial", 8)
            g.DrawString("Velocity (mps)", font, Brushes.Black, 5, 5)
            g.DrawString("Turns", font, Brushes.Black, panelWidth - 40, panelHeight - 15)
        End Using

        ' Draw grid lines (every 20 velocity units, every 10 turns)
        Using pen As New Pen(Color.LightGray, 1)
            For i As Integer = 0 To 10
                Dim y = panelHeight - 20 - (i * (panelHeight - 40) / 10)  ' Scale 0-200 velocity
                g.DrawLine(pen, CSng(20), CSng(y), CSng(panelWidth - 20), CSng(y))  ' Horizontal grid
                g.DrawString((i * 20).ToString(), Font, Brushes.Black, 5, y - 5)
            Next
            For i As Integer = 0 To 10
                Dim x = 20 + (i * (panelWidth - 40) / 10)  ' Scale 0-100 turns
                g.DrawLine(pen, CSng(x), CSng(20), CSng(x), CSng(panelHeight - 20))  ' Vertical grid
                g.DrawString((i * 10).ToString(), Font, Brushes.Black, x - 5, panelHeight - 15)
            Next
        End Using

        ' Draw velocity plot
        If velocityHistory.Count > 1 Then
            Using pen As New Pen(Color.Blue, 2)
                For i As Integer = 1 To velocityHistory.Count - 1
                    Dim x1 = 20 + ((i - 1) * (panelWidth - 40) / (MaxTurns - 1))
                    Dim y1 = panelHeight - 20 - (Math.Min(velocityHistory(i - 1), 200) * (panelHeight - 40) / 200)
                    Dim x2 = 20 + (i * (panelWidth - 40) / (MaxTurns - 1))
                    Dim y2 = panelHeight - 20 - (Math.Min(velocityHistory(i), 200) * (panelHeight - 40) / 200)
                    g.DrawLine(pen, CSng(x1), CSng(y1), CSng(x2), CSng(y2))
                Next
            End Using
        End If
    End Sub

    Private Sub UpdateStatus()
        lblAltitude.Text = $"Altitude: {altitude}"
        lblVelocity.Text = $"Velocity: {velocity}"
        lblFuel.Text = $"Fuel: {fuel}"
        lblTurns.Text = $"Turns: {turns}"
    End Sub
End Class

