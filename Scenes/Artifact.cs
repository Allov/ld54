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

    public void OnBodyEntered(Area2D area)
    {
        if (area.CollisionLayer == 2)
        {
            if (!isCollected)
            {
                playerInRange = true;
                Collect();

            }
        }
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

    public void OnBodyExited(Area2D area)
    {
        if (area.CollisionLayer == 2)
        {
            playerInRange = false;
        }
    }

    public void PanicDropThisArtifact(Vector2 tilePosition)
    {
        Vector2 worldPos = tileMap.MapToWorld(tilePosition) + tileMap.CellSize * 0.5f; // 0.5 pour center l'objet (?)

        this.Position = worldPos;

        // On drop l'artifact à la position actuelle.
        this.Show();
        isCollected = false;

        GD.Print($"Artifact dropped at: {worldPos}");
    }
}

