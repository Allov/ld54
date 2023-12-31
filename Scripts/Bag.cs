using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bag : Node2D
{
    [Export]
    public int TileSize = 16;

    [Export]
    public NodePath InventoryGridNode;
    private TileMap InventoryGrid;
    public Tile[,] Tiles;
    private Vector2 OffsetGridCell;
    private Vector2 OffsetGridPosition;
    public TileMap CurrentArtifactShape;
    private bool Placed;
    [Export]
    public Vector2 BoardSize = new Vector2(8, 8);


    [Export]
    public List<PackedScene> ArtifactScenes;
    public List<TileMap> Artifacts = new List<TileMap>();
    private Tween Tween;

    [Export]
    public float SnapPositionSpeed = 0.128f;
    private bool CanPlace;
    private bool Replacing;
    public bool CollectingArtifact;

    public Vector2 OriginalPosition { get; private set; }

    public event EventHandler OnPlacedArtifact;
    public event EventHandler OnDropArtifact;
    public event EventHandler OnClosedBag;

    public override void _Ready()
    {
        InventoryGrid = GetNode<TileMap>(InventoryGridNode);

        foreach (var artifactScene in ArtifactScenes)
        {
            Artifacts.Add(artifactScene.Instance() as TileMap);
        }

        Tiles = new Tile[(int)BoardSize.x, (int)BoardSize.y];

        for (var x = 0; x < ((int)BoardSize.x); x++)
        {
            for (var y = 0; y < ((int)BoardSize.y); y++)
            {
                Tiles[x, y] = new Tile
                {
                    HasShape = false
                };
            }
        }

        // this is our (0,0) for the inventory grid.
        OffsetGridCell = new Vector2(8, 3);
        OffsetGridPosition = InventoryGrid.MapToWorld(OffsetGridCell);

        // CurrentArtifactShape = Artifacts[0];

        // AddChild(CurrentArtifactShape);

        Tween = new Tween();
        AddChild(Tween);
    }

    public override void _Process(float delta)
    {
		if (!Visible) return;

        // Input.MouseMode = Visible ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
        if (CurrentArtifactShape != null && Input.IsActionJustPressed("rotate_artifact"))
        {
            RotateCurrentShape();
        }

        SnapShapeToBoard();

        if (CurrentArtifactShape != null && Input.IsActionJustPressed("place_artifact"))
        {
            var shapeSize = GetShapeSize(CurrentArtifactShape);
            var placementPosition = GetGlobalMousePosition() - shapeSize;
            if (IsValidShapePlacement(CurrentArtifactShape, placementPosition))
            {
                GD.Print("Placing position: ", placementPosition - OffsetGridPosition);

                PlaceShape(CurrentArtifactShape, placementPosition - OffsetGridPosition);
                CurrentArtifactShape = null;
                Placed = true;
            }
        }
        else if (Visible && CurrentArtifactShape == null && Input.IsActionJustPressed("place_artifact"))
        {
            PickupShape();
        }

        if (Visible && Input.IsActionJustPressed("ui_cancel"))
        {
            if (CurrentArtifactShape != null)
                CurrentArtifactShape.Modulate = Colors.White;


            if (CollectingArtifact && Placed)
            {
                CollectingArtifact = false;
                Placed = false;
                OnPlacedArtifact?.Invoke(this, EventArgs.Empty);
                Visible = false;
	        }
            else if (Replacing && CurrentArtifactShape != null)
            {
                Replacing = false;
                PlaceShape(CurrentArtifactShape, OriginalPosition);
				CurrentArtifactShape.GlobalPosition = OriginalPosition + OffsetGridPosition;

                GD.Print("Replacing cancel: ", OriginalPosition, CurrentArtifactShape.GlobalPosition);

                CurrentArtifactShape = null;
            }
            else
            {
                CurrentArtifactShape = null;
                OnDropArtifact?.Invoke(this, EventArgs.Empty);
	            Visible = false;
				Replacing = false;
				Placed = false;
            }


            OnClosedBag?.Invoke(this, EventArgs.Empty);
        }
    }

    private void PickupShape()
    {
        if (CurrentArtifactShape != null) return;

        Replacing = false;

        var mouseGlobalPosition = GetGlobalMousePosition();
        var localPosition = InventoryGrid.ToLocal(mouseGlobalPosition);
        var cellPosition = InventoryGrid.WorldToMap(localPosition);

        cellPosition = cellPosition - OffsetGridCell;

        if (cellPosition.x < 0 || cellPosition.x > BoardSize.x - 1) return;
        if (cellPosition.y < 0 || cellPosition.y > BoardSize.y - 1) return;

        var selectedTile = Tiles[(int)cellPosition.x, (int)cellPosition.y];

        var selectedShape = selectedTile.Shape;

        if (selectedShape == null)
        {
            return;
        }

        OriginalPosition = selectedShape.GlobalPosition - OffsetGridPosition;
        GD.Print("Replacing start: ", OriginalPosition);
        Replacing = true;

        for (var x = 0; x < BoardSize.x; x++)
        {
            for (var y = 0; y < BoardSize.y; y++)
            {
                var tile = Tiles[x, y];

                if (tile.Shape == selectedShape)
                {
                    tile.HasShape = false;
                    tile.Shape = null;
                }
            }
        }

        CurrentArtifactShape = selectedShape;
    }

    public void PlaceShape(TileMap shape, Vector2 globalPosition)
    {
        var localPosition = InventoryGrid.ToLocal(globalPosition);
        var cellPosition = new Vector2(Mathf.Round(localPosition.x / TileSize), Mathf.Round(localPosition.y / TileSize));
        var cells = shape.GetUsedCells();
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = (Vector2)cells[i] + cellPosition;
            Tiles[(int)cell.x, (int)cell.y] = new Tile { Shape = shape, HasShape = true };
        }
    }

    public Vector2 GetCellSnapPosition(Vector2 globalPosition)
    {
        var snapCell = new Vector2(-1, -1);

        if (IsOnBoard(InventoryGrid.ToLocal(globalPosition)))
        {
            var localPosition = InventoryGrid.ToLocal(globalPosition);
            snapCell = new Vector2(Mathf.Round(localPosition.x / TileSize) * TileSize, Mathf.Round(localPosition.y / TileSize) * TileSize);
        }

        return ToGlobal(snapCell);
    }

    private bool IsOnBoard(Vector2 localPosition)
    {
        return localPosition.x >= OffsetGridPosition.x && localPosition.x <= (BoardSize.x - 1) * TileSize + OffsetGridPosition.x &&
                localPosition.y >= OffsetGridPosition.y && localPosition.y <= (BoardSize.y - 1) * TileSize + OffsetGridPosition.y;
    }

    private void SnapShapeToBoard()
    {
        if (CurrentArtifactShape == null) return;

        var mousePosition = GetGlobalMousePosition();

        var shapeSize = GetShapeSize(CurrentArtifactShape);

        var snapPosition = GetCellSnapPosition(mousePosition - shapeSize);
        if (IsValidShapePlacement(CurrentArtifactShape, snapPosition))
        {
            CurrentArtifactShape.Modulate = Colors.White;
			CurrentArtifactShape.GlobalPosition = snapPosition;

            // Tween.InterpolateProperty(CurrentArtifactShape, "global_position", null, snapPosition, SnapPositionSpeed, Tween.TransitionType.Quad, Tween.EaseType.Out);
            // Tween.Start();
            CanPlace = true;
        }
        else
        {
            CurrentArtifactShape.Modulate = Colors.Red;
            CurrentArtifactShape.GlobalPosition = mousePosition - shapeSize;
            CanPlace = false;
        }
    }

    public bool IsValidShapePlacement(TileMap tileMap, Vector2 globalPosition)
    {
        var localPosition = InventoryGrid.ToLocal(globalPosition);
        var cellPosition = new Vector2(Mathf.Round(localPosition.x / TileSize), Mathf.Round(localPosition.y / TileSize));
        var cells = tileMap.GetUsedCells();
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = (Vector2)cells[i] + cellPosition;

            if (
                (cell.x < OffsetGridCell.x || cell.x > BoardSize.x + OffsetGridCell.x - 1) ||
                (cell.y < OffsetGridCell.y || cell.y > BoardSize.y + OffsetGridCell.y - 1)
               )
            {
                return false;
            }

            // GD.Print((int)(cell.x - OffsetGridCell.x), (int)(cell.y - OffsetGridCell.y));

            if (Tiles[(int)(cell.x - OffsetGridCell.x), (int)(cell.y - OffsetGridCell.y)].HasShape)
            {
                return false;
            }
        }

        return true;
    }

    private Vector2 GetShapeSize(TileMap shape)
    {
        var cells = shape.GetUsedCells();
        var maxX = 0;
        var maxY = 0;
        var minX = 0;
        var minY = 0;

        foreach (Vector2 cell in cells)
        {
            if (cell.x < minX)
                minX = (int)cell.x;
            if (cell.x > maxX)
                maxX = (int)cell.x;
            if (cell.y < minY)
                minY = (int)cell.y;
            if (cell.y > maxY)
                maxY = (int)cell.y;
        }

        var shapeSize = new Vector2((maxX - minX) * TileSize, (maxY - minY) * TileSize) * .5f;
        return shapeSize;
    }

    public void RotateCurrentShape()
    {
        var cells = CurrentArtifactShape.GetUsedCells();
        CurrentArtifactShape.Clear();
        for (var i = 0; i < cells.Count; i++)
        {
            var pos = (Vector2)cells[i];
            var newPos = new Vector2((-(int)pos.y), (int)pos.x);
            CurrentArtifactShape.SetCellv(newPos, 0);
        }
        CurrentArtifactShape.UpdateBitmaskRegion();
    }
}

public class Tile
{
    public TileMap Shape;
    public bool HasShape;
}
