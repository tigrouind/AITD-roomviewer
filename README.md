# AITD room viewer

This is a room viewer (and 3D model viewer) for the game Alone in The Dark (1992).
It has been created mostly for speedrunning.

AITD2, AITD3 and JITD are also supported.

## Instructions
1. Extract AITD room viewer archive somewhere (eg : on your desktop)
2. Create a folder named "GAMEDATA" inside AITD room viewer folder
3. Make sure you have original version of the game installed on your computer.
4. Download [QuickBMS](http://aluigi.altervista.org/quickbms.htm) and [alonedark.bms](http://aluigi.altervista.org/bms/alonedark.bms) script ([alternative link](https://github.com/tigrouind/AITD-roomviewer/releases/download/1.1.14/alonedark.bms)).
5. Start QuickBMS
   - Select "alonedark.bms" script
   - Select all files named ETAGE00.PAK, ETAGE01.PAK, ETAGE02.PAK, ... from AITD original game folder (use ctrl + left click)
   - Select as output folder the folder "GAMEDATA" you created previously.

## Commands

- Mouse wheel : zoom in / out
- Left mouse button : drag map / highlight box
- Right mouse button : show options menu
- Up or down arrows : change floor
- Left right arrows : change room
- Esc : quit (only in fullscreen mode)

## Shortcuts

- L : link DosBox
- D : display mode
- F : camera follow mode
- R : room's visibility
- C : camera area's visibility
- T : trigger's visibility
- A : actor's visibility
- E : show extra info (AITD1 only)
- V : show vars (AITD1 only)
- Page up / down : camera rotate
- Tab : switch to model viewer

Put mouse on a box to highlight it.
Gray boxes are colliders which player cannot passthrough
Blue boxes are colliders that player can interact with
Red and amber boxes are triggers. It is usually used to switch from one room to another. It can also trigger other things like enemies, sounds, scripted sequences, ...

## Link to DOSBOX process:
This feature allows to view all active entities in the game, displayed and updated in realtime from DOSBOX.

To do this :

- Start DOSBOX
- Start AIDT
- Start a new game in AIDT
- Skip intro, when player is in the attic, don't move
- In the menu options, choose "Link to DOSBOX"

If everything is OK, you should now view a white square with an arrow, this is player position.
If you leave current room, next room is switched automatically.
Note : once game is linked, you can start another new game, or load a saved game, it will continue to work.

## Warp actor
When the game is linked to DOSBOX, it is possible to change the position of an actor by selecting it, then pressing CTRL-W. The actor will be warped to mouse position. It is also possible to manually edit positions of an actor by right clicking on it or by pressing numpad keys :
- 4, 6, 2, 8 : move actor left / right / down / up
- 7, 9 : rotate actor left / right
- If you hold 0 while pressing the numpad keys above, position is updated at a higher rate.

# Model viewer

## Installation

Repeat room viewer installation steps with QuickBMS but select files LISTBODY.PAK and LISTBOD2.PAK.
If you want to be able to view animations, unpack files LISTANIM.PAK and LISTANI2.PAK.

## Commands

- Left, right arrows : change model
- Up, down arrows : change animation
- Space : change model folder (Edward or Emily)
- Shift : hold it while pressing left or right keys to skip 10 models at once
- Mouse wheel : zoom in / out
- Left mouse button : click on model and drag to rotate it
- Right mouse button : show options menu / move model

## Shortcuts

- N : noise material
- G : gradient material
- R : camera auto rotate mode
- E : show extra info
- A : enable animation
- Tab : switch to room viewer