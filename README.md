# 🎾 Phone-Controlled Tennis Game

## 📱 Overview

A **real-time tennis game** controlled entirely by your iPhone's motion sensors. Swing your phone like a tennis racket to hit the ball — no keyboard or mouse required!

The game streams IMU (Inertial Measurement Unit) data from your iPhone via TCP, mapping your phone's orientation and acceleration directly to the in-game racket.

---

## 🚀 Features

- **Phone-Controlled Racket** — Rotate your phone to move the racket, swing to hit
- **Real-time IMU Streaming** — Low-latency TCP connection from iPhone to Unity
- **Swing Detection** — Acceleration-based swing detection with configurable sensitivity
- **Predictable Hitting** — Ball goes where you aim (controlled, not random)
- **Auto-Movement** — Player automatically tracks the ball's Z position
- **AI Opponent** — Intelligent AI with strategic targeting (aims away from player)
- **Score Tracking** — First to 11 points wins
- **Ball Trail** — Visual trail when ball is moving fast
- **Smooth Camera** — Broadcast-style camera follow with no dizzy motion
- **Cross-Platform** — Works on Windows, macOS, Linux (Unity 6 LTS)

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|------------|
| Game Engine | Unity 6 LTS |
| Networking | TCP Sockets (Custom Protocol) |
| Data Format | Protobuf (sensor.proto) |
| Phone App | SensorStream (iOS) |
| Physics | Unity Physics (Rigidbody) |
| Rendering | Universal Render Pipeline (URP) |

---

## 📱 iPhone Setup

### 1. Install SensorStream App
- Download **SensorStream** from the App Store
- Free with IMU streaming support

### 2. Connect to Unity
1. Open the **SensorStream** app on your iPhone
2. Go to **Settings → Network**
3. Enter your computer's IP address and port **5678**
4. Tap **Connect**
5. Tap **Start Streaming**

### 3. IP Address
- The game displays your local IP and port in the top-left corner when running
- Make sure your iPhone and computer are on the **same Wi-Fi network**

---

## 🎮 Controls

| Action | How To |
|--------|--------|
| **Rotate Racket** | Rotate your phone in 3D space |
| **Swing/Hit** | Swing your phone with acceleration > threshold |
| **Reset Ball** | Press `R` key |
| **Serve** | Automatic (or press `Space` for testing) |

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Network/
│   │   ├── SensorNetworkReceiver.cs   # TCP server + protobuf parser
│   │   └── SensorData.cs              # Data container
│   ├── Game/
│   │   ├── BallController.cs          # Ball physics + trail
│   │   ├── GameManager.cs             # Scoring, serving, game state
│   │   └── GameOverManager.cs         # Win/lose logic
│   ├── Player/
│   │   └── BatController.cs           # Phone-controlled bat
│   ├── Enemy/
│   │   └── EnemyController.cs         # AI opponent
│   └── Utils/
│       ├── CameraFollow.cs            # Smooth camera
│       └── ShotManager.cs             # Shot types (TopSpin/Flat)
├── Scenes/
│   ├── GameScene.unity
│   ├── WinScene.unity
│   └── LoseScene.unity
├── Materials/
│   └── (Physics materials, trail materials)
└── README.md
```

---

## ⚙️ Configuration

### Key Settings (Inspector)

| Component | Setting | Default | Description |
|-----------|---------|---------|-------------|
| `SensorNetworkReceiver` | TCP Port | `5678` | Network port for iPhone connection |
| `BatController` | Swing Threshold | `2.5` | Minimum acceleration to trigger hit |
| `BatController` | Power Multiplier | `2.5` | Hit power scaling |
| `BatController` | Max Power | `30` | Maximum hit power |
| `BallController` | Max Speed | `25` | Ball speed cap |
| `BallController` | Bounce Damping | `0.5` | Energy loss on bounce |

---

## 🔧 Building the Game

### Windows
1. File → Build Settings → Windows
2. Click Build

### macOS
1. File → Build Settings → macOS
2. Click Build

### Linux
1. File → Build Settings → Linux
2. Click Build

### iOS
- Requires a Mac with Xcode
- Build an Xcode project from Unity on Windows, then compile on Mac

---

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| iPhone won't connect | Check firewall, ensure same Wi-Fi, verify IP address |
| No data received | Check port 5678 is open, restart app |
| Ball flies out of court | Reduce `Max Power` or increase `Bounce Damping` |
| Racket doesn't rotate | Check phone orientation, verify IMU data is streaming |
| Jittery motion | Increase `Rotation Smoothing` |

### Firewall Setup (Windows)
```powershell
New-NetFirewallRule -DisplayName "Tennis TCP 5678" -Direction Inbound -Protocol TCP -LocalPort 5678 -Action Allow
```

### Firewall Setup (Linux - UFW)
```bash
sudo ufw allow 5678/tcp
```

---

## 📊 IMU Data Format

The game uses **Protobuf** for data serialization:

```protobuf
message IMUData {
  uint64 timestamp = 1;
  Vector3 accel = 2;      // Linear acceleration (m/s²)
  Vector3 gyro = 3;       // Angular velocity (rad/s)
  Quaternion orientation = 4;
  string frame_id = 5;
}
```

---

## 🎮 Gameplay Tips

1. **Hold phone naturally** — Screen facing you, top pointing forward
2. **Swing like a real racket** — Fast swings = more power
3. **Tilt racket** — Up = TopSpin, Level = Flat shot
4. **Time your swing** — Hit the ball when it's close to you
5. **AI gets harder** — Opponent aims away from your position

---

## 📄 License

MIT License — Feel free to use, modify, and distribute.

---

## 🙏 Credits

- **SensorStream App** — [GitHub](https://github.com/martijnhabers/sensorstream_driver)
- **Protobuf** — Google Protocol Buffers
- **Unity Engine** — Unity Technologies

---

## 📞 Questions?

Open an issue on GitHub or reach out to the repository owner.

---

## 🏆 Final Notes

This project was built as a proof-of-concept for phone-controlled gaming. The entire game logic is driven by your phone's sensors — no keyboard or mouse needed for gameplay.

**Made with ❤️ and Unity 6 LTS**
