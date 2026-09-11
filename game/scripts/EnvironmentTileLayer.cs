using Godot;

namespace Zest.Game;

/// <summary>Reusable native tile layer for the Z-EN-01 ground pass.</summary>
public partial class EnvironmentTileLayer : Node2D
{
    private const string AtlasPath = "res://art/production/native-v01/environment/tile_grass_path_5x1_v01.png";
    private const int TileSize = 32;

    public override void _Ready()
    {
        Texture2D atlas = ResourceLoader.Load<Texture2D>(AtlasPath);
        if (atlas is null) return;

        // 20x11 grass field; path runs through the middle with explicit edge tiles.
        for (int row = 0; row < 11; row++)
        for (int column = 0; column < 20; column++)
        {
            int tile = row switch
            {
                4 => column is 0 or 19 ? 2 : 1,
                5 => column is 0 or 19 ? 2 : 1,
                _ => 0,
            };
            AddTile(atlas, tile, new Vector2(column * TileSize, row * TileSize));
        }
    }

    private void AddTile(Texture2D atlas, int index, Vector2 position)
    {
        AtlasTexture frame = new()
        {
            Atlas = atlas,
            Region = new Rect2(index * TileSize, 0, TileSize, TileSize),
        };
        AddChild(new Sprite2D
        {
            Texture = frame,
            Position = position,
            Centered = false,
            TextureFilter = TextureFilterEnum.Nearest,
        });
    }
}
