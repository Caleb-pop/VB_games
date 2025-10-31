Imports System.Windows.Forms

Public Class BreakoutForm
    ' Paddle properties
    Private paddle As PictureBox
    Private paddleSpeed As Integer = 10

    ' Ball properties
    Private ball As PictureBox
    Private ballSpeedX As Double
    Private ballSpeedY As Double
    Private initialSpeed As Integer = 5

    ' Brick properties
    Private bricks As New List(Of PictureBox)()
    Private brickRows As Integer = 5
    Private brickCols As Integer = 10
    Private brickWidth As Integer = 60
    Private brickHeight As Integer = 20

    ' Game state
    Private WithEvents gameTimer As New Timer()
    Private score As Integer = 0
    Private lives As Integer = 3
    Private currentLevel As Integer = 1
    Private maxLevels As Integer = 3
    Private goldenRatio As Double = (1 + Math.Sqrt(5)) / 2
    Private isGameRunning As Boolean = False

    ' Key flags for movement
    Private isLeftPressed As Boolean = False
    Private isRightPressed As Boolean = False

    Private Sub BreakoutForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Set up the form
        Me.Text = "Breakout"
        Me.Size = New Size(800, 600)
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = True

        ' Create paddle
        paddle = New PictureBox()
        With paddle
            .Size = New Size(100, 20)
            .Location = New Point(Me.ClientSize.Width \ 2 - 50, Me.ClientSize.Height - 40)
            .BackColor = Color.Blue
            .Tag = "paddle"
        End With
        Me.Controls.Add(paddle)

        ' Create ball
        ball = New PictureBox()
        With ball
            .Size = New Size(20, 20)
            .Location = New Point(Me.ClientSize.Width \ 2 - 10, Me.ClientSize.Height - 60)
            .BackColor = Color.White
            .Tag = "ball"
        End With
        Me.Controls.Add(ball)

        ' Initialize speeds for level 1
        ResetBallSpeed()

        ' Create initial bricks
        ResetBricks()

        ' Set up game timer
        gameTimer.Interval = 20  ' Faster for smoother movement
        gameTimer.Start()

        ' Display initial score and lives
        UpdateTitle()
    End Sub

    ' Game loop
    Private Sub GameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        If Not isGameRunning Then
            Return
        End If

        ' Move paddle
        If isLeftPressed AndAlso paddle.Left > 0 Then paddle.Left -= paddleSpeed
        If isRightPressed AndAlso paddle.Right < Me.ClientSize.Width Then paddle.Left += paddleSpeed

        ' Move ball
        ball.Left += ballSpeedX
        ball.Top += ballSpeedY

        ' Ball wall collisions
        If ball.Left <= 0 OrElse ball.Right >= Me.ClientSize.Width Then
            ballSpeedX = -ballSpeedX  ' Bounce horizontally
        End If
        If ball.Top <= 0 Then
            ballSpeedY = -ballSpeedY  ' Bounce vertically (top)
        End If

        ' Ball falls below paddle (lose life)
        If ball.Bottom >= Me.ClientSize.Height Then
            lives -= 1
            isGameRunning = False  ' Pause game
            UpdateTitle()
            If lives <= 0 Then
                gameTimer.Stop()
                MessageBox.Show("Game Over! Score: " & score)
                Me.Close()
                Return
            End If
            ' Reset ball position and speed
            ResetBallPosition()
            ResetBallSpeed()
            Return  ' Prevent further processing this tick
        End If

        ' Ball-paddle collision
        If ball.Bounds.IntersectsWith(paddle.Bounds) Then
            ballSpeedY = -Math.Abs(ballSpeedY)  ' Bounce up
            ' Adjust X speed based on hit position for variety
            Dim hitPos As Integer = (ball.Left + ball.Width / 2) - paddle.Left
            ballSpeedX = (hitPos - paddle.Width / 2) / 10
        End If

        ' Ball-brick collisions
        For i As Integer = bricks.Count - 1 To 0 Step -1
            If ball.Bounds.IntersectsWith(bricks(i).Bounds) Then
                Me.Controls.Remove(bricks(i))
                bricks.RemoveAt(i)
                score += 10
                ballSpeedY = -ballSpeedY  ' Bounce vertically
                UpdateTitle()
                Exit For  ' Only hit one brick per tick
            End If
        Next

        ' Check level complete
        If bricks.Count = 0 Then
            If currentLevel < maxLevels Then
                currentLevel += 1
                ResetBricks()
                ResetBallSpeed()
                ResetBallPosition()
                isGameRunning = False  ' Pause for next level
                UpdateTitle()
            Else
                gameTimer.Stop()
                MessageBox.Show("You Win! Score: " & score)
                Me.Close()
            End If
        End If
    End Sub

    ' Handle key presses (down)
    Private Sub BreakoutForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.Left
                isLeftPressed = True
            Case Keys.Right
                isRightPressed = True
            Case Keys.Space
                If Not isGameRunning AndAlso lives > 0 Then
                    isGameRunning = True
                    UpdateTitle()
                End If
        End Select
    End Sub

    ' Handle key releases (up)
    Private Sub BreakoutForm_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Select Case e.KeyCode
            Case Keys.Left
                isLeftPressed = False
            Case Keys.Right
                isRightPressed = False
        End Select
    End Sub

    ' Reset bricks for the current level
    Private Sub ResetBricks()
        For Each brick In bricks
            Me.Controls.Remove(brick)
        Next
        bricks.Clear()

        ' Create bricks with level-specific colors
        For row As Integer = 0 To brickRows - 1
            For col As Integer = 0 To brickCols - 1
                Dim brick As New PictureBox()
                With brick
                    .Size = New Size(brickWidth, brickHeight)
                    .Location = New Point(50 + col * (brickWidth + 5), 50 + row * (brickHeight + 5))
                    .Tag = "brick"
                    ' Set color based on level
                    If currentLevel = 2 Then
                        .BackColor = Color.Yellow  ' Yellow for level 2
                    Else
                        Dim redValue As Integer = 255 - row * 50 - (currentLevel - 1) * 50
                        .BackColor = Color.FromArgb(Math.Max(50, redValue), 0, 0)  ' Minimum red value of 50
                    End If
                End With
                bricks.Add(brick)
                Me.Controls.Add(brick)
            Next
        Next
    End Sub

    ' Reset ball position
    Private Sub ResetBallPosition()
        ball.Location = New Point(Me.ClientSize.Width \ 2 - 10, Me.ClientSize.Height - 60)
    End Sub

    ' Reset ball speed based on current level
    Private Sub ResetBallSpeed()
        Dim factor As Double = Math.Pow(goldenRatio, currentLevel - 1)
        ballSpeedX = initialSpeed * factor
        ballSpeedY = -initialSpeed * factor
    End Sub

    ' Update title with level, score, and lives
    Private Sub UpdateTitle()
        Dim status As String = If(isGameRunning, "", " - Press Space to Start/Continue")
        Me.Text = $"Breakout - Level: {currentLevel} | Score: {score} | Lives: {lives}{status}"
    End Sub
End Class