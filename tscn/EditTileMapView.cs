using Godot;
using System;
using horizoncraft.script.WorldControl;

public partial class EditTileMapView : Godot.TileMapLayer
{
    public enum PenMode
    {
        Draw,
        Remove,
    }

    public Action<Vector2I> OnSetCell;
    public Action<Vector2I> OnRemoveCell;
    private bool LastFrame;
    private Vector2 offsetPos;
    public PenMode Mode;
    private bool focus = false;

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
            var pos = LocalToMap(GetLocalMousePosition());
            if (Mode == PenMode.Draw) OnSetCell?.Invoke(pos);
            if (Mode == PenMode.Remove) OnRemoveCell?.Invoke(pos);
        }
        else if (Input.IsMouseButtonPressed(MouseButton.Right))
        {
            if (LastFrame == false)
            {
                //offsetPos = GetLocalMousePosition() * GlobalScale;
            }

            GlobalPosition = GetGlobalMousePosition() - offsetPos;
            LastFrame = true;
            return;
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
    }
}