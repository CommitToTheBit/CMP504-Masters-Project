using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Console.Title = "CMP504 Artefact";
        Console.WindowWidth = 192;
        Console.WindowHeight = 40;

        Console.WriteLine("\n*****************************");
        Console.WriteLine("*** INSTRUMENT PLAYING... ***");
        Console.WriteLine("*****************************\n");

        DateTime runtime = DateTime.UtcNow;
        DateTime time = runtime;

        string timestamp = runtime.ToString().Replace('/', '-').Replace(':', '-');

        int duration = 60;

        // NB: The current LIKELIHOODS.json was generated over 50 rehearsals...
        // ...but 10 is much faster for video demonstration purposes!
        int rehearsalCount = 10;

        int microstorySampleSize = 5;

        int seed = rehearsalCount;
        Random rng = new Random(seed);

        GreenRoom room = new GreenRoom();
        Dictionary<string, Characterisation> characterisations = room.GetCharacterisations();

        // DEBUG: Filter to focus on 'core' characters...
        //List<string> core = new List<string>() { "#A", "#B" };
        //foreach (string character in characterisations.Keys.ToList().FindAll(x => !core.Contains(x)))
        //    characterisations.Remove(character);

        // STEP 1: If necessary/desired, generate control data...
        Play play = new Play((int)runtime.Ticks, characterisations);

        SiftingPatterns felt = new SiftingPatterns();
        SiftingDialogue yarn = new SiftingDialogue(ref play.m_script);
        SiftingRoles wool = new SiftingRoles(ref play.m_script);

        SurfacingRandomness randomBaseline = new SurfacingRandomness(rng.Next());
        SurfacingUnexpectedness selectTheUnexpected = new SurfacingUnexpectedness(rng.Next()); // NB: Should come up with a `default' declaration of Select the Unexpected...

        bool rehearse = !selectTheUnexpected.LoadLikelihoods();

        if (!rehearse)
        {
            // TEXT OPTION: Refresh control data?
            string choice = "";
            while (!choice.Equals("Y") && !choice.Equals("N"))
            {
                Console.Write("\rRefresh statistical sample? [Y/N] ");

                int inputCursorLeft = Console.CursorLeft;
                int inputCursorTop = Console.CursorTop;
                choice = Console.ReadLine().ToUpper();

                Console.SetCursorPosition(inputCursorLeft, inputCursorTop);
                for (int i = 0; i <= choice.Length; i++)
                {
                    Console.Write(" ");
                }
                Console.SetCursorPosition(inputCursorLeft, inputCursorTop);
            }
            Console.Write("\r".PadRight(Console.WindowWidth) + "\r");

            rehearse = choice.Equals("Y");
        }

        if (rehearse)
        {
            // STEP 1.1: Run simulations...
            Console.WriteLine("--- REHEARSING SIMULATIONS... ---\n");
            time = DateTime.UtcNow;

            List<Play> rehearsals = new List<Play>();
            for (int i = 0; i < rehearsalCount; i++)
            {
                rehearsals.Add(new Play(i, characterisations));
                for (int j = 0; j < duration; j++)
                    rehearsals[i].Tick("REHEARSAL " + (i+1) + "/" + rehearsalCount);
            }
            Console.Write("\r".PadRight(Console.WindowWidth) + "\r");

            // FIXME: Add focalised 'one-on-ones', if possible...

            Console.WriteLine("--- SIMULATIONS REHEARSED AFTER " + (((float)(DateTime.UtcNow.Ticks - time.Ticks)) / Math.Pow(10, 7)).ToString("0.000") + "s ---\n\n\n");

            // STEP 1.2: Sift microstories...
            Console.WriteLine("--- DRAFTING MICROSTORIES... ---\n");
            time = DateTime.UtcNow;

            List<Microanthology> drafts = new List<Microanthology>();
            for (int i = 0; i < rehearsalCount; i++)
                drafts.Add(felt.SiftMicrostories(rehearsals[i].GetTraces(), "DRAFT " + (i+1) + "/" + rehearsalCount));
            Console.Write("\r".PadRight(Console.WindowWidth) + "\r");

            Console.WriteLine("--- MICROSTORIES DRAFTED AFTER " + (((float)(DateTime.UtcNow.Ticks - time.Ticks)) / Math.Pow(10, 7)).ToString("0.000") + "s ---\n\n\n");

            // STEP 1.3: Calculate likelihoods...
            Console.WriteLine("--- INITIALISING HEURISTICS... ---\n");
            time = DateTime.UtcNow;

            selectTheUnexpected.SaveLikelihoods(timestamp, drafts);
            selectTheUnexpected.LoadLikelihoods();

            Console.WriteLine("--- HEURISTICS INTIALISED AFTER " + (((float)(DateTime.UtcNow.Ticks - time.Ticks)) / Math.Pow(10, 7)).ToString("0.000") + "s ---\n\n\n");
        }

        // STEP 2.1: Run simulation...
        Console.WriteLine("--- PERFORMING SIMULATION... ---\n");

        for (int i = 0; i <= duration; i++)
            play.Tick("PLAY", i % 10 == 0 || i == duration);
        Console.Write("\r".PadRight(Console.WindowWidth) + "\r");

        Console.Write("WRITING TRACES TO FILE...");
        play.WriteTracesToFile(timestamp, false, false);
        Console.Write("\r".PadRight(Console.WindowWidth) + "\r");

        Console.WriteLine("--- SIMULATION PERFORMED AFTER " + (((float)(DateTime.UtcNow.Ticks - time.Ticks)) / Math.Pow(10, 7)).ToString("0.000") + "s ---\n\n\n");

        // STEP 2.2: Sift microstories...
        Console.WriteLine("--- WRITING MICROSTORIES... ---\n");
        time = DateTime.UtcNow;

        Microanthology microanthology = felt.SiftMicrostories(play.GetTraces(), "MICROANTHOLOGY");

        // DEBUG: Printing additional logs...
        //Microanthology rumours = yarn.SiftMicrostories(play.GetTraces(), "RUMOURS");
        //Microanthology roles = wool.SiftMicrostories(play.GetTraces(), "RUMOURS");
        Console.Write("\r".PadRight(Console.WindowWidth) + "\r");

        Console.WriteLine("--- MICROSTORIES WRTTEN AFTER " + (((float)(DateTime.UtcNow.Ticks - time.Ticks)) / Math.Pow(10, 7)).ToString("0.000") + "s ---\n\n\n");

        // STEP 2.3: Surface via heurstics...
        Console.WriteLine("--- APPLYING HEURISTICS... ---\n");
        time = DateTime.UtcNow;

        Console.Write("\rWRITING MICROSTORIES TO FILE...");

        // FIXME: Found the place to apply grammars... but do we want them applied?
        //Grammar grammar = new Grammar(rng.Next());
        //foreach (string character in characterisations.Keys)
        //    foreach (string attribute in characterisations[character].m_attributes.Keys)
        //        grammar.InitialiseProductionRule(character + "::" + attribute, characterisations[character].m_attributes[attribute]);

        microanthology.WriteMicroanthologyToFile(timestamp, "DEBUG-MICROSTORIES", false, false);
        //selectTheUnexpected.SurfaceMicrostories(microanthology).WriteMicroanthologyToFile(timestamp, "DEBUG-UNEXPECTEDNESS", false, false);

        randomBaseline.SurfaceMicrostories(microanthology, microstorySampleSize).WriteMicroanthologyToFile(timestamp, "RANDOMNESS", false, false);
        selectTheUnexpected.SurfaceMicrostories(microanthology, microstorySampleSize).WriteMicroanthologyToFile(timestamp, "UNEXPECTEDNESS", false, false);

        // DEBUG: Printing additional logs...
        //rumours.WritePerspectiveToFile(timestamp, "DIALOGUE", false, false);
        //roles.WritePerspectiveToFile(timestamp, "ROLE", false, false);

        Console.Write("\r".PadRight(Console.WindowWidth) + "\r");

        Console.WriteLine("--- HEURISTICS APPLIED AFTER " + (((float)(DateTime.UtcNow.Ticks - time.Ticks)) / Math.Pow(10, 7)).ToString("0.000") + "s ---\n\n\n");

        // DEBUG: Checking script has loaded correctly...
        //play.m_script.PrintAdjencyMatrix();

        string played = "*** INSTRUMENT PLAYED AFTER " + (((float)(DateTime.UtcNow.Ticks - runtime.Ticks)) / Math.Pow(10, 7)).ToString("0.000") + "s ***";

        Console.WriteLine(new String('*', played.Length));
        Console.WriteLine(played);
        Console.WriteLine(new String('*', played.Length));
    }
}