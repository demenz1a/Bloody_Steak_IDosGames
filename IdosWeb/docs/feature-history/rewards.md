# Rewards: BloodyCoin per won shift and leaderboard #1

- **Shift reward.** On a won shift Unity computes `coins = min(60, money / 10)` (`ScoreManager.moneyPerCoin`,
  `maxCoinsPerShift`), shows it on the victory screen and calls `IdosBridge.ReportVictory(coins)` →
  `window.BloodySteakHost.reportVictory(coins)`.
- `client.reward.claimReward` pays only a fixed amount, so the shell pays the number as denominations:
  title config `Reward.Claims.shift_coin_{1,2,4,8,16,32}` (PROD: `Main` = BloodyCoin, DEV: `CO`), one
  `claimReward` per denomination, ~700 ms apart (the SDK throttles the same endpoint within 600 ms).
  Every denomination is limited server-side: cooldown 60 s, 5 per 86400 s. The server cannot verify a
  win, so a cheater can take at most 63 per shift and 5 × 63 per day; the 60 cap is client-side.
- Replaced the earlier flat `shift_victory` claim (20 BloodyCoin, later 300 for a test).
- **Leaderboard.** `best_score` (BestScore) runs in 12-hour cycles: `Schedule.Mode = Cyclic`,
  `Reset = FixedInterval`, `IntervalSeconds = 43200`. Rank 1 of a cycle gets 100 `Main` (DEV: `CO`),
  claimed by the player through the leaderboard module's cycle-reward popup (grace 168 h).
- Crypto rewards are paid from the title's players' reward pool; if it is unfunded the player gets
  the IOU `Main_IOU` instead of on-chain BloodyCoin.
