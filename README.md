# AITD room viewer

This is a room viewer (and 3D model viewer) for Alone in the Dark series.
It has been created mostly for speedrunning.

The following games are supported :
- Alone in the Dark 1 (CD-ROM, floppy, demo)
- Alone in the Dark 2 (CD-ROM, floppy, demo)
- Alone in the Dark 3 (CD-ROM, demo)
- Jack in the Dark
- Time Gate: Knight's Chase (CD-ROM, demo)

## Instructions
1. You need to have the original version of the game installed somewhere on your computer (eg: from GOG)
2. Extract AITD room viewer archive in a folder (eg : on your desktop)
3. Create a new folder named "GAMEDATA" inside of it.
4. Copy the following files from the game to GAMEDATA folder :
  - ETAGE00.PAK, ETAGE01.PAK, ETAGE02.PAK, ...
  - CAMSALxx.PAK (Time Gate only)

## Commands

- <kbd>Mouse wheel</kbd> : zoom in / out
- <kbd>Left mouse button</kbd> : drag map / highlight box
- <kbd>Middle mouse button</kbd> : reset camera zoom and position
- <kbd>Right mouse button</kbd> : show options menu
- <kbd>↑</kbd> or <kbd>↓</kbd> : change floor
- <kbd>←</kbd> or <kbd>→</kbd> : change room
- <kbd>Esc</kbd> : quit (only in fullscreen mode)
- <kbd>W</kbd> : reset last distance for all actors
- <kbd>Q</kbd> : reset total delay (AITD1 only)
- <kbd>Shift</kbd> + <kbd>Alpha1</kbd> : Timer 1 goes back 5 seconds (AITD1 only)
- <kbd>Shift</kbd> + <kbd>Alpha2</kbd> : Timer 2 goes back 5 seconds (AITD1 only)

## Shortcuts

- <kbd>D</kbd> : display mode
- <kbd>F</kbd> : camera follow mode
- <kbd>R</kbd> : room's visibility
- <kbd>C</kbd> : camera area's visibility
- <kbd>T</kbd> : trigger's visibility
- <kbd>A</kbd> : actor's visibility
- <kbd>E</kbd> : show extra info (AITD1 only)
- <kbd>Page up</kbd> / <kbd>page down</kbd> : camera rotate
- <kbd>Tab</kbd> : switch to model viewer

Put mouse on a box to highlight it.
- Light gray boxes are colliders which player cannot passthrough
- Dark gray boxes are usually non-walls colliders
- Blue boxes are colliders that player can interact with
- Teal boxes are links between rooms and is used for pathfinding
- Red and amber boxes are triggers. It is usually used to switch from one room to another. It can also trigger other things like enemies, sounds, scripted sequences, ...

## DOSBox
It's possible to view all active entities in the game, displayed and updated realtime from DOSBox.

To do this, simply play AITD at the same time room viewer is running.
If everything is OK, you should view a white square with an arrow, this is player position.

Common issue: if AITD has been started with administrator rights, room viewer will not be able to see DOSBox process.
To fix this, run room viewer with administrator rights (or run AITD without administrator rights).

## Warp actor
When the game is linked to DOSBox, it is possible to change the position of an actor using drag and drop. Hold <kbd>left mouse button</kbd> while mouse is on an actor. Move cursor (keeping <kbd>left mouse button</kbd> pressed) and right click to warp (you can do this multiple times). Then, release <kbd>left mouse button</kbd>. It is also possible to manually edit position of an actor by right clicking on it or by pressing numpad keys :
- <kbd>4</kbd>, <kbd>6</kbd>, <kbd>2</kbd>, <kbd>8</kbd> : move actor left / right / down / up
- <kbd>7</kbd>, <kbd>9</kbd> : rotate actor left / right
- If you hold <kbd>0</kbd> while pressing the numpad keys above, position is updated at a higher rate.

## Actor slot swap
When the game is linked to DOSBox, you can swap two actors slot positions by highlighting an actor, typing a number (with keypad or alphanumeric keys) then pressing <kbd>enter</kbd>.

# Model viewer

## Installation

Copy the following files to GAMEDATA folder:
- LISTBODY.PAK, LISTANIM.PAK
- LISTBOD2.PAK, LISTANI2.PAK (AITD1 only)
- ITD_RESS.PAK (for palette)
- CAMERAxx.PAK (for palette, JITD only)
- TEXTURES.PAK (Time Gate only)

## Commands

- <kbd>←</kbd> or <kbd>→</kbd> : change model
- <kbd>↑</kbd> or <kbd>↓</kbd> : change animation
- <kbd>Space</kbd> : change model folder (Edward or Emily)
- <kbd>Shift</kbd> : hold it while pressing left or right keys to skip 10 models at once
- <kbd>Mouse wheel</kbd> : zoom in / out
- <kbd>Left mouse button</kbd> : click on model and drag to rotate it
- <kbd>Right mouse button</kbd> : show options menu / move model

## Shortcuts

- <kbd>D</kbd> : details high / low
- <kbd>R</kbd> : camera auto rotate mode
- <kbd>E</kbd> : show extra information
- <kbd>A</kbd> : enable animation
- <kbd>Tab</kbd> : switch to room viewer