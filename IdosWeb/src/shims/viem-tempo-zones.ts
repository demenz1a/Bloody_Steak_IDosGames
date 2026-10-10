// Stand-in for "viem/tempo/zones". wagmi 3.7.2's nested @wagmi/core imports `Abis` from it for its
// Tempo-chain zone actions, but the newest published viem (2.57.4) does not export that path yet, so
// the build fails to resolve it. Bloody Steak is a Solana title and never calls those actions; the
// alias in vite.config.ts points the import here. Remove it once viem ships "./tempo/zones".
export const Abis = {} as Record<string, never>;
