# Smart-Camping-App


## Overview
This project presents an **interactive camping simulation application** designed for the course *Human–Computer Interaction*.  
The goal is to offer users a realistic and engaging experience of managing and exploring a **virtual camping environment**, focusing on intuitive interaction, user feedback, and environmental realism.


---

## Authors
- **Anargyrou Lamprou Aikaterini**  
- **Stoikos Ioannis Panagiotis**

---

## Main Features

### Tent Setup
- Interactive map for selecting a camping spot.  
- Real-time environmental feedback (soil stability, humidity, sunlight, wind).  
- Visual indicators for ideal or unsafe areas.

**Includes:**  
 Moving pointer on map  
 Real-time environmental suggestions  
 Contextual messages and warnings  

---

### Stake Placement
- Users virtually place tent stakes by adjusting **angle** and **tension** using sliders.  
- The app evaluates the setup and provides a **score** (good/medium/poor) with improvement tips.  
- Gamified experience promoting learning through feedback.

**Includes:**  
Angle and pressure sliders  
Automatic evaluation system  
 Helpful recommendations  

---

### Protective Tarps
- Manual **drag-and-drop placement** or automatic **“Auto” mode**.  
- Tarps can be resized or rotated for optimal coverage.  
- Weather-aware system that prompts tarp usage under strong winds.

**Includes:**  
Manual or auto tarp placement  
Resizing and rotation  
Weather-based notifications  

---

### Lighting Control
- Smart LED system with adjustable **brightness**, **color**, and **modes** (e.g., *Night*, *Reading*, *Party*).  
- Users personalize the ambiance according to their activity.  

**Includes:**  
Brightness slider  
Color picker  
Predefined lighting presets  

---

### Energy Management
- Simulates **solar power generation** and **battery storage**.  
- Displays live stats: battery percentage, power consumption, and estimated autonomy.  
- Includes **energy-saving mode** and **air conditioning control**.  

**Includes:**  
Real-time battery and power data  
Energy-saving options  
AC control simulation  

---

### Navigation System
- Interactive map with tourist spots, trails, beaches, and shelters.  
- Multiple route options: *Recommended*, *Fastest*, *Balanced*.  
- Real-time alerts for obstacles or weather hazards.  

**Includes:**  
Tourist point selection  
Alternative routes  
Dynamic safety notifications  

---

### Weather Monitoring
- Displays **current weather conditions** (temperature, humidity, wind) with icons.  
- Provides proactive suggestions based on weather (e.g., “Strong wind — deploy tarps”).  

**Includes:**  
Live weather data  
Visual weather icons  
Contextual advice  

---

### Ordering & Communication
- Users can browse daily menus (breakfast/lunch/dinner), place orders, and track status (*Open*, *Preparing*, *Ready*).  
- Integrated **chat system** for communication with staff.  

**Includes:**  
Menu-based ordering  
Order tracking system  
Live chat support  

---

### Events System
- Displays upcoming, live, and past events.  
- Users can view details, images, and register participation.  
- Notifications for updates or schedule changes.  

**Includes:**  
Event list with status  
Event details and visuals  
Join & reminder options  

---

### Integrated Help
- Built-in help features to enhance usability:
  - **Quick Tips**  
  - **Full User Manual (PDF)**  
  - **F1** key for on-screen guidance  
  - **Tooltips** for key interface elements  

**Includes:**  
In-app documentation  
Contextual tooltips  
Keyboard-accessible help  

---

## Application Architecture
The app follows a **modular MVC-like architecture** separating:
- **Models** – Data and state representation  
- **Views** – Interactive UI components  
- **Services** – Core logic and simulations  

**Data flow:**  
User → Interface → Service → Model → Updated View  

This ensures scalability, cleaner design, and future expansion (e.g., real API integration for weather, GPS, or payments).

---

## Simulations
To ensure the app runs offline, we implemented **mock simulations**:
- Weather: Randomized temperature, humidity, and wind values  
- Energy: Simulated solar production based on time of day  
- Navigation: Static map-based routes  
- Orders: Mock order statuses without database connection  

---

## Conclusions
The **Smart Camping** project successfully demonstrates:
- An integrated and interactive user experience  
- Clear application of **Human–Computer Interaction principles**  
- Realistic simulation of camping conditions  

Although some components are simplified, the **modular architecture** allows for easy enhancement and future integration with real-world APIs and data sources.  

This project provided valuable hands-on experience in UI/UX design, system simulation, and combining multiple modules into a cohesive and interactive application.

---

## Technologies Used
- **Languages:** JavaScript / HTML / CSS  
- **Architecture:** MVC-like modular design  
- **Simulations:** Custom logic for weather, energy, navigation, and orders  
- **UI Tools:** Sliders, color pickers, interactive maps, tooltips  

---

## License
This project is intended for **academic and educational purposes**.
