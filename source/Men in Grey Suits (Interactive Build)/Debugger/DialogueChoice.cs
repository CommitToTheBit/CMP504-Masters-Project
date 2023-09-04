using Godot;
using System;
using System.Collections.Generic;

public partial class DialogueChoice : CenterContainer
{
    [Signal]
    public delegate void PlayerPressedChoiceEventHandler();

	ColorRect m_outline;
	ShaderMaterial m_outlineMaterial;

	RichTextLabel m_label;

	Button m_button;

	int m_choice;

	public override void _Ready()
	{
		m_outline = GetNode<ColorRect>("OutlineContainer/Outline");
		m_outlineMaterial = (ShaderMaterial)m_outline.Material;

		m_label = GetNode<RichTextLabel>("OutlineContainer/LabelContainer/LabelCentering/Label"); 

		m_button = GetNode<Button>("OutlineContainer/Button"); 
		m_button.Connect("pressed", new Callable(this, "Pressed"));
		m_button.Connect("mouse_entered", new Callable(this, "MouseEntered"));
        m_button.Connect("mouse_exited", new Callable(this, "MouseExited"));
        m_button.Connect("button_down", new Callable(this, "ButtonDown"));
        m_button.Connect("button_up", new Callable(this, "ButtonUp"));
		m_button.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);

		m_choice = -1;
		Hide();
	}

	public override void _Process(double delta)
	{
		// DEBUG: Change to once-only...

		//UpdateMinimumSize();
	}

	public async void InitialiseChoice(int choice, string text, bool show)
	{
		// STEP 0: Get parsed text...
		m_label.Text = text;
		string parsedText = m_label.GetParsedText();

		// STEP 1: Split text into two halves...
		float split = m_label.GetThemeFont("font").GetStringSize(parsedText).X / 2.0f;
		string splitText = string.Empty;

		List<string> words = new List<string>(text.Split(" "));
		List<string> parsedWords = new List<string>(parsedText.Split(" "));

		int i = 1;
		for (; i < parsedWords.Count; i++)
		{
			if (m_label.GetThemeFont("font").GetStringSize(string.Join(" ", parsedWords.GetRange(0, i))).X > split)
				break;
		}

		// NB: words/parsedWords de-sync only occurs if a bbcode [/command] has spaces on either side...
		// NB: ...and even at that, we'll just end up with a slightly misaligned label!
		if (i < words.Count)
		{
			words[i-1] = words[i-1]+"\n"+words[i];
			words.RemoveAt(i);
		} 

		m_choice = choice;
		m_label.Text = "[center]" + string.Join(" ", words) + "[/center]";

		await ToSignal(GetTree(), "process_frame");

		m_outlineMaterial.SetShaderParameter("textWidth", m_outline.Size.X);

		if (show)
			Show();
	}

	private void Pressed()
    {
		EmitSignal("PlayerPressedChoice", new List<Variant>() { m_choice }.ToArray());
    }

	private void MouseEntered()
    {
        m_outlineMaterial.SetShaderParameter("hover", 0.25f);
    }

	private void MouseExited()
    {
        m_outlineMaterial.SetShaderParameter("hover", 0.0f);
    }


    private void ButtonDown()
    {
        m_outlineMaterial.SetShaderParameter("scale", 0.97f);
    }

    private void ButtonUp()
    {
        m_outlineMaterial.SetShaderParameter("scale", 1.0f);
    }
}
