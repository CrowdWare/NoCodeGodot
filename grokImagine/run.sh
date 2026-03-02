#!/bin/bash

# Sample run script for GrokImagine CLI
# Usage: ./run.sh prompts/img2img.sml

dotnet run --project GrokImagine.Cli -- "$@"
