using System;
using System.Numerics;
using Emotion.Common;
using Emotion.Graphics.Camera;
using Emotion.Platform.Input;
using Emotion.Plugins.ImGuiNet;
using Quadrimere.Scenes;

namespace Quadrimere
{
    class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Engine.Setup(new Configurator
                {
                    DebugMode = true,
                    // RenderSize = new Vector2(1280, 720),
                    // HostSize = new Vector2(1280, 720)
                }
                .AddPlugin(new ImGuiNetPlugin())
            );

            Engine.Renderer.Camera = new CamHub();
            
            Engine.SceneManager.SetScene(new StartScene());
            
            Engine.Run();
        }
    }


    class CamHub : PixelArtCamera
    {
        private float MoveSpeed { get; set; } = 0.35f;

        /// <inheritdoc/>
        public CamHub(Vector3 position = default, float zoom = 1) : base(position, zoom)
        {
        }

        /// <inheritdoc />
        public override void Update()
        {
            Vector3 cameraMoveDirection = Vector3.Zero;

            // note any-and-all 'WASD' move input
            if (Engine.InputManager.IsKeyHeld(Key.W)) cameraMoveDirection.Y -= 1;
            if (Engine.InputManager.IsKeyHeld(Key.A)) cameraMoveDirection.X -= 1;
            if (Engine.InputManager.IsKeyHeld(Key.S)) cameraMoveDirection.Y += 1;
            if (Engine.InputManager.IsKeyHeld(Key.D)) cameraMoveDirection.X += 1;

            // If mouse scroll-ed, note Zoom amount and direction
            if (Engine.InputManager.GetMouseScrollRelative() < 0) Zoom += 0.035f * Engine.DeltaTime;
            if (Engine.InputManager.GetMouseScrollRelative() > 0) Zoom -= 0.035f * Engine.DeltaTime;

            // Clamp Camera Zoom
            if (Zoom > 6) Zoom = 6;
            if (Zoom < 0.5) Zoom = 0.5f;

            // If fast-move key down -> quadruple speed coefficient 
            float speed = MoveSpeed;
            if (Engine.InputManager.IsKeyHeld(Key.LeftControl)) speed *= 4;
            speed *= Engine.DeltaTime;

            // Since the camera is isometric its X and Y axes are rotated - however we want the movement to be relative to the screen 
            // based on the perspective. To do this we need to find the "proper" up and right directions and move along them.
            cameraMoveDirection *= new Vector3(speed, speed, speed);

            // Finally apply the movement Vector to the camera.
            Engine.Renderer.Camera.Position += cameraMoveDirection;

            if (Engine.InputManager.IsKeyHeld(Key.Home)) Engine.Renderer.Camera.Position = Vector3.Zero;

            // todo: Currently the matrix must be manually recreated as zooming doesn't trigger a recreation.
            RecreateMatrix();
        }

    }
}