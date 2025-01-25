# Cloak Tunnel
CloakTunnel is an application for creating an encrypted UDP/Websocket tunnel between two computers, with one computer acting as the 'server' and the other as the 'client'. Initially, CloakTunnel was designed to bypass WireGuard protocol blocking on Android devices, but it can be used to transmit any UDP traffic. The encrypted packets of CloakTunnel do not have any explicit signature and appear to censors as an unrecognized encrypted protocol or just https traffic (websocket).


![flowchart](github/traffic-flow.png)

## Supported Platforms
 - [x] Windows
 - [x] Linux (`x86`, `x64`, `arm`, `arm64`)
 - [x] Android (only client)

There are [releases](https://github.com/casualshammy/CloakTunnel/releases) for `windows`, `linux` and `android 9+`; also CloakTunnel is available as open beta in Google Play:

<a href="https://play.google.com/store/apps/details?id=com.axiolab.slowudppipe">
  <img alt="Google Play open beta" width="200px" src="https://play.google.com/intl/en_us/badges/static/images/badges/en_badge_web_generic.png" />
</a>

## Ciphers
CloakTunnel supports the following ciphers: `aes-128`, `aes-256`, `aes-gcm-128`, `aes-gcm-256`, `chacha20-poly1305` and `xor`. They all are safe (except `xor`, but `xor` is extremely fast and usually enough for obfuscating traffic). Some cyphers are not available on all platforms, please use `test` command to get additional info. 
## Key
Key is used to encrypt data. Key **must be the same** on `client` and `server`. `Server` will not respond to packets encrypted with wrong key, so censors will not be able to detect CloakTunnel server instance using scanning (but of course server instance will be detectable while serving traffic for clients).

# Quick start for WireGuard
### Server
1. Run `cloaktunnel-server genkey` to generate new key;
2. Run `cloaktunnel-server -L udp://0.0.0.0:1935 -R udp://127.0.0.1:51820 -p <key from [1]>`
### Client
1. Change wireguard client's `Endpoint` to `127.0.0.1:52280` (or any other you prefer);
2. Change wireguard client's `MTU` to `1280`;
3. Adjust `AllowedIPs` so traffic to `<server-ip>` will not be routed via wireguard (you can use [this tool](https://www.procustodibus.com/blog/2021/03/wireguard-allowedips-calculator/));  
![flowchart](github/wireguard-client-options-adjust.png)
4. Run `cloaktunnel-client -L udp://127.0.0.1:52280 -R udp://<server-ip>:1935 -p <key from [1]>`

# Command line arguments
There are 2 useful command line commands:
  - `test` : run benchmark of cryptographic algorithms
  - `genkey` : generate new random key
