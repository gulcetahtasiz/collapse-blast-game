
# Collapse & Blast Game 

<p align="center">
  <img src="https://github.com/user-attachments/assets/6f7d7e85-24d6-4d86-ab54-0a31557118e2" width="32%" />
  <img src="https://github.com/user-attachments/assets/ecb1a4c2-63c4-4183-9fc6-ea5b35af5cc1" width="32%" />
  <img src="https://github.com/user-attachments/assets/0c4b180d-61fd-4b76-a05c-e6a169888e29" width="32%" />
</p>
This is a grid-based collapse & blast puzzle game developed in Unity.

## Gameplay
 - The game runs on an M x N grid (2-10).
 - Blocks with the same color form groups.
 - Groups of 2 or more can be blasted.
 - After a blast, blocks collapse downward.
 - New blocks spawn from the top.
 - The game always guarantees at least one valid move.


## Player Configuration
Before starting, the player can set:
- M : rows
- N : columns
- K : number of colors (1-6)
- A, B, C : group size thresholds for visual icons
Blocks display different icons depending on their group size.


## Deadlock Handling
- The game detects when no valid move exists.
- Instead of blind shuffling, a single block is modified to create a valid group.


## Performance Highlights
- Logic and visuals are fully separated
- Local group updates instead of full grid scans
- BFS with reused data structures
- Object pooling for blocks

## How to Run
 1. Open the project in Unity
 2. Press Play
 3. Enter values and click Start
 4. Click groups to play
Tested on Unity 6000.0.33f1 (Silicon)
