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

| Key | Description |
| :-: | - |
| <kbd>4</kbd>  <kbd>6</kbd>| move actor left / right
| <kbd>8</kbd> <br/> <kbd>2</kbd> | move actor down / up
| <kbd>7</kbd>  <kbd>9</kbd> | rotate actor left / right
| <kbd>0</kbd> | hold it while pressing the numpad keys above to update at a higher rate

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
| <kbd>Tab</kbd> | switch to room viewer