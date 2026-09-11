using Godot;

namespace Zest.Game;

/// <summary>Isolated Z-EN-01 tile proof; kept out of the production world until layout approval.</summary>
public partial class EnvironmentTilePreview : Node2D
{
    private const string AtlasPath = "res://art/production/native-v01/environment/tile_grass_path_5x1_v01.png";

    public override void _Ready()
    {
        Texture2D atlas = ResourceLoader.Load<Texture2D>(AtlasPath);
        if (atlas is null) return;
        for (int index = 0; index < 5; index++)
            AddTile(atlas, index, new Vector2(index * 32, 0), $"Palette{index}");

        // Small connectivity proof: grass / path / grass with a corner turn.
        for (int x = 0; x < 7; x++)
            AddTile(atlas, x is 0 or 6 ? 2 : 1, new Vector2(x * 32, 64), $"Path{x}");
        AddTile(atlas, 3, new Vector2(6 * 32, 96), "InsideCorner");
        AddTile(atlas, 0, new Vector2(0, 96), "GrassBelow");
    }

    private void AddTile(Texture2D atlas, int index, Vector2 position, string name)
    {
        AtlasTexture frame = new()
        {
            Atlas = atlas,
            Region = new Rect2(index * 32, 0, 32, 32),
        };
        AddChild(new Sprite2D
        {
            Name = name,
            Texture = frame,
            Position = position,
            Centered = false,
            TextureFilter = TextureFilterEnum.Nearest,
        });
    }
}
