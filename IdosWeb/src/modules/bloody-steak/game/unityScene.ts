import type { EngineScene, SceneMountContext } from "@idosgames/module-sdk";

// The Unity WebGL build of Bloody Steak as an EngineScene. The build itself is produced by Unity
// (Assets/Scripts/Editor/WebGLBuild.cs → Builds/WebGL) and copied to public/unity/ before
// `npm run build` (scripts/copy-unity.mjs); here we only load and drive it.

const BUILD_URL = "unity/Build";
const BUILD_NAME = "WebGL";
/** Aspect of the game's canvas (Player Settings → default web screen 960×600). */
const ASPECT = 960 / 600;

/** A phone or tablet: the game shows its on-screen joystick and wants a landscape screen. */
const isTouchDevice = () =>
  typeof window !== "undefined" && window.matchMedia?.("(pointer: coarse)").matches === true;

/** Portrait phone → "turn your phone" hint over the game (CSS only, follows rotation by itself). */
const ROTATE_CSS = `
.bs-rotate{position:absolute;inset:0;display:none;flex-direction:column;align-items:center;justify-content:center;
  gap:16px;background:#000;color:#fff;font:600 18px/1.4 system-ui,sans-serif;text-align:center;padding:24px;z-index:2}
.bs-rotate__icon{width:64px;height:64px;border:4px solid #c8102e;border-radius:10px;animation:bs-rotate 1.6s ease-in-out infinite}
@keyframes bs-rotate{0%,20%{transform:rotate(0)}60%,100%{transform:rotate(-90deg)}}
@media (orientation:portrait) and (pointer:coarse){.bs-rotate{display:flex}}
`;

/** Ask the browser for landscape; works only where allowed (Android fullscreen), silently ignored elsewhere. */
function lockLandscape() {
  const orientation = screen.orientation as ScreenOrientation & { lock?: (o: string) => Promise<void> };
  orientation?.lock?.("landscape").catch(() => {});
}

interface UnityInstance {
  SendMessage(objectName: string, method: string, value?: string | number): void;
  Quit(): Promise<void>;
}

declare global {
  interface Window {
    createUnityInstance?: (
      canvas: HTMLCanvasElement,
      config: Record<string, unknown>,
      onProgress?: (progress: number) => void,
    ) => Promise<UnityInstance>;
  }
}

export interface UnitySceneState {
  loaded: boolean;
  progress: number;
  active: boolean;
  error: string | null;
}

function loadScript(src: string): Promise<void> {
  return new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.src = src;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error(`Failed to load ${src}`));
    document.head.appendChild(script);
  });
}

/**
 * @param currentSkin — the skin the player has equipped right now; sent to the game when it has
 * loaded and every time it comes back on screen (IdosBridge.OnHostSetSkin).
 */
export function createUnityScene(
  currentSkin: () => string,
): EngineScene & { state(): UnitySceneState; setSkin(skinId: string): void } {
  let instance: UnityInstance | null = null;
  let canvas: HTMLCanvasElement | null = null;
  let resolveReady: () => void = () => {};
  const readyPromise = new Promise<void>((r) => (resolveReady = r));
  const state: UnitySceneState = { loaded: false, progress: 0, active: false, error: null };

  // Pause/resume go through IdosBridge.cs (Time.timeScale + AudioListener.pause), so a player who
  // steps out to the lobby does not come back to failed orders.
  const send = (method: "OnHostSuspend" | "OnHostResume") =>
    instance?.SendMessage("IdosBridge", method);
  const setSkin = (skinId: string) => instance?.SendMessage("IdosBridge", "OnHostSetSkin", skinId);
  // IdosBridge.OnHostSetMobile → TouchControls (joystick + interact button) on a touch screen.
  const sendMobile = () => instance?.SendMessage("IdosBridge", "OnHostSetMobile", isTouchDevice() ? "1" : "0");

  return {
    surface: "fullbleed-canvas",

    mount({ host }: SceneMountContext) {
      const wrap = document.createElement("div");
      wrap.style.cssText =
        "position:absolute;inset:0;display:flex;align-items:center;justify-content:center;background:#000;";
      canvas = document.createElement("canvas");
      // Unity registers its keyboard handlers by "#" + canvas.id — without an id it throws on start.
      canvas.id = "unity-canvas";
      canvas.tabIndex = -1;
      // The game keeps its 16:10 picture; the rest of the screen is black bars.
      // touch-action:none — the browser must not scroll or zoom while a finger drives the joystick.
      canvas.style.cssText = `display:block;outline:none;width:min(100cqw, calc(100cqh * ${ASPECT}));aspect-ratio:${ASPECT};touch-action:none;user-select:none;-webkit-user-select:none;-webkit-touch-callout:none;`;
      canvas.addEventListener("contextmenu", (e) => e.preventDefault());
      wrap.style.containerType = "size";
      wrap.appendChild(canvas);

      const style = document.createElement("style");
      style.textContent = ROTATE_CSS;
      const rotate = document.createElement("div");
      rotate.className = "bs-rotate";
      rotate.innerHTML = `<div class="bs-rotate__icon"></div><div>Поверните телефон горизонтально<br><span style="opacity:.6;font-size:14px">Rotate your phone to landscape</span></div>`;
      wrap.append(style, rotate);
      host.appendChild(wrap);
      // Keyboard input in an iframe needs focus on the canvas.
      canvas.addEventListener("pointerdown", () => canvas?.focus());

      void (async () => {
        try {
          await loadScript(`${BUILD_URL}/${BUILD_NAME}.loader.js`);
          if (!window.createUnityInstance) throw new Error("Unity loader did not start");
          instance = await window.createUnityInstance(
            canvas!,
            {
              dataUrl: `${BUILD_URL}/${BUILD_NAME}.data.unityweb`,
              frameworkUrl: `${BUILD_URL}/${BUILD_NAME}.framework.js.unityweb`,
              codeUrl: `${BUILD_URL}/${BUILD_NAME}.wasm.unityweb`,
              streamingAssetsUrl: "unity/StreamingAssets",
              companyName: "DefaultCompany",
              productName: "Bloody Steak",
              productVersion: "1.0",
              // Lets capture() read the last frame for the platform's "Shot" button.
              webglContextAttributes: { preserveDrawingBuffer: true },
              // Phones report 3–4× pixel ratios; 2 is sharp enough and keeps the frame rate up.
              devicePixelRatio: Math.min(window.devicePixelRatio || 1, isTouchDevice() ? 2 : 3),
            },
            (p) => (state.progress = p),
          );
          state.loaded = true;
          setSkin(currentSkin());
          sendMobile();
          canvas!.focus();
          if (!state.active) send("OnHostSuspend");
        } catch (e) {
          state.error = e instanceof Error ? e.message : String(e);
          console.error("[bloody-steak]", e);
        } finally {
          resolveReady();
        }
      })();
    },

    ready: () => readyPromise,

    activate() {
      state.active = true;
      setSkin(currentSkin());
      sendMobile();
      if (isTouchDevice()) lockLandscape();
      send("OnHostResume");
      canvas?.focus();
    },

    suspend() {
      state.active = false;
      send("OnHostSuspend");
    },

    capture() {
      if (!canvas || !state.loaded) return Promise.resolve(null);
      return new Promise((resolve) => canvas!.toBlob(resolve, "image/png"));
    },

    destroy() {
      void instance?.Quit();
      instance = null;
    },

    state: () => ({ ...state }),
    setSkin,
  };
}
