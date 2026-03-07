FROM node:24

ARG TZ
ENV TZ="$TZ"

# Install basic development tools and iptables/ipset
RUN apt-get update && apt-get install -y --no-install-recommends \
  less \
  git \
  procps \
  sudo \
  fzf \
  zsh \
  man-db \
  unzip \
  gnupg2 \
  gh \
  iptables \
  ipset \
  iproute2 \
  dnsutils \
  aggregate \
  jq \
  nano \
  vim \
  && apt-get clean && rm -rf /var/lib/apt/lists/*

# Ensure default node user has access to /usr/local/share
RUN mkdir -p /usr/local/share/npm-global && \
  chown -R node:node /usr/local/share

ARG USERNAME=node

# Persist bash history.
RUN SNIPPET="export PROMPT_COMMAND='history -a' && export HISTFILE=/commandhistory/.bash_history" \
  && mkdir /commandhistory \
  && touch /commandhistory/.bash_history \
  && chown -R $USERNAME /commandhistory

# Set `DEVCONTAINER` environment variable to help with orientation
ENV DEVCONTAINER=true

# Create workspace and config directories and set permissions
RUN mkdir -p /workspace /home/node/.claude && \
  chown -R node:node /workspace /home/node/.claude

WORKDIR /workspace

ARG GIT_DELTA_VERSION=0.18.2
RUN ARCH=$(dpkg --print-architecture) && \
  wget "https://github.com/dandavison/delta/releases/download/${GIT_DELTA_VERSION}/git-delta_${GIT_DELTA_VERSION}_${ARCH}.deb" && \
  sudo dpkg -i "git-delta_${GIT_DELTA_VERSION}_${ARCH}.deb" && \
  rm "git-delta_${GIT_DELTA_VERSION}_${ARCH}.deb"

# Install .NET 10 SDK via dotnet-install.sh (works on all architectures)
ENV DOTNET_ROOT=/usr/local/share/dotnet
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_GENERATE_ASPNET_CERTIFICATE=false
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && \
  chmod +x /tmp/dotnet-install.sh && \
  /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/local/share/dotnet && \
  rm /tmp/dotnet-install.sh && \
  ln -sf /usr/local/share/dotnet/dotnet /usr/local/bin/dotnet

# Install csharp-language-server and GodotEnv as shared global tools
RUN dotnet tool install --tool-path /usr/local/share/dotnet-global-tools csharp-ls && \
  dotnet tool install --tool-path /usr/local/share/dotnet-global-tools Chickensoft.GodotEnv
ENV PATH="$PATH:/usr/local/share/dotnet-global-tools"

# Set up non-root user
USER node

# Install Godot via GodotEnv (manages versions via symlinks)
# GodotEnv v2.16.2 bug: always downloads linux_x86_64 even on ARM64.
# Work around by manually installing the ARM64 binary into GodotEnv's
# expected directory structure so all GodotEnv commands still work.
ARG GODOT_VERSION=4.6.1
RUN ARCH=$(uname -m) && \
  if [ "$ARCH" = "x86_64" ]; then \
    godotenv godot install ${GODOT_VERSION}; \
  else \
    GODOT_VER_UNDERSCORED=$(echo "${GODOT_VERSION}" | tr '.' '_') && \
    VERSION_DIR="$HOME/.config/godotenv/godot/versions/godot_dotnet_${GODOT_VER_UNDERSCORED}_stable" && \
    BIN_DIR="$HOME/.config/godotenv/godot/bin" && \
    DOWNLOAD_URL="https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_arm64.zip" && \
    wget -q -O /tmp/godot.zip "$DOWNLOAD_URL" && \
    unzip -q /tmp/godot.zip -d /tmp/godot && \
    mkdir -p "$VERSION_DIR" "$BIN_DIR" && \
    mv /tmp/godot/Godot_v${GODOT_VERSION}-stable_mono_linux_arm64/* "$VERSION_DIR/" && \
    ln -sf "$VERSION_DIR/Godot_v${GODOT_VERSION}-stable_mono_linux.arm64" "$BIN_DIR/godot" && \
    ln -sf "$VERSION_DIR/GodotSharp" "$BIN_DIR/GodotSharp" && \
    rm -rf /tmp/godot.zip /tmp/godot && \
    godotenv godot env setup; \
  fi

# Add GodotEnv's Godot symlink to PATH and set GODOT env var
ENV GODOT_BIN="/home/node/.config/godotenv/godot/bin"
ENV PATH="$PATH:$GODOT_BIN"
ENV GODOT="$GODOT_BIN/godot"

# Install global packages
ENV NPM_CONFIG_PREFIX=/usr/local/share/npm-global
ENV PATH=$PATH:/usr/local/share/npm-global/bin:/home/node/.local/bin

# Set the default shell to zsh rather than sh
ENV SHELL=/bin/zsh

# Set the default editor and visual
ENV EDITOR=nano
ENV VISUAL=nano

# Default powerline10k theme
ARG ZSH_IN_DOCKER_VERSION=1.2.0
RUN sh -c "$(wget -O- https://github.com/deluan/zsh-in-docker/releases/download/v${ZSH_IN_DOCKER_VERSION}/zsh-in-docker.sh)" -- \
  -p git \
  -p fzf \
  -a "source /usr/share/doc/fzf/examples/key-bindings.zsh" \
  -a "source /usr/share/doc/fzf/examples/completion.zsh" \
  -a "export PROMPT_COMMAND='history -a' && export HISTFILE=/commandhistory/.bash_history" \
  -a '. "$HOME/.config/godotenv/env"' \
  -x

# Install Claude using native installer
RUN curl -fsSL https://claude.ai/install.sh | bash -s stable

USER node
