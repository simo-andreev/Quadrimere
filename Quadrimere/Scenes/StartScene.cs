using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Emotion.Common;
using Emotion.Common.Threading;
using Emotion.Game.QuadTree;
using Emotion.Graphics;
using Emotion.Graphics.Command;
using Emotion.Graphics.Data;
using Emotion.Graphics.Objects;
using Emotion.IO;
using Emotion.Plugins.ImGuiNet;
using Emotion.Plugins.ImGuiNet.Windowing;
using Emotion.Primitives;
using Emotion.Scenography;
using Emotion.Tools.Windows;
using Emotion.Utility;
using ImGuiNET;
using Solution12.Graphics.Data;

namespace Quadrimere.Scenes
{
    public class StartScene : IScene
    {
        private readonly Random _random = new Random();

        // For debug/tools menu
        private WindowManager _menu = new WindowManager();
        private int _rendered;


        private DrawableFontAtlas _ubuntuFontAsset;
        private TextureAsset _tileMntTexture;
        private TextureAsset _tileGrs1Texture;
        private TextureAsset _tileGrs2Texture;

        private const int MAP_WIDTH = 64; // X axis tile count
        private const int MAP_HEIGHT = 64; // Y axis tile count
        private static readonly Vector2 TileSize = new Vector2(32, 32); // The size of individual tiles.

        private readonly byte[,] _heightMap = new byte[MAP_WIDTH, MAP_HEIGHT];


        private TileQuadrilateral[,] tiles = new TileQuadrilateral[MAP_WIDTH, MAP_HEIGHT];
        private QuadTree<TileQuadrilateral> quadTree;

        private List<TileQuadrilateral> _drawMemory = new List<TileQuadrilateral>(64);
        private FrameBuffer _frameBuffer;

        private byte _lastMouseX = 0;
        private byte _lastMouseY = 0;
        private TileQuadrilateral _lastMouseOver = null;

        public void Load()
        {
            _tileMntTexture = Engine.AssetLoader.Get<TextureAsset>("Image/mnt.png");
            _tileGrs1Texture = Engine.AssetLoader.Get<TextureAsset>("Image/grs1.png");
            _tileGrs2Texture = Engine.AssetLoader.Get<TextureAsset>("Image/grs2.png");

            // _ubuntuFontAsset = Engine.AssetLoader.Get<FontAsset>("font/UbuntuMono-R.ttf").GetAtlas(16f);

            GenHeightMap();
            GetTileMap();

            Rectangle boundsOfMap = Rectangle.BoundsFromPolygonPoints(new[]
            {
                tiles[0, 0].Vertex0,
                tiles[MAP_WIDTH - 1, 0].Vertex1,
                tiles[MAP_WIDTH - 1, MAP_HEIGHT - 1].Vertex2,
                tiles[0, MAP_HEIGHT - 1].Vertex3,
            });

            quadTree = new QuadTree<TileQuadrilateral>(boundsOfMap, 100);
            foreach (var tile in tiles)
            {
                quadTree.Add(tile);
            }

            GLThread.ExecuteGLThread(() => { _frameBuffer = new FrameBuffer(new Texture(Engine.Renderer.DrawBuffer.Size)); });
        }

        private void GenHeightMap()
        {
            for (int x = 0; x < MAP_HEIGHT; x++)
            {
                for (int y = 0; y < MAP_WIDTH; y++)
                {
                    if (x == 0 || y == 0 || _heightMap[x - 1, y] != _heightMap[x, y - 1])
                    {
                        _heightMap[x, y] = (byte) (_random.Next(100) < 70 ? 0 : 1);
                    }
                    else
                    {
                        _heightMap[x, y] = (byte) (_random.Next(100) < 75 ? _heightMap[x - 1, y] : _random.Next(2));
                    }
                }
            }
        }

        private void GetTileMap()
        {
            for (var x = 0; x < MAP_WIDTH; x++)
            {
                for (var y = 0; y < MAP_HEIGHT; y++)
                {
                    // Assign in height map @ appropriate position, storing for later use
                    tiles[x, y] = new TileQuadrilateral(x * TileSize.X, y * TileSize.Y, _heightMap[x, y], TileSize);
                }
            }
        }

