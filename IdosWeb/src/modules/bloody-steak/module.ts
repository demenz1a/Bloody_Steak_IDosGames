import { defineModule, defineTopic, shape, type Module } from "@idosgames/module-sdk";
import { equippedSkin } from "../../shared/skins";
import { createUnityScene } from "./game/unityScene";
import { t } from "./i18n";

/** Copy of the skins module's topic (modules never import each other). */
const skinEquipped = defineTopic("skins:equipped@1", shape({ skinId: "string" }));

/** Leaderboard of the best shift score (title config: Leaderboard.Definitions.best_score, 12-hour cycles; #1 gets 100 BloodyCoin). */
export const LEADERBOARD_ID = "best_score";
/** Reward for a won shift (title config: Reward.Claims.shift_victory — 20 BloodyCoin, server-side rate limits). */
export const VICTORY_CLAIM_ID = "shift_victory";

declare global {
  interface Window {
    /** What the Unity build calls through Assets/Plugins/WebGL/IdosBridge.jslib. */
    BloodySteakHost?: { submitScore(score: number): void; reportVictory(): void };
  }
}

// Bloody Steak itself: the Unity WebGL build as a full-screen game behind the lobby's "Play". Sign-in
// and the leaderboard screen belong to the base and the leaderboard module; this module only runs
// the game and turns the score the game reports at the end of a shift into a leaderboard submission.
export const bloodySteakModule: Module = defineModule({
  id: "bloody-steak",
  meta: { name: "Bloody Steak", type: "game", genre: "simulation", engine: "dom" },
  setup(ctx) {
    const scene = createUnityScene(() => equippedSkin(ctx.client));
    // The equipped skin lives in the player's custom data; load it once so a skin bought in an
    // earlier session is on the player from the first shift.
    void ctx.client.userCustomData.getMyUserCustomData().then(() => scene.setSkin(equippedSkin(ctx.client)));
    ctx.events.on(skinEquipped, ({ skinId }) => scene.setSkin(skinId));
    let lastScore: number | null = null;
    let best: number | null = null;

    const submitScore = async (score: number) => {
      lastScore = score;
      // The server rejects a score <= 0 anyway; a shift with no points is simply not recorded.
      if (!(score > 0)) return;
      const res = await ctx.client.leaderboard.submitScore(LEADERBOARD_ID, Math.floor(score));
      if (!res.ok) {
        ctx.notifications.toast({ level: "error", message: t("scoreFailed"), source: "leaderboard" });
        return;
      }
      // best_score keeps the maximum (BestScore): NewScore equals this shift only when it is the record.
      const isRecord = res.data.NewScore === Math.floor(score);
      best = res.data.NewScore;
      ctx.notifications.toast({
        level: isRecord ? "reward" : "success",
        title: isRecord ? t("newRecord") : undefined,
        message: `${t("scoreSent")}: ${Math.floor(score)}`,
        icon: "trophy",
        source: "leaderboard",
      });
    };

    // The server decides whether the win pays (cooldown 5 min, at most 5 per day); a refusal is normal.
    const reportVictory = async () => {
      const res = await ctx.client.reward.claimReward(VICTORY_CLAIM_ID);
      if (!res.ok) {
        ctx.notifications.toast({ level: "info", message: t("victoryLimit"), key: "victory-reward" });
        return;
      }
      ctx.notifications.toast({
        level: "reward",
        title: t("victoryReward"),
        message: "+20 BloodyCoin",
        icon: "gift",
        key: "victory-reward",
      });
    };

    ctx.effect(() => {
      window.BloodySteakHost = {
        submitScore: (score) => void submitScore(score),
        reportVictory: () => void reportVictory(),
      };
      return () => {
        delete window.BloodySteakHost;
      };
    });

    ctx.registerScene(scene);
    ctx.registerRoute({ id: "bloody-steak", label: t("play"), icon: "🥩" });

    ctx.exposeToAgent({
      state: () => ({
        unity: scene.state(),
        lastScore,
        best,
        leaderboard: LEADERBOARD_ID,
        skin: equippedSkin(ctx.client),
      }),
      actions: {
        submitScore: (args) => submitScore(Number(args?.score ?? 0)),
        reportVictory: () => reportVictory(),
      },
      describeActions: {
        submitScore:
          "submitScore({ score: number }) — submit a shift score exactly as the Unity build does at the end of a shift.",
        reportVictory: "reportVictory() — claim the shift_victory reward exactly as the Unity build does after a won shift.",
      },
    });
  },
});
