import { defineUiConfig } from "@idosgames/react/ui";

// The game's interface in ONE place: colours, animations and sounds of every module built on
// @idosgames/react/ui (sign-in, lobby, leaderboard, profile). Change them here — never in a module.
//
// Bloody Steak palette — taken from the game itself: the blood red of the main menu art and the
// order-fail crosses, black, white and the greys of the kitchen tiles. "gold"/"green"/"blue" are
// only the kit's names for highlight / main action / secondary action — here they are red, red and
// grey, so nothing in the interface leaves the red-black-white-grey range.
const RED = "#e8243c"; // crosses, highlights
const BLOOD = "#b3121f"; // main buttons
const BLOOD_DEEP = "#5e0710";
const BLACK = "#0b0b0b";

export default defineUiConfig({
  theme: {
    bgTop: "#2a0b0e",
    bgBottom: BLACK,
    panel: "#1b1b1b",
    panelDeep: "#121212",
    panelEdge: "#5a1016",
    text: "#ffffff",
    textDim: "#a6a6a6",
    gold: RED,
    goldDeep: "#7d0b17",
    green: BLOOD,
    greenDeep: BLOOD_DEEP,
    blue: "#3b3b3b",
    blueDeep: "#202020",
    red: "#ff3d4f",
    redDeep: "#7a0a12",
    onGreen: "#ffffff",
    onGold: "#ffffff",
    onBlue: "#ffffff",
    onRed: "#ffffff",
    shadow: "rgba(0, 0, 0, 0.6)",
    backdrop: "rgba(0, 0, 0, 0.78)",
    line: "rgba(255, 255, 255, 0.12)",
    well: "#0f0f0f",
    wellSoft: "#262626",
    wellShadow: "inset 0 2px 4px rgba(0, 0, 0, 0.6)",
    edge: "#000000",
    glow: "rgba(232, 36, 60, 0.22)",
    textOutline: "none",
    panelShadow: "0 4px 0 #000000",
    radius: 8,
    buttonRadius: 6,
    font: "'Oswald', 'Arial Narrow', system-ui, sans-serif",
    rarity: { Common: "#8c8c8c", Rare: "#d9d9d9", Epic: "#e8243c", Legendary: "#ffffff" },
  },
  motion: {
    intensity: 0.6, // a sharper, less cartoonish feel than the default bounce
  },
  sound: {
    volume: 0.5,
  },
});
