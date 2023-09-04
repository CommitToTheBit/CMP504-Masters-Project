using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// NB: Lots of (clunky) signals from this the Play.cs, and vice versa; 
// This interactive build was very much thrown together as a last-minute experiment!
public partial class DialogueDebugger : Control
{
	[Signal]
	public delegate void BeatTweenedEventHandler();

	// DEPRECATED: ...
    [Signal]
    public delegate void TickedEventHandler();

	[Signal]
    public delegate void ChoosingEventHandler();

	[Signal]
	public delegate void PlayerPressedChoiceEventHandler();

	private PackedScene m_packedDialogueEntry;
	private PackedScene m_packedDialogueChoice;

	private Play m_play;
	private int m_duration;
	private int m_seed, m_resets;

	private SiftingPatterns m_felt;
	private SiftingUnexpectedness m_stu; // NB: Should come up with a `default' declaration of Select the Unexpected...

	private ScrollContainer m_traceScroll;
	private VBoxContainer m_traceContainer;
	private ColorRect m_traceFade;

	private HBoxContainer m_mapContainer;

	private CenterContainer m_buttonCentering;
	private HBoxContainer m_buttonContainer;
	private TextureButton m_playButton, m_pauseButton, m_stopButton;
	private TextureButton m_focusedButton;

	private ScrollContainer m_choiceScroll;
	private HBoxContainer m_choiceContainer;
	private ColorRect m_choiceFade;

	private MoveButton m_moveButton;
	private BackButton m_backButton;
	private WaitButton m_waitButton;

	private int m_choice;
	private int m_choiceSplit;

	private Random m_rng;

	public override void _Ready()
	{
		m_rng = new Random((int)DateTime.UtcNow.Ticks);
		m_seed = m_rng.Next();

		m_packedDialogueEntry = ResourceLoader.Load<PackedScene>("res://Debugger/DialogueEntry.tscn");
		m_packedDialogueChoice = ResourceLoader.Load<PackedScene>("res://Debugger/DialogueChoice.tscn");

		m_play = new Play(m_seed);
		m_play.Connect("Beat", new Callable(this, "Beat"));
		m_play.Connect("Choose", new Callable(this, "Choose"));
		Connect("Choosing", new Callable(m_play, "RegisterChoosing"));
		AddChild(m_play);
		
		m_felt = new SiftingPatterns();
		m_stu = new SiftingUnexpectedness(0);

		m_duration = 60;

		m_traceScroll = GetNode<ScrollContainer>("Centering/DebuggerUI/TraceScroll");
		m_traceScroll.GetVScrollBar().Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);

		m_traceContainer = m_traceScroll.GetNode<VBoxContainer>("TraceContainer");
		m_traceFade = GetNode<ColorRect>("TraceFade");

		m_mapContainer = GetNode<HBoxContainer>("Centering/DebuggerUI/MapContainer");
		m_mapContainer.Hide();

		m_buttonCentering = GetNode<CenterContainer>("Centering/DebuggerUI/ButtonCentering");
		m_buttonContainer = m_buttonCentering.GetNode<HBoxContainer>("ButtonContainer");

		m_playButton = m_buttonContainer.GetNode<TextureButton>("PlayButton");
        m_playButton.Connect("pressed", new Callable(this, "PlayPressed"));

		m_pauseButton = m_buttonContainer.GetNode<TextureButton>("PauseButton");
        m_pauseButton.Connect("pressed", new Callable(this, "PausePressed"));

		m_stopButton = m_buttonContainer.GetNode<TextureButton>("StopButton");
        m_stopButton.Connect("pressed", new Callable(this, "StopPressed"));

		m_moveButton = m_buttonContainer.GetNode<MoveButton>("MoveButton");
		m_moveButton.Connect("PlayerPressedMove", new Callable(this, "MovePressed"));

		m_backButton = m_buttonContainer.GetNode<BackButton>("BackButton");
		m_backButton.Connect("PlayerPressedBack", new Callable(this, "BackPressed"));

		m_waitButton = m_buttonContainer.GetNode<WaitButton>("WaitButton");
		m_waitButton.Connect("PlayerPressedWait", new Callable(this, "WaitPressed"));

