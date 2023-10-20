# Slow Udp Pipe

SlowUdpPipe is an application for creating an encrypted UDP tunnel between two computers, with one computer acting as the 'server' and the other as the 'client'. Initially, SlowUdpPipe was designed to bypass WireGuard protocol blocking on Android devices, but it can be used to transmit any UDP traffic. The encrypted packets of SlowUdpPipe do not have any explicit signature and appear to censors as an unrecognized encrypted protocol. Due to this, **SlowUdpPipe is not suitable for use in networks where censorship restricts or blocks unrecognized protocols**.


![flowchart](github/traffic-flow.png)

# Quick start for WireGuard
### Server
1. Run `slowudppipeserver genkey` to generate new key;
2. Run `slowudppipeserver --remote=127.0.0.1:51820 --local=0.0.0.0:1935 --key=<generated-key-from-[1]>`;
### Client
3. Change wireguard client's `Endpoint` option to `127.0.0.1:52280` (or any other you prefer);
4. Change wireguard client's `MTU` option to `1280`;
5. Adjust `AllowedIPs` options so traffic to `<server-ip>` will not be routed via wireguard (you can use [this tool](https://www.procustodibus.com/blog/2021/03/wireguard-allowedips-calculator/)); 
6. Run `slowudppipeclient --remote=<server-ip>:1935 --local=127.0.0.1:52280 --key=<generated-key-from-[1]>`
![flowchart](github/wireguard-client-options-adjust.png)

# Command line arguments
SlowUdpPipe doesn't use config files, all setup is done using command line arguments:
  - `test` : run benchmark of cryptographic algorithms
  - `genkey` : generate new random key
  - `--remote=<host:port>` : SlowUdpPipe will send processed traffic to this address
  - `--local=<host:port>` : SlowUdpPipe will listen for incoming traffic to this address
  - `--key=<key>` : this key will be used to encrypt data. Key **must be the same** on `client` and `server`. `Server` will not respond to packets encrypted with wrong key, so censors will not be able to detect SlowUdpPipe server instance using scanning (but of course server instance will be detectable while serving traffic for clients).
  - `--cyphers=<cyphers>` : (optional, server-only) server will accept clients that use this ciphers. Valid cyphers: `aes-128`, `aes-256`, `aes-gcm-128`, `aes-gcm-256`, `chacha20-poly1305`. If omitted all cyphers will be accepted. Some cyphers are not available on all platforms, please use `test` command to get additional info.
  - `--cypher=<cypher>` : (optional, client-only) client will be use this cipher to encrypt data. Valid cyphers: `aes-128`, `aes-256`, `aes-gcm-128`, `aes-gcm-256`, `chacha20-poly1305`. If omitted `aes-gcm-128` will be used. Some cyphers are not available on all platforms, please use `test` command to get additional info. 

### Why 'Slow'
Because the performance of SlowUdpPipe is insufficient to provide an transfer speed of 1Gbps on cheap VPS. But it is fast enought to provide an transfer speed of 100Mbps on 6$ DigitalOcean droplet.
