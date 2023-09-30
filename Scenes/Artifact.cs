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
    [Export]
    public NodePath tileMapNodePath;
    private bool isCollected = false;
    private bool playerInRange = false;

    public override void _Ready()
    {
        tileMap = GetNode<TileMap>(tileMapNodePath);
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
}

