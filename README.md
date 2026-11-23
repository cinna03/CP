# Cinnarios

Cinnarios is an iPhone app that hangs a shop print on the buyer’s wall at the centimetres the shop recorded. The next three pictured suggestions stay on that same wall.

This repository is the product: one Unity scene, built for iOS. The shop that supplies the paintings is a separate web app, [cinna03/marketplace-repo](https://github.com/cinna03/marketplace-repo), running at [https://cinnarios.onrender.com](https://cinnarios.onrender.com).

Capstone project, BSc Software Engineering (AR/VR), African Leadership University.

## What the session does

The app opens on the shop page inside a web view. The camera stays off until the buyer asks to try a painting.

1. The buyer signs in on the phone with the same account as the laptop, or taps **Open in camera** on the phone itself.
2. A try link arrives: `cinnarios-ar://try/{sourceId}?api={shop origin}`. The older `cinnarios://try/` form is still accepted. The link carries the painting id and the shop address. It carries no password. An own-piece link is ignored.
3. The app loads `GET /v1/integrations/ar/artworks/{sourceId}` and shows a card at the shop’s centimetres as soon as the size is known. The picture fills in afterwards.
4. The buyer aims at a wall until the card appears, then taps, drags, and locks. Drag stays on the wall that was captured. It does not tilt or resize the print.
5. Lock stores the world pose and scale 1, and puts them back every frame, so walking closer makes the print larger and walking away makes it smaller.
6. A sentence such as “it is too big” returns at most three pictures. Choosing one calls Move, then loads that painting onto the same wall. “Move it” starts a new spot of the same painting.

Scanning can move to aiming, aiming to positioning, and positioning to locked. Scanning cannot jump to locked.

## Size, price, and pictures

Centimetres become metres in one class, `PaintingDimensions`, by dividing by 100. A 60 cm side is 0.6 m. A size of 0 is refused.

The phone shows the reproduction price in Rwandan francs. A 60 × 80 cm print is RWF 502,311.

The museum image host often answers this app with a block page. `TextureDownloader` skips that host and loads the same photograph from Wikimedia Commons.

The session uses AR Foundation 6.7.1 on ARKit. Placement depends on the camera’s vertical plane detection. A depth sensor is not required. The iOS deployment target is 15.0. The bundle id is `com.mwiza.cinnarios`.

## Project layout

Open `Assets/cinnarios/Scenes/WallPlacement.unity`. It is the only scene in the iOS build.

```
Assets/cinnarios/
├── Scenes/WallPlacement.unity     the session
├── Rules/                         plain C# rules, no scene objects
│   ├── PaintingDimensions.cs      the only centimetre-to-metre conversion
│   ├── AreaPricingRule.cs         the reproduction price, shown in RWF
│   ├── TryLink.cs                 cinnarios-ar://try/ and cinnarios://try/
│   ├── PlacementStateMachine.cs   Scanning, Aiming, Positioning, Locked
│   ├── DecisionTalk.cs            reads what the buyer typed
│   └── WallAdvice.cs              at most three next paintings
├── Scripts/
│   ├── Launch/                    the shop web view and the try link
│   ├── Placement/                 the wall, the finger, the lock
│   ├── Painting/                  the mesh and the picture on the front
│   └── Catalogue/                 the shop record and the image download
├── Tests/Editor/                  Edit Mode tests for the rules
└── Shaders/Print.shader           the unlit front of the print
```

Scene objects coordinate the session. The rules are plain classes created in code. `WallPlacementController` contains the state machine, the slider, and the smoother. It does not inherit them. `ARWallRaycaster` implements `IWallHitProvider` and accepts a vertical plane only. `CanvasMeshBuilder` and `PanelMeshBuilder` share one mesh builder and differ by the painting’s medium.

`ShopStage` shows the shop and pauses the page when the camera starts, so a sign-in survives. The sealed scene points at `https://cinnarios.onrender.com`.

## Open it

Use **Unity 6000.5.0f1**. Open this folder as the project. The packages in `Packages/manifest.json` include AR Foundation 6.7.1, ARKit 6.7.1, the Input System, and the gree unity-webview package.

Build for iOS from `WallPlacement`. The phone must be able to reach the shop. A laptop **Open in camera** reaches only the phone signed into the same account. The shop keeps that request for 90 seconds.

## Tests

In the Unity Editor, open **Window → General → Test Runner**, choose **Edit Mode**, and run `Cinnarios.EditModeTests`.

The cases do not start a camera session. They check that 60 cm becomes 0.6 m, that a size of 0 is refused, that a 60 × 80 cm print keeps the shop’s price, shown on the phone as RWF 502,311, that a try link keeps the painting and the shop, that an own-piece link is ignored, that scanning cannot jump to locked, that “too big” asks for a smaller print, and that “move it” stays on the same painting.

The shop’s own tests live in the marketplace repository.

## What this build leaves out

The shop checkout does not take a card. That is the shop’s rule, not a screen in this app. A page for a print the buyer already owns exists on the shop. This app does not open it.
