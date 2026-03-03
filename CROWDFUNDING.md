# Crowdfunding Campaign – CrowdWare: Forge Poser

**Status:** Pre-launch (strong demo with Grok Imagine expected next week – March 2026)  
**Platform recommendation:** Indiegogo (flexible funding – keep what you raise)  
**Working Title:** "Forge Poser: Asset-Free AI Character Animation – Just Pose + Prompt"

## CrowdWare – The Company Philosophy

**CrowdWare** is my one-person indie label dedicated to tools that remove barriers for creators.

Our guiding philosophy is inspired by Joseph Beuys' concept of **Social Sculpture** (Soziale Plastik): everyone is an artist, everyone can shape society through creative acts – if the tools are accessible enough.

**Forge** is the tech stack I built to make this possible:

- Built on **Godot 4.6** (latest stable, open-source game engine)  
- Powered by two custom languages I created:  
  - **SML** – Simple Markup Language (clean, declarative UI & structure)  
  - **SMS** – Simple Multiplatform Script (lightweight, cross-platform scripting)

The first product (internally called **Poser**) is an **asset-free AI character animation tool**:

- Start with a simple posed figure (greybox, basic skeleton, or imported GLB)  
- Add one descriptive prompt  
- Generate short animated videos in virtually any style – no rigged marketplace assets, no complex pipelines required

Target users: animators, educators, indie game devs, storytellers, social projects, small studios.

Personal goal: Become independent from Jobcenter support in Germany → fund 6 focused months of development → release a solid open-source v1.0 (free OSS core; commercial features via subscription).

## Tech Sneak Peek – SML + SMS in action

The entire UI and logic are written in SML & SMS – extremely lean and under full control.

**SML example** (declarative UI – main window skeleton):

```sml
Window {
    id: mainWindow
    title: "ForgePoser"
    minSize: 1024, 680
    size: 1440, 900

    MenuBar {
        preferGlobalMenu: true

        PopupMenu {
            id: menuFile
            title: "File"

            Item { id: menuNew     text: "New"           }
            Item { id: menuOpen    text: "Open Project…" }
            Item { id: menuSave    text: "Save"          }
            Item { id: menuSaveAs  text: "Save As…"      }
            Item { id: menuOpenGlb text: "Load Model…"   }
            Item { id: menuExport  text: "Export as GLB…" }
        }
    }

    VBoxContainer {
        anchors: left | top | right | bottom
        spacing: 0

        DockingHost {
            id: mainDockHost
            sizeFlagsHorizontal: expandFill
            sizeFlagsVertical: expandFill
            gap: 4

            DockingContainer {
                id: leftDock
                dockSide: left
                fixedWidth: 240
                dragToRearrangeEnabled: true
                tabsRearrangeGroup: 1

                VBoxContainer {
                    id: scenePanel
                    leftDock.title: "Scene"
                    // ... AI prompt input, asset list, etc. coming soon
                }
            }
            // ... center 3D viewport, timeline, right inspector
        }
    }
}
```
SMS example (scripting – ready function + file menu logic):
```sms

fun ready() {
    kfTreeRef    = ui.getObject("keyframeTree")
    sceneListRef = ui.getObject("sceneAssetList")
    editor       = ui.getObject("editor")
    timeline     = ui.getObject("timeline")
    statusLabel  = ui.getObject("statusLabel")
    editor.setBoneTree("boneTree")
    editor.setJointSpheresVisible(false)
    activateArrangeMove()
    updateScenePanel()
    updateKeyframeTree()
    log.success("ForgePoser ready.")
}

// ── File menu ─────────────────────────────────────────────────────────────────

on menuNew.clicked() {
    editor.resetPose()
    currentProjectPath = ""
    hasCharacter = false
    sceneListRef.Clear()
    kfTreeRef.Clear()
    statusLabel.text = "New project."
}

on menuOpen.clicked() {
    ui.openFileDialog("onProjectFileSelected", "*.scene")
}
```

This combination gives full control over performance, look & feel – and keeps the project lightweight.

## Timing & Demo

Current bottleneck: hardware (Mac Mini M2 + old PC with 8 GB VRAM) + AI assistant limits.
Next week: First strong public demo using Grok Imagine – real end-to-end flow:
simple pose → style/motion prompt → animated video output.
→ Teaser rollout planned on X, Reddit (r/godot, r/MachineLearning, r/animation, r/opensource), Mastodon, Bluesky.

## Funding Goal & Budget

**Realistic stretch target:**
Breakdown (6 months, DE prices ~ March 2026):

Living expenses 6 months (modest independence)     € 15.000
Hardware upgrade (Mac Studio M4 Max or equivalent) €  2.600
Targeted marketing (Meta/X/TikTok/YouTube Shorts)  €  2.000
API credits & test servers (Claude, Grok Imagine)  €    800
Buffer (fees ~5–8%, taxes, misc)                   €  1.500
---
Total realistic range: € 21.000 - € 24.000 -> € 22.000

## Rewards
The core of Forge Poser remains fully open-source under a permissive license – free for all non-commercial use (personal projects, education, experiments, open-source contributions, etc.).
Commercial use (games, client work, products, studios, agencies, …) requires a subscription that unlocks legal commercial rights + priority support + early access to new features.
The pledge amounts have been intentionally chosen with numerological meaning in mind – a small gesture for those who see magic in numbers:

| Tier          | Pledge Amount | Rewards |
|---------------|---------------|---------|
| Supporter     | €13+          | Thank you + credits + early updates |
| Creator       | €310          | 6 months commercial Subscription + Priority Support + Beta |
| Studio        | €580          | 13 months commercial Subscription + Priority Support + Beta + "Special Thanks" Credit |
| Patron        | €1,300+       | Lifetime commercial Subscription + Co-Creator-Credit in Showcases |
