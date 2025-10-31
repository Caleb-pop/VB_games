Imports System.Windows.Forms
Imports System.Drawing

Public Class AsteroidsForm
    ' Game constants
    Private Const ScreenWidth As Integer = 800
    Private Const ScreenHeight As Integer = 600
    Private Const ShipSize As Integer = 13
    Private Const AsteroidMinSize As Integer = 20
    Private Const AsteroidMaxSize As Integer = 40
    Private Const MaxAsteroids As Integer = 25
    Private Const BulletSpeed As Integer = 10
    Private Const RotationSpeed As Single = 5.0F
    Private Const ThrustSpeed As Single = 0.1F

    ' Player properties
    Private shipX As Single
    Private shipY As Single
    Private shipAngle As Single = 0.0F
    Private shipVelocityX As Single = 0.0F
    Private shipVelocityY As Single = 0.0F
    Private shipColor As Color = Color.White

    ' Asteroid properties
    Private asteroids As New List(Of Asteroid)()

    ' Bullet properties (both player and alien bullets use this)
    Private bullets As New List(Of Bullet)()
    Private lastShotTime As DateTime = DateTime.MinValue
    Private fireCooldown As Integer = 250 ' milliseconds
    Private lastRearShotTime As DateTime = DateTime.MinValue
    Private rearFireCooldown As Integer = 500 ' milliseconds

    ' Alien properties
    Private aliens As New List(Of SpaceAlien)()
    Private lastAlienShotTimes As New Dictionary(Of SpaceAlien, DateTime)()
    Private alienFireCooldown As Integer = 5000 ' ms between alien shots
    Private lastAlienDeathTime As DateTime = DateTime.MinValue
    Private alienRespawnDelay As Integer = 2000 ' 2 seconds in ms
    Private alienSpawned As Boolean = False ' Track initial alien spawn
    Private aliensKilled As Integer = 0 ' Track number of aliens killed

    ' Game state
    Private WithEvents gameTimer As New Timer()
    Private score As Integer = 0
    Private lives As Integer = 3
    Private isThrusting As Boolean = False
    Private isRotatingLeft As Boolean = False
    Private isRotatingRight As Boolean = False
    Private isFiring As Boolean = False
    Private isGameOver As Boolean = False

    ' Drawing surface
    Private gamePanel As New Panel()

    ' Asteroid class
    Private Class Asteroid
        Public X As Single
        Public Y As Single
        Public Size As Integer
        Public VelocityX As Single
        Public VelocityY As Single
        Public Angle As Single

        Public Sub New(x As Single, y As Single, size As Integer)
            Me.X = x
            Me.Y = y
            Me.Size = size
            Me.VelocityX = CSng(Rnd() * 2 - 1) ' Random velocity between -1 and 1
            Me.VelocityY = CSng(Rnd() * 2 - 1)
            Me.Angle = CSng(Rnd() * 360)
        End Sub
    End Class

    ' Alien class
    Private Class SpaceAlien
        Public X As Single
        Public Y As Single
        Public Size As Integer
        Public VelocityX As Single
        Public VelocityY As Single

        Public Sub New(x As Single, y As Single, size As Integer)
            Me.X = x
            Me.Y = y
            Me.Size = size * 1.62 ' Make alien larger than ship
            Me.VelocityX = CSng(Rnd() * 2 - 1)
            Me.VelocityY = CSng(Rnd() * 2 - 1)
        End Sub
    End Class

    ' Bullet class
    Private Class Bullet
        Public X As Single
        Public Y As Single
        Public VelocityX As Single
        Public VelocityY As Single
        Public Size As Integer
        Public Owner As String ' "player" or "alien"

        Public Sub New(x As Single, y As Single, angle As Single, Optional owner As String = "player", Optional size As Integer = 4)
            Me.X = x
            Me.Y = y
            Me.Size = size
            Me.Owner = owner
            Dim rad As Single = angle * CSng(Math.PI) / 180.0F
            Dim speed As Single = BulletSpeed
            Me.VelocityX = CSng(Math.Cos(rad) * speed)
            Me.VelocityY = CSng(-Math.Sin(rad) * speed)
        End Sub
    End Class

    Private Sub AsteroidsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize random seed
        Randomize()
        ' Set up the form
        Me.Text = "Asteroids 1979"
        Me.Size = New Size(ScreenWidth + 40, ScreenHeight + 60)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = True

        ' Set up game panel
        With gamePanel
            .Size = New Size(ScreenWidth, ScreenHeight)
            .Location = New Point(20, 20)
            .BackColor = Color.Black
        End With
        Me.Controls.Add(gamePanel)
        AddHandler gamePanel.Paint, AddressOf GamePanel_Paint

        ' Initialize ship position
        shipX = ScreenWidth / 2
        shipY = ScreenHeight / 2

        ' Initialize asteroids
        InitializeAsteroids()

        ' Set up game timer
        gameTimer.Interval = 16 ' ~60 FPS
        gameTimer.Start()
    End Sub

    Private Sub InitializeAsteroids()
        asteroids.Clear()
        For i As Integer = 0 To MaxAsteroids - 1
            Dim x As Single = CSng(Rnd() * ScreenWidth)
            Dim y As Single = CSng(Rnd() * ScreenHeight)
            Dim size As Integer = AsteroidMinSize + CInt(Rnd() * (AsteroidMaxSize - AsteroidMinSize))
            asteroids.Add(New Asteroid(x, y, size))
        Next

        ' Reset alien state for new level
        aliens.Clear()
        lastAlienShotTimes.Clear()
        lastAlienDeathTime = DateTime.MinValue
        alienSpawned = False
        aliensKilled = 0 ' Reset aliens killed
        isGameOver = False
        UpdateTitle()
    End Sub

    ' Game loop
    Private Sub GameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        If isGameOver Then
            gameTimer.Stop()
            Me.Close()
            Return
        End If

        ' Handle thrusting
        If isThrusting Then
            Dim rad As Single = shipAngle * CSng(Math.PI) / 180.0F
            shipVelocityX += CSng(Math.Cos(rad) * ThrustSpeed)
            shipVelocityY += CSng(-Math.Sin(rad) * ThrustSpeed)
        End If

        ' Handle rotation
        If isRotatingLeft Then shipAngle -= RotationSpeed
        If isRotatingRight Then shipAngle += RotationSpeed

        ' Update ship position with wraparound
        shipX += shipVelocityX
        shipY += shipVelocityY
        If shipX < 0 Then shipX += ScreenWidth
        If shipX > ScreenWidth Then shipX -= ScreenWidth
        If shipY < 0 Then shipY += ScreenHeight
        If shipY > ScreenHeight Then shipY -= ScreenHeight

        ' Slow down velocity (friction)
        shipVelocityX *= 0.98F
        shipVelocityY *= 0.98F

        ' Update bullets
        For i As Integer = bullets.Count - 1 To 0 Step -1
            Dim bullet As Bullet = bullets(i)
            bullet.X += bullet.VelocityX
            bullet.Y += bullet.VelocityY
            If bullet.X < -bullet.Size OrElse bullet.X > ScreenWidth + bullet.Size OrElse bullet.Y < -bullet.Size OrElse bullet.Y > ScreenHeight + bullet.Size Then
                bullets.RemoveAt(i)
            End If
        Next

        ' Update asteroids
        For Each asteroid In asteroids
            asteroid.X += asteroid.VelocityX
            asteroid.Y += asteroid.VelocityY
            If asteroid.X < 0 Then asteroid.X += ScreenWidth
            If asteroid.X > ScreenWidth Then asteroid.X -= ScreenWidth
            If asteroid.Y < 0 Then asteroid.Y += ScreenHeight
            If asteroid.Y > ScreenHeight Then asteroid.Y -= ScreenHeight
        Next

        ' Update aliens (if any)
        For Each alien In aliens
            alien.X += alien.VelocityX
            alien.Y += alien.VelocityY
            If alien.X < 0 Then alien.X += ScreenWidth
            If alien.X > ScreenWidth Then alien.X -= ScreenWidth
            If alien.Y < 0 Then alien.Y += ScreenHeight
            If alien.Y > ScreenHeight Then alien.Y -= ScreenHeight

            ' Alien firing at player or another alien
            If Not lastAlienShotTimes.ContainsKey(alien) Then
                lastAlienShotTimes(alien) = DateTime.MinValue
            End If
            If (DateTime.Now - lastAlienShotTimes(alien)).TotalMilliseconds >= alienFireCooldown Then
                ShootAlienBullet(alien)
                lastAlienShotTimes(alien) = DateTime.Now
            End If
        Next

        ' Determine max aliens based on asteroid count
        Dim maxAliens As Integer = If(asteroids.Count > 0, 1, 2)

        ' Respawn aliens if needed (only if asteroids remain and initial alien has spawned)
        If asteroids.Count > 0 AndAlso alienSpawned AndAlso aliens.Count < maxAliens AndAlso (DateTime.Now - lastAlienDeathTime).TotalMilliseconds >= alienRespawnDelay Then
            SpawnAlien(1) ' Spawn one alien to replace the one killed
        End If

        ' Handle rear cannon firing
        If isFiring AndAlso aliensKilled >= 15 AndAlso (DateTime.Now - lastRearShotTime).TotalMilliseconds >= rearFireCooldown Then
            ShootRearBullet()
            lastRearShotTime = DateTime.Now
        End If

        ' Check collisions
        CheckCollisions()

        ' Handle firing (player)
        If isFiring AndAlso (DateTime.Now - lastShotTime).TotalMilliseconds >= fireCooldown Then
            ShootBullet()
            lastShotTime = DateTime.Now
        End If

        ' Redraw the panel
        gamePanel.Invalidate()
    End Sub

    Private Sub CheckCollisions()
        If isGameOver Then Return

        ' Ship-asteroid collision
        For Each asteroid In asteroids
            Dim shipRect As New RectangleF(CSng(shipX - ShipSize / 2), CSng(shipY - ShipSize / 2), CSng(ShipSize), CSng(ShipSize))
            Dim asteroidRect As New RectangleF(CSng(asteroid.X - asteroid.Size / 2), CSng(asteroid.Y - asteroid.Size / 2), CSng(asteroid.Size), CSng(asteroid.Size))
            If shipRect.IntersectsWith(asteroidRect) Then
                lives -= 1
                UpdateTitle()
                If lives <= 0 Then
                    gameTimer.Stop()
                    MessageBox.Show("Game Over! Score: " & score)
                    isGameOver = True
                Else
                    ResetShip()
                End If
                Return
            End If
        Next

        ' Bullet-asteroid, Bullet-alien, and AlienBullet-Ship collisions
        For i As Integer = bullets.Count - 1 To 0 Step -1
            Dim bullet As Bullet = bullets(i)

            ' If it's a player's bullet, it can hit asteroids and aliens
            If bullet.Owner = "player" Then
                Dim bulletRect As New RectangleF(CSng(bullet.X - bullet.Size / 2), CSng(bullet.Y - bullet.Size / 2), CSng(bullet.Size), CSng(bullet.Size))

                ' Check asteroid collisions
                For j As Integer = asteroids.Count - 1 To 0 Step -1
                    Dim asteroid As Asteroid = asteroids(j)
                    Dim asteroidRect As New RectangleF(CSng(asteroid.X - asteroid.Size / 2), CSng(asteroid.Y - asteroid.Size / 2), CSng(asteroid.Size), CSng(asteroid.Size))
                    If bulletRect.IntersectsWith(asteroidRect) Then
                        bullets.RemoveAt(i)
                        asteroids.RemoveAt(j)
                        score += asteroid.Size * 10

                        ' Spawn two smaller asteroids if size allows
                        If asteroids.Count < MaxAsteroids Then
                            Dim newX As Single = asteroid.X
                            Dim newY As Single = asteroid.Y
                            Dim newSize As Integer = asteroid.Size \ 2
                            If newSize >= AsteroidMinSize Then
                                asteroids.Add(New Asteroid(newX, newY, newSize))
                                asteroids.Add(New Asteroid(newX, newY, newSize))
                            End If
                        End If
                        Exit For
                    End If
                Next

                ' Check alien collisions if bullet still exists
                If i < bullets.Count AndAlso bullet.Owner = "player" Then
                    For j As Integer = aliens.Count - 1 To 0 Step -1
                        Dim alien As SpaceAlien = aliens(j)
                        Dim alienRect As New RectangleF(CSng(alien.X - alien.Size / 2), CSng(alien.Y - alien.Size / 2), CSng(alien.Size), CSng(alien.Size))
                        If bulletRect.IntersectsWith(alienRect) Then
                            bullets.RemoveAt(i)
                            aliens.RemoveAt(j)
                            lastAlienShotTimes.Remove(alien)
                            lastAlienDeathTime = DateTime.Now ' Record time of alien death
                            aliensKilled += 1 ' Increment aliens killed
                            score += 1000
                            Exit For
                        End If
                    Next
                End If
            ElseIf bullet.Owner = "alien" Then
                ' Alien bullets hit the player
                Dim shipRect As New RectangleF(CSng(shipX - ShipSize / 2), CSng(shipY - ShipSize / 2), CSng(ShipSize), CSng(ShipSize))
                Dim bulletRect As New RectangleF(CSng(bullet.X - bullet.Size / 2), CSng(bullet.Y - bullet.Size / 2), CSng(bullet.Size), CSng(bullet.Size))
                If shipRect.IntersectsWith(bulletRect) Then
                    lives -= 1
                    bullets.RemoveAt(i)
                    UpdateTitle()
                    If lives <= 0 Then
                        gameTimer.Stop()
                        MessageBox.Show("Game Over! Hit by alien! Score: " & score)
                        isGameOver = True
                    Else
                        ResetShip()
                    End If
                    Return
                End If
            End If
        Next

        ' Spawn one alien when half of asteroids are cleared
        If Not alienSpawned AndAlso asteroids.Count <= MaxAsteroids \ 2 AndAlso asteroids.Count > 0 AndAlso aliens.Count = 0 Then
            SpawnAlien(1)
            alienSpawned = True
        End If

        ' Spawn two aliens when all asteroids are cleared
        If asteroids.Count = 0 AndAlso aliens.Count = 0 AndAlso alienSpawned Then
            SpawnAlien(2)
            alienFireCooldown = CSng((Rnd() * 3000)) + 2000 ' Randomize alien fire rate between 2-5 seconds
        End If

        ' Win condition: all asteroids and aliens cleared
        If (asteroids.Count = 0 AndAlso aliens.Count = 0) Or aliensKilled = 21 Then
            gameTimer.Stop()
            MessageBox.Show("Aliens Purged: Mission Complete! Score: " & score & " | Aliens Killed: " & aliensKilled)
            Me.Close() ' End game
            ' Optionally, reset for a new game below
            'InitializeAsteroids()
            'ResetShip()
            'gameTimer.Start()
            Return
        End If
    End Sub

    Private Sub ShootBullet()
        If aliensKilled >= 6 Then
            ' Dual cannons: Fire two bullets at slight angles
            Dim bullet1 As New Bullet(shipX, shipY, shipAngle - 4, "player", 4)
            Dim bullet2 As New Bullet(shipX, shipY, shipAngle + 4, "player", 4)
            bullets.Add(bullet1)
            bullets.Add(bullet2)
        Else
            ' Single cannon
            Dim bullet As New Bullet(shipX, shipY, shipAngle, "player", 4)
            bullets.Add(bullet)
        End If
    End Sub

    Private Sub ShootRearBullet()
        ' Rear cannon: Fire a bullet 180 degrees from ship angle
        Dim rearAngle As Single = (shipAngle + 180) Mod 360
        Dim bullet As New Bullet(shipX, shipY, rearAngle, "player", 4)
        bullets.Add(bullet)
    End Sub

    Private Sub ShootAlienBullet(alien As SpaceAlien)
        If alien Is Nothing Then Return
        Dim targets As New List(Of Object)()
        targets.Add(New With {.Type = "player", .X = shipX, .Y = shipY})
        ' Only add other aliens if there are more than one alien
        If aliens.Count > 1 Then
            For Each otherAlien In aliens
                If otherAlien IsNot alien Then
                    targets.Add(New With {.Type = "alien", .X = otherAlien.X, .Y = otherAlien.Y})
                End If
            Next
        End If
        If targets.Count = 0 Then Return
        Dim target = targets(CInt(Rnd() * (targets.Count - 1))) ' Randomly select a target
        Dim dx As Single = target.X - alien.X
        Dim dy As Single = target.Y - alien.Y
        Dim angleRad As Single = CSng(Math.Atan2(-dy, dx))
        Dim angleDeg As Single = angleRad * 180.0F / CSng(Math.PI)
        Dim blastSize As Integer = 2 * ShipSize
        Dim b As New Bullet(alien.X, alien.Y, angleDeg, "alien", blastSize)
        bullets.Add(b)
    End Sub

    Private Sub SpawnAlien(count As Integer)
        For i As Integer = 1 To count
            ' Determine max aliens based on asteroid count
            Dim maxAliens As Integer = If(asteroids.Count > 0, 1, 2)
            If aliens.Count >= maxAliens Then Exit For
            Dim spawnX As Single
            Dim spawnY As Single
            Dim attempts As Integer = 0
            Dim validPosition As Boolean
            Do
                spawnX = CSng(Rnd() * ScreenWidth)
                spawnY = CSng(Rnd() * ScreenHeight)
                attempts += 1
                validPosition = True
                ' Check distance from player
                Dim dx As Single = spawnX - shipX
                Dim dy As Single = spawnY - shipY
                If (dx * dx + dy * dy) <= CSng((ShipSize + 100) * (ShipSize + 100)) Then
                    validPosition = False
                End If
                ' Check distance from other aliens
                For Each existingAlien In aliens
                    dx = spawnX - existingAlien.X
                    dy = spawnY - existingAlien.Y
                    If (dx * dx + dy * dy) <= CSng((ShipSize + 100) * (ShipSize + 100)) Then
                        validPosition = False
                    End If
                Next
                If validPosition OrElse attempts > 20 Then Exit Do
            Loop
            Dim newAlien As New SpaceAlien(spawnX, spawnY, CInt(ShipSize * 1.5F))
            aliens.Add(newAlien)
            lastAlienShotTimes(newAlien) = DateTime.Now
        Next
    End Sub

    Private Sub ResetShip()
        shipX = ScreenWidth / 2
        shipY = ScreenHeight / 2
        shipVelocityX = 0
        shipVelocityY = 0
        shipAngle = 0.0F
    End Sub

    ' Draw the game
    Private Sub GamePanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        ' Draw ship as an isosceles triangle (base a = 5, height h = 8), scaled by ShipSize
        Dim a As Single = 5.0F
        Dim h As Single = 8.0F
        Dim scale As Single = CSng(ShipSize) / 5.0F
        Dim baseWidth As Single = a * scale
        Dim height As Single = h * scale
        Dim tipLocalX As Single = 2.0F * height / 3.0F
        Dim baseLocalX As Single = -height / 3.0F
        Dim halfBase As Single = baseWidth / 2.0F

        Dim rad As Single = shipAngle * CSng(Math.PI) / 180.0F
        Dim cosR As Single = CSng(Math.Cos(rad))
        Dim sinR As Single = CSng(Math.Sin(rad))

        Dim Transform As Func(Of Single, Single, PointF) = Function(lx As Single, ly As Single) As PointF
                                                               Dim X As Single = shipX + (cosR * lx - sinR * ly)
                                                               Dim Y As Single = shipY - (sinR * lx + cosR * ly)
                                                               Return New PointF(X, Y)
                                                           End Function

        Dim shipPoints(2) As PointF
        shipPoints(0) = Transform(tipLocalX, 0.0F)                  ' Tip
        shipPoints(1) = Transform(baseLocalX, halfBase)           ' Base top
        shipPoints(2) = Transform(baseLocalX, -halfBase)          ' Base bottom

        Using shipBrush As New SolidBrush(shipColor)
            g.FillPolygon(shipBrush, shipPoints)
        End Using

        ' Draw thrust (flame) behind the base if thrusting
        If isThrusting Then
            Dim rearLocalX As Single = baseLocalX - (height * 0.35F)
            Dim flameHalf As Single = baseWidth / 6.0F
            Dim flamePoints(2) As PointF
            flamePoints(0) = Transform(rearLocalX - (height * 0.2F), 0.0F)  ' Flame tip (rear-most)
            flamePoints(1) = Transform(baseLocalX + (height * 0.05F), flameHalf) ' Upper near base
            flamePoints(2) = Transform(baseLocalX + (height * 0.05F), -flameHalf) ' Lower near base

            Using flameBrush As New SolidBrush(Color.Orange)
                g.FillPolygon(flameBrush, flamePoints)
            End Using
        End If

        ' Draw asteroids
        Using grayBrush As New SolidBrush(Color.Gray)
            For Each asteroid In asteroids
                g.FillRectangle(grayBrush, CSng(asteroid.X - asteroid.Size / 2), CSng(asteroid.Y - asteroid.Size / 2), CSng(asteroid.Size), CSng(asteroid.Size))
            Next
        End Using

        ' Draw aliens (if any)
        For Each alien In aliens
            Using ufoBodyBrush As New SolidBrush(Color.Red) ' Main body color
                ' Draw larger, flattened ellipse for UFO body
                g.FillEllipse(ufoBodyBrush, CSng(alien.X - alien.Size / 2), CSng(alien.Y - alien.Size / 4), CSng(alien.Size), CSng(alien.Size / 2))
            End Using
            Using ufoCockpitBrush As New SolidBrush(Color.Silver) ' Cockpit color
                ' Draw smaller, taller ellipse for UFO cockpit on top
                Dim cockpitWidth As Single = alien.Size * 0.5F
                Dim cockpitHeight As Single = alien.Size * 0.4F
                g.FillEllipse(ufoCockpitBrush, CSng(alien.X - cockpitWidth / 2), CSng(alien.Y - alien.Size / 2 - cockpitHeight / 4), cockpitWidth, cockpitHeight)
            End Using
        Next

        ' Draw bullets
        For Each bullet In bullets
            If bullet.Owner = "player" Then
                Using b As New SolidBrush(Color.Yellow)
                    g.FillRectangle(b, CSng(bullet.X - bullet.Size / 2), CSng(bullet.Y - bullet.Size / 2), CSng(bullet.Size), CSng(bullet.Size))
                End Using
            Else
                Using b As New SolidBrush(Color.OrangeRed)
                    g.FillEllipse(b, CSng(bullet.X - bullet.Size / 2), CSng(bullet.Y - bullet.Size / 2), CSng(bullet.Size), CSng(bullet.Size))
                End Using
            End If
        Next

        ' Draw score, lives, and aliens killed
        Using scoreBrush As New SolidBrush(Color.White)
            g.DrawString($"Score: {score}  Lives: {lives}  Aliens Killed: {aliensKilled}", New Font("Arial", 12), scoreBrush, 10, 10)
        End Using
    End Sub

    ' Handle key presses (down)
    Private Sub AsteroidsForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.Right
                isRotatingLeft = True
            Case Keys.Left
                isRotatingRight = True
            Case Keys.Up
                isThrusting = True
            Case Keys.Space
                isFiring = True
        End Select
    End Sub

    ' Handle key releases (up)
    Private Sub AsteroidsForm_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Select Case e.KeyCode
            Case Keys.Right
                isRotatingLeft = False
            Case Keys.Left
                isRotatingRight = False
            Case Keys.Up
                isThrusting = False
            Case Keys.Space
                isFiring = False
        End Select
    End Sub

    ' Update title with score, lives, and aliens killed
    Private Sub UpdateTitle()
        Me.Text = $"Asteroids 1979 - Score: {score} Lives: {lives} Aliens Killed: {aliensKilled}"
    End Sub
End Class