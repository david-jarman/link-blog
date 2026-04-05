---
id: 83d2e487-0174-4e01-9058-53fcf22b4d19
title: Pokemon Red RL
short-title: pokemon-red-rl
type: post
created: 2025-03-25T15:52:35.0881790+00:00
updated: 2025-03-25T15:55:16.2464560+00:00
link: https://github.com/PWhiddy/PokemonRedExperiments/tree/master
link-title: Train RL agents to play Pokemon Red - GitHub
tags:
- pokemon
- rl
- ai
- uv
- python
---

I'm very late to the trend of AI playing Pokemon gameboy games, but I just started playing Pokemon Red myself on the iOS Delta emulator, and have been having lots of fun. To be clear, this is my first time doing *anything* with Pokemon. It wasn't something I was into as a child, but am for some reason discovering it as an adult and enjoying it.

I just wanted to make a quick post to show how I got the PokemonRedExperiments project running on my MacBook Pro M4 using [uv](https://github.com/astral-sh/uv).

Full steps:

```
# Clone the repo
git clone https://github.com/PWhiddy/PokemonRedExperiments.git

# Install ffmpeg
brew install ffmpeg

# Copy ROM to git root path
cd PokemonRedExperiments
cp /path/to/pokemon-red.gb PokemonRed.gb
# Validate rom is valid. Should produce ea9bcae617fdf159b045185467ae58b2e4a48b9a
shasum ./PokemonRed.gb

# Set up python environment
cd baselines
uv venv --python 3.10
uv pip install -r requirements.txt

# Start the pre-trained RL agent
uv run ./run_pretrained_interactive.py
```

I first tried using Python 3.12, as the README suggested using Python 3.10+, but I found that there are package dependency conflicts with 3.12, so I changed my uv command to use 3.10 and everything worked. This is why I love uv. I can very easily try out other versions of python and not worry about messing up other projects. 

[!\[\](https://linkblog.blob.core.windows.net/images/2025/03/25/15/54/59/Screenshot%202025-03-25%20at%208.53.37%E2%80%AFAM.png)Running Pokemon Red with RL](https://linkblog.blob.core.windows.net/images/2025/03/25/15/54/59/Screenshot%202025-03-25%20at%208.53.37%E2%80%AFAM.png)
