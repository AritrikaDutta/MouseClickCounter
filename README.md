# Mouse Click Counter

Lightweight Windows tray app that counts **mouse clicks** and **keyboard activity** per day, saves them so they survive restarts, and keeps **phase/hour buckets** for later analysis.

- Silent background + system tray icon  
- Live **daily totals** in the tray tooltip and a Today window  
- Same bucket table for clicks + scrolls + keys / spaces / enters / backspaces  
- Durable JSON logs under `%LOCALAPPDATA%\MouseClickCounter\`  
- No installer, no .NET SDK required to build (uses the built-in Windows C# compiler)  
- Small single `.exe` you can copy to other Windows laptops  

## Bucket layout

Daily UI total is separate from analysis buckets. Each day file stores both.

### Weekdays (Mon–Fri) — 11 entries

| Bucket | Window |
|--------|--------|
| 1 | 00:00 – 09:30 |
| 2–10 | Nine **:30-aligned** hours: 09:30–10:30 … 17:30–18:30 |
| 11 | 18:30 – 24:00 |

### Weekends (Sat–Sun) — 2 entries

| Bucket | Window |
|--------|--------|
| 1 | 00:00 – 12:00 |
| 2 | 12:00 – 24:00 |

## Requirements

- Windows 10/11  
- .NET Framework 4.x (already included with Windows)  

No Python, no Visual Studio, no internet required at runtime.

## Build (any Windows PC)

```bat
build.bat
```

Output:

```text
dist\MouseClickCounter.exe
```

## Run

1. Double-click `dist\MouseClickCounter.exe` (or `run.bat`)
2. If Windows SmartScreen / Application Control warns, choose **More info → Run anyway** (local build)
3. Find the blue tray icon (you may need to click the `^` overflow arrow)
4. Double-click the icon → **Today** window with live total + buckets
5. Right-click → **Start with Windows** (optional), **Open data folder**, **Exit**

Only one instance runs at a time.

## Data files

Path:

```text
%LOCALAPPDATA%\MouseClickCounter\YYYY-MM-DD.json
```

Example:

```json
{
  "date": "2026-09-18",
  "dayType": "weekday",
  "daily": {
    "left": 4120, "right": 180, "middle": 12, "total": 4312, "scrolls": 890,
    "keys": 9800, "spaces": 1400, "enters": 220, "backspaces": 310
  },
  "buckets": [
    {
      "start": "00:00", "end": "09:30",
      "left": 40, "right": 2, "middle": 0, "total": 42, "scrolls": 15,
      "keys": 120, "spaces": 18, "enters": 3, "backspaces": 5
    }
  ]
}
```

Notes:

- `keys` = every key-down (including space / enter / backspace)  
- `spaces` / `enters` / `backspaces` are also counted separately  
- `scrolls` = vertical or horizontal wheel / trackpad scroll notches  
- Existing day files without newer fields still load; missing fields start at 0  

Counts flush to disk every 30 seconds, every 25 events, and on exit. After a reboot, the app reloads today’s file and continues.

## Use on other laptops

### Option A — copy the exe (simplest)

1. Build once with `build.bat`
2. Copy `dist\MouseClickCounter.exe` to the other laptop
3. Run it (data is stored per-user on that machine)

### Option B — from GitHub

1. Push this folder to a GitHub repo
2. On another laptop: clone the repo (or download ZIP)
3. Run `build.bat`
4. Start `dist\MouseClickCounter.exe`

Optional GitHub Release: attach `MouseClickCounter.exe` so others can download without building.

```bash
cd mouse-click-counter
git init
git add .
git commit -m "Add lightweight Windows mouse click counter"
gh repo create mouse-click-counter --public --source=. --remote=origin --push
```

## Privacy

Counts **click button types**, **scroll wheel events**, and **key category totals** (keys / spaces / enters / backspaces) into time buckets only.  
It does **not** log the characters you typed, passwords, window titles, URLs, or what you clicked.

## Project layout

```text
mouse-click-counter/
  build.bat
  README.md
  LICENSE
  src/
    Program.cs
    TrayApplicationContext.cs
    TodayForm.cs
    MouseHook.cs
    KeyboardHook.cs
    ClickStore.cs
    DayRecord.cs
    BucketSchedule.cs
  dist/                  (created by build.bat, gitignored)
```

## License

MIT — see [LICENSE](LICENSE).
