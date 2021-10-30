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

- Mouse wheel : zoom in / out
- Left mouse button : drag map / highlight box
- Middle mouse button : reset camera zoom and position
- Right mouse button : show options menu
- Up or down arrows : change floor
- Left right arrows : change room
- Esc : quit (only in fullscreen mode)

- W : reset last distance for all actors
- Q : reset total delay (AITD1 only)
- Shift + Alpha1 : Timer 1 goes back 5 frames (AITD1 only)
- Shift + Alpha2 : Timer 2 goes back 5 frames (AITD1 only)

## Shortcuts

- D : display mode
- F : camera follow mode
- R : room's visibility
- C : camera area's visibility
- T : trigger's visibility
- A : actor's visibility
- E : show extra info (AITD1 only)
- Page up / down : camera rotate
- Tab : switch to model viewer

Put mouse on a box to highlight it.
- Gray boxes are colliders which player cannot passthrough
- Blue boxes are colliders that player can interact with
- Purple boxes are links between rooms and is used for pathfinding
- Red and amber boxes are triggers. It is usually used to switch from one room to another. It can also trigger other things like enemies, sounds, scripted sequences, ...

## Link to DOSBox
This feature allows to view all active entities in the game, displayed and updated realtime from DOSBox.

To do this, simply play AITD at the same time room viewer is running.
If everything is OK, you should view a white square with an arrow, this is player position.

Common issue: if AITD has been started with administrator rights, room viewer will not be able to see DOSBox process (link to DOSBox feature won't work).
To fix this, run room viewer with administrator rights (or run AITD without administrator rights).

## Warp actor
When the game is linked to DOSBox, it is possible to change the position of an actor using drag and drop. Hold left mouse button while mouse is on an actor. Move cursor (keeping left button pressed) and right click to warp (you can do this multiple times). Then, release left button. It is also possible to manually edit position of an actor by right clicking on it or by pressing numpad keys :
- 4, 6, 2, 8 : move actor left / right / down / up
- 7, 9 : rotate actor left / right
- If you hold 0 while pressing the numpad keys above, position is updated at a higher rate.

## Actor slot swap
When the game is linked to DOSBox, you can swap two actors slot positions by highlighting an actor, typing a number (with keypad or alphanumeric keys) then pressing enter.

# Model viewer

## Installation

Repeat room viewer installation steps. Copy the following files :
- LISTBODY.PAK, LISTBOD2.PAK
- LISTANIM.PAK, LISTANI2.PAK
- ITD_RESS.PAK
- CAMERA16.PAK (JITD only)
- TEXTURES.PAK (Time Gate only)

## Commands

- Left, right arrows : change model
- Up, down arrows : change animation
- Space : change model folder (Edward or Emily)
- Shift : hold it while pressing left or right keys to skip 10 models at once
- Mouse wheel : zoom in / out
- Left mouse button : click on model and drag to rotate it
- Right mouse button : show options menu / move model

## Shortcuts

- D : details high / low
- R : camera auto rotate mode
- E : show extra information
- A : enable animation
- Tab : switch to room viewer