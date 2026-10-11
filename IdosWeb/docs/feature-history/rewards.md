# Rewards: shift victory and leaderboard #1

- **Victory reward.** On a won shift Unity calls `IdosBridge.ReportVictory()` →
  `window.BloodySteakHost.reportVictory()` → `client.reward.claimReward("shift_victory")`.
  Title config `Reward.Claims.shift_victory` (Manual): PROD 20 `Main` (BloodyCoin), DEV 20 `CO`
  (DEV title has no crypto). The server cannot verify a win, so it is protected only by
  `Availability.Limits`: cooldown 300 s, at most 5 per 86400 s (user's choice: "5 wins per day").
  A refusal is shown as an info toast, not an error.
- **Leaderboard.** `best_score` (BestScore) runs in 12-hour cycles: `Schedule.Mode = Cyclic`,
  `Reset = FixedInterval`, `IntervalSeconds = 43200`. Rank 1 of a cycle gets 100 `Main` (DEV: `CO`),
  claimed by the player through the leaderboard module's cycle-reward popup (grace 168 h).
- Crypto rewards are paid from the title's players' reward pool; if it is unfunded the player gets
  the IOU `Main_IOU` instead of on-chain BloodyCoin.
- The toast text "+20 BloodyCoin" is hard-coded in `src/modules/bloody-steak/module.ts`; change it
  together with the title config.
