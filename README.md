# Capstone

A portrait prototype that places one catalogue work on a detected wall at the centimetres stored on that work. The buyer starts with one of 25 consent codes, sets style, mood, and colour, then picks a work. The camera opens when that work is placed. One question is matched from a written English, French, and Kinyarwanda table. Keep, save, or dismiss ranks three other works and shows their story notes.

This folder is the implementation. The five-page technical report and the walkthrough video are separate from this code.

## Open the project

1. Install Unity `6000.5.0f1`.
2. Open this folder as the project.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press Play.

Packages used by the prototype:

| Package | Version |
| --- | --- |
| Universal Render Pipeline | 17.5.0 |
| AR Foundation | 6.5.0 |
| ARCore | 6.5.0 |
| Input System | 1.19.0 |

The study build target is Android, minimum SDK API 30, ARM64, IL2CPP. ARCore is required. The scene in the build is `Assets/Scenes/SampleScene.unity`.

In the editor, Play asks for Room 1, Room 2, or Room 3 when the buyer is ready to place. Place then uses that room wall. On a phone, Place raycasts the centre of the screen onto a vertical plane.

## Buyer order

`BuyerPath` is the order. The placement screen follows it.

1. Consent code. `ConsentCodes` holds the 25 codes. `BuyerSession.TryStart` accepts one of them, trims spaces, and marks consent. Any other text stays on the code page and says `Try again.`
2. Style, mood, and colour. The choice becomes `PreferenceProfile.FromInitialTags`.
3. Catalogue. The buyer picks a work, then presses `Place this work`.
4. Wall. This is the only step that opens the camera. The quad uses `Artwork.WallSizeMetres`, which comes from the stored centimetres.
5. One question, or Skip. `RuleEngine.Match` reads `StarterRules`. The reply language is the language of the matched words. No match returns one fixed prompt and logs nothing.
6. Keep, save, or dismiss. `Recommender.RankThree` returns three other works. The status line includes each story note.
7. `Change tags` starts again at step 2. `End` stops. The five uncertainty questions are not in the app.

A keep, save, dismiss, matched question, or chosen rank is stored only after consent. `SessionTranscript.Format` prints that record. The same text is written to `capstone-session.txt` under `Application.persistentDataPath` when the device allows the write. The rules and the catalogue do not call a network.

## Where the behaviour lives

| Behaviour | Code | Test |
| --- | --- | --- |
| 25 consent codes and session start | `Assets/Capstone/Session` | `BuyerSessionTests`, `BuyerPathTests` |
| Buyer order, camera only at the wall | `BuyerPath` | `BuyerPathTests` |
| Stored centimetres to wall size | `Assets/Capstone/Catalogue` | `ArtworkScaleTests`, `ArtworkRecordTests` |
| Pose on a vertical face | `ArtworkWallPose` | `ArtworkWallPoseTests` |
| Style, mood, colour filter | `CatalogueFilter` | `CatalogueFilterTests` |
| Keyword table, three languages, fixed prompt | `Assets/Capstone/Dialogue` | `RuleEngineTests` |
| Tag weights, keep, save, dismiss, top three | `Assets/Capstone/Ranking` | `RecommenderTests` |
| Session text | `SessionTranscript` | `SessionTranscriptTests` |
| Catalogue-only study condition | `StudyCondition`, `PrototypeScreen` | `StudyConditionTests`, `PrototypeScreenTests` |

The five catalogue records are stand-ins. Each one already has an id, stored centimetres, style, mood, colour, and a story note. A consented work replaces the image and the notes. The ranker and the wall keep reading the same fields. That is the catalogue this build can run. It is not a set of pictures pulled from the internet.

## Algorithms

The rule table tokenizes the question into letters. Each rule counts hits in English, French, and Kinyarwanda. The row with the most hits wins. A tie uses the fixed concern order, then the rule id. The reply uses the language of the matched words. If two languages tie, the language of the earliest matched word wins. Zero hits return the fixed prompt. The match does not invent a sentence and does not call a model.

Ranking adds the style weight, the mood weight, and the colour weight on each other work. Keep and save add one to the placed work's tags. Dismiss subtracts one. A matched colour concern adds one to that colour. The current work is left out. A higher score wins. An equal score uses the artwork id, earlier id first. The result is three works.

Placement does not choose a size. `ArtworkScale` turns the stored centimetres into metres, and `ArtworkWallPose` puts that quad on the hit point, flat to the wall, facing the camera.

## Tests

The tests are Edit Mode tests in the Unity Test Runner.

1. Open `Window > General > Test Runner`.
2. Choose `EditMode`.
3. Press `Run All`.

The fixtures sit under `Assets/Capstone/**/Tests`. They cover the empty code, a code outside the 25, each of the 25 codes, a question in each language, an unmatched question, a tie, keep, save, and dismiss, a rank that leaves out the current work, a missing story note, and a size that stays the stored centimetres.

On 31 Oct 2026 the 79 tests that do not need the Unity editor were compiled and run: catalogue, ranking, dialogue, and session. All 79 passed. The list is `TEST-RESULTS.txt`. The wall-pose and room tests use Unity types, so they run in the Test Runner with the rest.

Two runtimes were used for the placement path. The editor runs the same session order in one of three rooms. An iPhone 13 on iOS 27.2 ran the same wall raycast and the same stored-size quad on a real wall: the phone asks for a slow scan, the centre ring turns green on a vertical plane, and Place uses that hit. Frame time was not instrumented, so this folder does not claim a performance number. The study package target remains Android API 30 with ARCore required.

## Play

Stop Play and wait for the compile spinner before a new Play. Let the ball finish, tap the blue circle, type `AZURE`, and press Return. Set the tags, choose a work, then place it. Ask one thing or press Skip. Keep, save, or dismiss shows three works with their story notes.
