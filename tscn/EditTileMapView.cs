using Godot;
using System;
using Horizoncraft.script.WorldControl;

public partial class EditTileMapView : Godot.TileMapLayer
{
    public enum PenMode
    {
        Draw,
        Remove,
    }

    [Export] public TileMapLayer BackGround;
    public Action<Vector2I> OnSetCell;
    public Action<Vector2I> OnRemoveCell;
    public Action<Vector2I> PickCell;
    private bool LastFrame;
    private Vector2 offsetPos;
    public PenMode Mode;
    private bool focus = false;
    public Vector2I MaxPos;
    public Vector2I MinPos;

    public override void _Ready()
    {
        GetParent<PanelContainer>().FocusEntered += () => { focus = true; };
        GetParent<PanelContainer>().FocusExited += () => { focus = false; };
        GetParent<PanelContainer>().MouseExited += () => { focus = false; };
        GetParent<PanelContainer>().MouseEntered += () => { focus = true; };
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
        if (!focus) return;

        if (Input.IsActionJustPressed("roller_up"))
        {
            var pos = GetLocalMousePosition();
            GlobalScale *= 1.1f;
        }

        if (Input.IsActionJustPressed("roller_down"))
        {
            var pos = GetLocalMousePosition();
            GlobalScale *= 0.9f;
        }

        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            if (Input.IsActionPressed("alt"))
            {
                var pos = LocalToMap(GetLocalMousePosition());
                PickCell?.Invoke(pos);
            }
            else
            {
                var pos = LocalToMap(GetLocalMousePosition());
                OnSetCell?.Invoke(pos);
            }
        }

        if (Input.IsMouseButtonPressed(MouseButton.Right))
        {
            var pos = LocalToMap(GetLocalMousePosition());
            OnRemoveCell?.Invoke(pos);
        }

        else if (Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            if (LastFrame == false)
            {
                //offsetPos = GetLocalMousePosition() * GlobalScale;
            }

            GlobalPosition = GetGlobalMousePosition() - offsetPos;
            LastFrame = true;
            return;
        }

        if (Input.IsKeyPressed(Key.Space))
        {
            var pos = GetViewport().GetVisibleRect().Size;
            GlobalPosition = pos / 2;
        }

        offsetPos = GetLocalMousePosition() * GlobalScale;
        LastFrame = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {
            LastFrame = true;
        }
        else
        {
            LastFrame = false;
        }

        if (@event is InputEventMouseButton mouseButtonEvent)
        {
        }
    }

    public override void _Draw()
    {
        DrawLine(new Vector2(0, -Chunk.Size * 16), new Vector2(0, Chunk.Size * 16), Color.Color8(255, 255, 255));
        DrawLine(new Vector2(-Chunk.Size * 16, 0), new Vector2(Chunk.Size * 16, 0), Color.Color8(255, 255, 255));
        var pos = LocalToMap(GetLocalMousePosition());
        DrawRect(new Rect2(pos * 16, 16, 16), Color.Color8(255, 255, 255, 192));

        var w = (MaxPos.X - MinPos.X) * 16f;
        var h = (MaxPos.Y - MinPos.Y) * 16f;
        //DrawRect(new Rect2(MinPos * 16, w, h), Color.Color8(0, 255, 0));

        DrawLine(MinPos * 16, new Vector2I(MaxPos.X, MinPos.Y) * 16, Color.Color8(255, 255, 255));
        DrawLine(MinPos * 16, new Vector2I(MinPos.X, MaxPos.Y) * 16, Color.Color8(255, 255, 255));

        DrawLine(MaxPos * 16, new Vector2I(MaxPos.X, MinPos.Y) * 16, Color.Color8(255, 255, 255));
        DrawLine(MaxPos * 16, new Vector2I(MinPos.X, MaxPos.Y) * 16, Color.Color8(255, 255, 255));
    }
}