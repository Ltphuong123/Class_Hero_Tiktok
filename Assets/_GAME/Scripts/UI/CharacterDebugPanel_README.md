# Character Debug Panel - Hướng dẫn Setup

## Tổng quan
Hệ thống cho phép click vào LeaderboardRow để hiển thị debug menu với các chức năng:
- ➕ Thêm kiếm
- ❤️ Thêm máu
- ⚡ Tăng tốc độ
- 🗡️ Đổi loại kiếm (Default, Fire, Lightning, Miasma, Snow)

---

## Setup trong Unity Editor

### 1. Tạo CharacterDebugPanel UI

#### Hierarchy:
```
Canvas
├── CharacterLeaderboardUI (existing)
└── CharacterDebugPanel (NEW)
    ├── Background (Image - semi-transparent black)
    └── Panel (Image - white background)
        ├── Title (TextMeshProUGUI)
        ├── ButtonsGroup
        │   ├── AddSwordButton (Button + Text)
        │   ├── AddHealthButton (Button + Text)
        │   ├── ChangeSpeedButton (Button + Text)
        │   └── CloseButton (Button + Text)
        └── SwordTypeGroup
            ├── DefaultButton (Button + Text: "Default")
            ├── FireButton (Button + Text: "Fire")
            ├── LightningButton (Button + Text: "Lightning")
            ├── MiasmaButton (Button + Text: "Miasma")
            └── SnowButton (Button + Text: "Snow")
```

#### Layout Suggestions:
- **Background**: Full screen, Color: (0, 0, 0, 150)
- **Panel**: Center, Width: 400, Height: 500
- **Buttons**: Height: 50, Spacing: 10

---

### 2. Gán Components

#### CharacterDebugPanel GameObject:
1. Add Component → `CharacterDebugPanel`
2. Gán references:
   - Panel: Panel GameObject
   - Title Text: Title TextMeshProUGUI
   - Add Sword Button: AddSwordButton
   - Add Health Button: AddHealthButton
   - Change Speed Button: ChangeSpeedButton
   - Close Button: CloseButton
   - Sword Type Buttons: DefaultButton, FireButton, etc.

3. Settings:
   - Swords To Add: `1` (hoặc tùy chỉnh)
   - Health To Add: `20` (hoặc tùy chỉnh)
   - Speed Multiplier: `1.5` (tăng 50% tốc độ)

---

### 3. Kết nối với CharacterLeaderboardUI

1. Chọn `CharacterLeaderboardUI` GameObject
2. Trong Inspector, tìm field **Debug Panel**
3. Kéo `CharacterDebugPanel` GameObject vào field này

---

### 4. Setup CharacterDebugHelper

1. Tạo Empty GameObject trong scene: `CharacterDebugHelper`
2. Add Component → `CharacterDebugHelper`
3. Gán **Sword Prefab** vào field `Sword Prefab`
   - Tìm Sword prefab trong Project (thường ở Assets/_GAME/Prefabs/)

---

### 5. Setup LeaderboardRow để có thể click

Mỗi LeaderboardRow cần có:
1. **Image component** (để raycast)
2. Image → **Raycast Target = TRUE** ✅

Nếu không có Image, thêm:
- Add Component → Image
- Color: Transparent (0, 0, 0, 0) hoặc màu nền
- Raycast Target: TRUE

---

## Sử dụng

### Trong Game:
1. Nhấn **Tab** để mở Leaderboard
2. **Click vào bất kỳ row nào** → Debug Panel hiện ra
3. Chọn chức năng:
   - **Add Sword**: Thêm kiếm cho character
   - **Add Health**: Hồi máu
   - **Change Speed**: Tăng tốc độ
   - **Sword Type Buttons**: Đổi loại kiếm
4. Nhấn **Close** hoặc **ESC** để đóng panel

---

## Troubleshooting

### ❌ Click vào row không có phản ứng
**Giải pháp:**
- Kiểm tra LeaderboardRow có Image component với Raycast Target = TRUE
- Kiểm tra EventSystem có trong scene (Canvas tự tạo)
- Check Console có log "[LeaderboardRow] Clicked on..." không

### ❌ Debug Panel không hiện
**Giải pháp:**
- Kiểm tra CharacterLeaderboardUI đã gán Debug Panel chưa
- Kiểm tra Panel GameObject ban đầu phải SetActive(false)

### ❌ Add Sword không hoạt động
**Giải pháp:**
- Kiểm tra CharacterDebugHelper đã được tạo trong scene
- Kiểm tra Sword Prefab đã được gán
- Check Console có error không

### ❌ Change Speed không có hiệu ứng
**Giải pháp:**
- Tăng Speed Multiplier lên cao hơn (ví dụ: 2.0 = tăng gấp đôi)
- Kiểm tra character có đang bị stuck không

---

## Customization

### Thay đổi số lượng items:
```csharp
// Trong CharacterDebugPanel Inspector
Swords To Add: 5        // Thêm 5 kiếm mỗi lần
Health To Add: 50       // Hồi 50 HP
Speed Multiplier: 2.0   // Tăng gấp đôi tốc độ
```

### Thêm chức năng mới:
1. Thêm Button vào Panel UI
2. Trong `CharacterDebugPanel.cs`:
```csharp
[SerializeField] private Button myNewButton;

private void Awake()
{
    myNewButton.onClick.AddListener(OnMyNewFeature);
}

private void OnMyNewFeature()
{
    if (targetCharacter == null) return;
    // Your code here
}
```

---

## Notes

- Debug Panel chỉ nên dùng trong Development Build
- Có thể disable bằng cách tắt CharacterDebugPanel GameObject
- Tất cả thay đổi đều real-time, không cần restart game
- ESC để đóng panel nhanh

---

## Files Created

1. `CharacterDebugPanel.cs` - Main debug UI controller
2. `CharacterDebugHelper.cs` - Helper để spawn items
3. `LeaderboardRow.cs` - Updated với click handler
4. `CharacterLeaderboardUI.cs` - Updated với debug panel integration
5. `CharacterBase.cs` - Added MultiplySpeed() và SetSpeed()

---

Enjoy debugging! 🎮
