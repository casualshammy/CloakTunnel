import os
import re
import shutil
from build_common import utils
import build_common.packages
import build_common.git as git
import argparse

signingPassword = os.environ['ANDROID_SIGNING_KEY_PASSWORD']

sourceDirName = "src/MauiClient"

argParser = argparse.ArgumentParser()
argParser.add_argument('--framework', type=str, default= "net8.0-android", required=False, help='Target framework of server')
args = argParser.parse_args()
framework: str = args.framework

artifactsDir = os.path.join(os.getcwd(), "artifacts")
if (not os.path.isdir(artifactsDir)):
    os.makedirs(artifactsDir)

outputDir = os.path.join(os.getcwd(), "output")
if (os.path.isdir(outputDir)):
    shutil.rmtree(outputDir, ignore_errors=True)

outputFileRegex = ""
if (framework.endswith("android")):
    outputFileRegex = r"-(Signed)\.apk$|-(Signed)\.aab$"

if (outputFileRegex == ""):
    raise FileNotFoundError(f"Can't find pkg format for framework '{framework}'")

branch = git.get_version_from_current_branch()
commitIndex = git.get_last_commit_index()
version = f"{branch}.{commitIndex}"

print(f"===========================================", flush=True)
print(f"Output folder: '{outputDir}'", flush=True)
print(f"===========================================", flush=True)

print(f"===========================================", flush=True)
print(f"Adjusting csproj version: '{version}'", flush=True)
print(f"===========================================", flush=True)
build_common.packages.adjust_csproj_version(os.path.join(os.getcwd(), sourceDirName), version, "ApplicationDisplayVersion")
build_common.packages.adjust_csproj_version(os.path.join(os.getcwd(), sourceDirName), str(commitIndex), "ApplicationVersion")

print(f"===========================================", flush=True)
print(f"Compiling client for framework '{framework}'...", flush=True)
print(f"===========================================", flush=True)
build_common.packages.callThrowIfError("dotnet workload install maui")
build_common.packages.callThrowIfError(f"dotnet publish {sourceDirName} -c Release -p:AndroidSigningKeyPass={signingPassword} -p:AndroidSigningStorePass={signingPassword} -f {framework} -o \"{outputDir}\"")

print(f"===========================================", flush=True)
print(f"Creating pkg...", flush=True)
print(f"===========================================", flush=True)
for entry in os.listdir(outputDir):
    entryPath = os.path.join(outputDir, entry)
    if (os.path.isfile(entryPath) ):
        match = re.search(outputFileRegex, entryPath)
        if (match != None):
            newName = entry.replace(match.group(1) or match.group(2), version)
            shutil.move(entryPath, os.path.join(artifactsDir, newName))

print(f"===========================================", flush=True)
print(f"Done!", flush=True)
print(f"===========================================", flush=True)

git.create_tag_and_push(version)
utils.callThrowIfError("git stash", True)
git.merge("main", git.get_current_branch_name(), True, "casualshammy", True)