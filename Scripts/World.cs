using Godot;
using System;
using System.Linq;

public class World : Node
{
    private PlayerCharacter player;
    private int totalScore = 0;
    private int runScore = 0;
    private Label scoreLabel;

    public override void _Ready()
    {
        Area2D exitArea = GetNode<Area2D>("ExitArea");
        player = GetNode<PlayerCharacter>("Player");
        scoreLabel = GetNode<Label>("ScoreLabel");
    }

    public void TriggerEndLevel()
    {
        CalculateRunScore();
        CalculateTotalScore();
        DisplayEndOfLevelUI();

    }

    private void CalculateRunScore()
    {
        // On calcule pour le niveau actuel seulement
        runScore = player.collectedArtifacts.Sum(artifact => artifact.artifactValue);
    }

    private void CalculateTotalScore()
    {
        // Ici, le cumulatif de tous les niveaux
        totalScore += runScore;
    }

    private void DisplayEndOfLevelUI()
    {
        scoreLabel.Text = $"Total Score: {totalScore}";

        // Logique pour le UI de la fin d'un niveau
        GD.Print("Level Ended!");
    }

    private void OnExitAreaBodyEntered(Node body)
    {
        if (body == player)
        {
            GD.Print("Player enters exit zone");
            player.nearExitZone = true;
        }
    }

    private void OnExitAreaBodyExited(Node body)
    {
        if (body == player)
        {
            GD.Print("Player leaving exit zone");
            player.nearExitZone = false;
        }
    }

}
