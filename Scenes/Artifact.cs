using Godot;
using System;

public class Artifact : Area2D
{
    public enum ArtifactRarity { Common, Rare, Legendary }
    [Export]
    public ArtifactRarity artifactType = ArtifactRarity.Common;
    [Export]
    public int artifactValue = 100; // On a un score? Surement... Argent / cash?
    private TileMap tileMap;
    private bool isCollected = false;
    private bool playerInRange = false;

    public bool isScoreCounted = false;
    
    [Export]
    public PackedScene ArtifactShapeScene;
    public TileMap ArtifactShape;

    public override void _Ready()
    {
        ArtifactShape = GetNode<TileMap>("ArtifactShape");
    }

    public override void _Process(float delta)
    {
        GetNode<CollisionShape2D>("StaticBody2D/CollisionShape2D").Disabled = isCollected;
    }

    public void Collect()
    {
        GD.Print("Debug: artifact collection process");

        if (!isCollected)
        {
            // Gerer la collecte de l'Artifact. Devrait trigger l'ouverture du UI du sac.
            GD.Print("Artifact collected!");

            // Je considere que l'objet est dans le sac, donc on le cache sur la map
            this.Hide();

            isCollected = true;
            // Todo: on a un système de scoring? Si oui, mettre à jour ici.
        }
    }

    public void PanicDropThisArtifact(Vector2 playerPosition)
    {
        this.Position = playerPosition;
        // On drop l'artifact à la position actuelle.
        this.Show();
        isCollected = false;
        GD.Print($"Artifact dropped at: {playerPosition}");
    }

    public void _on_Artifact_body_entered(KinematicBody2D body)
    {
        if (!isCollected && body is PlayerCharacter player)
        {
            player.nearArtifact = true;
            player.nearestArtifact = this;
        }
    }

    public void _on_Artifact_body_exited(KinematicBody2D body)
    {
        if (!isCollected && body is PlayerCharacter player)
        {
            player.nearArtifact = false;
            player.nearestArtifact = null;
        }
    }
}

