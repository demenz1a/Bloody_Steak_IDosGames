// Copies the Unity WebGL build (Builds/WebGL/Build, made by Unity's WebGLBuild.Build) into
// public/unity/Build, where the bloody-steak module loads it from. Runs before `vite build`.
import { cpSync, existsSync, rmSync } from "node:fs";
import { fileURLToPath } from "node:url";

const from = fileURLToPath(new URL("../../Builds/WebGL/Build", import.meta.url));
const to = fileURLToPath(new URL("../public/unity/Build", import.meta.url));

if (!existsSync(from)) {
  console.error(`[copy-unity] ${from} not found — build the game in Unity first (Bloody Steak/Build WebGL).`);
  process.exit(1);
}
rmSync(to, { recursive: true, force: true });
cpSync(from, to, { recursive: true });
console.log(`[copy-unity] ${from} -> ${to}`);
