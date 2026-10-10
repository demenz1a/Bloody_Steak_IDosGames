import { defineAppConfig } from "@idosgames/app-shell";

// What happens after sign-in and how the lobby is ordered. The look, animations and sounds are in
// ./ui.config.ts. Which systems and games the app has is ./modules.ts.
export default defineAppConfig({
  // Bloody Steak takes the whole screen: sign-in → the lobby (leaderboard, profile) → "Play".
  start: "lobby",
  lobby: {
    order: ["play", "leaderboard"],
  },
  auth: {
    // "Play as guest" — a device account with no e-mail or wallet.
    guest: true,
  },
});
