import os
import shutil
import zipfile
from build_common import utils
from build_common import packages
from build_common import git
import argparse

sourceDirNameServer = "src/Server"
sourceDirNameClient = "src/Client"

argParser = argparse.ArgumentParser()
argParser.add_argument('--platform', type=str, default= "win-x64", required=False, help='Target platfrom of server')
args = argParser.parse_args()
platform = args.platform

artifactsDir = os.path.join(os.getcwd(), "artifacts")
if (not os.path.isdir(artifactsDir)):
    os.makedirs(artifactsDir)
outputDir = os.path.join(os.getcwd(), "output")
if (os.path.isdir(outputDir)):
    shutil.rmtree(outputDir, ignore_errors=True)

pkgFileServer = os.path.join(artifactsDir, f"server-{platform}.zip")
if (os.path.isfile(pkgFileServer)):
    os.remove(pkgFileServer)

pkgFileClient = os.path.join(artifactsDir, f"client-{platform}.zip")
if (os.path.isfile(pkgFileClient)):
    os.remove(pkgFileClient)

branch = git.get_version_from_current_branch()
commitIndex = git.get_last_commit_index()
version = f"{branch}.{commitIndex}"

print(f"===========================================", flush=True)
print(f"Output folder: '{outputDir}'", flush=True)
print(f"===========================================", flush=True)

print(f"===========================================", flush=True)
print(f"Compiling server for platform '{platform}'...", flush=True)
print(f"Version: '{version}'", flush=True)
print(f"===========================================", flush=True)
serverOutputDir = os.path.join(outputDir, "server")
packages.adjust_csproj_version(os.path.join(os.getcwd(), sourceDirNameServer), version)
utils.callThrowIfError(f"dotnet publish {sourceDirNameServer} -r {platform} -o \"{serverOutputDir}\"", True)

print(f"===========================================", flush=True)
print(f"Compiling client for platform '{platform}'...", flush=True)
print(f"Version: '{version}'", flush=True)
print(f"===========================================", flush=True)
clientOutputDir = os.path.join(outputDir, "client")
packages.adjust_csproj_version(os.path.join(os.getcwd(), sourceDirNameClient), version)
utils.callThrowIfError(f"dotnet publish {sourceDirNameClient} -r {platform} -o \"{clientOutputDir}\"", True)

print(f"===========================================", flush=True)
print(f"Creating server pkg...", flush=True)
print(f"===========================================", flush=True)
with zipfile.ZipFile(pkgFileServer, 'w', zipfile.ZIP_DEFLATED) as pkgZipFile:
    for root, _, files in os.walk(serverOutputDir):
        for file in files:
            filePath = os.path.join(root, file)
            pkgZipFile.write(filePath, os.path.relpath(filePath, serverOutputDir))

print(f"===========================================", flush=True)
print(f"Creating client pkg...", flush=True)
print(f"===========================================", flush=True)
with zipfile.ZipFile(pkgFileClient, 'w', zipfile.ZIP_DEFLATED) as pkgZipFile:
    for root, _, files in os.walk(clientOutputDir):
        for file in files:
            filePath = os.path.join(root, file)
            pkgZipFile.write(filePath, os.path.relpath(filePath, clientOutputDir))

print(f"===========================================", flush=True)
print(f"Done! Server package file is '{pkgFileServer}'", flush=True)
print(f"Done! Client package file is '{pkgFileClient}'", flush=True)
print(f"===========================================", flush=True)

git.create_tag_and_push(version, "origin", "casualshammy", True)
