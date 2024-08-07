# AITD room viewer

This is a room viewer (and 3D model viewer) for Alone in the Dark series.
It has been created mostly for speedrunning.

The following games are supported :
- Alone in the Dark 1 / 2 / 3 (CD-ROM, floppy, demo)
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

| Mouse | Description |
| :-: | - |
| <kbd>Wheel</kbd> | zoom in / out |
| <kbd>Left button</kbd> | drag map / select box |
| <kbd>Middle button</kbd> | reset camera zoom and position
| <kbd>Right button</kbd> | show options menu

| Key | Description |
| :-: | - |
| <kbd>↑</kbd> <br/> <kbd>↓</kbd> | change floor
| <kbd>←</kbd> <kbd>→</kbd> | change room
| <kbd>W</kbd> | reset last distance for all actors
| <kbd>Q</kbd> | reset total delay (AITD1 only)
| <kbd>Shift</kbd> + <kbd>Alpha1</kbd> | Timer 1 goes back 5 seconds (AITD1 only)
| <kbd>Shift</kbd> + <kbd>Alpha2</kbd> | Timer 2 goes back 5 seconds (AITD1 only)
| <kbd>Esc</kbd> | quit (only in fullscreen mode)

## Shortcuts

| Key | Description |
| :-: | - |
| <kbd>D</kbd> | display mode
| <kbd>F</kbd> | camera follow mode
| <kbd>R</kbd> | room's visibility
| <kbd>C</kbd> | camera area's visibility
| <kbd>T</kbd> | trigger's visibility
| <kbd>A</kbd> | actor's visibility
| <kbd>E</kbd> | show extra info (AITD1 only)
| <kbd>Page up</kbd> <br/> <kbd>page down</kbd> | camera rotate
| <kbd>Tab</kbd> | switch to model viewer

Put mouse on a box to highlight it.
- Light gray boxes are colliders which player cannot passthrough
- Blue boxes are colliders that player can interact with or used for actor instantiation
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

| Key | Description |
| :-: | - |
| <kbd>4</kbd>  <kbd>6</kbd>| move actor left / right
| <kbd>8</kbd> <br/> <kbd>2</kbd> | move actor down / up
| <kbd>7</kbd>  <kbd>9</kbd> | rotate actor left / right
| <kbd>0</kbd> | hold it while pressing the numpad keys above to update at a higher rate

## Actor slot swap
When the game is linked to DOSBox, you can swap two actors slot positions this way : 
- Highlight an actor
- Press <kbd>X</kbd>
- Type a number between 0 - 49 (with keypad or alphanumeric keys)
- Press <kbd>Enter</kbd>

# Multi-monitor setup

It's not possible to have the viewer running fullscreen and at same time focus to be on another window (eg: the game itself).
One solution is to play the viewer in window mode (by pressing <kbd>Alt</kbd> + <kbd>Enter</kbd>)

# Building it from the source

You can use any recent version of but Unity [5.5.4p3](https://unity.com/releases/editor/archive) is recommended.

# Model viewer

## Installation

Copy the following files to GAMEDATA folder:
- LISTBODY.PAK, LISTANIM.PAK
- LISTBOD2.PAK, LISTANI2.PAK (AITD1 only)
- ITD_RESS.PAK (for palette)
- CAMERAxx.PAK (for palette, JITD only)
- TEXTURES.PAK (Time Gate only)

## Commands

| Mouse | Description |
| :-: | - |
| <kbd>Wheel</kbd> | zoom in / out
| <kbd>Left button</kbd> | click on model and drag to rotate it
| <kbd>Middle button</kbd> | move model
| <kbd>Right button</kbd> | show options menu

| Key | Description |
| :-: | - |
| <kbd>←</kbd> <kbd>→</kbd> | change model
| <kbd>↑</kbd> <br/> <kbd>↓</kbd> | change animation
| <kbd>Shift</kbd> | hold it while pressing <kbd>←</kbd> or <kbd>→</kbd> to skip 10 models at once
| <kbd>Space</kbd> | change model folder (Edward or Emily)
| <kbd>B</kbd> | bounding box mode (default / cube / max)
| <kbd>Esc</kbd> | quit (only in fullscreen mode)

## Shortcuts

| Key | Description |
| :-: | - |
| <kbd>D</kbd> | details high / low
| <kbd>R</kbd> | camera auto rotate mode
| <kbd>E</kbd> | show extra information
| <kbd>A</kbd> | enable animation
| <kbd>X</kbd> | export current model to OBJ format
| <kbd>Shift</kbd> + <kbd>X</kbd> | export all models to OBJ format
| <kbd>Tab</kbd> | switch to room viewer

## OBJ export

To view models properly in Blender, you have to create custom shaders.

DaSalba has created a [python script](https://gist.github.com/DaSalba/87dff887d88f6abc4bb7c22a902ba369) that automatically import the exported OBJ file into blender and create the necessary materials.