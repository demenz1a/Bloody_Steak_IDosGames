import { LazySolanaWalletLogin } from "@idosgames/wallet/react/lazy";
import type { RenderLinkWallet, RenderWalletLogin } from "@idosgames/app-shell";
import { ENV_WALLETCONNECT_PROJECT_ID } from "./env";
import {
  IDOS_WEB3_NETWORK_ID,
  IDOS_WEB3_WALLETCONNECT_PROJECT_ID,
} from "./idos.title";

// Wallet sign-in seam. Bloody Steak is a SOLANA title (Blockchain.Networks.solana), so the button is
// the Solana one (Phantom & co); the base template's EVM button would ask for a network the title
// does not have.
//
// The import is "@idosgames/wallet/react/lazy", NOT "@idosgames/wallet/react": the wallet stack is
// megabytes of code most players never need; the lazy entry keeps it out of the first load until the
// player actually taps sign-in. Inside the idosgames.com frame the SITE's connected wallet signs the
// login message — the button handles that by itself.
//
// walletConnectProjectId adds MOBILE wallets (QR / deep link); without it only browser-extension
// wallets are offered. It comes from the title's blockchain config (WalletLogin.WalletConnectProjectId).

const walletConnectProjectId =
  IDOS_WEB3_WALLETCONNECT_PROJECT_ID || ENV_WALLETCONNECT_PROJECT_ID || undefined;

export const renderWalletLogin: RenderWalletLogin = ({
  client,
  onAuthenticated,
  disabled,
  style,
}) => (
  <LazySolanaWalletLogin
    client={client}
    networkID={IDOS_WEB3_NETWORK_ID}
    onAuthenticated={onAuthenticated}
    disabled={disabled}
    style={style}
    appName="Bloody Steak"
    walletConnectProjectId={walletConnectProjectId}
  />
);

// "Link wallet" on the play-access screen — the same button in link mode, used only when the title's
// login mode requires a token balance.
export const renderWalletLink: RenderLinkWallet = ({
  client,
  networkID,
  onLinked,
  disabled,
  style,
}) => (
  <LazySolanaWalletLogin
    mode="link"
    client={client}
    networkID={networkID || IDOS_WEB3_NETWORK_ID}
    onLinked={onLinked}
    disabled={disabled}
    style={style}
    appName="Bloody Steak"
    walletConnectProjectId={walletConnectProjectId}
  />
);
