Imports System.Windows.Forms
Imports System.Drawing

Public Class StarTrek1971Form
    ' Game constants
    Private Const ScreenWidth As Integer = 800
    Private Const ScreenHeight As Integer = 800 ' Increased height
    Private Const QuadSize As Integer = 8  ' 8x8 sectors per quadrant
    Private Const SectorSize As Integer = ScreenWidth / QuadSize  ' 100 pixels
    Private Const ShipSize As Integer = 20
    Private Const KlingonSize As Integer = 20
    Private Const StarbaseSize As Integer = 30
    Private Const StarSize As Integer = 10
    Private Const MaxKlingonsPerQuad As Integer = 3

    ' Galaxy and quadrant state
    Private galaxy(7, 7) As Integer  ' 100*Klingons + 10*Starbases + Stars
    Private currentQuadX, currentQuadY As Integer

    ' Player properties (USS Enterprise)
    Private enterpriseX As Single
    Private enterpriseY As Single
    Private enterpriseSectorX, enterpriseSectorY As Integer
    Private enterpriseAngle As Single = 0.0F  ' Angle in degrees, updated with movement

    ' Objects in current quadrant
    Private klingons As New List(Of Klingon)()
    Private starbases As New List(Of Starbase)()
    Private stars As New List(Of Star)()

    ' Game state
    Private WithEvents gameTimer As New Timer()
    Private score As Integer = 0
    Private energy As Integer = 3000
    Private shields As Integer = 0
    Private photonTorpedoes As Integer = 8
    Private stardates As Integer = 365
    Private totalKlingons As Integer
    Private isGameOver As Boolean = False
    Private damaged(5) As Boolean ' Warp, Phasers, Torpedoes, SRS, LRS, Shields

    ' UI elements
    Private gamePanel As New Panel()
    Private lblStatus As New Label()
    Private lblLRS As New Label()
    Private lblLegend As New Label()
    Private lblGalaxyMap As New Label()
    Private WithEvents txtActionLog As New TextBox()  ' Changed from ListBox to TextBox
    Private WithEvents btnMoveUp As New Button()
    Private WithEvents btnMoveUpRight As New Button()
    Private WithEvents btnMoveRight As New Button()
    Private WithEvents btnMoveDownRight As New Button()
    Private WithEvents btnMoveDown As New Button()
    Private WithEvents btnMoveDownLeft As New Button()
    Private WithEvents btnMoveLeft As New Button()
    Private WithEvents btnMoveUpLeft As New Button()
    Private WithEvents btnFirePhasers As New Button()
    Private WithEvents btnFireTorpedo As New Button()
    Private WithEvents btnSRS As New Button()
    Private WithEvents btnLRS As New Button()
    Private WithEvents btnShields As New Button()
    Private WithEvents btnDamage As New Button()
    Private WithEvents btnWarpQuad As New Button()

    ' Classes
    Private Class Klingon
        Public SectorX As Integer
        Public SectorY As Integer
        Public X As Single
        Public Y As Single
        Public Energy As Integer
        Public IsCloaked As Boolean  ' New property for cloaking state
        Public Sub New(x As Single, y As Single, sectorX As Integer, sectorY As Integer)
            Me.X = x
            Me.Y = y
            Me.SectorX = sectorX
            Me.SectorY = sectorY
            Me.Energy = CInt(Rnd() * 200 + 100)
            Me.IsCloaked = (Rnd() < 0.3)  ' 30% chance to start cloaked
        End Sub
    End Class

    Private Class Starbase
        Public X As Single
        Public Y As Single
        Public SectorX As Integer
        Public SectorY As Integer
        Public Sub New(x As Single, y As Single, sectorX As Integer, sectorY As Integer)
            Me.X = x
            Me.Y = y
            Me.SectorX = sectorX
            Me.SectorY = sectorY
        End Sub
    End Class

    Private Class Star
        Public X As Single
        Public Y As Single
        Public SectorX As Integer
        Public SectorY As Integer
        Public Sub New(x As Single, y As Single, sectorX As Integer, sectorY As Integer)
            Me.X = x
            Me.Y = y
            Me.SectorX = sectorX
            Me.SectorY = sectorY
        End Sub
    End Class

    Private Sub StarTrek1971Form_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Randomize()
        Me.Text = "Star Trek 1971"
        Me.Size = New Size(ScreenWidth + 300, ScreenHeight + 60)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = False

        ' Set up game panel
        With gamePanel
            .Size = New Size(ScreenWidth, ScreenHeight)
            .Location = New Point(20, 20)
            .BackColor = Color.Black
        End With
        Me.Controls.Add(gamePanel)
        AddHandler gamePanel.Paint, AddressOf GamePanel_Paint

        ' Set up UI elements
        lblStatus.Location = New Point(20, ScreenHeight + 30)
        lblStatus.Size = New Size(ScreenWidth, 20)
        Me.Controls.Add(lblStatus)

        lblLRS.Location = New Point(ScreenWidth + 30, 20)
        lblLRS.Size = New Size(250, 100)
        Me.Controls.Add(lblLRS)

        lblLegend.Location = New Point(ScreenWidth + 30, 130)
        lblLegend.Size = New Size(250, 100)
        lblLegend.Text = "Legend:" & vbCrLf &
                         "<*> = Enterprise" & vbCrLf &
                         "+K+ = Klingon (visible)" & vbCrLf &
                         "+C+ = Cloaked Klingon (revealed by SRS)" & vbCrLf &
                         ">O< = Starbase" & vbCrLf &
                         "* = Star"
        Me.Controls.Add(lblLegend)

        ' Galaxy map (text-based)   - optional
        'lblGalaxyMap.Location = New Point(ScreenWidth + 30, 240)
        'lblGalaxyMap.Size = New Size(250, 200)
        'lblGalaxyMap.Font = New Font("Consolas", 8)
        'Me.Controls.Add(lblGalaxyMap)

        txtActionLog.Location = New Point(ScreenWidth + 30, 450)
        txtActionLog.Size = New Size(250, 150)
        txtActionLog.Multiline = True
        txtActionLog.WordWrap = True
        txtActionLog.ScrollBars = ScrollBars.Vertical
        txtActionLog.ReadOnly = True  ' Prevent user input
        txtActionLog.BackColor = SystemColors.Window  ' Match default background
        Me.Controls.Add(txtActionLog)

        ' Initialize buttons in bottom right corner (5x3 grid layout)
        Dim startX As Integer = ScreenWidth + 30
        Dim startY As Integer = ScreenHeight - 120 ' Adjusted to show all buttons
        Dim buttonWidth As Integer = 45  ' Made buttons smaller to fit
        Dim buttonHeight As Integer = 30
        Dim spacing As Integer = 5

        ' Row 0: Movement buttons (top row)
        btnMoveUpLeft.Text = "↖"
        btnMoveUpLeft.Size = New Size(buttonWidth, buttonHeight)
        btnMoveUpLeft.Location = New Point(startX, startY)
        Me.Controls.Add(btnMoveUpLeft)

        btnMoveUp.Text = "↑"
        btnMoveUp.Size = New Size(buttonWidth, buttonHeight)
        btnMoveUp.Location = New Point(startX + buttonWidth + spacing, startY)
        Me.Controls.Add(btnMoveUp)

        btnMoveUpRight.Text = "↗"
        btnMoveUpRight.Size = New Size(buttonWidth, buttonHeight)
        btnMoveUpRight.Location = New Point(startX + (buttonWidth + spacing) * 2, startY)
        Me.Controls.Add(btnMoveUpRight)

        ' Row 1: Movement buttons (middle row)
        btnMoveLeft.Text = "←"
        btnMoveLeft.Size = New Size(buttonWidth, buttonHeight)
        btnMoveLeft.Location = New Point(startX, startY + buttonHeight + spacing)
        Me.Controls.Add(btnMoveLeft)

        ' Center space empty (enterprise position)

        btnMoveRight.Text = "→"
        btnMoveRight.Size = New Size(buttonWidth, buttonHeight)
        btnMoveRight.Location = New Point(startX + (buttonWidth + spacing) * 2, startY + buttonHeight + spacing)
        Me.Controls.Add(btnMoveRight)

        ' Row 2: Movement buttons (bottom row)
        btnMoveDownLeft.Text = "↙"
        btnMoveDownLeft.Size = New Size(buttonWidth, buttonHeight)
        btnMoveDownLeft.Location = New Point(startX, startY + (buttonHeight + spacing) * 2)
        Me.Controls.Add(btnMoveDownLeft)

        btnMoveDown.Text = "↓"
        btnMoveDown.Size = New Size(buttonWidth, buttonHeight)
        btnMoveDown.Location = New Point(startX + buttonWidth + spacing, startY + (buttonHeight + spacing) * 2)
        Me.Controls.Add(btnMoveDown)

        btnMoveDownRight.Text = "↘"
        btnMoveDownRight.Size = New Size(buttonWidth, buttonHeight)
        btnMoveDownRight.Location = New Point(startX + (buttonWidth + spacing) * 2, startY + (buttonHeight + spacing) * 2)
        Me.Controls.Add(btnMoveDownRight)

        ' Action buttons (right side)
        Dim actionStartX As Integer = startX + (buttonWidth + spacing) * 3 + spacing * 2
        Dim actionButtonWidth As Integer = 80

        btnFirePhasers.Text = "Phasers"
        btnFirePhasers.Size = New Size(actionButtonWidth, buttonHeight)
        btnFirePhasers.Location = New Point(actionStartX, startY)
        Me.Controls.Add(btnFirePhasers)

        btnFireTorpedo.Text = "Torpedo"
        btnFireTorpedo.Size = New Size(actionButtonWidth, buttonHeight)
        btnFireTorpedo.Location = New Point(actionStartX, startY + (buttonHeight + spacing))
        Me.Controls.Add(btnFireTorpedo)

        btnShields.Text = "Shields"
        btnShields.Size = New Size(actionButtonWidth, buttonHeight)
        btnShields.Location = New Point(actionStartX, startY + (buttonHeight + spacing) * 2)
        Me.Controls.Add(btnShields)

        ' Bottom row buttons
        Dim bottomStartY As Integer = startY + (buttonHeight + spacing) * 3
        btnDamage.Text = "Damage"
        btnDamage.Size = New Size(buttonWidth * 1.5, buttonHeight)
        btnDamage.Location = New Point(startX, bottomStartY)
        Me.Controls.Add(btnDamage)

        btnLRS.Text = "LRS"
        btnLRS.Size = New Size(buttonWidth * 2, buttonHeight)
        btnLRS.Location = New Point(startX + (buttonWidth * 1.5 + spacing), bottomStartY)
        Me.Controls.Add(btnLRS)

        btnWarpQuad.Text = "Warp"
        btnWarpQuad.Size = New Size(actionButtonWidth, buttonHeight)
        btnWarpQuad.Location = New Point(actionStartX, bottomStartY)
        Me.Controls.Add(btnWarpQuad)

        ' SRS button above log
        btnSRS.Text = "SRS"
        btnSRS.Size = New Size(buttonWidth * 2, buttonHeight) ' Match LRS width for consistency
        btnSRS.Location = New Point(startX, txtActionLog.Location.Y - buttonHeight - spacing) ' Above log with spacing
        Me.Controls.Add(btnSRS)

        ' Initialize game
        InitializeGalaxy()
        currentQuadX = 4
        currentQuadY = 4
        enterpriseSectorX = 4
        enterpriseSectorY = 4
        enterpriseX = enterpriseSectorX * SectorSize + SectorSize / 2
        enterpriseY = enterpriseSectorY * SectorSize + SectorSize / 2
        InitializeQuadrant()
        UpdateStatus()
        UpdateGalaxyMap()

        gameTimer.Interval = 100 ' ~10 FPS, for status updates only
        gameTimer.Start()
    End Sub

    Private Sub InitializeGalaxy()
        totalKlingons = CInt(Rnd() * 15 + 10)
        Dim totalBases As Integer = CInt(Rnd() * 3 + 2)
        Dim totalStars As Integer = CInt(Rnd() * 50 + 20)
        For i = 0 To 7
            For j = 0 To 7
                galaxy(i, j) = 0
            Next
        Next
        For i = 1 To totalKlingons
            Dim qx As Integer = CInt(Rnd() * 7)
            Dim qy As Integer = CInt(Rnd() * 7)
            galaxy(qx, qy) += 100
        Next
        For i = 1 To totalBases
            Dim qx As Integer = CInt(Rnd() * 7)
            Dim qy As Integer = CInt(Rnd() * 7)
            galaxy(qx, qy) += 10
        Next
        For i = 1 To totalStars
            Dim qx As Integer = CInt(Rnd() * 7)
            Dim qy As Integer = CInt(Rnd() * 7)
            galaxy(qx, qy) += 1
        Next
    End Sub

    Private Sub InitializeQuadrant()
        klingons.Clear()
        starbases.Clear()
        stars.Clear()
        Dim quadData As Integer = galaxy(currentQuadX, currentQuadY)
        Dim numKlingons As Integer = Math.Min(quadData \ 100, MaxKlingonsPerQuad)
        Dim numBases As Integer = (quadData Mod 100) \ 10
        Dim numStars As Integer = quadData Mod 10
        Dim occupiedSectors As New List(Of Point)
        occupiedSectors.Add(New Point(enterpriseSectorX, enterpriseSectorY))
        For i = 1 To numKlingons
            Dim sectorX, sectorY As Integer
            Do
                sectorX = CInt(Rnd() * 7)
                sectorY = CInt(Rnd() * 7)
            Loop Until Not occupiedSectors.Contains(New Point(sectorX, sectorY))
            Dim x As Single = sectorX * SectorSize + SectorSize / 2
            Dim y As Single = sectorY * SectorSize + SectorSize / 2
            klingons.Add(New Klingon(x, y, sectorX, sectorY))
            occupiedSectors.Add(New Point(sectorX, sectorY))
        Next
        For i = 1 To numBases
            Dim sectorX, sectorY As Integer
            Do
                sectorX = CInt(Rnd() * 7)
                sectorY = CInt(Rnd() * 7)
            Loop Until Not occupiedSectors.Contains(New Point(sectorX, sectorY))
            Dim x As Single = sectorX * SectorSize + SectorSize / 2
            Dim y As Single = sectorY * SectorSize + SectorSize / 2
            starbases.Add(New Starbase(x, y, sectorX, sectorY))
            occupiedSectors.Add(New Point(sectorX, sectorY))
        Next
        For i = 1 To numStars
            Dim sectorX, sectorY As Integer
            Do
                sectorX = CInt(Rnd() * 7)
                sectorY = CInt(Rnd() * 7)
            Loop Until Not occupiedSectors.Contains(New Point(sectorX, sectorY))
            Dim x As Single = sectorX * SectorSize + SectorSize / 2
            Dim y As Single = sectorY * SectorSize + SectorSize / 2
            stars.Add(New Star(x, y, sectorX, sectorY))
            occupiedSectors.Add(New Point(sectorX, sectorY))
        Next
        gamePanel.Invalidate() ' Ensure initial draw
    End Sub

    Private Sub GameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        If isGameOver Then
            gameTimer.Stop()
            ' Show appropriate game over message
            If totalKlingons = 0 Then
                MessageBox.Show($"Victory! All Klingons destroyed!{vbCrLf}Final Score: {score}", "Mission Accomplished", MessageBoxButtons.OK, MessageBoxIcon.Information)
            ElseIf stardates <= 0 Then
                MessageBox.Show($"Mission Failed: Stardates expired!{vbCrLf}Final Score: {score}", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            ElseIf shields < 0 Then
                MessageBox.Show($"Enterprise destroyed by Klingon fire!{vbCrLf}Final Score: {score}", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ElseIf energy <= 0 Then
                MessageBox.Show($"Enterprise powerless and drifting in space!{vbCrLf}Final Score: {score}", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
            Me.Close()
            Return
        End If

        ' Update status text and map
        UpdateStatus()
        ' concatenate status and me.txt to legend
        ' remove the name of the game from status
        lblLegend.Text = Me.Text.Replace($"Star Trek 1971 - Score: {score} ", " ").Trim()
        lblLegend.Text = $"{vbCrLf}" + lblStatus.Text + $"{vbCrLf}" + lblLegend.Text

        UpdateGalaxyMap()
    End Sub

    Private Sub PerformAction()
        Try
            stardates -= 1
            If stardates <= 0 Then
                isGameOver = True
                MessageBox.Show($"Mission failed: Stardates expired!{vbCrLf}Final Score: {score}", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            KlingonAttack()
            CheckDocking()
            CheckWinCondition()
            UpdateStatus()
            UpdateGalaxyMap()
            gamePanel.Invalidate() ' Redraw panel
        Catch ex As Exception
            txtActionLog.AppendText("Error: " & ex.Message & vbCrLf)  ' Use AppendText instead of Insert
            txtActionLog.ScrollToCaret()  ' Auto-scroll to the latest entry
        End Try
    End Sub

    Private Sub KlingonAttack()
        For Each klingon In klingons
            If Not klingon.IsCloaked AndAlso Rnd() < 0.5 Then  ' Only attack if not cloaked
                Dim damage As Integer = CInt(Rnd() * 100 + 50)
                shields -= damage
                txtActionLog.AppendText($"Klingon at {klingon.SectorX + 1},{klingon.SectorY + 1} deals {damage} damage to shields!" & vbCrLf)
                txtActionLog.ScrollToCaret()
                If shields < 0 Then
                    isGameOver = True
                    txtActionLog.AppendText("Game Over: Enterprise destroyed by Klingon fire!" & vbCrLf)
                    txtActionLog.ScrollToCaret()
                    MessageBox.Show($"Enterprise destroyed by Klingon fire!{vbCrLf}Final Score: {score}", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Return
                End If
            Else
                ' Klingon moves but does not attack
                Dim moveDirection As Integer = CInt(Rnd() * 8) + 1
                Dim angles() As Single = {90, 45, 0, 315, 270, 225, 180, 135} ' 1=Up, 2=Up-Right, ..., 8=Up-Left
                Dim angle As Single = angles(moveDirection - 1)
                Dim rad As Single = angle * CSng(Math.PI) / 180.0F
                Dim dx As Integer = CInt(Math.Round(Math.Cos(rad)))
                Dim dy As Integer = CInt(Math.Round(-Math.Sin(rad)))

                Dim newSectorX As Integer = klingon.SectorX + dx
                Dim newSectorY As Integer = klingon.SectorY + dy

                If newSectorX >= 0 AndAlso newSectorX < QuadSize AndAlso newSectorY >= 0 AndAlso newSectorY < QuadSize Then
                    ' Check if new sector is occupied
                    Dim occupied As Boolean = False
                    For Each otherKlingon In klingons
                        If otherKlingon IsNot klingon AndAlso otherKlingon.SectorX = newSectorX AndAlso otherKlingon.SectorY = newSectorY Then
                            occupied = True
                            Exit For
                        End If
                    Next
                    For Each starbase In starbases
                        If starbase.SectorX = newSectorX AndAlso starbase.SectorY = newSectorY Then
                            occupied = True
                            Exit For
                        End If
                    Next
                    For Each star In stars
                        If star.SectorX = newSectorX AndAlso star.SectorY = newSectorY Then
                            occupied = True
                            Exit For
                        End If
                    Next

                    If Not occupied Then ' not occupied or next to Enterprise
                        klingon.SectorX = newSectorX
                        klingon.SectorY = newSectorY
                        klingon.X = klingon.SectorX * SectorSize + SectorSize / 2
                        klingon.Y = klingon.SectorY * SectorSize + SectorSize / 2
                        ' Log movement (for debugging)
                        'txtActionLog.AppendText($"Klingon moves to sector {klingon.SectorX + 1},{klingon.SectorY + 1}")
                        'txtActionLog.ScrollToCaret()
                        ' check if Klingon moved into Enterprise sector
                        If klingon.SectorX = enterpriseSectorX AndAlso klingon.SectorY = enterpriseSectorY Then
                            Dim damage As Integer = CInt(Rnd() * 100 + 50)
                            shields -= damage
                            txtActionLog.AppendText($"Klingon rams Enterprise at {klingon.SectorX + 1},{klingon.SectorY + 1} dealing {damage} damage!" & vbCrLf)
                            txtActionLog.ScrollToCaret()
                            If shields < 0 Then
                                isGameOver = True
                                txtActionLog.AppendText("Game Over: Enterprise destroyed by Klingon ramming!" & vbCrLf)
                                txtActionLog.ScrollToCaret()
                                MessageBox.Show($"Enterprise destroyed by Klingon ramming!{vbCrLf}Final Score: {score}", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                Return
                            End If
                        End If
                    End If

                End If

                ' 30% chance to decloak after moving
                If klingon.IsCloaked Then
                    If Rnd() < 0.3 Then ' 30% chance 
                        klingon.IsCloaked = False
                        txtActionLog.AppendText($"Klingon at {klingon.SectorX + 1},{klingon.SectorY + 1} decloaks!" & vbCrLf)
                        txtActionLog.ScrollToCaret()
                    End If
                End If


            End If
        Next
    End Sub

    Private Sub CheckDocking()
        For Each starbase In starbases
            If enterpriseSectorX = starbase.SectorX AndAlso enterpriseSectorY = starbase.SectorY Then
                ' Restore all systems and energy
                energy = 3000
                photonTorpedoes = 8
                shields = 500
                For i = 0 To 5
                    damaged(i) = False
                Next

                ' Notify player of docking and repairs
                txtActionLog.AppendText("Docked at starbase - All systems restored:" & vbCrLf)
                txtActionLog.AppendText(vbCrLf & vbCrLf &
                              "- Energy restored to 3000" & vbCrLf & vbCrLf &
                              "- Torpedoes restocked to 8" & vbCrLf & vbCrLf &
                              "- Shields restored" & vbCrLf)
                txtActionLog.ScrollToCaret()

                ' Force immediate display update
                UpdateStatus()
                gamePanel.Invalidate()
                Exit For
            End If
        Next
    End Sub

    Private Sub CheckWinCondition()
        If totalKlingons = 0 Then
            isGameOver = True
            txtActionLog.AppendText($"Mission Complete! All Klingons destroyed! Score: {score}" & vbCrLf)
            txtActionLog.ScrollToCaret()
            MessageBox.Show($"Victory! The Federation is saved!{vbCrLf}Final Score: {score}", "Mission Accomplished", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub MoveEnterprise(direction As Integer)
        Try
            If damaged(0) Then
                txtActionLog.AppendText("Warp engines damaged!" & vbCrLf)
                txtActionLog.ScrollToCaret()
                Return
            End If
            If energy < 10 Then
                txtActionLog.AppendText("Insufficient energy to move!" & vbCrLf)
                txtActionLog.ScrollToCaret()
                Return
            End If
            Dim angles() As Single = {90, 45, 0, 315, 270, 225, 180, 135} ' 1=Up, 2=Up-Right, ..., 8=Up-Left
            enterpriseAngle = angles(direction - 1)  ' Set angle based on last move
            Dim rad As Single = enterpriseAngle * CSng(Math.PI) / 180.0F
            Dim dx As Integer = CInt(Math.Round(Math.Cos(rad)))
            Dim dy As Integer = CInt(Math.Round(-Math.Sin(rad)))
            enterpriseSectorX += dx
            enterpriseSectorY += dy
            ' Check if we need to move to a new quadrant
            Dim quadrantChanged As Boolean = False
            If enterpriseSectorX < 0 Then
                enterpriseSectorX = QuadSize - 1
                currentQuadX -= 1
                quadrantChanged = True
            ElseIf enterpriseSectorX >= QuadSize Then
                enterpriseSectorX = 0
                currentQuadX += 1
                quadrantChanged = True
            End If
            If enterpriseSectorY < 0 Then
                enterpriseSectorY = QuadSize - 1
                currentQuadY -= 1
                quadrantChanged = True
            ElseIf enterpriseSectorY >= QuadSize Then
                enterpriseSectorY = 0
                currentQuadY += 1
                quadrantChanged = True
            End If

            ' Check if new quadrant is valid
            If currentQuadX < 0 OrElse currentQuadX > 7 OrElse currentQuadY < 0 OrElse currentQuadY > 7 Then
                ' Move back to previous position if we hit galaxy edge
                currentQuadX = Math.Max(0, Math.Min(7, currentQuadX))
                currentQuadY = Math.Max(0, Math.Min(7, currentQuadY))
                enterpriseSectorX = Math.Max(0, Math.Min(QuadSize - 1, enterpriseSectorX))
                enterpriseSectorY = Math.Max(0, Math.Min(QuadSize - 1, enterpriseSectorY))
                txtActionLog.AppendText("Cannot move beyond the galaxy boundary!" & vbCrLf)
                txtActionLog.ScrollToCaret()
            Else
                ' Update enterprise position
                enterpriseX = enterpriseSectorX * SectorSize + SectorSize / 2
                enterpriseY = enterpriseSectorY * SectorSize + SectorSize / 2
                energy -= 10

                If quadrantChanged Then
                    txtActionLog.AppendText($"Entered quadrant {currentQuadX + 1},{currentQuadY + 1}" & vbCrLf)
                    txtActionLog.ScrollToCaret()
                    InitializeQuadrant()
                End If
                txtActionLog.AppendText($"Moved to sector {enterpriseSectorX + 1},{enterpriseSectorY + 1}" & vbCrLf)
                txtActionLog.ScrollToCaret()
            End If
            PerformAction()
        Catch ex As Exception
            txtActionLog.AppendText("Move error: " & ex.Message & vbCrLf)
            txtActionLog.ScrollToCaret()
        End Try
    End Sub

    Private Sub UpdateGalaxyMap()
        Try
            Dim map As String = "Galaxy Map:" & vbCrLf
            For y = 0 To 7
                For x = 0 To 7
                    Dim value As Integer = galaxy(x, y)
                    ' Current quadrant gets brackets
                    If x = currentQuadX AndAlso y = currentQuadY Then
                        ' Use actual counts from lists for current quadrant
                        value = (klingons.Count * 100) + (starbases.Count * 10) + stars.Count
                        galaxy(x, y) = value ' Update galaxy array with current counts
                        map &= "[" & value.ToString("D3") & "] "
                    Else
                        map &= " " & value.ToString("D3") & "  "
                    End If
                Next
                map &= vbCrLf
            Next
            lblGalaxyMap.Text = map
        Catch ex As Exception
            txtActionLog.AppendText("Map update error: " & ex.Message & vbCrLf)
            txtActionLog.ScrollToCaret()
        End Try
    End Sub

    Private Sub GamePanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        ' Draw sector grid
        Using gridPen As New Pen(Color.DimGray)
            For x = 0 To QuadSize
                g.DrawLine(gridPen, x * SectorSize, 0, x * SectorSize, ScreenHeight)
                g.DrawLine(gridPen, 0, x * SectorSize, ScreenWidth, x * SectorSize)
            Next
        End Using

        ' Draw Enterprise with rotation based on last move
        g.TranslateTransform(enterpriseX, enterpriseY) ' Move origin to ship center
        g.RotateTransform(enterpriseAngle) ' Rotate around the center
        g.TranslateTransform(-enterpriseX, -enterpriseY) ' Restore origin

        Dim saucerWidth As Single = ShipSize
        Dim saucerHeight As Single = ShipSize / 2
        g.FillEllipse(New SolidBrush(Color.LightBlue), CSng(enterpriseX - saucerWidth / 2), CSng(enterpriseY - saucerHeight / 2), CSng(saucerWidth), CSng(saucerHeight))
        Dim engHullX As Single = 0
        Dim engHullY As Single = saucerHeight / 2 + 5
        g.FillRectangle(New SolidBrush(Color.LightBlue), CSng(enterpriseX - 5), CSng(enterpriseY + engHullY), CSng(10), CSng(10))
        Dim nacelleOffsetX As Single = saucerWidth / 2 + 5
        Dim nacelleY As Single = saucerHeight / 2
        g.FillEllipse(New SolidBrush(Color.Red), CSng(enterpriseX - nacelleOffsetX - 10), CSng(enterpriseY + nacelleY), CSng(20), CSng(5))
        g.FillEllipse(New SolidBrush(Color.Red), CSng(enterpriseX + nacelleOffsetX - 10), CSng(enterpriseY + nacelleY), CSng(20), CSng(5))

        ' Reset transformation
        g.ResetTransform()

        ' Draw Klingons (only if not cloaked)
        Using redBrush As New SolidBrush(Color.DarkRed)
            For Each klingon In klingons
                If Not klingon.IsCloaked Then
                    g.FillRectangle(redBrush, CSng(klingon.X - KlingonSize / 2), CSng(klingon.Y - KlingonSize / 2), CSng(KlingonSize), CSng(KlingonSize))
                End If
            Next
        End Using

        ' Draw starbases
        Using greenBrush As New SolidBrush(Color.Green)
            For Each starbase In starbases
                g.FillRectangle(greenBrush, CSng(starbase.X - StarbaseSize / 2), CSng(starbase.Y - StarbaseSize / 2), CSng(StarbaseSize), CSng(StarbaseSize))
            Next
        End Using

        ' Draw stars
        Using yellowBrush As New SolidBrush(Color.Yellow)
            For Each star In stars
                g.FillEllipse(yellowBrush, CSng(star.X - StarSize / 2), CSng(star.Y - StarSize / 2), CSng(StarSize), CSng(StarSize))
            Next
        End Using
    End Sub

    ' Button handlers
    Private Sub BtnMoveUp_Click(sender As Object, e As EventArgs) Handles btnMoveUp.Click
        MoveEnterprise(1)
    End Sub

    Private Sub BtnMoveUpRight_Click(sender As Object, e As EventArgs) Handles btnMoveUpRight.Click
        MoveEnterprise(2)
    End Sub

    Private Sub BtnMoveRight_Click(sender As Object, e As EventArgs) Handles btnMoveRight.Click
        MoveEnterprise(3)
    End Sub

    Private Sub BtnMoveDownRight_Click(sender As Object, e As EventArgs) Handles btnMoveDownRight.Click
        MoveEnterprise(4)
    End Sub

    Private Sub BtnMoveDown_Click(sender As Object, e As EventArgs) Handles btnMoveDown.Click
        MoveEnterprise(5)
    End Sub

    Private Sub BtnMoveDownLeft_Click(sender As Object, e As EventArgs) Handles btnMoveDownLeft.Click
        MoveEnterprise(6)
    End Sub

    Private Sub BtnMoveLeft_Click(sender As Object, e As EventArgs) Handles btnMoveLeft.Click
        MoveEnterprise(7)
    End Sub

    Private Sub BtnMoveUpLeft_Click(sender As Object, e As EventArgs) Handles btnMoveUpLeft.Click
        MoveEnterprise(8)
    End Sub

    Private Sub BtnFirePhasers_Click(sender As Object, e As EventArgs) Handles btnFirePhasers.Click
        If damaged(1) Then
            txtActionLog.AppendText("Phasers damaged!" & vbCrLf)
            txtActionLog.ScrollToCaret()
            Return
        End If

        ' Check if there are any Klingons to shoot at
        If klingons.Count = 0 Then
            txtActionLog.AppendText("No Klingon ships in this quadrant!" & vbCrLf)
            txtActionLog.ScrollToCaret()
            Return
        End If

        Dim input As String = InputBox("Enter energy for phasers (1-" & energy.ToString() & "):" & vbCrLf &
                                     "Current Klingons in range: " & klingons.Count.ToString(), "Fire Phasers")
        Dim phaserEnergy As Integer

        If Integer.TryParse(input, phaserEnergy) AndAlso phaserEnergy > 0 AndAlso phaserEnergy <= energy Then
            energy -= phaserEnergy
            Dim totalEnergy As Integer = phaserEnergy
            Dim klingonsToRemove As New List(Of Klingon)
            Dim damageReport As String = "Phaser Report:" & vbCrLf

            ' Divide energy among klingons
            Dim energyPerKlingon As Integer = phaserEnergy \ klingons.Count

            For Each klingon In klingons
                ' Calculate distance-based damage
                Dim dx As Integer = Math.Abs(klingon.SectorX - enterpriseSectorX)
                Dim dy As Integer = Math.Abs(klingon.SectorY - enterpriseSectorY)
                Dim distance As Double = Math.Sqrt(dx * dx + dy * dy)
                If distance = 0 Then distance = 0.5  ' Minimum distance to prevent division by zero

                ' Damage falls off with square of distance
                Dim damage As Integer = CInt(energyPerKlingon / (distance * distance))
                damage = Math.Max(0, damage) ' Ensure non-negative damage

                ' Apply damage
                klingon.Energy -= damage
                damageReport &= $"Klingon at {klingon.SectorX + 1},{klingon.SectorY + 1}: {damage} damage"
                damageReport &= $" (remaining shield: {Math.Max(0, klingon.Energy)})" & vbCrLf

                ' Check if Klingon is destroyed
                If klingon.Energy <= 0 Then
                    klingonsToRemove.Add(klingon)
                    score += 100
                    totalKlingons -= 1
                    galaxy(currentQuadX, currentQuadY) -= 100
                    damageReport &= "*** Klingon ship destroyed! ***" & vbCrLf
                End If
            Next

            ' Remove destroyed Klingons
            For Each klingon In klingonsToRemove
                klingons.Remove(klingon)
            Next

            ' Add summary to log
            txtActionLog.AppendText($"Fired phasers: {phaserEnergy} energy, destroyed {klingonsToRemove.Count} Klingons" & vbCrLf)
            txtActionLog.ScrollToCaret()

            PerformAction()
        Else
            txtActionLog.AppendText($"Invalid energy amount! Must be between 1 and {energy}." & vbCrLf)
            txtActionLog.ScrollToCaret()
        End If
    End Sub

    Private Sub BtnFireTorpedo_Click(sender As Object, e As EventArgs) Handles btnFireTorpedo.Click
        If damaged(2) Then
            txtActionLog.AppendText("Torpedo tubes damaged!" & vbCrLf)
            txtActionLog.ScrollToCaret()
            Return
        End If
        If photonTorpedoes <= 0 Then
            txtActionLog.AppendText("No torpedoes remaining!" & vbCrLf)
            txtActionLog.ScrollToCaret()
            Return
        End If
        Dim sectorX As String = InputBox("Enter target sector X (1-8):")
        Dim sectorY As String = InputBox("Enter target sector Y (1-8):")
        Dim x, y As Integer
        If Integer.TryParse(sectorX, x) AndAlso Integer.TryParse(sectorY, y) AndAlso x >= 1 AndAlso x <= 8 AndAlso y >= 1 AndAlso y <= 8 Then
            photonTorpedoes -= 1
            x -= 1
            y -= 1
            For i As Integer = klingons.Count - 1 To 0 Step -1
                Dim klingon As Klingon = klingons(i)
                If klingon.SectorX = x AndAlso klingon.SectorY = y Then
                    klingons.RemoveAt(i)
                    score += 100
                    totalKlingons -= 1
                    ' Update galaxy map for current quadrant
                    galaxy(currentQuadX, currentQuadY) = klingons.Count * 100 + starbases.Count * 10 + stars.Count
                    txtActionLog.AppendText($"Torpedo hit Klingon at {x + 1},{y + 1}" & vbCrLf)
                    txtActionLog.ScrollToCaret()
                    PerformAction()
                    Return
                End If
            Next
            For i As Integer = starbases.Count - 1 To 0 Step -1
                Dim starbase As Starbase = starbases(i)
                If starbase.SectorX = x AndAlso starbase.SectorY = y Then
                    starbases.RemoveAt(i)
                    ' Update galaxy map for current quadrant
                    galaxy(currentQuadX, currentQuadY) = klingons.Count * 100 + (starbases.Count - 1) * 10 + stars.Count
                    txtActionLog.AppendText("Starbase destroyed! Court-martial pending." & vbCrLf)
                    txtActionLog.ScrollToCaret()
                    PerformAction()
                    Return
                End If
            Next
            For Each star In stars
                If star.SectorX = x AndAlso star.SectorY = y Then
                    txtActionLog.AppendText("Torpedo absorbed by star." & vbCrLf)
                    txtActionLog.ScrollToCaret()
                    PerformAction()
                    Return
                End If
            Next
            txtActionLog.AppendText($"Torpedo missed at {x + 1},{y + 1}" & vbCrLf)
            txtActionLog.ScrollToCaret()
            PerformAction()
        Else
            txtActionLog.AppendText("Invalid sector!" & vbCrLf)
            txtActionLog.ScrollToCaret()
        End If
    End Sub

    Private Sub BtnSRS_Click(sender As Object, e As EventArgs) Handles btnSRS.Click
        If damaged(3) Then
            txtActionLog.AppendText("Short-range sensors damaged!" & vbCrLf)
            txtActionLog.ScrollToCaret()
            Return
        End If
        Dim cloakedDetected As Boolean = False
        For Each klingon In klingons
            If klingon.IsCloaked Then
                cloakedDetected = True
                txtActionLog.AppendText($"Cloaked Klingon detected at sector {klingon.SectorX + 1},{klingon.SectorY + 1}!" & vbCrLf)
                txtActionLog.ScrollToCaret()
                ' Optionally decloak temporarily (e.g., for 1 turn) or permanently
                ' For simplicity, we'll decloak permanently here; adjust as needed
                klingon.IsCloaked = False
            End If
        Next
        If Not cloakedDetected Then
            txtActionLog.AppendText("No cloaked Klingons detected." & vbCrLf)
            txtActionLog.ScrollToCaret()
        End If
        energy -= 5
        gamePanel.Invalidate()
        PerformAction()
    End Sub

    Private Sub BtnLRS_Click(sender As Object, e As EventArgs) Handles btnLRS.Click
        If damaged(4) Then
            txtActionLog.AppendText("Long-range sensors damaged!" & vbCrLf)
            txtActionLog.ScrollToCaret()
            Return
        End If
        Dim lrs As String = "Long-Range Scan:" & vbCrLf
        For y = currentQuadY - 2 To currentQuadY + 2
            For x = currentQuadX - 2 To currentQuadX + 2
                If x >= 0 AndAlso x <= 7 AndAlso y >= 0 AndAlso y <= 7 Then
                    Dim value As Integer = galaxy(x, y)
                    If x = currentQuadX AndAlso y = currentQuadY Then
                        lrs &= "[" & value.ToString("D3") & "] "
                    Else
                        lrs &= " " & value.ToString("D3") & "  "
                    End If
                Else
                    lrs &= "  ***  " ' Out of bounds marker
                End If
            Next
            lrs &= vbCrLf
        Next
        energy -= 10
        lblLRS.Text = lrs
        PerformAction()
    End Sub

    Private Sub BtnShields_Click(sender As Object, e As EventArgs) Handles btnShields.Click
        If damaged(5) Then
            txtActionLog.AppendText("Shield control damaged!" & vbCrLf)
            txtActionLog.ScrollToCaret()
            Return
        End If
        Dim input As String = InputBox("Enter energy to allocate to shields:")
        Dim shieldEnergy As Integer
        If Integer.TryParse(input, shieldEnergy) AndAlso shieldEnergy >= 0 AndAlso shieldEnergy <= energy Then
            energy -= shieldEnergy
            shields += shieldEnergy
            PerformAction()
        Else
            txtActionLog.AppendText("Invalid energy amount!" & vbCrLf)
            txtActionLog.ScrollToCaret()
        End If
    End Sub

    Private Sub BtnDamage_Click(sender As Object, e As EventArgs) Handles btnDamage.Click
        Dim report As String = "Damage Report:" & vbCrLf
        Dim systems() As String = {"Warp Engines", "Phasers", "Torpedo Tubes", "SRS", "LRS", "Shields"}
        For i = 0 To 5
            report &= systems(i) & ": " & If(damaged(i), "Damaged", "Operational") & vbCrLf
        Next
        txtActionLog.AppendText(report & vbCrLf)
        txtActionLog.ScrollToCaret()
        PerformAction()
    End Sub

    Private Sub BtnWarpQuad_Click(sender As Object, e As EventArgs) Handles btnWarpQuad.Click
        If damaged(0) Then
            txtActionLog.AppendText("Warp engines damaged!" & vbCrLf)
            txtActionLog.ScrollToCaret()
            Return
        End If
        Dim quadX As String = InputBox("Enter quadrant X (1-8):")
        Dim quadY As String = InputBox("Enter quadrant Y (1-8):")
        Dim x, y As Integer
        If Integer.TryParse(quadX, x) AndAlso Integer.TryParse(quadY, y) AndAlso x >= 1 AndAlso x <= 8 AndAlso y >= 1 AndAlso y <= 8 Then
            x -= 1
            y -= 1
            Dim distance As Integer = Math.Abs(currentQuadX - x) + Math.Abs(currentQuadY - y)
            Dim energyCost As Integer = distance * 8
            If energyCost <= energy Then
                currentQuadX = x
                currentQuadY = y
                InitializeQuadrant() ' Refresh quadrant data
                enterpriseSectorX = 4
                enterpriseSectorY = 4
                enterpriseX = enterpriseSectorX * SectorSize + SectorSize / 2
                enterpriseY = enterpriseSectorY * SectorSize + SectorSize / 2
                energy -= energyCost
                txtActionLog.AppendText($"Warped to {currentQuadX + 1},{currentQuadY + 1}" & vbCrLf)
                txtActionLog.ScrollToCaret()
                PerformAction()
            Else
                txtActionLog.AppendText("Insufficient energy for warp!" & vbCrLf)
                txtActionLog.ScrollToCaret()
            End If
        Else
            txtActionLog.AppendText("Invalid quadrant!" & vbCrLf)
            txtActionLog.ScrollToCaret()
        End If
    End Sub

    Private Sub UpdateStatus()
        Try
            Me.Text = $"Star Trek 1971 - Score: {score} Energy: {energy} - Shields: {shields} - Torpedoes: {photonTorpedoes} {vbCrLf}Stardates: {stardates}"
            lblStatus.Text = $"Quad: {currentQuadX + 1},{currentQuadY + 1} - Sector: {enterpriseSectorX + 1},{enterpriseSectorY + 1} {vbCrLf}Bases: {starbases.Count} - Stars: {stars.Count}"
            If energy <= 0 Then
                isGameOver = True
                txtActionLog.AppendText("Game Over: Energy depleted!" & vbCrLf)
                txtActionLog.ScrollToCaret()
                MessageBox.Show($"Enterprise powerless and drifting in space!{vbCrLf}Final Score: {score}", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            txtActionLog.AppendText("Status update error: " & ex.Message & vbCrLf)
            txtActionLog.ScrollToCaret()
        End Try
    End Sub
End Class