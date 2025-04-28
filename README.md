# Haptic-VR-Project

This is the Haptic Navigation Unity Demo owned by Flavin Neuromachines Lab (https://flavinlab.io/)

In the Haptic Navigation Scene (Assets\Scenes\Haptic_Navigation.unity) 

The core of this scene is the four scripts stored here (Assets\Scripts_X)

PLAYER CONTROL

There is a player that you can control with WASD for movement and the Mouse for orientation.
You can find the WASD Control Script here (Assets\Scripts_X\Player_Ctrl.cs)
and Mouse Control Script here (Assets\Scripts_X\Camera_Ctrl.cs)

TCP COMMUNICATION

There is a TCP Client Script in our demo, it can connect to the TCP Server if the two machines are connected to the same WIFI
(Assets\Scripts_X\TCP_Client_X.cs) and with the correct Port and IP.

HAPTIC NAVIGATION GUI

This module detects the obstacles around the player and display them with the GUI. (Assets\Scripts_X\Haptic_GUI_X.cs)

the DetectObstacles() function detects the obstacles in the scene. 

the player is the center of the radar, and the obstacles are in the layer you set in the inspector
the obstacles are divided into 6 sectors, each sector is 60 degrees
the function will return a bool array, each element of the array indicates whether there is an obstacle in the sector or not
there is a loop in this function. It will detect every object in your radius. And if the object is labelled with Obstacle or another appointed label (such as high or low). Maybe this can be used to judge whether an object can be walked over.
And if a object labelled as obstacle is detected, this function will get the dirction of this object and get the angle of it and player (0-360, 360 are divided into 6 sectors equally, every sector has 60 degrees.) This function will assign the angle to 
the corresponding sector and set the panel red.

the OnGUI() function is used to draw the GUI in the game view

it will draw the buttons (panels) in the hexagonal shape
the color of the button is red if there is an obstacle in the sector, and green if there is no obstacle

For more info, open the C# script.

Update on April 21th 2025 PS: thanks to Aaron's suggestions

A height  module was added to detect whether an obstacle can be walked over. Two methods are combined: the label way and the RayCast way. I am still working on the perfection of this module to test which method works best (label, raycast, or the hybird). 

A training scene was added to this demo: (Assets\Scenes\Training_Navigation.unity) 

This scene serves as an "obstacle course" to teach participants how to play in VR enviroment.

Panel color:

Yellow: low far obstacle. Red: high far obstacle. Orange: low close obstacle. Purple: high close obstacle.

TODO: 1. Connect our VR with Raspberry Pi (OpenThread Primer) 2. Fixing some minor bugs in the obstacle detection.

I really appreciate your suggestions which help me perfect this demo greatly!



Update on April 27th 2025

rewrite the obstacle detection logic with RayCast() (Assets\Scripts_X\Obs_Det_GUI.cs)

In this new C# script, you can change the number of rays in each sector. If the ray collide the an obstacle, this ray will rise to a high place (you can change the height step) to evaluate the height of an obstacle and categorise it. And the two radii are kept.

Connect it with Raspberry Pi 4B