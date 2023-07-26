# Code Royale
_A Software and Game Development Adventure by Collin Eddy_

## Introduction
This project is a part of a multi-part course in which we dive into the skills and techniques used in building software and creating digital games. We use C# and Unity to write the controlling code for a boat in an arena-style game. Each boat is functionally identical, and the arena is populated randomly, so the survival proficiency is entirely predicated on the quality of the boat's software.

## How do I get it?
First, you will need the following software:

* `Git Bash` (to clone this repository and its submodules)
* `Visual Studio Community 2022`, plus the `.NET Desktop Development` module (to edit the C# code)
* `Unity 2022.3.4f1 LTS` (to build the game)

Assuming you know or can look up how to use Git, use the following command to clone the project. Note that the `--recurse-submodules` is needed to get two additional repositories I use within in.
```bash
git clone https://github.com/linemouth/CodeRoyale.git --recurse-submodules
```

## What do I do?
The game is a framework in which you can try new ways of controlling the boats within it using C# code. To begin, create a new class which inherits from BoatController. This class serves as the interface to your boat, its radar, and its gun. Using the functions and properties it includes, you can start building your own boat software to see how it interacts with the other boats in the game.

_Happy coding!_
