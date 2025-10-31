Imports System.Windows.Forms

Public Class Form1
    Private Sub MainMenu_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "VB Games Menu"
        Me.Size = New Size(400, 500)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen

        ' Button for Space Invaders
        Dim btnSpaceInvaders As New Button()
        With btnSpaceInvaders
            .Text = "Play Space Invaders"
            .Size = New Size(200, 50)
            .Location = New Point(100, 50)
            AddHandler .Click, Sub()
                                   Dim si As New SpaceInvadersForm()
                                   si.ShowDialog()
                               End Sub
        End With
        Me.Controls.Add(btnSpaceInvaders)

        ' Button for Breakout
        Dim btnBreakout As New Button()
        With btnBreakout
            .Text = "Play Breakout"
            .Size = New Size(200, 50)
            .Location = New Point(100, 120)
            AddHandler .Click, Sub()
                                   Dim bo As New BreakoutForm()
                                   bo.ShowDialog()
                               End Sub
        End With
        Me.Controls.Add(btnBreakout)

        ' Button for Tron
        Dim btnTron As New Button()
        With btnTron
            .Text = "Play Tron"
            .Size = New Size(200, 50)
            .Location = New Point(100, 190)
            AddHandler .Click, Sub()
                                   Dim tr As New TronForm()
                                   tr.ShowDialog()
                               End Sub
        End With
        Me.Controls.Add(btnTron)

        ' Button for Asteroids
        Dim btnAsteroids As New Button()
        With btnAsteroids
            .Text = "Play Asteroids"
            .Size = New Size(200, 50)
            .Location = New Point(100, 260)
            AddHandler .Click, Sub()
                                   Dim ast As New AsteroidsForm()
                                   ast.ShowDialog()
                               End Sub
        End With
        Me.Controls.Add(btnAsteroids)

        ' Button for Star Trek 1971
        Dim btnStarTrek As New Button()
        With btnStarTrek
            .Text = "Play Star Trek"
            .Size = New Size(200, 50)
            .Location = New Point(100, 330)
            AddHandler .Click, Sub()
                                   Dim st As New StarTrek1971Form()
                                   st.ShowDialog()
                               End Sub
        End With
        Me.Controls.Add(btnStarTrek)

        ' button for Moonlander
        Dim btnMoonlander As New Button()
        With btnMoonlander
            .Text = "Play Moonlander"
            .Size = New Size(200, 50)
            .Location = New Point(100, 400)
            AddHandler .Click, Sub()
                                   Dim ml As New MoonlanderForm()
                                   ml.ShowDialog()
                               End Sub
        End With
        Me.Controls.Add(btnMoonlander)
    End Sub
End Class