# AITD room viewer

This is a room viewer(and 3D model viewer) for the game Alone in The Dark (1992).
It has been created mostly for speedrunning.

## Instructions 

1. Make sure you have original version of the game installed somewhere.
2. Start QuickBMS
 - Select "alonedark.bms" script
 - Select all files named ETAGE00.PAK, ETAGE01.PAK, ETAGE02.PAK, ... in game folder (use ctrl + left click)
 - Select an output folder
3. Now you should have folders like ETAGE00, ETAGE01, ETAGE02, ... in output folder
4. Copy ETAGEXX folders to the same folder as "AITD room viewer" application (where the EXE is)

## Commands

- Click on the map and drag cursor to move it.
- Mouse wheel : zoom in / out
- Left mouse cursor : drag map
- Insert, delete : rotate camera
- Home, end: change floor
- Page up, page down : change room
- C : change camera to perspective / orthographic mode
- T : display / hide triggers (red boxes)
- L : link / unlink game to DOSBOX process (see dedicated part)
- F : free camera / camera follows current room / camera follows player
- H : display / hide room walls
- A : display / hide camera areas
- J : display / hide actors
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
- Press "L" in the viewer

If everything is OK, you should now view a white square with an arrow, this is player position.
If you leave current room, next room is switched automatically.
Note : once game is linked, you can start another new game, or load a saved game, it will continue to work.

# Model viewer 

## Installation 

Repeat room viewer installation steps with QuickBMS but select files LISTBODY.PAK and LISTBOD2.PAK. Use TAB to switch between room viewer and model viewer

## Commands 

- Left, right arrows : change model 
- Up, down arrows : change model folder (only difference is Edward vs Emily) 
- Shift : hold it while pressing Left or Right to skip 10 models at once
- Mouse wheel : zoom in / out
- Left mouse button : click and drag cursor on model to move camera 

## Known bugs/limitations :

- there is z-fighting on some models (eg : wardrobe doors, oil can, vinyl, ...)  
- some materials are not supported :
  * noise (as seen on doors, zombie chicken, ...)
  * vertical and horizontal gradients (eg: oil can, cookie box, sword, ...)
  3D models in the model viewer will appears like original game running in low quality 
    (except for transparency which is supported).
