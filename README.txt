This submission contains a working executable for the interactive build of 'Men in Grey Suits' (MiGS).

Opening the application, click the 'Play' button to begin a new simulation.
You will first be asked to select your character (scrolling left/right with the mouse wheel to see all options available).
You may also click the 'Ellipses' button to run the simulation automatically.

On selecting a character, the simulation will run from 7.00pm-8.00pm (in storyworld). In this time, you can:
- Speak to other characters (using the dialogue choices offered in a horizontal scrollbar at the bottom of the screen)
- Move room to room (toggling the 'Arrow' button to see movement choices; this UI is not incredibly clear)
- Pass your turn and let other characters speak (again, using the 'Ellipses' button)
After the simulation ends, it will return three microstories from other NPCs' perspectives, produced by focalised story sifting.
All functionality is shown in the accompanying video demonstration.

The seed for the interactive build only resets on closing the application; the 'Stop' button resets the simulation, but keeps the seed fixed.
Pressing 'Stop' therefore allows you to replay the simulation from the same initial conditions, but acting as a different character.

Use the mouse to navigate MiGS, left clicking to select choices.
The Escape key will immediately close the application.

The 'source' folder includes all code used in MiGS' interactive build.
It also contains the code for an earlier, experimental build (unchanged since late July).
The microstories, likelihoods data, etc., in the experimental build are those used in the research questionnaire.

Note that the experimental build was only intended as a researcher-facing text console (with potentially unclear UI).
Since it would only ever be run from Visual Studio, it did not seem necessary to include this as a second, standalone executable.

In the interests of showing my working, the 'source' folder further includes the code used to calculate edit distances in Table 4.5 of the dissertation. 
Again, there was no need to compile this into a working executable.

Video demonstration of both the interactive and non-interactive builds can be found in the 'docs' folder.
The submission also contains the project's dissertation, research poster, and the slides used in its Viva Voce.

MISCELLANEOUS:
* Note that the interactive build does not generate likelihoods data; this has always been handled in a text console.
  This functionality can instead be tested in the experimental build, by choosing to "REFRESH STATISTICAL SAMPLE" on running the console.
  While the experimental build's current LIKELIHOODS.json file was sampled over 50 simulations, I've reduced this to 1 simulation for the purposes of a faster video demonstration.
  Other than this one change, the experimental build is as it was at the release of the questionnaire (and will include bugs later fixed in the interactive build).
* If anything is unclear, please don't hesitate to email me! (2201312@uad.ac.uk) 