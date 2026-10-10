import type { EngineScene, SceneMountContext } from "@idosgames/module-sdk";

// The Unity WebGL build of Bloody Steak as an EngineScene. The build itself is produced by Unity
// (Assets/Scripts/Editor/WebGLBuild.cs → Builds/WebGL) and copied to public/unity/ before
// `npm run build` (scripts/copy-unity.mjs); here we only load and drive it.

const BUILD_URL = "unity/Build";
const BUILD_NAME = "WebGL";
/** Aspect of the game's canvas (Player Settings → default web screen 960×600). */
const ASPECT = 960 / 600;

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

export function createUnityScene(): EngineScene & { state(): UnitySceneState } {
  let instance: UnityInstance | null = null;
  let canvas: HTMLCanvasElement | null = null;
  let resolveReady: () => void = () => {};
  const readyPromise = new Promise<void>((r) => (resolveReady = r));
  const state: UnitySceneState = { loaded: false, progress: 0, active: false, error: null };

  // Pause/resume go through IdosBridge.cs (Time.timeScale + AudioListener.pause), so a player who
  // steps out to the lobby does not come back to failed orders.
  const send = (method: "OnHostSuspend" | "OnHostResume") =>
    instance?.SendMessage("IdosBridge", method);

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
      canvas.style.cssText = `display:block;outline:none;width:min(100cqw, calc(100cqh * ${ASPECT}));aspect-ratio:${ASPECT};`;
      wrap.style.containerType = "size";
      wrap.appendChild(canvas);
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
            },
            (p) => (state.progress = p),
          );
          state.loaded = true;
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
  };
}
