# Architectural Audit & Implementation Plan: Scrollbar & Container Flicker Resolution in GTL Editor

## Executive Summary
In **GTL Text Editor** (.NET 9.0 Windows Forms, x64), moving or hovering the mouse across scrollbars—including the library tile strip (`vScrollBar1`), the Stored Positions list (`vScrollBar2`), and the typography controls in the Text Working Area (`vScrollText`, `hScrollText`, `hScrollRotate`)—triggers rapid, visible, strobe-like flickering.

This audit investigates the entire control hierarchy, window messaging flow (`WndProc`), GDI+ rendering pipeline, and container synchronization logic to identify the exact root causes of hover/scroll flicker. It provides a targeted, zero-risk implementation plan to achieve smooth, 100% flicker-free scrollbars and scroll containers while strictly preserving control hierarchy, WinForms string lookups (`HostForm.Controls["groupBox1"]`), layout geometry, and `.gtl` binary format invariants.

---

## 1. Inventory of All Scrollbars & Scrollable Containers

| Control Identifier | Class Type | Container / Parent | Purpose / Binding |
| :--- | :--- | :--- | :--- |
| **`vScrollBar1`** | `HighContrastVScrollBar` | `Form1` (`this.Controls`) | Vertical scrolling for the loaded `.gtl` tile thumbnail strip in `panel1` / `panel1Viewport`. Anchored/docked at far right (`flushTop` to `statusStrip1.Top`). |
| **`vScrollBar2`** | `HighContrastVScrollBar` | `groupBox4` (Stored Positions) | Vertical scrolling for stored text preset cards (`positionTextDisplay`) in `panel3` inside viewport `panel2`. |
| **`vScrollText`** | `HighContrastVScrollBar` | `groupBox1` (Text Tools) | Y-coordinate placement of text stamp on `workingTile` (`PointPlacement.Y`). |
| **`hScrollText`** | `HighContrastHScrollBar` | `groupBox1` (Text Tools) | X-coordinate placement of text stamp on `workingTile` (`PointPlacement.X`). |
| **`hScrollRotate`**| `HighContrastHScrollBar` | `groupBox1` (Text Tools) | Interactive 0°–360° text rotation angle slider (`RotationDegrees`). |
| **`panel1Viewport`**| `Panel` | `Form1` (`this.Controls`) | Static outer clipping viewport for the `.gtl` tile list (`AutoScroll = false`). |
| **`panel1`** | `Panel` | `panel1Viewport` | Dynamic scroll canvas hosting rows of `gtlTileDisplay` controls (`panel1.Top = -vScrollBar1.Value`). |
| **`panel2`** | `Panel` | `groupBox4` | Outer clipping viewport for stored positions (`AutoScroll = false`). |
| **`panel3`** | `Panel` | `panel2` | Dynamic scroll canvas hosting `positionTextDisplay` user controls (`panel3.Top = -vScrollBar2.Value + 4`). |
| **`groupBox1`** | `GroupBox` | `Form1` (`this.Controls`) | Direct parent of `vScrollText`, `hScrollText`, `hScrollRotate`, and action buttons. |
| **`groupBox4`** | `GroupBox` | `Form1` (`this.Controls`) | Direct parent of `panel2`, `vScrollBar2`. |

---

## 2. Root Cause Analysis: Why Mouse Movement Causes Visible Flicker

