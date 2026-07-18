# CreativeFlow Tycoon – 3D Business Simulation Game
> **Bachelor's Thesis Project** | Grade: **9.10/10**
> Developed by: **Alexandru-Octavian Toie** (Economic Informatics Graduate)

---

## Project Overview
**CreativeFlow Tycoon** is an immersive 3D business simulation and organizational training game developed in **Unity Engine**. Designed at the intersection of game engineering and organizational psychology, the application bridges theoretical business workflows with active, gamified training environments. 

The game simulates the complete commercial ecosystem of a real-world advertising agency (*S.C. CreativeFlow S.R.L.*), translating manual corporate protocols, inventory gating, client management, and logistics into dynamic in-game systems and asynchronous mechanics.

---

## Media & Visuals
*Replace the placeholder text inside the brackets below with your own screenshot/GIF paths once you upload them to your repository.*

| Procedural City Generation | Management Dashboard & UI |
| :---: | :---: |
| ![Procedural Terrain Grid](Licenta-URA/Media/Department_UI.png) | ![Management UI Panel](Licenta-URA/Media/Road_Generation.png) |
| *Dynamic procedural relief and roads* | *Decoupled event-driven financial management* |

---

## Core Technical Architecture & Scripting Features

### 1. Singleton Design Pattern (State Management)
The core architecture uses a strict structural backend-in-client system controlled by a centralized `GameManager` script operating as a **Singleton**. This ensures a single, immutable global access point to runtime data configurations:
* Financial capital monitoring.
* Client reputation metrics (Dynamic `Client Reputation Score`).
* Active warehouse storage capacity bounds.

### 2. Event-Driven UI System
To achieve maximum CPU optimization and maintain a loose decoupling between calculations and the structural front-end interface, the system implements native C# `Action` delegates (e.g., `OnUIUpdate`). 
* Eliminates heavy frame-by-frame continuous execution loops (`Update()`).
* Pages dynamically subscribe/unsubscribe to state events, yielding a high-performance profile.

### 3. Advanced Procedural Landmass & Grid Generation
The simulation space is generated algorithmically via the `WindingPathTerrainGenerator` script using mathematical coordinate mapping:
* **Fractal Perlin Noise:** Implements native noise logic across structural octaves governed by customizable persistence and lacunarity fields to guarantee unique terrain elevation grids upon initialization.
* **Mathematical Relief Transitions:** Implements smooth transitions between structural reliefs, roads, and built zones utilizing linear interpolations (`Mathf.Lerp`) and dynamic `Mathf.SmoothStep` clamping to secure structural flatness across operational zones.

### 4. Asynchronous Operational Logic
* **Dynamic Calculations:** Automated C# computation logic dynamically parses inventory items, structural volumes, and raw material roll variables from the `Warehouse` index to calculate active invoice bills.
* **Quest System Architecture:** Features an extensible quest system mapping operational milestones (e.g., matching client design briefs with automated technical print sheet constraints).

---

## Tech Stack & Framework Bounds
* **Engine:** Unity Engine (LTS Architectures)
* **Language:** C# (.NET Framework / Core Core Systems)
* **UI Utilities:** TextMeshPro (TMPro) dynamic atlas scaling, Procedural Splatmapping
* **Data Management:** Structural dictionaries (`Dictionary<string, float>`), System.IO serialization routines

---

## How to Run the Environment
1. Clone the repository:
   ```bash
   git clone [https://github.com/Alexandru-Toie/Licenta-URA.git](https://github.com/Alexandru-Toie/Licenta-URA.git)
