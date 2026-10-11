import type { Module } from "@idosgames/module-sdk";
import { bloodySteakModule } from "./modules/bloody-steak";
import { leaderboardModule } from "./modules/leaderboard";
import { profileModule } from "./modules/profile";
import { skinsModule } from "./modules/skins";

// The modules this app is made of, on top of the base (sign-in and the lobby): the game launched by
// "Play" and the systems shown as lobby tabs. The array holds only the imports and the list.
export const modules: Module[] = [
  bloodySteakModule,
  skinsModule,
  leaderboardModule,
  profileModule,
];