### Root Cause 1: `WM_MOUSEMOVE` in `WndProc` Triggers Direct-to-Screen Repainting
In both `HighContrastVScrollBar` (lines 6269–6284) and `HighContrastHScrollBar` (lines 6461–6474):
```csharp
protected override void WndProc(ref Message m)
{
    base.WndProc(ref m);
    const int WM_PAINT = 0x000F;
    const int WM_MOUSEMOVE = 0x0200;
    const int WM_LBUTTONDOWN = 0x0201;
    const int WM_LBUTTONUP = 0x0202;
    const int WM_VSCROLL = 0x0115; // WM_HSCROLL = 0x0114 in HScrollBar

    if (m.Msg == WM_PAINT || m.Msg == WM_MOUSEMOVE || m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONUP || m.Msg == WM_VSCROLL)
    {
        CustomPaint();
    }
}
```
* **The Mechanism:** 
  1. `base.WndProc(ref m)` is executed first on every `WM_MOUSEMOVE`. Because `VScrollBar` wraps the Win32 `SCROLLBAR` common control class, Windows processes mouse movement by redrawing the native Windows scrollbar (tracking hot states, arrows, and thumb).
  2. Immediately following `base.WndProc`, `CustomPaint()` executes synchronously. Inside `CustomPaint()`, it calls `Graphics.FromHwnd(Handle)` and directly draws over the display DC.
  3. At typical mouse polling rates (125 Hz to 1000 Hz), moving the cursor across the scrollbar fires hundreds of `WM_MOUSEMOVE` events per second. The control visually thrashes back and forth between native Win32 painting and GDI+ custom painting on every sub-pixel movement.
  4. **The Redundancy:** `CustomPaint()` does not even use the mouse cursor's `(X, Y)` position! It only checks `isHovered` and `isDragging`. Both states are already tracked via `OnMouseEnter`, `OnMouseLeave`, `OnMouseDown`, and `OnMouseUp`. Invoking `CustomPaint()` on `WM_MOUSEMOVE` is 100% redundant and the primary driver of mouse-hover flicker.

---

### Root Cause 2: Unbuffered Direct-to-Window DC Painting (`Graphics.FromHwnd`)
Inside `CustomPaint()`:
```csharp
private void CustomPaint()
{
    if (Width <= 0 || Height <= 0 || !IsHandleCreated) return;

    using (Graphics g = Graphics.FromHwnd(Handle))
    {
        // 1. Fill entire track
        using (SolidBrush trackBrush = new SolidBrush(trackColor))
            g.FillRectangle(trackBrush, client);

        // 2. Fill arrow button rectangles
        // 3. Draw arrow glyph polygons
        // 4. Fill thumb rectangle
        // 5. Draw thumb border
        // 6. Draw grip marks
        // 7. Draw outer border and dividers
    }
}
```
* **The Mechanism:** `Graphics.FromHwnd(Handle)` renders directly to the visible screen without an intermediate back-buffer.
* Because the first step is `g.FillRectangle(trackBrush, client)` (erasing the entire scrollbar to the background track color), every redraw exposes an intermediate blank frame before the thumb and arrows are drawn.
* To the human eye, this rapid sequence of erase-and-draw directly on the physical display produces harsh visual tearing and strobing.

---

### Root Cause 3: Ineffective Double-Buffering Flags on Native Win32 Common Controls
In `HighContrastVScrollBar` and `HighContrastHScrollBar` constructors:
```csharp
DoubleBuffered = true;
SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
```
* **The Mechanism:** In Windows Forms, `ScrollBar` is a wrapper around the Win32 `SCROLLBAR` window class (`SetStyle(ControlStyles.UserPaint, false)` by default).
* Standard WinForms double-buffering flags only take effect when `ControlStyles.UserPaint` is `true`. On native common controls, Windows bypasses the .NET `BufferedGraphicsContext` during `WM_PAINT`.
* Consequently, `DoubleBuffered = true` was effectively a no-op on these scrollbars; painting remained entirely unbuffered.

---

### Root Cause 4: Background Erase (`WM_ERASEBKGND` / `0x0014`)
* When an invalidation is queued (e.g., from `OnMouseEnter` or `OnValueChanged`), Windows dispatches `WM_ERASEBKGND` (0x0014) prior to `WM_PAINT` (0x000F).
* Passing `WM_ERASEBKGND` to `base.WndProc(ref m)` causes the native window procedure to wipe the control background with the parent window brush before custom painting begins.
* Without suppressing `WM_ERASEBKGND` (by setting `m.Result = (IntPtr)1`), every hover state transition experiences a white/gray flash.

---

