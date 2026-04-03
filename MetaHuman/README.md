# MetaHuman Desktop Pet

A lightweight Unity desktop pet application featuring transparent window support.

## Features

- **Transparent Window**: The application runs as a transparent overlay on your desktop
- **Interactive Pet**: Click on the pet to make it move to a random position
- **Idle Animations**: The pet performs random idle animations when not moving
- **Always on Top**: The pet stays visible above other windows

## Requirements

- Unity 6000.3.10f1 or later
- Universal Render Pipeline (URP)
- Windows 10/11 (for transparent window support)

## Setup Instructions

### 1. Build Settings

1. Open **File > Build Settings**
2. Select **PC, Mac & Linux Standalone**
3. Set **Target Platform** to Windows
4. Set **Architecture** to x86_64

### 2. Player Settings

In **Edit > Project Settings > Player**:
- Set **Default Is Native Resolution**: Off
- Set **Default Screen Width**: 1920
- Set **Default Screen Height**: 1080
- Set **Resizable Window**: On
- Set **Fullscreen Mode**: Windowed
- Set **Run In Background**: On

### 3. Running the Pet

1. Press **Play** in Unity Editor or build the standalone application
2. The pet will appear on your desktop
3. Click on the pet to make it move randomly

## Project Structure

```
Assets/
├── Scripts/
│   ├── WindowTransparency.cs    # Windows API for transparent window
│   ├── DesktopPet.cs             # Pet behavior and movement
│   └── TransparentWindowInit.cs  # Initializes transparent window
├── Prefabs/
│   └── DesktopPetSetup.prefab    # Pre-configured pet prefab
└── Scenes/
    └── SampleScene.unity         # Main scene with pet setup
```

## Customization

### Changing the Pet Sprite

1. Create or import a sprite for your pet
2. Select the **DesktopPet** object in the scene
3. In the **Sprite Renderer** component, assign your new sprite

### Adjusting Movement Bounds

1. Select the **DesktopPet** object
2. Modify the **Screen Bounds** in the **Desktop Pet** component:
   - `x`: Horizontal bounds (default: 500)
   - `y`: Vertical bounds (default: 300)

### Animation Setup

The pet uses an Animator controller with the following parameters:
- `IsMoving` (bool): Controls moving animation
- `IdleType` (int): Selects random idle animation
- `PlayIdle` (trigger): Triggers idle animation playback

## Technical Details

### Transparent Window Implementation

The transparent window is achieved using Windows API:
- `SetWindowLong` with `GWL_EXSTYLE` to add `WS_EX_LAYERED` style
- `SetLayeredWindowAttributes` to set window transparency

### Rendering

- Uses **Orthographic** camera for 2D sprite rendering
- Camera background is set to fully transparent
- URP rendering mode set to **Transparent** for proper alpha blending

## Troubleshooting

### Window Not Transparent
- Ensure you're running on Windows 10/11
- Check that the game view background color is set to transparent
- Verify the desktop pet sprite has alpha channel

### Pet Not Clickable
- Ensure the **DesktopPet** object has a **Collider 2D** component
- Check that the **Physics 2D Raycaster** is attached to the camera

## Future Enhancements

- [ ] Add drag-and-drop movement
- [ ] Implement pet AI behaviors
- [ ] Add speech bubble / dialogue system
- [ ] Support for animated sprites (Sprite Sheet)
- [ ] System tray integration
- [ ] Pet-to-pet interactions
