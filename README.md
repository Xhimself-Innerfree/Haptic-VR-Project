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

A height module was added to detect whether an obstacle can be walked over. Two methods are combined: the label way and the RayCast way. I am still working on the perfection of this module to test which method works best (label, raycast, or the hybird).

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

Update on May 5th 2025

Dynamic Scene added here (haptic-3D-v1\Assets\Scenes\Newest Version\Dynamic_Navi.unity) not the "Dynamic_Navi" folder
Here, An AI module (Unity.AI_Navigation)was added to make the NPCs mobile.

In this demo, three common dynamic situtions are presented (man, car, traffic light)

you can check there is a man walking randomly on the lawn, a man and a car moving on their set paths.
Walk around them to see the changes of your panels.

A traffic light is added as well, once the traffic light turns red. your panel turns purple. Only standing infront of this traffic light can see this change (-45 degrees to 45 degrees)

Update on May 12th 2025

Based on Aaron's valuable inspiration, a sleep timer is added to the Obs_Det_GUI to stop it from vibrating all the time.
the logic as follows:
the "Ray" hits an obstacle and returns a struct with 1 the distance and 2 the type of obstacle. So, if the distance between player and obstacle is decreasing which means that the player is getting closer to the obstacle, the unit will keep vibrating to inform the user of incoming situation whether passsive or active(the obstacle is moving to the player or the player is walking to it). However, the sleep timer is "When the distance between player and obstacle is not decreasing", the unit will keep vibrating for 0.5 seconds and sleep for 2.5 seconds then vibrate for 0.5 seconds and so on. In a sleep duration, if the distance decreace again, the unit will vibrate at once. Plus, the sleep duration can be changed based on users' preferences.

For the detection of "Cars" or something is moving very fast.
A label based method is used here. And there is a CarRadius for its detection. A RayCast method will replace it soon.

For multiple haptic units, a Balance_GUI is added to assist the player to keep balance
This haptic device has 7 units, the center unit present the height differences between where the player stand on and where the player will step on if player moves forward. (height difference = step on - stand on, 0.1 < yellow < 0.3, 0.3 < orange, -0.3 < red < -0.1, purple < -0.3)
And the 6 units around represent the orientation player should lean to keep the balance of his body.(Angle: green < 5, yellow < 15, red < 30, 30 < orange )
There are a few slopes on the lean, try the Balance_GUI on them!

RIght now, "Obs_Det_GUI.cs" has 472 lines and contains too much contents which makes it difficult to understand. So, I am going to devide it into several minor modules.

June 2 2025
basic idea:
I am considering using a more active navigation method. The obstacle detection method could not tell whether there is a way that the user can walk, such as a situation like this: there are two trees in front of the user, so the upper 2 or 3 panels are red, which may confuse the user that there is a wall or at least 120° he/she need to avoid. But actually, there may be a way that the user can walk between the trees. So I am considering making the panels vibrate one by one: each panel vibrates for 1 second, and we divide this 1 second into several parts depending on how many rays we have in a 60° sector. And the panel vibrates based on the collision info of each ray, which may give a more precise perception of the environment to the user. Take the two trees example again, if the first tree is 0° to 15°, the other tree is 105° to 120°. The first panel will vibrate for 0.25 seconds (15°) and stay mute for 0.75 seconds (15°-60°), then work clockwise. The second panel stays mute for 0.75 seconds (60°-105°) and vibrates for 0.25 seconds (105°-120°). I suppose this can help the user find their way quicker.

A new script is added here(./Assets/Scripts_X/GUIs/Active_Mtd.cs)
the working panels will exhibit the collisoin information of each ray in time slice (divide a second by the number of the rays). while the mute panels remain grey. While the user may not tell the differences between accurate time gaps, this script aims to provide a vague active navigation for the user, ensuring users will not miss some shortcuts.
Plus, thanks to Aaron's feedback, the high resolution provides a good solution. Another script was made (./Assets/Scripts_X/GUIs/Obs_Det_GUI_HighReso.cs) which provide 12 panels by default while the user could change the number of panels in the inspector page of Unity Engine.