### Root Cause 5: Unbuffered Child Controls in Dynamic Viewports (`panel1` & `panel3`)
* While `panel1Viewport`, `panel1`, `panel2`, and `panel3` have double-buffering enabled via reflection (`EnableDoubleBuffering`), the dynamically instantiated children inside them do not:
  * `gtlTileDisplay` instances in `panel1` (each containing `fullTile`, `halfTile`, `smallTile` PictureBoxes).
  * `positionTextDisplay` instances in `panel3` (each containing multiple textboxes and buttons).
  * `GroupBox` containers (`groupBox1`, `groupBox2`, `groupBox3`, `groupBox4`) and `Form1` itself.
* When `vScrollBar1.Scroll` updates `panel1.Top = -val`, the unbuffered child controls participate in non-double-buffered erase/paint passes against the parent panel.

---

## 3. Targeted Fix Plan & Code Architecture

### Step 1: Overhaul `HighContrastVScrollBar` and `HighContrastHScrollBar`
Implement true double-buffering using an off-screen bitmap memory buffer, suppress `WM_ERASEBKGND`, and remove `WM_MOUSEMOVE` from `WndProc`:

```csharp
public class HighContrastVScrollBar : VScrollBar
{
    private bool isHovered = false;
    private bool isDragging = false;

    public HighContrastVScrollBar()
    {
        // Suppress native background erase
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        isHovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        isDragging = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        isDragging = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnValueChanged(EventArgs e)
    {
        base.OnValueChanged(e);
        Invalidate();
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_PAINT = 0x000F;
        const int WM_ERASEBKGND = 0x0014;
        const int WM_VSCROLL = 0x0115;

        // 1. Suppress native background erase to prevent white/gray flashes
        if (m.Msg == WM_ERASEBKGND)
        {
            m.Result = (IntPtr)1;
            return;
        }

        base.WndProc(ref m);

        // 2. Paint only on actual repaint triggers (NOT on WM_MOUSEMOVE!)
        if (m.Msg == WM_PAINT || m.Msg == WM_VSCROLL)
        {
            CustomPaint();
        }
    }

    private void CustomPaint()
    {
        if (Width <= 0 || Height <= 0 || !IsHandleCreated) return;

        // 3. True off-screen double buffering: render entire control in RAM
        using (Bitmap buffer = new Bitmap(Width, Height))
        {
            using (Graphics g = Graphics.FromImage(buffer))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                Rectangle client = ClientRectangle;

                // [Track, Arrows, Thumb, Borders rendered cleanly onto buffer]
                // ...
            }

            // 4. Single atomic blit to the screen DC
            using (Graphics gScreen = Graphics.FromHwnd(Handle))
            {
                gScreen.DrawImageUnscaled(buffer, 0, 0);
            }
        }
    }
}
```
*(Apply the exact symmetrical pattern to `HighContrastHScrollBar` replacing `WM_VSCROLL` with `WM_HSCROLL` = `0x0114`).*

---

### Step 2: Extend Double-Buffering to Containers & Dynamic Children
In `Read_GTL/GTL Text Editor.cs`:
1. Call `EnableDoubleBuffering` on `this` (`Form1`), `groupBox1`, `groupBox2`, `groupBox3`, and `groupBox4` during initialization:
   ```csharp
   EnableDoubleBuffering(this);
   EnableDoubleBuffering(groupBox1);
   EnableDoubleBuffering(groupBox2);
   EnableDoubleBuffering(groupBox3);
   EnableDoubleBuffering(groupBox4);
   ```
2. Enable double-buffering on dynamically added user controls in `panel1` and `panel3`:
   - In `panel1.ControlAdded`: Call `EnableDoubleBuffering(e.Control);` for each `gtlTileDisplay`.
   - In `panel3.ControlAdded`: Call `EnableDoubleBuffering(e.Control);` for each `positionTextDisplay`.

---

