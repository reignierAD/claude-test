# SpaceShooter - Unity 2022 LTS Top-Down 2D Space Shooter

## Setup Instructions

### 1. Open in Unity Hub
- Open Unity Hub
- Click "Add project from disk"
- Select the SpaceShooter folder
- Open with Unity 2022.3 LTS

### 2. Let Packages Import
- Unity will automatically import required packages
- Wait for compilation to complete

### 3. Open the Scene
- In the Project window, navigate to Assets/Scenes/
- Double-click MainScene.unity to open it

### 4. Create Prefabs
You need to create the following prefabs manually:

**Player Prefab:**
- Create a Quad GameObject
- Add PlayerController script
- Add BoxCollider2D (set as Trigger)
- Tag it as "Player"
- Drag to Assets/Prefabs/ folder

**Enemy Prefab:**
- Create a Quad GameObject
- Add Enemy script
- Add Rigidbody2D (set Gravity Scale to 0)
- Add BoxCollider2D (set as Trigger)
- Tag it as "Enemy"
- Drag to Assets/Prefabs/ folder

**Bullet Prefab:**
- Create a small Quad GameObject (scale 0.2, 0.5, 1)
- Add Bullet script
- Add BoxCollider2D (set as Trigger)
- Tag it as "PlayerBullet"
- Drag to Assets/Prefabs/ folder

**EnemyBullet Prefab:**
- Create a small Quad GameObject (scale 0.2, 0.4, 1)
- Add EnemyBullet script
- Add BoxCollider2D (set as Trigger)
- Tag it as "EnemyBullet"
- Drag to Assets/Prefabs/ folder

**UIManager:**
- Create a Canvas in the scene
- Add Text (TMP) objects for Score and Lives
- Add a Panel for Game Over screen with Text inside
- Add UIManager script to a GameObject
- Assign references in Inspector

### 5. Assign References in Inspector

**GameManager GameObject:**
- Add GameManager script component

**EnemySpawner GameObject:**
- Add EnemySpawner script component
- Assign Enemy prefab to "Enemy Prefab" field

**PlayerController:**
- Assign Bullet prefab to "Bullet Prefab" field
- Assign a child Transform as "Fire Point"

**Enemy Prefab:**
- Assign EnemyBullet prefab to "Enemy Bullet Prefab" field

### 6. Build for WebGL
- File -> Build Settings
- Select WebGL platform
- Click "Switch Platform"
- Click "Build and Run"

## Game Controls
- WASD - Move player ship
- SPACE - Shoot
- R - Restart (after game over)

## Game Rules
- Defeat enemies to score points (10 points each)
- Player has 3 lives
- 1.5 second invincibility after being hit
- Waves get harder (more enemies) as you progress
- Game over when all lives are lost
