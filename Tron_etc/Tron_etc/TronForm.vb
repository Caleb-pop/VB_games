Imports System.Windows.Forms
Imports System.Drawing

Public Class TronForm
    ' Game constants
    Private Const GridWidth As Integer = 60
    Private Const GridHeight As Integer = 40
    Private Const CellSize As Integer = 10

    ' Grid to track occupied cells (trails): 0=empty, 1=player, 2=ai
    Private occupied(GridWidth - 1, GridHeight - 1) As Integer

    ' Player properties
    Private playerX As Integer
    Private playerY As Integer
    Private playerDir As Direction = Direction.Right  ' Initial direction
    Private playerColor As Color = Color.Blue
    Private playerTrailColor As Color = Color.LightBlue

    ' AI opponent properties
    Private aiX As Integer
    Private aiY As Integer
    Private aiDir As Direction = Direction.Left
    Private aiColor As Color = Color.Red
    Private aiTrailColor As Color = Color.Orange

    ' Learning variables
    Private playerTurnCount As Integer = 0
    Private playerLeftTurns As Integer = 0
    Private playerRightTurns As Integer = 0
    Private playerMoveCount As Integer = 0
    Private learningEnabled As Boolean = False
    Private learnedTurnFrequency As Double = 0.05
    Private learnedLeftPreference As Double = 0.5  ' 0 = always right, 1 = always left
    Private lastPlayerDir As Direction = Direction.Right

    ' Score variables
    Private playerWins As Integer = 0
    Private aiWins As Integer = 0
    Private Const RoundsToWin As Integer = 5

    ' Random for AI decisions
    Private rnd As New Random()

    ' Game timer
    Private WithEvents gameTimer As New Timer()

    ' Direction enum
    Private Enum Direction
        Up
        Down
        Left
        Right
    End Enum

    ' Game panel for drawing
    Private gamePanel As New Panel()

    Private Sub TronForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Set up the form
        Me.Text = "Tron Light Cycle Game"
        Me.Size = New Size(GridWidth * CellSize + 40, GridHeight * CellSize + 60)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = True

        ' Set up game panel
        With gamePanel
            .Size = New Size(GridWidth * CellSize, GridHeight * CellSize)
            .Location = New Point(20, 20)
            .BackColor = Color.Black
        End With
        Me.Controls.Add(gamePanel)
        AddHandler gamePanel.Paint, AddressOf GamePanel_Paint

        ' Initialize scores
        playerWins = 0
        aiWins = 0

        ' Initialize game
        ResetGame()

        ' Set up timer
        gameTimer.Interval = 100  ' Speed of movement
        gameTimer.Start()
    End Sub

    ' Handle form closing to stop timer
    Private Sub TronForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        gameTimer.Stop()
    End Sub

    ' Reset game state for a new round
    Private Sub ResetGame()
        ' Clear occupied grid
        For x As Integer = 0 To GridWidth - 1
            For y As Integer = 0 To GridHeight - 1
                occupied(x, y) = 0
            Next
        Next

        ' Player starts on the left
        playerX = 10
        playerY = GridHeight \ 2
        playerDir = Direction.Right
        lastPlayerDir = Direction.Right
        occupied(playerX, playerY) = 1

        ' AI starts on the right
        aiX = GridWidth - 10
        aiY = GridHeight \ 2
        aiDir = Direction.Left
        occupied(aiX, aiY) = 2

        ' Reset per-round stats
        playerTurnCount = 0
        playerLeftTurns = 0
        playerRightTurns = 0
        playerMoveCount = 0

        ' Update title with score
        UpdateTitle()
    End Sub

    ' Game loop
    Private Sub GameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        ' Track player moves
        playerMoveCount += 1

        ' Move player
        MoveCycle(playerX, playerY, playerDir)
        If CheckCollision(playerX, playerY) Then
            gameTimer.Stop()
            aiWins += 1
            UpdateTitle()
            If aiWins >= RoundsToWin Then
                MessageBox.Show("AI Wins the Match!")
                Me.Close()
            Else
                MessageBox.Show("Player crashed! AI wins the round. Score: Player " & playerWins & " - AI " & aiWins)
                ResetGame()
                gameTimer.Start()
            End If
            Return
        End If
        occupied(playerX, playerY) = 1

        ' Move AI
        AiMove()
        If CheckCollision(aiX, aiY) Then
            gameTimer.Stop()
            playerWins += 1
            UpdateTitle()
            If playerWins >= RoundsToWin Then
                MessageBox.Show("Player Wins the Match!")
                Me.Close()
            Else
                MessageBox.Show("AI crashed! Player wins the round. Score: Player " & playerWins & " - AI " & aiWins)
                ResetGame()
                gameTimer.Start()
            End If
            Return
        End If
        occupied(aiX, aiY) = 2

        ' Redraw the panel
        gamePanel.Invalidate()
    End Sub

    ' Move a cycle in its direction
    Private Sub MoveCycle(ByRef x As Integer, ByRef y As Integer, dir As Direction)
        Select Case dir
            Case Direction.Up
                y -= 1
            Case Direction.Down
                y += 1
            Case Direction.Left
                x -= 1
            Case Direction.Right
                x += 1
        End Select
    End Sub

    ' Check if position is collision (out of bounds or occupied)
    Private Function CheckCollision(x As Integer, y As Integer) As Boolean
        If x < 0 OrElse x >= GridWidth OrElse y < 0 OrElse y >= GridHeight OrElse occupied(x, y) <> 0 Then
            Return True
        End If
        Return False
    End Function

    ' AI movement with learning
    Private Sub AiMove()
        ' Check next position in current direction
        Dim nextX As Integer = aiX
        Dim nextY As Integer = aiY
        MoveCycle(nextX, nextY, aiDir)

        Dim distanceToWall As Integer = GetDistanceToWall(aiX, aiY, aiDir)

        ' Decide if to turn
        Dim shouldTurn As Boolean = CheckCollision(nextX, nextY) OrElse distanceToWall < 3

        If learningEnabled Then
            shouldTurn = shouldTurn OrElse (rnd.NextDouble() < learnedTurnFrequency)
        End If

        If shouldTurn Then
            ' Get possible turns
            Dim possibleTurns As New List(Of Direction)

            ' Left turn
            Dim leftDir As Direction = GetTurn(aiDir, True)
            Dim leftX As Integer = aiX
            Dim leftY As Integer = aiY
            MoveCycle(leftX, leftY, leftDir)
            If Not CheckCollision(leftX, leftY) Then
                possibleTurns.Add(leftDir)
            End If

            ' Right turn
            Dim rightDir As Direction = GetTurn(aiDir, False)
            Dim rightX As Integer = aiX
            Dim rightY As Integer = aiY
            MoveCycle(rightX, rightY, rightDir)
            If Not CheckCollision(rightX, rightY) Then
                possibleTurns.Add(rightDir)
            End If

            ' Choose turn based on learned preference
            If possibleTurns.Count > 0 Then
                ' Sort or select based on preference
                If possibleTurns.Count = 2 Then
                    ' Both possible, choose left with probability learnedLeftPreference
                    If rnd.NextDouble() < learnedLeftPreference Then
                        aiDir = If(GetTurn(aiDir, True) = possibleTurns(0), possibleTurns(0), possibleTurns(1))
                    Else
                        aiDir = If(GetTurn(aiDir, False) = possibleTurns(0), possibleTurns(0), possibleTurns(1))
                    End If
                Else
                    ' Only one possible, take it
                    aiDir = possibleTurns(0)
                End If
            End If
        End If

        ' Move forward
        MoveCycle(aiX, aiY, aiDir)
    End Sub

    ' Get distance to wall or trail in current direction
    Private Function GetDistanceToWall(x As Integer, y As Integer, dir As Direction) As Integer
        Dim distance As Integer = 0
        Dim checkX As Integer = x
        Dim checkY As Integer = y

        While True
            MoveCycle(checkX, checkY, dir)
            If CheckCollision(checkX, checkY) Then
                Exit While
            End If
            distance += 1
        End While

        Return distance
    End Function

    ' Get new direction after turn (left=true, right=false)
    Private Function GetTurn(currentDir As Direction, turnLeft As Boolean) As Direction
        Select Case currentDir
            Case Direction.Up
                Return If(turnLeft, Direction.Left, Direction.Right)
            Case Direction.Down
                Return If(turnLeft, Direction.Right, Direction.Left)
            Case Direction.Left
                Return If(turnLeft, Direction.Down, Direction.Up)
            Case Direction.Right
                Return If(turnLeft, Direction.Up, Direction.Down)
        End Select
        Return currentDir
    End Function

    ' Draw the game
    Private Sub GamePanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        ' Draw trails (occupied cells)
        For x As Integer = 0 To GridWidth - 1
            For y As Integer = 0 To GridHeight - 1
                If occupied(x, y) = 1 Then
                    g.FillRectangle(New SolidBrush(playerTrailColor), x * CellSize, y * CellSize, CellSize, CellSize)
                ElseIf occupied(x, y) = 2 Then
                    g.FillRectangle(New SolidBrush(aiTrailColor), x * CellSize, y * CellSize, CellSize, CellSize)
                End If
            Next
        Next

        ' Draw player cycle (if in bounds)
        If playerX >= 0 AndAlso playerX < GridWidth AndAlso playerY >= 0 AndAlso playerY < GridHeight Then
            g.FillRectangle(New SolidBrush(playerColor), playerX * CellSize, playerY * CellSize, CellSize, CellSize)
        End If

        ' Draw AI cycle (if in bounds)
        If aiX >= 0 AndAlso aiX < GridWidth AndAlso aiY >= 0 AndAlso aiY < GridHeight Then
            g.FillRectangle(New SolidBrush(aiColor), aiX * CellSize, aiY * CellSize, CellSize, CellSize)
        End If
    End Sub

    ' Handle key presses to change player direction
    Private Sub TronForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Dim newDir As Direction = playerDir

        Select Case e.KeyCode
            Case Keys.Up
                If playerDir <> Direction.Down Then newDir = Direction.Up
            Case Keys.Down
                If playerDir <> Direction.Up Then newDir = Direction.Down
            Case Keys.Left
                If playerDir <> Direction.Right Then newDir = Direction.Left
            Case Keys.Right
                If playerDir <> Direction.Left Then newDir = Direction.Right
        End Select

        ' Track turns
        If newDir <> playerDir Then
            playerTurnCount += 1
            ' Determine if left or right turn
            Dim turnLeft As Boolean = (GetTurn(playerDir, True) = newDir)
            If turnLeft Then
                playerLeftTurns += 1
            Else
                playerRightTurns += 1
            End If
            playerDir = newDir
            lastPlayerDir = newDir
        End If
    End Sub

    ' Update title with score
    Private Sub UpdateTitle()
        Me.Text = "Tron Light Cycle Game - Player: " & playerWins & " AI: " & aiWins
    End Sub
End Class