### Step 3: Clean Scroll Event Synchronization
In `vScrollBar1_Scroll` and `vScrollBar2_Scroll`:
Avoid redundant `Value` re-assignment when the value is already in range to prevent triggering re-entrant `OnValueChanged` invalidations:
```csharp
private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
{
    try
    {
        int val = e.NewValue;
        int maxScroll = vScrollBar1.Maximum - vScrollBar1.LargeChange + 1;
        if (maxScroll < 0) maxScroll = 0;
        if (val > maxScroll) val = maxScroll;
        if (val < 0) val = 0;

        if (vScrollBar1.Value != val)
        {
            vScrollBar1.Value = val;
        }

        this.panel1.Top = -val;
    }
    catch
    {
    }
}
```

---

## 4. Critical Gotchas & Guardrails to Avoid

| Potential Pitfall | Risk / Consequence | Prevention Guardrail |
| :--- | :--- | :--- |
| **`WS_EX_COMPOSITED` (`0x02000000`)** | Severe visual artifacts: black boxes on `PictureBox` zoom preview (`pctZoom` / `workingTile`), cursor trail ghosting, and scroll lag on Windows 10/11. | **DO NOT USE `WS_EX_COMPOSITED`**. Use targeted off-screen `Bitmap` blitting in `CustomPaint()` and `Control.DoubleBuffered` reflection. |
| **GDI+ Resource Leaks** | Allocating `Bitmap`, `Graphics`, `Pen`, or `Brush` on repaint without disposal exhausts the 10,000 GDI handle limit and crashes the application with `OutOfMemoryException`. | Wrap **every** GDI+ object (`buffer`, `g`, `gScreen`, `trackBrush`, `pen`) in strict `using (...)` blocks. |
| **Control Hierarchy Drift** | Re-parenting `vScrollBar1`, `vScrollBar2`, `groupBox1`, or `groupBox4` breaks string lookups such as `HostForm.Controls["groupBox1"]` and crashes the app with `NullReferenceException`. | Maintain exact control parenting (`vScrollBar1` on `Form1`, `vScrollBar2` on `groupBox4`, `vScrollText` on `groupBox1`). |
| **`pctZoom` Anchor Drift** | Moving `groupBox4` or re-docking scrollbars alters `groupBox4.Bottom`, causing `pctZoom.Top` to shift vertically. | Preserve `groupBox4` bounds and layout calculations in `LayoutMainFormContainers()`. |
| **Zero-Size Bitmap Crash** | Creating `new Bitmap(Width, Height)` when a control is minimized or collapsed (`Width <= 0` or `Height <= 0`) throws `ArgumentException`. | Always guard: `if (Width <= 0 || Height <= 0 || !IsHandleCreated) return;` before allocating the buffer. |
| **Binary Serialization Safety** | Touching `GTL_File.cs` or tile byte streams can corrupt Cyberboard `.gtl` files. | Keep `Read_GTL/GTL_File.cs`, `PositionDataDto`, `CloseCurrentFile()`, and `createPNGtoolStripMenuItem` **100% untouched**. |

---

## 5. Verification Plan

1. **Compilation Check:**
   - Execute `dotnet build "GTL Editor.sln" -c Release` and confirm strictly **0 Warning(s), 0 Error(s)**.
2. **Hover Flicker Verification:**
   - Launch `GTL Editor.exe` and rapidly move the mouse cursor back and forth across `vScrollBar1` (tile strip), `vScrollBar2` (stored positions), and `hScrollRotate` / `vScrollText` / `hScrollText` (Text Working Area).
   - Verify that the scrollbar track, thumb, and arrows render rock-solid with zero strobing, zero flashing, and zero native/custom draw thrashing.
3. **Scroll Drag & Wheel Smoothness:**
   - Load a large `.gtl` file (with 20+ tiles) and verify smooth, fluid dragging of `vScrollBar1` without stuttering or picture box tearing.
   - Add multiple stored text positions in `groupBox4` and verify fluid scrolling of `panel3` via `vScrollBar2`.
   - Drag `hScrollRotate` and `hScrollText` and verify real-time, flicker-free canvas placement.
