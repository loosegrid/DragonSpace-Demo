# DragonSpace-Demo
 A simple boids simulation to show the difference between spatial partitioning structures

 ![a boids simulation switching between differen visualizations of spatial partitioning structures](BoidsGif.gif)

 This is a C# implementation of the data structures described in [this StackOverflow answer](https://stackoverflow.com/questions/41946007/efficient-and-well-explained-implementation-of-a-quadtree-for-2d-collision-det) by user Dragon Energy, along with some boids to test them with. 

 The linked series of answers are an extremely detailed explanation of the concepts behind spatial partitioning in general and quadtrees and grids in particular. [This section](https://stackoverflow.com/a/48355534) covers the fundamentals. They also include code in C and Java, which is what I started this project by porting. 

## Spatial Partitioning in Unity

This all started because I wanted to do something pretty common - find the nearest GameObject within a certain range. This is simple to do with `Physics.OverlapSphere()` but that can feel very frustrating or wasteful when you aren't using the physics system for anything else.

In reality, it's plenty fast, and *dramatically* simpler than spending a few weeks learning about spatial partitioning and low-level programming optimizations you can't actually use in Unity. Just use colliders configured as triggers and make your game. HOWEVER, if all you need to do is find the nearest object, or find the objects within a query area, it can certainly be faster. Especially in cases where you'll frequently be querying potentially empty areas, when you're using 3d objects but don't need 3d partitioning, and so on.

The project includes boids set up this way as a baseline comparison. In some cases, the speed is comparable, and a quick glance at the code shows how much simpler it is to use. The physics system may be even faster if set up with 2d colliders, but I didn't take the time to figure out how to make them work for a top-down view.

## Quadtrees for games

![a cluster of dots moving diagonally across a quadtree](BlobTree.gif)

I'm not going to be able to give a better explanation of the detailed concepts than the linked answer, but for a quick overview:

Quadtrees (and k-d trees, etc.) are one of the go-to answers when searching for solutions to this problem online. It can be a bit confusing as a beginner because quadtrees are frequently used for things like image compression, which is a completely different use and structure than a quadtree for finding objects in a space, or doing collision detection. In this case, we're (if I'm not mistaken) talking about what Wikipedia refers to as point-region quadtrees. Every object in the tree is stored with information about the space it occupies.

The quadtree splits the space into four quadrants, then when each of those spaces fills up, it splits them again, and so on. Essentially it's like keeping a list of all the objects in a space, but splitting up the list when it gets too long to go through every time you need to find something. Objects that overlap multiple leaves of the tree are inserted into multiple lists. This lets you avoid going through a list of every object in the scene, and if you 

Note that the quadtree in this implementation isn't faster for queries than the loose version below though it should be. That may be due to my implementation, limitations of C#, or a combination of both.

 ![a boids simulation on a loose quadtree, where bounding boxes enclose each group of boids](BoidsGif.gif)

The loose quadtree speeds up queries by only inserting objects into a single leaf, and each node of the tree changes its boundaries to exactly encompass its children. This means that they can overlap, handling objects on the border between quadrants. Most importantly, this means that if a query doesn't overlap with any nodes, there's no need to traverse the tree further or to look at the list of elements at all.

## Grids

Since a major benefit of quadtrees is that they allow you to ignore large empty areas, they aren't actually a great solution for games where space is very crowded and/or evenly filled with objects. In this case, the simplest solution is actually faster. Divide the space into grid squares like a map and insert objects into the relevant cells. When querying, figure out which cells overlap your query and go through their lists. 

This project includes two grid implementations. One is a grid for objects that all have identical sized bounding boxes (see the ["Dirty trick"](https://stackoverflow.com/a/48400502) section of the StackOverflow answer). This is a perfect structure for the boids simulation.

The second is the "[Loose/Tight Double-Grid](https://stackoverflow.com/a/48384354)". This applies the concept of loose nodes from the loose quadtree to a grid, allowing for a bit of the best of both worlds. A lot of the speed of grids in the original examples there (aside from being written in C/C++) comes from the ability to optimize the layout of elements in memory for efficient cache usage. I haven't done any of that optimization but I hopefully will come back to it in the future. On the other hand, I applied the singly-linked list concept from the other code to completely bypass storing lists of elements in the grid at all, by using an interface implemented by objects to be inserted. This can theoretically save memory at the cost of speed but I haven't profiled it. I mostly did it because it seemed like a fun idea and slightly easier to use.

## Beginner warning

Another one of the main concepts explained in those answers is the importance of not storing lists of elements in each node - again for cache locality. This means that usually elements are stored in one big list for the whole tree, and nodes store indexes to that list. Nodes and elements are often using singly-linked lists, which cuts down on memory. This is a great trick, but can be kind of confusing, or lead to bugs like infinite loops if an element isn't found in the node where it's supposed to be. I've tried to expand on explanations in the comments, but if you're for some reason finding this code without already understanding quadtrees, it's definitely easier to implement one in the simplest way possible before trying to make sense of this version.


## License

I don't really know what license is appropriate, but the quadtree C/Java code was posted on StackOverflow with this disclaimer:

> "But feel free to grab the code and put it on GitHub if you like. And adapt it all you like and use it in commercial applications, take full credit, whatever. I don't mind. It's just a couple hours of work and I like seeing people do cool things and I prefer to be in the background."

Any other code I used for reference was posted on Pastebin with no license. So use this code at your own risk, it is almost certainly riddled with bugs.