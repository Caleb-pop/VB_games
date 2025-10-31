Imports System.Windows.Forms

Public Class SpaceInvadersForm
    ' Player properties
    Private player As PictureBox
    Private playerSpeed As Integer = 10
    Private bullets As New List(Of PictureBox)()
    Private bulletSpeed As Integer = 15

    ' Enemy properties
    Private enemies As New List(Of PictureBox)()
    Private enemySpeed As Integer = 5
    Private moveDirection As Integer = 1 ' 1 for right, -1 for left

    Private WithEvents gameTimer As New Timer()
    Private score As Integer = 0

    ' Key flags for movement
    Private isLeftPressed As Boolean = False
    Private isRightPressed As Boolean = False

    ' Fire rate control
    Private lastShotTime As DateTime = DateTime.MinValue
    Private fireCooldown As Integer = 500 ' milliseconds (0.5 seconds)

    Private Sub SpaceInvadersForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Set up the form
        Me.Text = "Space Invaders"
        Me.Size = New Size(800, 600)
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = True

        ' Create player
        player = New PictureBox()
        With player
            .Size = New Size(40, 40)
            .Location = New Point(Me.ClientSize.Width \ 2 - 20, Me.ClientSize.Height - 60)
            .BackColor = Color.Green
            .Tag = "player"
        End With
        Me.Controls.Add(player)

        ' Create enemies (3x5 grid)
        For row As Integer = 0 To 2
            For col As Integer = 0 To 4
                Dim enemy As New PictureBox()
                With enemy
                    .Size = New Size(40, 40)
                    .Location = New Point(100 + col * 50, 50 + row * 50)
                    .BackColor = Color.Red
                    .Tag = "enemy"
                End With
                enemies.Add(enemy)
                Me.Controls.Add(enemy)
            Next
        Next

        ' Set up game timer
        gameTimer.Interval = 30 ' ~33 FPS
        gameTimer.Start()
    End Sub

    ' Game loop
    Private Sub GameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        ' Move player based on key flags
        If isLeftPressed AndAlso player.Left > 0 Then player.Left -= playerSpeed
        If isRightPressed AndAlso player.Right < Me.ClientSize.Width Then player.Left += playerSpeed

        ' Move bullets
        For i As Integer = bullets.Count - 1 To 0 Step -1
            Dim bullet As PictureBox = bullets(i)
            If bullet IsNot Nothing Then
                bullet.Top -= bulletSpeed
                If bullet.Top < 0 Then
                    Me.Controls.Remove(bullet)
                    bullets.RemoveAt(i)
                End If
            Else
                bullets.RemoveAt(i)
            End If
        Next

        ' Move enemies
        Dim validEnemies = enemies.Where(Function(item) item IsNot Nothing AndAlso Not item.IsDisposed).ToList()
        If validEnemies.Any() Then
            ' Always move horizontally first
            For Each enemy In validEnemies
                enemy.Left += enemySpeed * moveDirection
            Next

            ' Then check if hit edge after move
            Dim maxRight As Integer = validEnemies.Max(Function(enemy) enemy.Right)
            Dim minLeft As Integer = validEnemies.Min(Function(enemy) enemy.Left)

            If maxRight > Me.ClientSize.Width Or minLeft < 0 Then
                ' Reverse direction and drop down
                moveDirection = -moveDirection
                For Each enemy In validEnemies
                    enemy.Top += 40
                Next
            End If
        End If

        ' Check collisions
        For i As Integer = bullets.Count - 1 To 0 Step -1
            Dim bullet As PictureBox = bullets(i)
            If bullet IsNot Nothing Then
                For j As Integer = enemies.Count - 1 To 0 Step -1
                    Dim enemy As PictureBox = TryCast(enemies(j), PictureBox)
                    If enemy IsNot Nothing AndAlso bullet.Bounds.IntersectsWith(enemy.Bounds) Then
                        Me.Controls.Remove(bullet)
                        bullets.RemoveAt(i)
                        Me.Controls.Remove(enemy)
                        enemies.RemoveAt(j)
                        score += 10
                        Exit For
                    End If
                Next
            End If
        Next

        ' Update score (display in title bar)
        Me.Text = $"Space Invaders - Score: {score}"

        ' Check game over
        If Not validEnemies.Any() Then
            gameTimer.Stop()
            MessageBox.Show("You Win! Score: " & score)
            Me.Close()
        ElseIf validEnemies.Any(Function(enemy) enemy.Top > player.Top) Then
            gameTimer.Stop()
            MessageBox.Show("Game Over! Score: " & score)
            Me.Close()
        End If
    End Sub

    ' Handle key presses (down)
    Private Sub SpaceInvadersForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.Left
                isLeftPressed = True
            Case Keys.Right
                isRightPressed = True
            Case Keys.Space
                AttemptToShoot()
        End Select
    End Sub

    ' Handle key releases (up)
    Private Sub SpaceInvadersForm_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Select Case e.KeyCode
            Case Keys.Left
                isLeftPressed = False
            Case Keys.Right
                isRightPressed = False
        End Select
    End Sub

    Private Sub AttemptToShoot()
        ' Check if enough time has passed since the last shot
        If (DateTime.Now - lastShotTime).TotalMilliseconds >= fireCooldown Then
            ShootBullet()
            lastShotTime = DateTime.Now
        End If
    End Sub

    Private Sub ShootBullet()
        Dim bullet As New PictureBox()
        With bullet
            .Size = New Size(5, 20)
            .Location = New Point(player.Left + player.Width \ 2 - 2, player.Top - 20)
            .BackColor = Color.Yellow
            .Tag = "bullet"
        End With
        bullets.Add(bullet)
        Me.Controls.Add(bullet)
    End Sub
End Class