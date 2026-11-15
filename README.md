# AR artwork decision-support prototype

An iPhone prototype for online buyers of physical art in Kigali. A buyer can look through a catalogue stored on the phone, place a work on a wall at its stored size, ask short questions about fit, and keep or set aside the work before buying.

The buyer study itself is separate. This repository is the prototype.

**Technical report:** [Report/ProductTechnicalReport.pdf](Report/ProductTechnicalReport.pdf)

**Demonstration video:** not recorded yet. The link will be added here after the iPhone 13 recording.

## Requirements

| Item | Version or device |
| --- | --- |
| Unity | 6000.5.0f1 |
| AR Foundation | 6.5.0 |
| ARKit | 6.5.0 |
| Universal Render Pipeline | 17.5.0 |
| Input System | 1.19.0 |
| XR Interaction Toolkit | 3.5.0 |
| Phone | iPhone 13. It has no LiDAR, so the wall is found as a vertical ARKit plane. |
| Editor check | macOS, Unity Game view. If no vertical plane is tracked, the visit uses a flat studio wall. |

Android is not part of this build. ARCore is present in the Unity template packages and is not the path this prototype uses.

## Install and run

1. Open the project in Unity `6000.5.0f1`.
2. Open `Assets/Scenes/SampleScene.unity` and press Play.
3. Enter one of the 25 entrance codes, for example `AZURE`, or tap **New Participant (Auto-ID)** for `P01`, `P02`, and so on.
4. Open Catalogue, choose a size and a frame, then **Try on My Wall**.

The iPhone export and the demonstration recording are still to be done. When that build is made:

1. Use File, Build Settings, iOS, then Build. Unity clears the signing team on every export.
2. In Xcode, set the team again and keep the bundle id `com.unity.template.armobile`.
3. Install on the iPhone 13. A clear, lit wall helps vertical plane detection. A blank or crowded wall can fail to produce a plane.

If the camera is off, the visit asks for Camera in iPhone Settings. If tracking drops after the session has started, the hint asks the buyer to point slowly at a well-lit wall.

## Project structure

Product code is in `Assets/_Project/Scripts`.

| Folder | Responsibility |
| --- | --- |
| `Data` | The artwork record, the visit, taste notes, and theme colours. |
| `Services` | Search, written answers, recommendations, entrance-code profiles, gallery files, and the study log. These are plain C# classes behind interfaces. |
| `UI` | Screens that bind those results and listen to taps. |
| `AR` | The wall quad. Centimetres divided by 100 become metres. |
| `Editor` | Menu tools for demo notes, the study-log folder, and iPhone texture size. |
| `Tests` | EditMode checks for the services above. |

`Assets/Capstone` holds the earlier checked record, size line, filters, and ranking table, with its own EditMode tests. `Assets/Intro` holds the entrance screen, the main menu, and the live wall visit. `Assets/Resources/CatalogueShelf.asset` is the offline list of 25 works.

The pictures and records stay on the device. There is no account and no hosted catalogue.

## Tests

In Unity, open Window, General, Test Runner, choose EditMode, and run all tests.

On 14 November 2026 the EditMode suite recorded 24 passed and 0 failed. Those tests cover the stored record, centimetres divided by 100, the older filters, the written size reply, the first Keep and Dismiss ranking, and the 25 entrance codes.

`Assets/_Project/Scripts/Tests` adds later checks for word search, taste-aware recommendations, and separate favorites for two codes. Those later checks were not part of the 14 November run.

## Scope and study files

**Finish Visit** writes `StudyLogs/confidence_study_log.csv` under the app persistent data folder. Entrance codes are listed in `UserProfiles/users_registry.json`. Hearts are stored per code. Wall photos are stored per code under `WallGallery/{code}/`.

Tools, Capstone, Open Study Log Folder opens that folder in the editor.

This build does not take payment, arrange delivery, or keep a shop account. The pictures are bundled so the prototype can be tried. They are not a consented Kigali catalogue.
