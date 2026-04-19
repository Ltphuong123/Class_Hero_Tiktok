# Vision Radius Debug - Hướng dẫn sử dụng

## 🎯 Mục đích

Hiển thị vòng tròn tầm nhìn (vision radius) của character để debug AI behavior.

---

## 📦 2 Scripts có sẵn

### **1. SimpleVisionDebug** (Recommended)
- ✅ Đơn giản, nhẹ
- ✅ Chỉ hiển thị vòng tròn
- ✅ Dùng cho debug nhanh

### **2. VisionRadiusDebug** (Advanced)
- ✅ Hiển thị nhiều thông tin
- ✅ Show target lines
- ✅ Runtime GUI panel
- ✅ Dùng cho debug chi tiết

---

## 🚀 Setup - SimpleVisionDebug

### **Cách 1: Attach vào 1 character**
1. Chọn character GameObject trong Hierarchy
2. Add Component → Simple Vision Debug
3. Chạy game → Thấy vòng tròn xanh (vision) và vàng (give up)

### **Cách 2: Attach vào tất cả characters**
```csharp
// Tạo script auto-attach:
[ExecuteInEditMode]
public class AutoAttachVisionDebug : MonoBehaviour
{
    [ContextMenu("Attach to All Characters")]
    void AttachToAll()
    {
        var characters = FindObjectsOfType<CharacterBase>();
        foreach (var character in characters)
        {
            if (character.GetComponent<SimpleVisionDebug>() == null)
                character.gameObject.AddComponent<SimpleVisionDebug>();
        }
        Debug.Log($"Attached to {characters.Length} characters");
    }
}
```

---

## 🎨 Visualization

### **Vòng tròn hiển thị:**

```
        Yellow (18u - Give Up)
    ┌─────────────────────────┐
    │   Green (15u - Vision)  │
    │   ┌─────────────────┐   │
    │   │                 │   │
    │   │       ME        │   │
    │   │                 │   │
    │   └─────────────────┘   │
    └─────────────────────────┘
```

**Màu sắc:**
- 🟢 **Green (15 units)**: Vision radius - tầm nhìn tìm target
- 🟡 **Yellow (18 units)**: Chase give up - bỏ cuộc nếu target ra ngoài

---

## ⚙️ Settings (SimpleVisionDebug)

```csharp
Vision Radius: 15        // Tầm nhìn (units)
Vision Color: Green      // Màu vòng vision
Chase Give Up Color: Yellow  // Màu vòng give up
Segments: 64             // Độ mịn vòng tròn
```

---

## 🔧 Advanced - VisionRadiusDebug

### **Additional Features:**

**1. Show Target Lines:**
- Hiển thị line đến tất cả characters trong vision
- Color code:
  - 🟢 Green: Weaker (can attack)
  - 🟡 Yellow: Equal (neutral)
  - 🔴 Red: Stronger (threat)

**2. Runtime GUI Panel:**
- Hiển thị khi select character
- Info: State, Swords, HP, Nearby characters

**3. Separation Radius:**
- 🔴 Red circle (1.2 units)
- Hiển thị vùng tránh overlap

### **Settings:**
```csharp
Show Vision Radius: true
Show Chase Give Up Radius: true
Show Separation Radius: false
Show Target Line: true
Circle Segments: 64
```

---

## 🐛 Troubleshooting

### **Không thấy vòng tròn?**
1. ✅ Check Gizmos enabled trong Scene view (icon góc trên phải)
2. ✅ Check script đã attach vào character
3. ✅ Check character có CharacterStateMachine component

### **Vòng tròn không đúng size?**
1. ✅ Check `visionRadius` trong SimpleVisionDebug
2. ✅ Hoặc check `VisionRadius` trong CharacterStateMachine
3. ✅ Default: 15 units

### **Muốn thay đổi màu?**
```csharp
// Trong Inspector:
Vision Color: RGB(0, 255, 0) → Green
Chase Give Up Color: RGB(255, 255, 0) → Yellow

// Hoặc custom:
Vision Color: RGB(0, 150, 255) → Blue
```

---

## 📊 Use Cases

### **1. Debug "Không tấn công"**
```
Scenario: Character có kiếm nhưng không attack

Debug:
1. Attach SimpleVisionDebug
2. Chạy game
3. Xem target có trong vòng xanh không?
   ├─ Có → Check số kiếm (target phải yếu hơn)
   └─ Không → Target ngoài tầm nhìn
```

### **2. Debug "Bỏ cuộc quá sớm"**
```
Scenario: Character chase rồi bỏ cuộc

Debug:
1. Xem target có ra ngoài vòng vàng không?
2. Nếu có → Target quá xa (>18 units)
3. Solution: Tăng vision radius hoặc tăng chase duration
```

### **3. Debug "Tấn công sai target"**
```
Scenario: Character tấn công target xa thay vì gần

Debug:
1. Use VisionRadiusDebug với Show Target Line
2. Xem line màu xanh (valid targets)
3. Check logic FindWeakerTarget() - chọn nearest
```

---

## 🎮 Keyboard Shortcuts (Optional)

Thêm vào SimpleVisionDebug:

```csharp
private void Update()
{
    // Toggle vision display
    if (Input.GetKeyDown(KeyCode.V))
    {
        enabled = !enabled;
    }
}
```

**V key**: Toggle vision debug on/off

---

## 📝 Performance Notes

### **SimpleVisionDebug:**
- ✅ Chỉ chạy trong Editor (OnDrawGizmos)
- ✅ Không ảnh hưởng build
- ✅ ~0.01ms per character

### **VisionRadiusDebug:**
- ⚠️ Runtime GUI có overhead nhỏ
- ✅ Chỉ show khi select character
- ✅ ~0.05ms per character

**Recommendation:** Dùng SimpleVisionDebug cho debug thường xuyên.

---

## 🔮 Future Enhancements

### **1. Color by State:**
```csharp
Color GetVisionColor()
{
    if (stateMachine.CurrentState == stateMachine.Attack)
        return Color.red;
    else if (stateMachine.CurrentState == stateMachine.Flee)
        return Color.blue;
    else
        return Color.green;
}
```

### **2. Show Path:**
```csharp
// Draw pathfinding path
foreach (var waypoint in stateMachine.PathBuffer)
{
    Gizmos.DrawLine(prevPoint, waypoint);
    prevPoint = waypoint;
}
```

### **3. Show Sword Count:**
```csharp
// Draw text above character
DrawLabel(position + Vector3.up * 2f, $"⚔️ {swordCount}");
```

---

Enjoy debugging! 🎮🔍
