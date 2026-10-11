# Phones (touch controls, landscape)

- Phones play the same Unity WebGL build inside this shell. Unity owns the controls: `TouchControls`
  (joystick + interact button, built in code) appears when `MobileInput.IsMobile` is true.
- The shell tells Unity it runs on a touch screen: `SendMessage("IdosBridge", "OnHostSetMobile", "1")`
  after load and on every activate (`isTouchDevice()` = `(pointer: coarse)`).
- Landscape: `screen.orientation.lock("landscape")` on activate (only works where the browser allows,
  e.g. Android fullscreen; errors are ignored) plus a CSS-only "rotate your phone" overlay in portrait.
- The canvas keeps the game's 16:10 picture (black bars on wide phones), `touch-action:none` so the
  browser does not scroll/zoom under the joystick, and `devicePixelRatio` capped at 2 on phones.