        public void Update()
        {
            _menu.Update();
        }

        public void Draw(RenderComposer composer)
        {
            RenderMap(composer);
            RenderMouseOverTiles(composer);

            RenderGui(composer);
        }

        private void RenderMap(RenderComposer composer)
        {
            _drawMemory.Clear();
            var rect = new Rectangle(
                Engine.Renderer.Camera.ScreenToWorld(Vector2.Zero),
                Engine.Configuration.RenderSize * (Engine.Renderer.Scale - (Engine.Renderer.IntScale - 1)) / Engine.Renderer.Camera.Zoom
            );
            quadTree.GetObjects(rect, ref _drawMemory);
            _rendered = _drawMemory.Count;

            composer.RenderOutline(new Vector3(rect.Position, 0f), rect.Size, Color.CornflowerBlue, 2);

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _drawMemory.Count; i++)
            {
                var tile = _drawMemory[i];

                var c = Color.White.ToUint();
                var texture = tile.Height == 1 ? _tileMntTexture : i % 27 == 0 ? _tileGrs2Texture : _tileGrs1Texture;

                var a = composer.GetBatch();
                var data = a.GetData(texture.Texture);
                data[0].Vertex = tile.Vertex0.ToVec3();
                data[0].Color = c;
                data[0].UV = Vector2.Zero;

                data[1].Vertex = tile.Vertex1.ToVec3();
                data[1].Color = c;
                data[1].UV = new Vector2(1, 0);

                data[2].Vertex = tile.Vertex2.ToVec3();
                data[2].Color = c;
                data[2].UV = new Vector2(1, 1);

                data[3].Vertex = tile.Vertex3.ToVec3();
                data[3].Color = c;
                data[3].UV = new Vector2(0, 1);
            }
        }

        private void RenderMouseOverTiles(RenderComposer composer)
        {
            var mouseRect = new Rectangle(Engine.Renderer.Camera.ScreenToWorld(Vector2.Zero), new Vector2(1, 1))
            {
                Center = Engine.Renderer.Camera.ScreenToWorld(Engine.InputManager.MousePosition)
            };

            composer.RenderOutline(new Vector3(mouseRect.Position, 10), mouseRect.Size, Color.Magenta, 2);

            //composer.PushModelMatrix(Matrix4x4.CreateTranslation(new Vector3(-mouseRect.Position, 0)));
            composer.RenderTo(_frameBuffer);
            _drawMemory.Clear();
            quadTree.GetObjects(mouseRect, ref _drawMemory);
            for (var i = 0; i < _drawMemory.Count; i++)
            {
                var tile = _drawMemory[i];

                var a = composer.GetBatch();
                var data = a.GetData(null);
                data[0].Vertex = tile.Vertex0.ToVec3();
                data[0].Color = new Color(50 + i, i, 0).ToUint();

                data[1].Vertex = tile.Vertex1.ToVec3();
                data[1].Color = new Color(i * 20, i, 0).ToUint();

                data[2].Vertex = tile.Vertex2.ToVec3();
                data[2].Color = new Color(i * 20, i * 20, 0).ToUint();

                data[3].Vertex = tile.Vertex3.ToVec3();
                data[3].Color = new Color(i, i * 20, 0).ToUint();
            }

            composer.PushCommand(new ExecCodeCommand()
            {
                Func = () =>
                {
                    byte[] pixels = _frameBuffer.Sample(new Rectangle(Engine.InputManager.MousePosition, Vector2.One));

                    _lastMouseX = pixels[3];
                    _lastMouseY = pixels[2];
                }
            });

            //composer.PopModelMatrix();
            composer.RenderTo(null);
        }

        private void RenderGui(RenderComposer composer)
        {
            composer.SetUseViewMatrix(false);
            composer.SetDepthTest(false);

            ImGui.NewFrame();
            ImGui.Begin("InfoBox", ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.Text("Camera " + Engine.Renderer.Camera.Position + " | @z: " + Engine.Renderer.Camera.Zoom);
            ImGui.Text("RenderTileCount [" + _rendered + "]");
            ImGui.End();

            composer.RenderToolsMenu(_menu);
            _menu.Render(composer);

            composer.RenderUI();
        }


        public void Unload()
        {
        }
    }
}