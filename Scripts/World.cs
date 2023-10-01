using Godot;
using System;
using System.Linq;

public class World : Node
{
    private PlayerCharacter player;
    private int totalScore = 0;
    private int runScore = 0;
    private Label scoreLabel;

    [Export]
    public PackedScene[] packedScenes;
    Node2D currentLevel;
    private int currentLevelIndex = 0;

    public override void _Ready()
    {
        currentLevel = packedScenes[0].Instance<Node2D>();
        AddChild(currentLevel);

        Area2D exitArea = currentLevel.GetNode<Area2D>("ExitArea");
        player = currentLevel.GetNode<PlayerCharacter>("Player");
        scoreLabel = GetNode<CanvasLayer>("CanvasLayer").GetNode<Label>("ScoreLabel");
    }

    public override void _Process(float delta)
    {
        UpdatePlayerNoise();

        if (player.endOfLevelTriggered)
        {
            TriggerEndLevel();
        }
    }

    private void UpdatePlayerNoise()
    {
        if (player.isMakingNoise)
        {
            GetNode<Label>("CanvasLayer/NoiseLabel").Text = "Noise level: " + player.noiseShape.Radius / 16;
        }
    }

    public void TriggerEndLevel()
    {
        GD.Print("End of level triggered");

        player.endOfLevelTriggered = false;


        CalculateRunScore();
        CalculateTotalScore();
        DisplayEndOfLevelUI();

        LoadNextLevel();
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
}
