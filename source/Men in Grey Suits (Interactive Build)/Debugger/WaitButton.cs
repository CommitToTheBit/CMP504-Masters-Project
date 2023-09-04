using Godot;
using System;

public partial class WaitButton : CenterContainer
{
    [Signal]
    public delegate void PlayerPressedWaitEventHandler();

    public TextureButton m_button;

	public override void _Ready()
	{
        m_button = GetNode<TextureButton>("Pivot/Button");
        m_button.Connect("pressed", new Callable(this, "Pressed"));
        m_button.Connect("button_down", new Callable(this, "ButtonDown"));
        m_button.Connect("button_up", new Callable(this, "ButtonUp"));
    } 

    private void Pressed()
    {
        EmitSignal("PlayerPressedWait");
    }

    private void ButtonDown()
    {
        m_button.Scale = 0.97f * Vector2.One;
    }

    private void ButtonUp()
    {
        m_button.Scale = Vector2.One;
    }
}
