using Godot;
using System;
using System.Collections.Generic;
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
    private Timer gameTimer; // Reference to our Timer node
    private Label timerLabel; // Reference to our Timer label

    Dictionary<string, bool> busMuteStates = new Dictionary<string, bool>
    {
        { "Music", false },
        { "SFX", false }
    };

    public override void _Ready()
    {

        HSlider musicVolumeSlider = GetNode<HSlider>("OptionMenu/MusicSlider");
        musicVolumeSlider.Connect("value_changed", this, "OnMusicVolumeSliderValueChanged");


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

        gameTimer = new Timer();
        this.AddChild(gameTimer);
        gameTimer.WaitTime = 240;
        gameTimer.OneShot = true;
        gameTimer.Connect("timeout", this, "OnGameTimerTimeout");
        timerLabel = GetNode<Label>("CanvasLayer/Timer");

        StartGameTimer();
    }

    public override void _Process(float delta)
    {
        if (player.artifactScoreToCount)
        {
            CalculateArtifactsScore();
            UpdateTotalScoreUI();
            player.artifactScoreToCount = false;
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

        float totalSeconds = gameTimer.TimeLeft;
        float minutes = totalSeconds / 60;
        float seconds = totalSeconds % 60;

        string formattedTime = $"{(int)minutes}m{(int)seconds}s";

        timerLabel.Text = $"Time: {formattedTime}";
    }

    private void OnGameTimerTimeout()
    {
        player.isDead = true;
        DisplayGameOver();
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
        scoreLabel.Show();
        StartGameTimer();
    }

    private void StartGameTimer()
    {
        gameTimer.Stop();
        gameTimer.Start();
    }

    private void DisplayGameOver()
    {
        gameOverScoreLabel.Text = "Score: " + totalScore.ToString();
        gameOverCanvas.Show();
        scoreLabel.Hide();
    }

    private void OnRestartButtonPressed()
    {
        totalScore = 0;
        runScore = 0;
        UpdateTotalScoreUI();  // Add this line to update the score UI
        gameOverCanvas.Hide();
        currentLevelIndex = -1;
        LoadNextLevel();
    }

    public void ToggleMuteBus(string busName)
    {
        if (busMuteStates.ContainsKey(busName))
        {
            busMuteStates[busName] = !busMuteStates[busName];
            AudioServer.SetBusMute(AudioServer.GetBusIndex(busName), busMuteStates[busName]);
        }
    }

    public void OnMusicVolumeSliderValueChanged(float percentageValue)
    {
        // Map le pourcentage (0-100) en dB (-80 à 6)
        float dBVolume = MapPercentageToDb(percentageValue);

        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), dBVolume);
    }

    private float MapPercentageToDb(float percentage)
    {
        float minValue = -80.0f;
        float maxValue = 6.0f;

        return (1 - percentage / 100.0f) * minValue + (percentage / 100.0f) * maxValue;
    }
}