		m_choiceScroll = m_buttonContainer.GetNode<ScrollContainer>("ChoiceScroll");
		m_choiceScroll.GetHScrollBar().Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);

		m_choiceContainer = m_choiceScroll.GetNode<HBoxContainer>("ChoiceContainer");
		m_choiceFade = GetNode<ColorRect>("ChoiceFade");

		// DEBUG: MVP focus handling...
		m_stopButton.GrabFocus();
		m_focusedButton = m_stopButton;

		m_choice = -1;
	}

	public override void _Process(double delta)
	{
		// DEBUG: Change to once-only...
		m_traceFade.SetPosition(m_traceScroll.GetScreenPosition());
		m_traceFade.Size = m_traceScroll.Size;

		m_choiceFade.SetPosition(m_choiceScroll.GetScreenPosition());
		m_choiceFade.Size = m_choiceScroll.Size;
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionPressed("ui_escape"))
			GetTree().Quit();
	}

	private async void Play()
	{
		int resets = m_resets;

		// STEP 1: Allow character selection...
		string player = await SelectCharacter();
		m_play.SelectCharacter(player);

		// STEP 2: Run simulation...
		for (int i = m_play.GetTick(); i <= m_duration && m_playButton.HasFocus() && resets == m_resets; i++)
		{
			await m_play.Tick();
		}

		// STEP 3: Apply sifting at end of simulation...
		Func<string, int> ordering = (m_play.m_agents.ContainsKey(m_play.m_player)) ? x => m_play.m_agents[m_play.m_player].m_trace.FindBeats(y => y.GetAllValues("audience").Contains(x)).Count : x => m_rng.Next();
		List<string> names = m_play.GetNamedCharacters().Keys.ToList().FindAll(x => !x.Equals(m_play.m_player)).OrderByDescending(ordering).ToList();//.Take(3).ToList();

		//new Dictionary<string,Trace>() { { "#J", m_play.m_agents["#J"].m_trace } });
		//GD.Print(microanthology.PrintMicroanthology("#J"));

		if (m_stu.LoadLikelihoods())
		{
			int count = 0;
			foreach (string name in names)
			{
				string perspective = await Sift(name);
				if (resets != m_resets)
					return;

				if (perspective.Length > 0)
					perspective = m_play.ParseForUI(perspective);

				if (perspective.Length > 0)
				{
					Beat(perspective);
					await ToSignal(this, "BeatTweened");
				}

				if (++count >= 3)
					break;
			}
		}

		// DEBUG: MVP focus handling...
		m_pauseButton.GrabFocus();
		m_focusedButton = m_pauseButton;
	}

	private async Task<string> SelectCharacter()
	{
		Dictionary<string, string> names = m_play.GetNamedCharacters();
		for (int i = 0; i < names.Count; i++)
		{
			DialogueChoice characterChoice = m_packedDialogueChoice.Instantiate<DialogueChoice>();
			characterChoice.Connect("PlayerPressedChoice", new Callable(this, "ChoicePressed"));

			m_choiceContainer.AddChild(characterChoice);
			m_choiceContainer.MoveChild(characterChoice, m_choiceContainer.GetChildCount() - 2);	
			characterChoice.InitialiseChoice(i, names.Values.ElementAt(i), true);	
		}		

		m_moveButton.Hide();
		m_backButton.Hide();
		m_waitButton.Show();

		await ToSignal(this, "PlayerPressedChoice");
		m_playButton.GrabFocus();
		m_focusedButton = m_playButton;

		for (int i = m_choiceContainer.GetChildCount() - 1; --i >= 1;)
			m_choiceContainer.RemoveChild(m_choiceContainer.GetChild(i));

		m_moveButton.Hide();
		m_backButton.Hide();
		m_waitButton.Hide();

		return (m_choice >= 0) ? names.Keys.ElementAt(m_choice) : "NONFOCALISED";
	}

	private async void Choose(string[] texts, string[] places, bool passable)
	{
		await ToSignal(GetTree(), "process_frame");
		if ((texts.Length == 0 && places.Length == 0) || m_play.GetTick() <= 0)
		{
			EmitSignal("Choosing", m_choice);
			return;
		}

		m_choiceSplit = texts.Length;
		for (int i = 0; i < texts.Length + places.Length; i++)
		{
			DialogueChoice dialogueChoice = m_packedDialogueChoice.Instantiate<DialogueChoice>();
			dialogueChoice.Connect("PlayerPressedChoice", new Callable(this, "ChoicePressed"));

			m_choiceContainer.AddChild(dialogueChoice);
			m_choiceContainer.MoveChild(dialogueChoice, m_choiceContainer.GetChildCount() - 2);	
			dialogueChoice.InitialiseChoice(i, (i < m_choiceSplit) ? texts[i] : places[i - m_choiceSplit], i < m_choiceSplit);	
		}

		m_moveButton.Show();
		m_backButton.Hide();

		if (passable)
			m_waitButton.Show();
		else
			m_waitButton.Hide();

		await ToSignal(this, "PlayerPressedChoice");
		m_playButton.GrabFocus();
		m_focusedButton = m_playButton;

		for (int i = m_choiceContainer.GetChildCount() - 1; --i >= 1;)
			m_choiceContainer.RemoveChild(m_choiceContainer.GetChild(i));

		m_traceScroll.Show();
		m_mapContainer.Hide();

		m_moveButton.Hide();
		m_backButton.Hide();
		m_waitButton.Hide();

		EmitSignal("Choosing", m_choice);
	}

	private async void Beat(string text)
	{
		if (m_pauseButton.HasFocus())
		{
			await ToSignal(m_pauseButton, "focus_exited");
			await ToSignal(GetTree(), "process_frame");
		}
		//await ToSignal(GetTree(), "process_frame");

		if (m_stopButton.HasFocus())
			return;

		DialogueEntry traceEntry = m_packedDialogueEntry.Instantiate<DialogueEntry>();
		traceEntry.Text = text;//.Replace("\n", " ").Replace(" +", "\n  +");

		double scrollFrom = m_traceScroll.GetVScrollBar().MaxValue;
		m_traceContainer.AddChild(traceEntry);
		m_traceContainer.MoveChild(traceEntry, m_traceContainer.GetChildCount() - 2);
		await ToSignal(GetTree(), "process_frame");

		double scrollTo = m_traceScroll.GetVScrollBar().MaxValue;

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(m_traceScroll.GetVScrollBar(), "value", scrollTo, 1.25f);
		tween.SetProcessMode(Tween.TweenProcessMode.Idle);
		tween.SetEase(Tween.EaseType.OutIn);
		tween.Play();

		await ToSignal(tween, "finished");

		//m_traceScroll.GetVScrollBar().Value = m_traceScroll.GetVScrollBar().MaxValue;

		EmitSignal("BeatTweened");
	}

	private async Task<string> Sift(string name)
	{
		// NB: Absolutely a 'one-size-fits-all' approach to surfacing microstories, could well be more creative about *which* pattern to show here!
		Microanthology microanthology = await Task.Run(() => m_felt.SiftMicrostories(new Dictionary<string,Trace>() { { name, m_play.m_agents[name].m_trace } }));
		return m_stu.SurfaceMicrostories(microanthology, 1).PrintFirstMicrostory(name, "He Said, She Said");		
	}

	private void PlayPressed()
    {    
		// DEBUG: MVP focus handling...
		if (m_focusedButton.Equals(m_playButton))
			return;
		
		m_playButton.GrabFocus();
		m_focusedButton = m_playButton;

		Play();
    }

	private void PausePressed()
    {    
		// DEBUG: MVP focus handling...
		if (!m_focusedButton.Equals(m_playButton))
			return;
		
		m_pauseButton.GrabFocus();
		m_focusedButton = m_pauseButton;
    }

	private async void StopPressed()
    {   
		// DEBUG: MVP focus handling...
		if (m_focusedButton.Equals(m_stopButton))
			return;
		
		m_stopButton.GrabFocus();
		m_focusedButton = m_stopButton;

		EmitSignal("Choosing", int.MinValue); // NB: Cancels out of current tick function...
		EmitSignal("BeatTweened"); // NB: Cancels out of current tick function...
		await ToSignal(GetTree(), "process_frame");

		m_play.Initialise(m_seed);
		m_resets++;

		m_traceScroll.Show();
		m_mapContainer.Hide();

		for (int i = m_traceContainer.GetChildCount() - 2; i > 0; i--)
			m_traceContainer.RemoveChild(m_traceContainer.GetChild(i));

		for (int i = m_choiceContainer.GetChildCount() - 1; --i >= 1;)
			m_choiceContainer.RemoveChild(m_choiceContainer.GetChild(i));

		m_moveButton.Hide();
		m_backButton.Hide();
		m_waitButton.Hide();
    }

	private void ChoicePressed(int choice)
	{
		m_choice = choice;

		EmitSignal("PlayerPressedChoice");
	}

	private void MovePressed()
	{
		m_traceScroll.Hide();
		m_mapContainer.Show();

		for (int i = 1; i < m_choiceContainer.GetChildCount() - 1; i++)
		{
			if (i < m_choiceSplit + 1)
				m_choiceContainer.GetChild<CanvasItem>(i).Hide();
			else
				m_choiceContainer.GetChild<CanvasItem>(i).Show();
		}

		m_moveButton.Hide();
		m_backButton.Show();
	}

	private void BackPressed()
	{
		m_traceScroll.Show();
		m_mapContainer.Hide();

		for (int i = 1; i < m_choiceContainer.GetChildCount() - 1; i++)
		{
			if (i < m_choiceSplit + 1)
				m_choiceContainer.GetChild<CanvasItem>(i).Show();
			else
				m_choiceContainer.GetChild<CanvasItem>(i).Hide();
		}

		m_moveButton.Show();
		m_backButton.Hide();
	}

	private void WaitPressed()
	{
		m_choice = -1;

		EmitSignal("PlayerPressedChoice");
	}
}
