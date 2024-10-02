# UnderseaExplorers
Interactive Agents and Procedural Generation coursework

## Video

[![Undersea Explorers Demo Video](https://img.youtube.com/vi/Xg4p_Rpp4bc/0.jpg)](https://www.youtube.com/watch?v=Xg4p_Rpp4bc)

## Level Generator

The level generator is comprised of 3 main sections:

- Generate a random grid
- Apply cellular automata
- Post processing

To generate a random grid a map of int is created of size [width, height], each element of which corresponds to a 
tile coordinate in the level. This is then populated with random values of either 0 or 1 to create a random noise 
map. These values are determined by a System.Random variable where the seed is either random (based off the 
current datetime ticks in ms) or predetermined so that levels can be recreated.

Next, the cellular automata functions are applied to the noise map. First a smoothing function is applied a 
specified number of times to create more distinct regions. This works by iterating through each coordinate in the 
map and calculating how many surrounding cells there are which are 1 space away and have a value of 1. If the 
surrounding cells with a value of 1 exceeds a certain threshold, this cell is assigned as 1, and if not it is 0. 

Following this a branching function is applied, the purpose of which is to extend tendrils of filled regions to 
give a more reef-like structure. Similar to with the smoothing function, it iterates through each cell and 
calculates how many filled cells with a value of 1 surround it. This time it checks all cells which are within 2 
of the cell we are checking. On top of this there is another condition which checks whether the cells immediately
surrounding this one are all on one side. If they are, and the current cell has a value of 0, then the current 
cell is randomly filled with a 2/3 chance of being set to 1. This has the effect of extending a branch in a 
specific direction to create the tendrils. Additionally, if 4 of the cells immediately surrounding it are filled
then it will also be set to 1. The branching function is also iterated a specified number of times before a cell
cleaning algorithm is finally applied.

The cell cleaning algroithm works by separating each region of 1's or 0's into a list of Vector2Int's, and then
determining whether the size of this region is above the preset minimum size. If it is not then the region is 
converted from 1's to 0's or vice versa, essentially removing this region from the final level. To calculate 
each region the entire map is iterated over, with a flood fill algorithm being applied to each cell. Once a cell
has been assigned to a specific region it will not have the algorithm applied again. When all the regions have 
been sorted into lists of vectors, the list size is checked to see whether it is above or below the minimum 
threshold for a region of that type (1 or 0).

Finally, post processing is applied which fills the level with tiles of a type & orientation determined by its' 
surrounding cells. The type is calculated by checking the cells to the north, south, east and west of the 
current tile. Depending on which of these have a value of 1 or 0 tells you whether it is a corner, collumn, edge,
center, end, floating or ocean tile. Next, the orientation is determined in a similar fasion and an appropriate 
rotation in the z axis is applied. Once all of this is calculated the tile is instantiated from the prefab and 
the level is finally finished!

## Agents

Both agents have a basic movement script attached to their prefabs and use behaviour trees based on the NPBehave
library to determine which action to execute.

DIVER

The motion of the diver is staggered, to simulate a breast stroke style swimming motion. This is done by adding
the amount of movement for a certain number of updates, and then executing it in the next updates. The diver 
continues to freely rotate throughout the entirety of the motion as it does not ruin the effect of the swimming
pulsations. 

The basic behaviour of the diver is to follow the reef around the edge until it finds a pearl, at which point it 
goes to collect it, or a mermaid, at which point it will immediately swim in the opposite direction. The edge 
searching behaviour is comprised of two parts, turning towards the wall and turning away from it. The blackboard is 
updated via a raycast from the diver at +-45 degrees to determine which side the wall is on, and how far the diver 
is from it. If the diver is too far then it will turn into the wall, if it is too near it will slow down and turn 
away from it. If both walls are close there is another blackboard condition which lets the diver know which is 
closer and it will carefully navigate between them.

When the diver evades the mermaids based on their proximity. If they get too close it will first determine if any
walls are near using the same turn away from wall action as before. If there are no walls close by it will face 
the opposite direction of the mermaid and continue at full speed. The closest mermaid distance is determined by a
blackboard variable which is populated by a function which iterates through the list of mermaids (enemies) and 
checks the distance from each before outputting the transform of the closest one. This value is then used to 
calculate the exact position of the mermaid and let the diver know which way to turn to face the opposite direction.

If the diver come close to a pearl and there are no mermaids around it well head towards it in a similar way to how
it avoids the mermaids. The only real difference is that it aims towards the pearl rather than away from it.

The priority order for this is evading mermaids > persuing pearls > searching the edge of the region.

MERMAID

The mermaids exhibit continuous motion as they are sea creatures who find it easy traverse underwater environments.
The basic behaviour tree consists of the mermaid circling in a figure of 8 pattern until a diver gets within a 
specified distance, at which point it will chase the diver for 5 seconds before giving up and returning to the 
figure of 8 pattern.

The figure of 8 pattern is acheived by using a blackboard conditon to determine the turn direction. The blackboard
variable is changed from true to false and back again when the roation of the mermaid returns to (close to) its'
original rotation when it is spawned. By moving forwards and turning a constant amount the mermaid will swim in a
circle, and then when the turn direction is changed it will swim in a circle in the opposite direction. Similar to
the diver, the mermaid will first check if there are any walls close by and avoid those first.

Similar to the divers evading behaviour, the mermaids seeking behaviour finds the closest diver and if they are within
a specified distance then the mermaid will persue. This works in an almost identical fasion to evading except that the 
target direction is opposite. Also similar to the diver, the mermaid checks to see if it is going to hit and walls first
and turns away if it is.
