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

# Install Godot .NET binary (headless-capable via --headless flag)
ARG GODOT_VERSION=4.6.1
RUN ARCH=$(dpkg --print-architecture) && \
  if [ "$ARCH" = "amd64" ]; then GODOT_ARCH="x86_64"; else GODOT_ARCH="$ARCH"; fi && \
  GODOT_RELEASE="Godot_v${GODOT_VERSION}-stable" && \
  wget -q "https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/${GODOT_RELEASE}_mono_linux_${GODOT_ARCH}.zip" && \
  unzip -q "${GODOT_RELEASE}_mono_linux_${GODOT_ARCH}.zip" && \
  mv "${GODOT_RELEASE}_mono_linux_${GODOT_ARCH}/${GODOT_RELEASE}_mono_linux.${GODOT_ARCH}" /usr/local/bin/godot && \
  mv "${GODOT_RELEASE}_mono_linux_${GODOT_ARCH}/GodotSharp" /usr/local/bin/GodotSharp && \
  rm -rf "${GODOT_RELEASE}_mono_linux_${GODOT_ARCH}" "${GODOT_RELEASE}_mono_linux_${GODOT_ARCH}.zip" && \
  chmod +x /usr/local/bin/godot

# Install csharp-language-server as a shared global tool
RUN dotnet tool install --tool-path /usr/local/share/dotnet-global-tools csharp-ls
ENV PATH="$PATH:/usr/local/share/dotnet-global-tools"

# Set up non-root user
USER node

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
  -x

# Install Claude using native installer
RUN curl -fsSL https://claude.ai/install.sh | bash -s stable

USER node
