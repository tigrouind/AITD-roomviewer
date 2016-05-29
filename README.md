# AITD room viewer

This is a room viewer (and 3D model viewer) for the game Alone in The Dark (1992).
It has been created mostly for speedrunning.

## Instructions 
1. Extract AITD room viewer archive somewhere (eg : on the desktop)
2. Make sure you have original version of the game installed on your computer.
3. Start QuickBMS
 - Select "alonedark.bms" script
 - Select all files named ETAGE00.PAK, ETAGE01.PAK, ETAGE02.PAK, ... from AITD original game folder (use ctrl + left click)
 - Select as output folder the folder "GAMEDATA" (which is inside "AITD room viewer" folder).

## Commands

- Click on the map and drag cursor to move it.
- Mouse wheel : zoom in / out
- Left mouse cursor : drag map
- Right mouse cursor : show options menu
- Up or down arrows : change floor
- Left right arrows : change room
- Esc : quit (only in fullscreen mode)

Put mouse on a box to highlight it.
Gray boxes are colliders which player cannot pass trought
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

# Model viewer 

## Installation 

Repeat room viewer installation steps with QuickBMS but select files LISTBODY.PAK and LISTBOD2.PAK. 

## Commands 

- Left, right arrows : change model 
- Up, down arrows : change model folder (only difference is Edward vs Emily) 
- Shift : hold it while pressing Left or Right to skip 10 models at once
- Mouse wheel : zoom in / out
- Left mouse button : click and drag cursor on model to rotate model
- Right mouse cursor : show options menu / move model

## Known bugs/limitations :

- there is z-fighting on some models (eg : wardrobe doors, oil can, vinyl, ...)  
- vertical and horizontal gradient materials are not supported (eg: oil can, cookie box, sword, ...)
