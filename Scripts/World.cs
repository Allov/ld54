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
    private bool gameOverDisplayed = false;

    private CanvasLayer victoryScreenCanvas;

    private Label victoryScoreLabel;


    [Export]
    public PackedScene[] packedScenes;
    Node2D currentLevel;
    private int currentLevelIndex = 0;

    private Label gameOverScoreLabel;
    private Button restartButton;
    private Button victoryRestartButton;
    private Timer gameTimer; // Reference to our Timer node
    private Label timerLabel; // Reference to our Timer label

    Dictionary<string, bool> busMuteStates = new Dictionary<string, bool>
    {
        { "Music", false },
        { "SFX", false }
    };

    private CanvasLayer optionMenu;
    private Button optionButton;
    private Button xButton;
    private HSlider sfxSlider;
    private HSlider musicSlider;

    private AudioStreamPlayer mainMusicPlayer;
    private AudioStreamPlayer victoryMusicPlayer;
    private AudioStreamPlayer gameOverPlayer;

    private bool isOptionMenuOpen = false;



    public override void _Ready()
    {
        optionMenu = GetNode<CanvasLayer>("OptionMenu");
        optionButton = GetNode<Button>("CanvasLayer/OptionButton");
        xButton = GetNode<Button>("OptionMenu/XButton");
        sfxSlider = GetNode<HSlider>("OptionMenu/SFXSlider");
        musicSlider = GetNode<HSlider>("OptionMenu/MusicSlider");

        mainMusicPlayer = GetNode<AudioStreamPlayer>("MainMusicPlayer");
        victoryMusicPlayer = GetNode<AudioStreamPlayer>("VictoryMusicPlayer");
        gameOverPlayer = GetNode<AudioStreamPlayer>("GameOverPlayer");

        mainMusicPlayer.Play();

        // Connect signals
        optionButton.Connect("pressed", this, "ToggleOptionMenu");
        xButton.Connect("pressed", this, "CloseOptionMenu");

        sfxSlider.Connect("value_changed", this, "OnSFXSliderValueChanged");
        musicSlider.Connect("value_changed", this, "OnMusicVolumeSliderValueChanged");

        // Initialize sliders
        sfxSlider.Value = 100;
        musicSlider.Value = 100;


        gameOverCanvas = GetNode<CanvasLayer>("GameOverCanvas");
        gameOverScoreLabel = GetNode<Label>("GameOverCanvas/CenterContainer/VBoxContainer/GameOverScoreLabel");
        restartButton = GetNode<Button>("GameOverCanvas/CenterContainer/VBoxContainer/RestartButton");
        restartButton.Connect("pressed", this, "OnRestartButtonPressed");
        gameOverCanvas.Hide();

        victoryScreenCanvas = GetNode<CanvasLayer>("VictoryScreen");
        victoryScoreLabel = GetNode<Label>("VictoryScreen/CenterContainer/VBoxContainer/VictoryScoreLabel");
        victoryRestartButton = GetNode<Button>("VictoryScreen/CenterContainer/VBoxContainer/RestartButton");
        victoryRestartButton.Connect("pressed", this, "OnRestartButtonPressed");

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

        if (player.isDead && !gameOverDisplayed)
        {
            DisplayGameOver();
            gameOverDisplayed = true;
        }

        float totalSeconds = gameTimer.TimeLeft;
        float minutes = totalSeconds / 60;
        float seconds = totalSeconds % 60;

        string formattedTime = $"{(int)minutes}m{(int)seconds}s";

        timerLabel.Text = $"Time: {formattedTime}";


        // Pour tester Victory screen
        // if (Input.IsKeyPressed((int)KeyList.V))
        // {
        //     DisplayVictoryScreen();
        // }
    }

    public void ToggleOptionMenu()
    {
        if (!isOptionMenuOpen)
        {
            isOptionMenuOpen = true;
            optionMenu.Visible = true;
            GetTree().Paused = true;
        }
        else
        {
            isOptionMenuOpen = false;
            optionMenu.Visible = false;
            GetTree().Paused = false;
        }
    }

    public void CloseOptionMenu()
    {

        isOptionMenuOpen = false;
        optionMenu.Visible = false;
        GetTree().Paused = false;
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

    public void OnSFXSliderValueChanged(float percentageValue)
    {
        // Map the percentage (0-100) to dB (-80 to 6)
        float dBVolume = MapPercentageToDb(percentageValue);

        // Set the volume of the SFX bus using the mapped value
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), dBVolume);
    }


    private float MapPercentageToDb(float percentage)
    {
        float linearValue = percentage / 100.0f; // Convert to 0-1 scale
        return GD.Linear2Db(linearValue);
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
            // Gérer ce qui se passe après le dernier niveau
            DisplayVictoryScreen();
            return;
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

    private void DisplayVictoryScreen()
    {

        GetTree().Paused = true;

        victoryScoreLabel.Text = "Score: " + totalScore.ToString();
        victoryScreenCanvas.Show();
        scoreLabel.Hide();

        mainMusicPlayer.Stop();
        victoryMusicPlayer.Play();
        gameOverPlayer.Stop();
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

        mainMusicPlayer.Stop();
        victoryMusicPlayer.Stop();
        gameOverPlayer.Play();
    }

    private void OnRestartButtonPressed()
    {
        totalScore = 0;
        runScore = 0;
        UpdateTotalScoreUI();
        gameOverCanvas.Hide();
        victoryScreenCanvas.Hide();
        currentLevelIndex = -1;
        LoadNextLevel();

        mainMusicPlayer.Play();
        victoryMusicPlayer.Stop();
        gameOverPlayer.Stop();

        GetTree().Paused = false;
    }
}
