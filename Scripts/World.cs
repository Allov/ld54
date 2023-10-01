using Godot;
using System;
using System.Linq;

public class World : Node
{
    private PlayerCharacter player;
    private int totalScore = 0;
    private int runScore = 0;
    private Label scoreLabel;

    private CanvasLayer gameOverCanvas;

    [Export]
    public PackedScene[] packedScenes;
    Node2D currentLevel;
    private int currentLevelIndex = 0;

    private Label gameOverScoreLabel;
    private Button restartButton;

    public override void _Ready()
    {
        gameOverCanvas = GetNode<CanvasLayer>("GameOverCanvas");
        gameOverScoreLabel = GetNode<Label>("GameOverCanvas/CenterContainer/VBoxContainer/GameOverScoreLabel");
        restartButton = GetNode<Button>("GameOverCanvas/CenterContainer/VBoxContainer/RestartButton");
        restartButton.Connect("pressed", this, "OnRestartButtonPressed");
        gameOverCanvas.Hide();

        currentLevel = packedScenes[0].Instance<Node2D>();
        AddChild(currentLevel);

        Area2D exitArea = currentLevel.GetNode<Area2D>("ExitArea");
        player = currentLevel.GetNode<PlayerCharacter>("Player");
        scoreLabel = GetNode<CanvasLayer>("CanvasLayer").GetNode<Label>("ScoreLabel");
    }

    public override void _Process(float delta)
    {
        if (player.artifactScoreToCount)
        {
            CalculateArtifactsScore();
            UpdateTotalScoreUI();
            player.artifactScoreToCount = false;  // Reset the flag
        }

        UpdatePlayerNoise();

        if (player.endOfLevelTriggered)
        {
            TriggerEndLevel();
        }

        if (player.isDead)
        {
            // player should be 'fixed' and not being able to move --> géré dans player class
            DisplayGameOver();
        }
    }

    private void UpdatePlayerNoise()
    {
        if (player.isMakingNoise)
        {
            GetNode<Label>("CanvasLayer/NoiseLabel").Text = "Noise level: " + player.noiseShape.Radius / 16;
        }
    }

    private void UpdateTotalScoreUI()
    {
        GetNode<Label>("CanvasLayer/ScoreLabel").Text = "Score: " + totalScore.ToString();
    }

    public void TriggerEndLevel()
    {
        GD.Print("End of level triggered");

        player.endOfLevelTriggered = false;

        LoadNextLevel();
    }

        private void CalculateArtifactsScore()
    {
        foreach (Artifact artifact in player.collectedArtifacts)
        {
            if (!artifact.isScoreCounted)
            {
                totalScore += artifact.artifactValue;
                artifact.isScoreCounted = true; 
            }
        }
    }
    public void AddToTotalScore(int value)
    {
        totalScore += value;
        UpdateTotalScoreUI();
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

    private void LoadNextLevel()
    {
        // Liberer le niveau actuel de la memoire
        currentLevel.QueueFree();

        // Passer au niveau suivant
        currentLevelIndex++;
        if (currentLevelIndex >= packedScenes.Length)
        {
            // Gerer ce qui se passe après le dernier niveau
            // Pour l'instant, on retourne simplement au premier niveau. Ou un victory screen?
            currentLevelIndex = 0;
        }

        // Instancier et charger le nouveau niveau
        currentLevel = packedScenes[currentLevelIndex].Instance<Node2D>();
        AddChild(currentLevel);

        // Reconnecter les nodes necessaires du prochain niveau
        Area2D exitArea = currentLevel.GetNode<Area2D>("ExitArea");
        player = currentLevel.GetNode<PlayerCharacter>("Player");
    }

    private void DisplayGameOver()
    {
        gameOverScoreLabel.Text = "Score: " + totalScore.ToString();
        gameOverCanvas.Show();
    }

    private void OnRestartButtonPressed()
    {
        // todo
        totalScore = 0;
        runScore = 0;
        gameOverCanvas.Hide();
        currentLevelIndex = -1;
        LoadNextLevel();
    }
}
